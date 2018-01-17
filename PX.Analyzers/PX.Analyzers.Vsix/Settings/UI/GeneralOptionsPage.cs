﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;

namespace PX.Analyzers.Vsix
{
    public class GeneralOptionsPage : DialogPage
    {
        private const string AllSettings = "All"; 

        private object syncLock = new object();
        private bool colorSettingsChanged = false;
        public event EventHandler<SettingChangedEventArgs> ColoringSettingChanged;
        public const string PageTitle = "General";

        private bool coloringEnabled = true;

        [Category(AcuminatorVSPackage.SettingsCategoryName)]
        [DisplayName("Coloring enabled")]
        [Description("Syntax coloring enabled")]
        public bool ColoringEnabled
        {
            get => coloringEnabled;
            set
            {
                if (coloringEnabled != value)
                {
                    coloringEnabled = value;
                    colorSettingsChanged = true;
                }
            }
        }

        private bool useRegexColoring;

        [Category(AcuminatorVSPackage.SettingsCategoryName)]
        [DisplayName("Use RegEx coloriser")]
        [Description("Use syntax coloriser implemented via regular expressions, provide worse coloring but works faster")]
        public bool UseRegexColoring
        {
            get => useRegexColoring;
            set
            {
                if (useRegexColoring != value)
                {
                    useRegexColoring = value;
                    colorSettingsChanged = true;
                }
            }
        }

        public override void ResetSettings()
        {
            coloringEnabled = true;
            useRegexColoring = false;
            base.ResetSettings();
            OnSettingsChanged(AllSettings);
        }

        public override void SaveSettingsToStorage()
        {
            base.SaveSettingsToStorage();

            if (coloringEnabled)
            {
                OnSettingsChanged(AllSettings);
            }
        }

        private void OnSettingsChanged(string setting)
        {          
            ColoringSettingChanged?.Invoke(this, new SettingChangedEventArgs(setting));
        }
    }
}