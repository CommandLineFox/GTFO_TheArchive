﻿using CellMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Utilities;
using UnityEngine;
using TheArchive.Core.Models;

namespace TheArchive.Features.Dev
{
    [EnableFeatureByDefault, HideInModSettings]
    public class ModSettings : Feature
    {
        public override string Name => "Mod Settings (this)";

        public override bool RequiresRestart => true;

        public override string Group => FeatureGroups.Dev;

#if MONO
        private static readonly FieldAccessor<CM_PageSettings, eSettingsSubMenuId> A_CM_PageSettings_m_currentSubMenuId = FieldAccessor<CM_PageSettings, eSettingsSubMenuId>.GetAccessor("m_currentSubMenuId");
        private static readonly MethodAccessor<CM_PageSettings> A_CM_PageSettings_ResetAllValueHolders = MethodAccessor<CM_PageSettings>.GetAccessor("ResetAllValueHolders");
        private static readonly MethodAccessor<CM_PageSettings> A_CM_PageSettings_ShowSettingsWindow = MethodAccessor<CM_PageSettings>.GetAccessor("ShowSettingsWindow");
        private static readonly FieldAccessor<CM_SettingsEnumDropdownButton, CM_ScrollWindow> A_CM_SettingsEnumDropdownButton_m_popupWindow = FieldAccessor<CM_SettingsEnumDropdownButton, CM_ScrollWindow>.GetAccessor("m_popupWindow");
        private static readonly FieldAccessor<CM_SettingsInputField, int> A_CM_SettingsInputField_m_maxLen = FieldAccessor<CM_SettingsInputField, int>.GetAccessor("m_maxLen");
        private static readonly FieldAccessor<CM_SettingsInputField, iStringInputReceiver> A_CM_SettingsInputField_m_stringReceiver = FieldAccessor<CM_SettingsInputField, iStringInputReceiver>.GetAccessor("m_stringReceiver");
        //m_stringReceiver
#endif
        private static MethodAccessor<TMPro.TextMeshPro> A_TextMeshPro_ForceMeshUpdate;

        private static bool _restartRequested = false;

        public static Color ORANGE = new Color(1f, 0.5f, 0.05f, 0.8f);
        public static Color RED = new Color(0.8f, 0.1f, 0.1f, 0.8f);
        public static Color GREEN = new Color(0.1f, 0.8f, 0.1f, 0.8f);
        public static Color DISABLED = new Color(0.3f, 0.3f, 0.3f, 0.8f);


        public class JankTextMeshProUpdaterOnce : MonoBehaviour
        {
#if IL2CPP
            public JankTextMeshProUpdaterOnce(IntPtr ptr) : base(ptr)
            {

            }
#endif

            public void Awake()
            {
                TMPro.TextMeshPro textMesh = this.GetComponent<TMPro.TextMeshPro>();

                A_TextMeshPro_ForceMeshUpdate.Invoke(textMesh);

                Destroy(this);
            }
        }

        public class CustomStringReceiver
#if MONO
            : iStringInputReceiver
        {
            public CustomStringReceiver(Func<string> getFunc, Action<string> setAction)
            {
                _getValue = getFunc;
                _setValue = setAction;
            }
#else
            : Il2CppSystem.Object
        {
            public CustomStringReceiver(IntPtr ptr) : base(ptr)
            {
            }

            public CustomStringReceiver(Func<string> getFunc, Action<string> setAction) : base(UnhollowerRuntimeLib.ClassInjector.DerivedConstructorPointer<CustomStringReceiver>())
            {
                UnhollowerRuntimeLib.ClassInjector.DerivedConstructorBody(this);

                _getValue = getFunc;
                _setValue = setAction;
            }
#endif

            private Func<string> _getValue;
            private Action<string> _setValue;

            string
#if MONO
                iStringInputReceiver.
#endif
                GetStringValue(eCellSettingID setting)
            {
                return _getValue?.Invoke() ?? string.Empty;
            }

            string
#if MONO
                iStringInputReceiver.
#endif
                SetStringValue(eCellSettingID setting, string value)
            {
                _setValue.Invoke(value);
                return value;
            }

        }


        public override void Init()
        {
#if IL2CPP
            UnhollowerRuntimeLib.ClassInjector.RegisterTypeInIl2Cpp<JankTextMeshProUpdaterOnce>();

            UnhollowerRuntimeLib.ClassInjector.RegisterTypeInIl2CppWithInterfaces<CustomStringReceiver>(true, typeof(iStringInputReceiver));
#endif

            A_TextMeshPro_ForceMeshUpdate = MethodAccessor<TMPro.TextMeshPro>.GetAccessor("ForceMeshUpdate", Array.Empty<Type>());
        }

        //Setup(MainMenuGuiLayer guiLayer)
        [ArchivePatch(typeof(CM_PageSettings), "Setup")]
        public class CM_PageSettings_SetupPatch
        {
            private static GameObject _settingsItemPrefab;
            private static List<iScrollWindowContent> _scrollWindowContentElements;
            private static Transform _mainScrollWindowTransform;
            private static CM_ScrollWindow _popupWindow;
            private static CM_PageSettings _settingsPageInstance;

#if IL2CPP
            public static void Postfix(CM_PageSettings __instance, MainMenuGuiLayer guiLayer)
            {
                try
                {
                    var m_subMenuButtonPrefab = __instance.m_subMenuButtonPrefab;
                    var m_subMenuItemOffset = __instance.m_subMenuItemOffset;
                    var m_movingContentHolder = __instance.m_movingContentHolder;
                    var m_scrollwindowPrefab = __instance.m_scrollwindowPrefab;
#else
            public static void Postfix(CM_PageSettings __instance, MainMenuGuiLayer guiLayer, GameObject ___m_subMenuButtonPrefab, ref float ___m_subMenuItemOffset, List<CM_ScrollWindow> ___m_allSettingsWindows)
            {
                var m_subMenuButtonPrefab = ___m_subMenuButtonPrefab;
                var m_subMenuItemOffset = ___m_subMenuItemOffset;
                var m_movingContentHolder = __instance.m_movingContentHolder;
                var m_scrollwindowPrefab = __instance.m_scrollwindowPrefab;
                try
                {
#endif
                    _settingsPageInstance = __instance;
                    _popupWindow = __instance.m_popupWindow;
                    _settingsItemPrefab = __instance.m_settingsItemPrefab;
                    if(_scrollWindowContentElements == null)
                    {
                        _scrollWindowContentElements = new List<iScrollWindowContent>();
                    }
                    _scrollWindowContentElements.Clear();

                    var title = "Mod Settings";

                    CM_Item mainModSettingsButton = guiLayer.AddRectComp(m_subMenuButtonPrefab, GuiAnchor.TopLeft, new Vector2(70f, m_subMenuItemOffset), m_movingContentHolder).TryCastTo<CM_Item>();

                    mainModSettingsButton.SetScaleFactor(0.85f);
                    mainModSettingsButton.UpdateColliderOffset();
#if IL2CPP
                    __instance.m_subMenuItemOffset -= 80f;
#else
                    ___m_subMenuItemOffset -= 80f;
#endif
                    mainModSettingsButton.SetText(title);

                    

                    SharedUtils.ChangeColorCMItem(mainModSettingsButton, Color.magenta);

                    CM_ScrollWindow mainModSettingsScrollWindow = guiLayer.AddRectComp(m_scrollwindowPrefab, GuiAnchor.TopLeft, new Vector2(420f, -200f), m_movingContentHolder).TryCastTo<CM_ScrollWindow>();

                    _mainScrollWindowTransform = mainModSettingsScrollWindow.transform;

                    mainModSettingsScrollWindow.Setup();
                    mainModSettingsScrollWindow.SetSize(new Vector2(1020f, 900f));
                    mainModSettingsScrollWindow.SetVisible(visible: false);
                    mainModSettingsScrollWindow.SetHeader(title);

                    TMPro.TextMeshPro scrollWindowHeaderTextTMP = mainModSettingsScrollWindow.GetComponentInChildren<TMPro.TextMeshPro>();

                    var infoText = GameObject.Instantiate(scrollWindowHeaderTextTMP, scrollWindowHeaderTextTMP.transform.parent);

                    infoText.name = "ModSettings_InfoText";
                    infoText.transform.localPosition += new Vector3(300, 0, 0);
                    infoText.SetText("");
                    infoText.enableWordWrapping = false;
                    infoText.fontSize = 16;
                    infoText.fontSizeMin = 16;


                    var odereredGroups = FeatureManager.Instance.GroupedFeatures.OrderBy(kvp => kvp.Key);

                    foreach(var kvp in odereredGroups)
                    {
                        var groupName = kvp.Key;
                        var featureSet = kvp.Value.OrderBy(fs => fs.Name);

                        if (groupName == FeatureGroups.Dev && !Feature.DevMode)
                            continue;

                        if(!Feature.DevMode && featureSet.All(f => f.IsHidden))
                            continue;

                        // Title
                        CreateHeader(groupName);

                        foreach(var feature in featureSet)
                        {
                            SetupEntriesForFeature(feature, infoText);

                        }

                        // Spacer
                        CreateSpacer();
                    }

                    IEnumerable<Feature> features;
                    if(Feature.DevMode)
                    {
                        features = FeatureManager.Instance.RegisteredFeatures;
                    }
                    else
                    {
                        features = FeatureManager.Instance.RegisteredFeatures.Where(f => !f.IsHidden);
                    }

                    features = features.Where(f => !f.BelongsToGroup);

                    features = features.OrderBy(f => f.GetType().Assembly.GetName().Name + f.Name);

                    Assembly currentAsm = null;
                    bool createSpacer = false;
                    foreach (var feature in features)
                    {
                        var featureAsm = feature.GetType().Assembly;
                        if (featureAsm != currentAsm)
                        {
                            if(createSpacer)
                            {
                                CreateSpacer();
                            }
                            createSpacer = true;
                            var headerTitle = featureAsm.GetCustomAttribute<ModDefaultFeatureGroupName>()?.DefaultGroupName ?? featureAsm.GetName().Name;
                            CreateHeader(headerTitle);
                            currentAsm = featureAsm;
                        }
                        SetupEntriesForFeature(feature, infoText);
                    }

#if IL2CPP
                    __instance.m_allSettingsWindows.Add(mainModSettingsScrollWindow);
#else
                    ___m_allSettingsWindows.Add(mainModSettingsScrollWindow);
#endif
                    mainModSettingsButton.SetCMItemEvents(delegate (int id)
                    {
                        CM_PageSettings.ToggleAudioTestLoop(false);
                        __instance.ResetAllInputFields();
#if IL2CPP
                        __instance.ResetAllValueHolders();
                        __instance.m_currentSubMenuId = eSettingsSubMenuId.None;
                        __instance.ShowSettingsWindow(mainModSettingsScrollWindow);
#else
                        A_CM_PageSettings_ResetAllValueHolders.Invoke(__instance);
                        A_CM_PageSettings_m_currentSubMenuId.Set(__instance, eSettingsSubMenuId.None);
                        A_CM_PageSettings_ShowSettingsWindow.Invoke(__instance, mainModSettingsScrollWindow);
#endif
                    });

#if IL2CPP
                    Il2CppSystem.Collections.Generic.List<iScrollWindowContent> allSWCsIL2CPP = new Il2CppSystem.Collections.Generic.List<iScrollWindowContent>();
                    foreach(var swc in _scrollWindowContentElements)
                    {
                        allSWCsIL2CPP.Add(swc);
                    }
                    mainModSettingsScrollWindow.SetContentItems(allSWCsIL2CPP, 5f);
#else
                    mainModSettingsScrollWindow.SetContentItems(_scrollWindowContentElements, 5f);
#endif
                }
                catch (Exception ex)
                {
                    ArchiveLogger.Exception(ex);
                }
            }

            private static void ShowPopupWindow(string header, Vector2 pos)
            {
                _popupWindow.SetHeader(header);
                _popupWindow.transform.position = pos;
                _popupWindow.SetVisible(true);
            }

            private static void SetPopupVisible(bool visible)
            {
                _popupWindow.SetVisible(visible);
            }

            private static CM_ScrollWindow CreateScrollWindow()
            {
                // TODO
                return null;
            }

            private static void CreateSpacer()
            {
                CreateHeader(string.Empty);
            }

            private static void CreateHeader(string title)
            {
                var settingsItemGameObject = GameObject.Instantiate(_settingsItemPrefab, _mainScrollWindowTransform);

                _scrollWindowContentElements.Add(settingsItemGameObject.GetComponentInChildren<iScrollWindowContent>());

                var cm_settingsItem = settingsItemGameObject.GetComponentInChildren<CM_SettingsItem>();

                var titleTextTMP = cm_settingsItem.transform.GetChildWithExactName("Title").GetChildWithExactName("TitleText").gameObject.GetComponent<TMPro.TextMeshPro>();

                if(!string.IsNullOrWhiteSpace(title))
                {
                    titleTextTMP.SetText($"<b>{title}</b>");
                    titleTextTMP.color = ORANGE;
                    cm_settingsItem.ForcePopupLayer(true);

                    if (BuildInfo.Rundown.IsIncludedIn(Utils.RundownFlags.RundownFour | Utils.RundownFlags.RundownFive))
                    {
                        titleTextTMP.gameObject.AddComponent<JankTextMeshProUpdaterOnce>();
                    }
                }
                else
                {
                    titleTextTMP.SetText(string.Empty);
                }
            }

            private static void CreateSettingsItem(string titleText, out CM_SettingsItem cm_settingsItem, Color? titleColor = null)
            {
                var settingsItemGameObject = GameObject.Instantiate(_settingsItemPrefab, _mainScrollWindowTransform);

                _scrollWindowContentElements.Add(settingsItemGameObject.GetComponentInChildren<iScrollWindowContent>());

                cm_settingsItem = settingsItemGameObject.GetComponentInChildren<CM_SettingsItem>();

                var titleTextTMP = cm_settingsItem.transform.GetChildWithExactName("Title").GetChildWithExactName("TitleText").gameObject.GetComponent<TMPro.TextMeshPro>();

                if (titleColor.HasValue)
                    titleTextTMP.color = titleColor.Value;

#if IL2CPP
                titleTextTMP.m_text = titleText;
                titleTextTMP.SetText(titleText);
#else
                titleTextTMP.SetText(titleText);
#endif

                if (BuildInfo.Rundown.IsIncludedIn(Utils.RundownFlags.RundownFour | Utils.RundownFlags.RundownFive))
                {
                    titleTextTMP.gameObject.AddComponent<JankTextMeshProUpdaterOnce>();
                }
            }

            private static void CreateColorSetting(ColorSetting setting)
            {
                CreateSettingsItem(setting.DisplayName, out var cm_settingsItem);

                CM_SettingsInputField cm_settingsInputField = GOUtil.SpawnChildAndGetComp<CM_SettingsInputField>(cm_settingsItem.m_textInputPrefab, cm_settingsItem.m_inputAlign);

                StringInputSetMaxLength(cm_settingsInputField, 7);

                var bg_rt = cm_settingsInputField.m_background.GetComponent<RectTransform>();
                bg_rt.anchoredPosition = new Vector2(-175, 0);
                bg_rt.localScale = new Vector3(0.19f, 1, 1);

                var colorPreviewGO = GameObject.Instantiate(cm_settingsInputField.m_background.gameObject, cm_settingsInputField.transform, true);
                colorPreviewGO.transform.SetParent(cm_settingsItem.m_inputAlign);
                colorPreviewGO.transform.localScale = new Vector3(0.07f, 1, 1);
                colorPreviewGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-225, -25);
                var renderer = colorPreviewGO.GetComponent<SpriteRenderer>();

                var scol = (SColor)setting.GetValue();

                renderer.color = scol.ToUnityColor();

                cm_settingsInputField.m_text.text = scol.ToHexString();

                var receiver = new CustomStringReceiver(new Func<string>(
                    () => {
                        ArchiveLogger.Debug($"[{nameof(CustomStringReceiver)}({nameof(ColorSetting)})] Gotten value of \"{setting.DEBUG_Path}\"!");
                        SColor color = (SColor) setting.GetValue();
                        renderer.color = color.ToUnityColor();
                        return color.ToHexString();
                    }),
                    (val) => {
                        ArchiveLogger.Debug($"[{nameof(CustomStringReceiver)}({nameof(ColorSetting)})] Set value of \"{setting.DEBUG_Path}\" to \"{val}\"");
                        SColor color = SColorExtensions.FromHexString(val);
                        setting.SetValue(color);
                    });

#if IL2CPP
                cm_settingsInputField.m_stringReceiver = new iStringInputReceiver(receiver.Pointer);
#else
                A_CM_SettingsInputField_m_stringReceiver.Set(cm_settingsInputField, receiver);
#endif


                cm_settingsItem.ForcePopupLayer(true);
            }

            private static void CreateStringSetting(StringSetting setting)
            {
                CreateSettingsItem(setting.DisplayName, out var cm_settingsItem);

                CM_SettingsInputField cm_settingsInputField = GOUtil.SpawnChildAndGetComp<CM_SettingsInputField>(cm_settingsItem.m_textInputPrefab, cm_settingsItem.m_inputAlign);

                StringInputSetMaxLength(cm_settingsInputField, setting.MaxInputLength);

                cm_settingsInputField.m_text.text = setting.GetValue().ToString();

                var receiver = new CustomStringReceiver(new Func<string>(
                    () => {
                        ArchiveLogger.Debug($"[{nameof(CustomStringReceiver)}] Gotten value of \"{setting.DEBUG_Path}\"!");
                        return setting.GetValue().ToString();
                    }),
                    (val) => {
                        ArchiveLogger.Debug($"[{nameof(CustomStringReceiver)}] Set value of \"{setting.DEBUG_Path}\" to \"{val}\"");
                        setting.SetValue(val);
                    });
                
#if IL2CPP
                cm_settingsInputField.m_stringReceiver = new iStringInputReceiver(receiver.Pointer);
#else
                A_CM_SettingsInputField_m_stringReceiver.Set(cm_settingsInputField, receiver);
#endif

                cm_settingsItem.ForcePopupLayer(true);
            }

            private static void StringInputSetMaxLength(CM_SettingsInputField sif, int maxLength)
            {
#if MONO
                A_CM_SettingsInputField_m_maxLen.Set(sif, maxLength);
#else
                sif.m_maxLen = maxLength;
#endif
            }

            private static void CreateBoolSetting(BoolSetting setting)
            {
                CreateSettingsItem(setting.DisplayName, out var cm_settingsItem);

                CM_SettingsToggleButton cm_SettingsToggleButton = GOUtil.SpawnChildAndGetComp<CM_SettingsToggleButton>(cm_settingsItem.m_toggleInputPrefab, cm_settingsItem.m_inputAlign);

                var toggleButton_cm_item = cm_SettingsToggleButton.gameObject.AddComponent<CM_Item>();



                Component.Destroy(cm_SettingsToggleButton);

                var toggleButtonText = toggleButton_cm_item.GetComponentInChildren<TMPro.TextMeshPro>();

                toggleButton_cm_item.SetCMItemEvents(delegate (int id) {
                    var val = !(bool)setting.GetValue();
                    setting.SetValue(val);

                    toggleButtonText.SetText(val ? "On" : "Off");
                    var color = val ? GREEN : RED;
                    SharedUtils.ChangeColorCMItem(toggleButton_cm_item, color);
                });

                var currentValue = (bool)setting.GetValue();
                toggleButtonText.SetText(currentValue ? "On" : "Off");
                var col = currentValue ? GREEN : RED;
                SharedUtils.ChangeColorCMItem(toggleButton_cm_item, col);

                cm_settingsItem.ForcePopupLayer(true);
            }

            private static void CreateEnumSetting(EnumSetting setting)
            {
                CreateSettingsItem(setting.DisplayName, out var cm_settingsItem);

                CM_SettingsEnumDropdownButton cm_settingsEnumDropdownButton = GOUtil.SpawnChildAndGetComp<CM_SettingsEnumDropdownButton>(cm_settingsItem.m_enumDropdownInputPrefab, cm_settingsItem.m_inputAlign);

                var enumButton_cm_item = cm_settingsEnumDropdownButton.gameObject.AddComponent<CM_Item>();

                enumButton_cm_item.Setup();
                enumButton_cm_item.SetCMItemEvents(delegate (int _) {
                    CreateAndShowEnumPopup(setting, enumButton_cm_item, cm_settingsEnumDropdownButton);
                });

                SharedUtils.ChangeColorCMItem(enumButton_cm_item, ORANGE);
                var bg = enumButton_cm_item.gameObject.transform.GetChildWithExactName("Background");
                if(bg != null)
                {
                    UnityEngine.Object.Destroy(bg.gameObject);
                }
                enumButton_cm_item.SetText(setting.GetCurrentEnumKey());

                var collider = enumButton_cm_item.GetComponent<BoxCollider2D>();
                collider.size = new Vector2(550, 50);
                collider.offset = new Vector2(250, -25);

                cm_settingsItem.ForcePopupLayer(true);

                cm_settingsEnumDropdownButton.enabled = false;
                UnityEngine.Object.Destroy(cm_settingsEnumDropdownButton);

#if MONO
                A_CM_SettingsEnumDropdownButton_m_popupWindow.Set(cm_settingsEnumDropdownButton, _popupWindow);
#else
                cm_settingsEnumDropdownButton.m_popupWindow = _popupWindow;
#endif

            }

            private static void CreateAndShowEnumPopup(EnumSetting setting, CM_Item enumButton_cm_item, CM_SettingsEnumDropdownButton cm_settingsEnumDropdownButton)
            {
#if MONO
                List<iScrollWindowContent> list = new List<iScrollWindowContent>();
#else
                Il2CppSystem.Collections.Generic.List<iScrollWindowContent> list = new Il2CppSystem.Collections.Generic.List<iScrollWindowContent>();
#endif
                foreach (var kvp in setting.Map)
                {
                    iScrollWindowContent iScrollWindowContent = GOUtil.SpawnChildAndGetComp<iScrollWindowContent>(cm_settingsEnumDropdownButton.m_popupItemPrefab, enumButton_cm_item.transform);
                    list.Add(iScrollWindowContent);
                    string enumKey = kvp.Key;
#if MONO
                    CM_Item cm_Item = iScrollWindowContent as CM_Item;
#else
                    CM_Item cm_Item = iScrollWindowContent.TryCast<CM_Item>();
#endif
                    if (cm_Item != null)
                    {
                        cm_Item.Setup();
                        cm_Item.SetText(enumKey);
                        //cm_Item.SetAnchor(GuiAnchor.TopLeft, true);
                        cm_Item.SetScaleFactor(1f);

                        cm_Item.ForcePopupLayer(true);
                        cm_Item.SetCMItemEvents((_) => {
                            setting.SetValue(kvp.Value);

                            enumButton_cm_item.SetText(enumKey);

                            SetPopupVisible(false);
                        });
                    }
                }

#if MONO
                _popupWindow.SetupFromButton(cm_settingsEnumDropdownButton as iCellMenuPopupController, _settingsPageInstance);
#else
                _popupWindow.SetupFromButton(cm_settingsEnumDropdownButton.TryCast<iCellMenuPopupController>(), _settingsPageInstance);
#endif
                _popupWindow.transform.position = cm_settingsEnumDropdownButton.m_popupWindowAlign.position;
                _popupWindow.SetContentItems(list, 5f);
                _popupWindow.SetHeader(setting.DisplayName);
                _popupWindow.SetVisible(true);
            }

            private static void SetupEntriesForFeature(Feature feature, TMPro.TextMeshPro infoText)
            {
                if (!Feature.DevMode && feature.IsHidden) return;

                try
                {
                    string featureName;
                    Color? col = null;
                    if (feature.IsHidden)
                    {
                        featureName = $"[H] {feature.Name}";
                        col = DISABLED;
                    }
                    else
                    {
                        featureName = feature.Name;
                    }

                    if (feature.RequiresRestart)
                    {
                        featureName = $"<u>{featureName}</u>";
                    }

                    CreateSettingsItem(featureName, out var cm_settingsItem, col);

                    CM_SettingsToggleButton cm_SettingsToggleButton = GOUtil.SpawnChildAndGetComp<CM_SettingsToggleButton>(cm_settingsItem.m_toggleInputPrefab, cm_settingsItem.m_inputAlign);

                    var toggleButton_cm_item = cm_SettingsToggleButton.gameObject.AddComponent<CM_Item>();



                    Component.Destroy(cm_SettingsToggleButton);

                    var toggleButtonText = toggleButton_cm_item.GetComponentInChildren<TMPro.TextMeshPro>();

                    toggleButton_cm_item.SetCMItemEvents(delegate (int id) {
                        FeatureManager.ToggleFeature(feature);

                        SetFeatureItemTextAndColor(feature, toggleButton_cm_item, toggleButtonText);

                        if (feature.RequiresRestart && !_restartRequested)
                        {
                            _restartRequested = true;
                            infoText.SetText($"<color=red><b>Restart required for some settings to apply!</b></color>");
                        }
                    });

                    SetFeatureItemTextAndColor(feature, toggleButton_cm_item, toggleButtonText);

                    var collider = toggleButton_cm_item.GetComponent<BoxCollider2D>();
                    collider.size = new Vector2(550, 50);
                    collider.offset = new Vector2(250, -25);

                    cm_settingsItem.ForcePopupLayer(true);

                    if(feature.HasAdditionalSettings)
                    {
                        // Create SubMenu for additional settings
                        foreach (var settingsHelper in feature.SettingsHelpers)
                        {
                            foreach (var setting in settingsHelper.Settings)
                            {
                                switch(setting)
                                {
                                    case ColorSetting cs:
                                        CreateColorSetting(cs);
                                        break;
                                    case StringSetting ss:
                                        CreateStringSetting(ss);
                                        break;
                                    case BoolSetting bs:
                                        CreateBoolSetting(bs);
                                        break;
                                    case EnumSetting es:
                                        CreateEnumSetting(es);
                                        break;
                                    default:
                                        CreateHeader(setting.DEBUG_Path);
                                        break;
                                }
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    ArchiveLogger.Exception(ex);
                }
            }

            private static void SetFeatureItemTextAndColor(Feature feature, CM_Item buttonItem, TMPro.TextMeshPro text)
            {
                bool enabled = (feature.AppliesToThisGameBuild && !feature.RequiresRestart) ? feature.Enabled : FeatureManager.IsEnabledInConfig(feature);
                text.SetText(enabled ? "Enabled" : "Disabled");

                SetFeatureItemColor(feature, buttonItem);
            }

            private static void SetFeatureItemColor(Feature feature, CM_Item item)
            {
                if (!feature.AppliesToThisGameBuild)
                {
                    SharedUtils.ChangeColorCMItem(item, DISABLED);
                    return;
                }

                Color col;

                if (feature.RequiresRestart)
                {
                    col = FeatureManager.IsEnabledInConfig(feature) ? GREEN : RED;
                    SharedUtils.ChangeColorCMItem(item, col);
                    return;
                }

                col = feature.Enabled ? GREEN : RED;
                SharedUtils.ChangeColorCMItem(item, col);
            }
        }
    }
}
