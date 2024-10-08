﻿using System.Reflection;
using TheArchive.Core.FeaturesAPI.Components;

namespace TheArchive.Core.FeaturesAPI.Settings
{
    public class ButtonSetting : FComponentSetting<FButton>
    {
        public string ButtonText => FComponent.ButtonText;
        public string ButtonID => FComponent.ButtonID;

        public ButtonSetting(FeatureSettingsHelper featureSettingsHelper, PropertyInfo prop, object instance, string debug_path = "") : base(featureSettingsHelper, prop, instance, debug_path)
        {

        }
    }
}
