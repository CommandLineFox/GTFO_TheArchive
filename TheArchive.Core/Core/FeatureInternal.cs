﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheArchive.Core.Attributes;
using TheArchive.Core.Exceptions;
using TheArchive.Core.Models;
using TheArchive.Utilities;

namespace TheArchive.Core
{
    internal class FeatureInternal
    {
        internal static GameBuildInfo BuildInfo => Feature.BuildInfo;
        internal bool InternalDisabled { get; private set; } = false;
        internal InternalDisabledReason DisabledReason {get; private set;}
        internal bool HasUpdateMethod => UpdateDelegate != null;
        internal Update UpdateDelegate { get; private set; }
        internal bool HasLateUpdateMethod => LateUpdateDelegate != null;
        internal LateUpdate LateUpdateDelegate { get; private set; }

        private Feature _feature;
        private HarmonyLib.Harmony _harmonyInstance;

        private List<Type> _patchTypes = new List<Type>();
        private HashSet<FeaturePatchInfo> _patchInfos = new HashSet<FeaturePatchInfo>();
        private HashSet<PropertyInfo> _settings = new HashSet<PropertyInfo>();

        private static HashSet<string> _usedIdentifiers = new HashSet<string>();

        private FeatureInternal() { }

        internal static void CreateAndAssign(Feature feature)
        {
            var fi = new FeatureInternal();
            feature.FeatureInternal = fi;
            fi.Init(feature);
        }

        public delegate void Update();
        public delegate void LateUpdate();

        internal void Init(Feature feature)
        {
            _feature = feature;

            ArchiveLogger.Msg(ConsoleColor.Green, $"[{nameof(FeatureInternal)}] Initializing {_feature.Identifier} ...");

            if(_usedIdentifiers.Contains(_feature.Identifier))
            {
                throw new ArchivePatchDuplicateIDException($"Provided feature id \"{_feature.Identifier}\" has already been registered by {FeatureManager.GetById(_feature.Identifier)}!");
            }

            if(!AnyRundownConstraintMatches(feature.GetType()))
            {
                InternalDisabled = true;
                DisabledReason |= InternalDisabledReason.RundownConstraintMismatch;
            }

            if (!AnyBuildConstraintMatches(feature.GetType()))
            {
                InternalDisabled = true;
                DisabledReason |= InternalDisabledReason.BuildConstraintMismatch;
            }

            if (InternalDisabled)
            {
                ArchiveLogger.Msg(ConsoleColor.Magenta, $"[{nameof(FeatureInternal)}] Feature \"{_feature.Identifier}\" has been disabled internally! ({DisabledReason})");
                return;
            }

            var updateMethod = _feature.GetType().GetMethods()
                .FirstOrDefault(mi => (mi.Name == "Update" || mi.GetCustomAttribute<IsUpdate>() != null)
                    && mi.GetParameters().Length == 0
                    && !mi.IsStatic
                    && AnyRundownConstraintMatches(mi)
                    && AnyBuildConstraintMatches(mi));

            var updateDelegate = updateMethod?.CreateDelegate(typeof(Update), _feature);
            if (updateDelegate != null)
            {
                ArchiveLogger.Debug($"[{nameof(FeatureInternal)}] {nameof(Update)} method found.");
                UpdateDelegate = (Update)updateDelegate;
            }

            var lateUpdateMethod = _feature.GetType().GetMethods()
                .FirstOrDefault(mi => (mi.Name == "LateUpdate" || mi.GetCustomAttribute<IsLateUpdate>() != null)
                    && mi.GetParameters().Length == 0
                    && !mi.IsStatic
                    && AnyRundownConstraintMatches(mi)
                    && AnyBuildConstraintMatches(mi));

            var lateUpdateDelegate = lateUpdateMethod?.CreateDelegate(typeof(LateUpdate), _feature);
            if(lateUpdateDelegate != null)
            {
                ArchiveLogger.Debug($"[{nameof(FeatureInternal)}] {nameof(LateUpdate)} method found.");
                LateUpdateDelegate = (LateUpdate)lateUpdateDelegate;
            }

            var settingsProps = _feature.GetType().GetProperties()
                .Where(pi => pi.GetCustomAttribute<FeatureConfig>() != null);

            foreach(var prop in settingsProps)
            {
                
                if((!prop.SetMethod?.IsStatic ?? true) || (!prop.GetMethod?.IsStatic ?? true))
                {
                    ArchiveLogger.Warning($"[{nameof(FeatureInternal)}] Feature \"{_feature.Identifier}\" has an invalid property \"{prop.Name}\" with a {nameof(FeatureConfig)} attribute! Make sure it's static with both a get and set method!");
                }
                else
                {
                    _settings.Add(prop);
                }
            }

            _harmonyInstance = new HarmonyLib.Harmony(_feature.Identifier);

            var potentialPatchTypes = _feature.GetType().GetNestedTypes(ArchivePatcher.AnyBindingFlags).Where(nt => nt.GetCustomAttribute<ArchivePatch>() != null);

            foreach(var type in potentialPatchTypes)
            {
                if(AnyRundownConstraintMatches(type) && AnyBuildConstraintMatches(type))
                {
                    _patchTypes.Add(type);
                    continue;
                }
                else
                {
                    ArchiveLogger.Debug($"[{nameof(FeatureInternal)}] {_feature.Identifier}: ignoring {type.FullName} (Rundown | Build not matching.)");
                }
            }

            ArchiveLogger.Notice($"[{nameof(FeatureInternal)}] Discovered {_patchTypes.Count} Patches matching constraints.");

            foreach (var patchType in _patchTypes)
            {
                var archivePatchInfo = patchType.GetCustomAttribute<ArchivePatch>();

                try
                {
                    if (!archivePatchInfo.HasType)
                    {
                        var typeMethod = patchType.GetMethods(Utils.AnyBindingFlagss)
                            .FirstOrDefault(mi => mi.ReturnType == typeof(Type)
                                && (mi.Name == "Type" || mi.GetCustomAttribute<IsTypeProvider>() != null)
                                && AnyRundownConstraintMatches(mi)
                                && AnyBuildConstraintMatches(mi));

                        if(typeMethod != null)
                        {
                            if (!typeMethod.IsStatic)
                            {
                                throw new ArchivePatchMethodNotStaticException($"Method \"{typeMethod.Name}\" in Feature \"{feature.Identifier}\" must be static!");
                            }

                            archivePatchInfo.Type = (Type) typeMethod.Invoke(null, new object[0]);
                            ArchiveLogger.Debug($"[{nameof(FeatureInternal)}] Discovered target Type for Patch \"{patchType.FullName}\" to be \"{archivePatchInfo.Type.FullName}\"");
                        }
                        else
                        {
                            throw new ArchivePatchNoTypeProvidedException($"Patch \"{patchType.FullName}\" has no Type to patch! Add a static method returning Type and decorate it with the {nameof(IsTypeProvider)} Attribute!");
                        }
                    }

                    var parameterTypesMethod = patchType.GetMethods(Utils.AnyBindingFlagss)
                            .FirstOrDefault(mi => mi.ReturnType == typeof(Type[])
                                && (mi.Name == "ParameterTypes" || mi.GetCustomAttribute<IsParameterTypesProvider>() != null)
                                && AnyRundownConstraintMatches(mi)
                                && AnyBuildConstraintMatches(mi));

                    if (parameterTypesMethod != null)
                    {
                        if(!parameterTypesMethod.IsStatic)
                            throw new ArchivePatchMethodNotStaticException($"Method \"{parameterTypesMethod.Name}\" in Feature \"{feature.Identifier}\" must be static!");

                        archivePatchInfo.ParameterTypes = (Type[]) parameterTypesMethod.Invoke(null, null);
                    }

                    MethodInfo original;

                    if (archivePatchInfo.ParameterTypes != null)
                    {
                        original = archivePatchInfo.Type.GetMethod(archivePatchInfo.MethodName, Utils.AnyBindingFlagss, null, archivePatchInfo.ParameterTypes, null);
                    }
                    else
                    {
                        original = archivePatchInfo.Type.GetMethod(archivePatchInfo.MethodName, Utils.AnyBindingFlagss);
                    }

                    if (original == null)
                    {
                        throw new ArchivePatchNoOriginalMethodException($"Method with name \"{archivePatchInfo.MethodName}\" couldn't be found in type \"{archivePatchInfo.Type.FullName}\", PatchClass: {patchType.FullName}.");
                    }

                    var prefixMethodInfo = patchType.GetMethods(Utils.AnyBindingFlagss)
                            .FirstOrDefault(mi => (mi.Name == "Prefix" || mi.GetCustomAttribute<IsPrefix>() != null)
                                && AnyRundownConstraintMatches(mi)
                                && AnyBuildConstraintMatches(mi));

                    var postfixMethodInfo = patchType.GetMethods(Utils.AnyBindingFlagss)
                            .FirstOrDefault(mi => (mi.Name == "Postfix" || mi.GetCustomAttribute<IsPostfix>() != null)
                                && AnyRundownConstraintMatches(mi)
                                && AnyBuildConstraintMatches(mi));

                    if (prefixMethodInfo == null && postfixMethodInfo == null)
                    {
                        throw new ArchivePatchNoPatchMethodException($"Patch class \"{patchType.FullName}\" doesn't contain a Prefix or Postfix method, at least one is required!");
                    }

                    _patchInfos.Add(new FeaturePatchInfo(original, prefixMethodInfo, postfixMethodInfo, archivePatchInfo));

                    try
                    {
                        var initMethod = patchType.GetMethods(Utils.AnyBindingFlagss)
                            .FirstOrDefault(mi => mi.IsStatic 
                                && (mi.Name == "Init" || mi.GetCustomAttribute<IsInitMethod>() != null)
                                && AnyRundownConstraintMatches(mi)
                                && AnyBuildConstraintMatches(mi));

                        var initMethodParameters = initMethod?.GetParameters();
                        if (initMethod != null)
                        {
                            ArchiveLogger.Debug($"[{nameof(FeatureInternal)}] invoking static Init method {initMethod.GetType().Name}.{initMethod.Name} on {_feature.Identifier}");
                            if (initMethodParameters.Length == 1 && initMethodParameters[0].ParameterType == typeof(GameBuildInfo))
                            {
                                initMethod.Invoke(null, new object[] { BuildInfo });
                            }
                            else
                            {
                                initMethod.Invoke(null, null);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        ArchiveLogger.Error($"[{nameof(FeatureInternal)}] static Init method on {_feature.Identifier} failed! - {ex}: {ex.Message}");
                        ArchiveLogger.Exception(ex);
                        InternalyDisableFeature(InternalDisabledReason.PatchInitMethodFailed);
                        return;
                    }
                }
                catch(Exception ex)
                {
                    ArchiveLogger.Error($"[{nameof(FeatureInternal)}] Patch discovery for \"{archivePatchInfo.Type.FullName}\" failed: {ex}: {ex.Message}");
                    ArchiveLogger.Exception(ex);
                }
            }

            LoadFeatureSettings();

            try
            {
                _feature.Init();
            }
            catch(Exception ex)
            {
                ArchiveLogger.Error($"[{nameof(FeatureInternal)}] Main Feature Init method on {_feature.Identifier} failed! - {ex}: {ex.Message}");
                ArchiveLogger.Exception(ex);
                InternalyDisableFeature(InternalDisabledReason.MainInitMethodFailed);
                return;
            }

            if (FeatureManager.IsEnabledInConfig(_feature))
            {
                Enable();
            }
            else
            {
                _feature.Enabled = false;
            }
        }

        internal void LoadFeatureSettings()
        {
            if (InternalDisabled) return;

            foreach (var setting in _settings)
            {
                ArchiveLogger.Info($"[{nameof(FeatureManager)}] Loading config {_feature.Identifier} [{setting.Name}] ({setting.GetMethod.ReturnType.Name}) ...");

                var configInstance = LocalFiles.LoadFeatureConfig($"{_feature.Identifier}_{setting.Name}", setting.GetMethod.ReturnType);

                setting.SetValue(_feature, configInstance);
            }
        }

        internal void SaveFeatureSettings()
        {
            if (InternalDisabled) return;

            foreach (var setting in _settings)
            {
                ArchiveLogger.Info($"[{nameof(FeatureManager)}] Saving config {_feature.Identifier} [{setting.Name}] ({setting.GetMethod.ReturnType.Name}) ...");
                
                var configInstance = setting.GetValue(_feature);

                LocalFiles.SaveFeatureConfig($"{_feature.Identifier}_{setting.Name}", setting.GetMethod.ReturnType, configInstance);
            }
        }

        private void ApplyPatches()
        {
            if (InternalDisabled) return;

            foreach(var patchInfo in _patchInfos)
            {
                try
                {
                    ArchiveLogger.Msg(ConsoleColor.DarkBlue, $"[{nameof(FeatureInternal)}] Patching {_feature.Identifier} : {patchInfo.ArchivePatchInfo.Type.FullName}.{patchInfo.ArchivePatchInfo.MethodName}()");
                    _harmonyInstance.Patch(patchInfo.OriginalMethod, patchInfo.HarmonyRefixMethod, patchInfo.HarmonyPostfixMethod);
                }
                catch(Exception ex)
                {
                    ArchiveLogger.Error($"[{nameof(FeatureInternal)}] Patch {patchInfo.ArchivePatchInfo.Type.FullName}.{patchInfo.ArchivePatchInfo.MethodName}() failed! {ex}: {ex.Message}");
                    ArchiveLogger.Exception(ex);
                }
            }
        }

        internal bool Enable()
        {
            if (InternalDisabled) return false;

            if (_feature.Enabled) return false;
            ApplyPatches();
            _feature.Enabled = true;
            _feature.OnEnable();
            return true;
        }

        internal bool Disable()
        {
            if (InternalDisabled) return false;

            if (!_feature.Enabled) return false;
            _harmonyInstance.UnpatchSelf();
            _feature.Enabled = false;
            _feature.OnDisable();
            return true;
        }

        internal void Quit()
        {
            try
            {
                _feature.OnQuit();
            }
            catch (Exception ex)
            {
                ArchiveLogger.Error($"[{nameof(FeatureManager)}] Exception thrown during {nameof(Feature.OnQuit)} in Feature {_feature.Identifier}!");
                ArchiveLogger.Exception(ex);
            }
        }

        private class FeaturePatchInfo
        {
            internal ArchivePatch ArchivePatchInfo { get; private set; }
            internal MethodInfo OriginalMethod { get; private set; }
            internal HarmonyLib.HarmonyMethod HarmonyRefixMethod { get; private set; }
            internal HarmonyLib.HarmonyMethod HarmonyPostfixMethod { get; private set; }
            internal MethodInfo PrefixPatchMethod { get; private set; }
            internal MethodInfo PostfixPatchMethod { get; private set; }

            public FeaturePatchInfo(MethodInfo original, MethodInfo prefix, MethodInfo postfix, ArchivePatch archivePatch)
            {
                OriginalMethod = original;

                PrefixPatchMethod = prefix;
                PostfixPatchMethod = postfix;

                ArchivePatchInfo = archivePatch;

                if (prefix != null)
                    HarmonyRefixMethod = new HarmonyLib.HarmonyMethod(prefix);
                if(postfix != null)
                    HarmonyPostfixMethod = new HarmonyLib.HarmonyMethod(postfix);
            }
        }

        private void InternalyDisableFeature(InternalDisabledReason reason)
        {
            InternalDisabled = true;
            DisabledReason |= reason;
            FeatureManager.Instance.DisableFeature(_feature);
        }

        private bool AnyRundownConstraintMatches(MemberInfo memberInfo)
        {
            var constraints = memberInfo.GetCustomAttributes<RundownConstraint>().ToArray();

            if (constraints.Length == 0)
                return true;

            var rundown = BuildInfo.Rundown;
            foreach (var constraint in constraints)
            {
                if (constraint.Matches(rundown))
                    return true;
            }

            return false;
        }

        private bool AnyBuildConstraintMatches(MemberInfo memberInfo)
        {
            var constraints = memberInfo.GetCustomAttributes<BuildConstraint>().ToArray();

            if (constraints.Length == 0)
                return true;

            int buildNumber = BuildInfo.BuildNumber;
            foreach (var constraint in constraints)
            {
                if (constraint.Matches(buildNumber))
                    return true;
            }

            return false;
        }

        [Flags]
        internal enum InternalDisabledReason
        {
            RundownConstraintMismatch,
            BuildConstraintMismatch,
            MainInitMethodFailed,
            PatchInitMethodFailed,
            UpdateMethodFailed,
            LateUpdateMethodFailed,
            Other
        }
    }
}
