﻿using System;
using UnityEngine;

namespace TheArchive.Core.FeaturesAPI.Components
{
    /// <summary>
    /// Used to define a Mod Settings Button element.<br/>
    /// Make sure to <b>not</b> implement a setter on your property!
    /// </summary>
    public class FButton : ISettingsComponent
    {
        public string ButtonText { get; set; }
        internal string ButtonID { get; private set; }

        public bool HasPrimaryText => PrimaryText != null;

        public MonoBehaviour PrimaryText { get; set; }

        public bool HasSecondaryText => SecondaryText != null;

        public MonoBehaviour SecondaryText { get; set; }

        public FButton() { }

        /// <summary>
        /// Creates a button
        /// </summary>
        /// <param name="buttonText">The button text</param>
        /// <param name="buttonId">The buttons ID, default is the property name</param>
        public FButton(string buttonText, string buttonId = null)
        {
            ButtonText = buttonText;
            ButtonID = buttonId;
        }
    }
}
