using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace StarCitizenLibrary
{
    public class StarCitizenLibrarySettings : ObservableObject
    {
        private string customInstallPath = string.Empty;
        private bool launchViaLauncher = false;
        private bool importPtu = true;
        private bool importEptu = true;
        private bool importTechPreview = true;

        public string CustomInstallPath
        {
            get => customInstallPath;
            set => SetValue(ref customInstallPath, value);
        }

        public bool LaunchViaLauncher
        {
            get => launchViaLauncher;
            set => SetValue(ref launchViaLauncher, value);
        }

        public bool ImportPtu
        {
            get => importPtu;
            set => SetValue(ref importPtu, value);
        }

        public bool ImportEptu
        {
            get => importEptu;
            set => SetValue(ref importEptu, value);
        }

        public bool ImportTechPreview
        {
            get => importTechPreview;
            set => SetValue(ref importTechPreview, value);
        }
    }

    public class StarCitizenLibrarySettingsViewModel : ObservableObject, ISettings
    {
        private readonly StarCitizenLibrary plugin;
        private StarCitizenLibrarySettings editingClone { get; set; }

        private StarCitizenLibrarySettings settings;
        public StarCitizenLibrarySettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public StarCitizenLibrarySettingsViewModel(StarCitizenLibrary plugin)
        {
            this.plugin = plugin;
            var savedSettings = plugin.LoadPluginSettings<StarCitizenLibrarySettings>();
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new StarCitizenLibrarySettings();
            }
        }

        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            Settings = editingClone;
        }

        public void EndEdit()
        {
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
