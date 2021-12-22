﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheArchive.Core;

namespace TheArchive.IL2CPP.R6
{
    public class R6BoosterModule : IArchiveModule
    {
        public bool ApplyHarmonyPatches => false;

        public ArchivePatcher Patcher { get; set; }
        public ArchiveMod Core { get; set; }

        public string[] SubModules => null;

        public void Init()
        {

        }

        public void OnExit()
        {

        }

        public void OnLateUpdate()
        {

        }

        public void OnSceneWasLoaded(int buildIndex, string sceneName)
        {

        }
    }
}
