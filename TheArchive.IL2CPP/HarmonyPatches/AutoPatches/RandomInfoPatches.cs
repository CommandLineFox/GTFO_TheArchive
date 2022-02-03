﻿using HarmonyLib;
using System;
using TheArchive.Utilities;

namespace TheArchive.HarmonyPatches.AutoPatches
{
    internal class RandomInfoPatches
    {


        [HarmonyPatch(typeof(SNet_GlobalManager), "PostSetup")]
        internal static class SNet_GlobalManager_PostSetupPatch
        {
            public static void Prefix() => ArchiveLogger.Msg(ConsoleColor.DarkBlue, $"SNet_GlobalManager - PostSetup running");
        }

        [HarmonyPatch(typeof(SNet_GlobalManager), "Setup")]
        internal static class SNet_GlobalManager_SetupPatch
        {
            public static void Prefix() => ArchiveLogger.Msg(ConsoleColor.DarkBlue, $"SNet_GlobalManager - Setup running");
        }

    }
}
