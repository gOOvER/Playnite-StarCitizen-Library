using Playnite.SDK;
using System.Windows;
using System.Windows.Controls;

namespace StarCitizenLibrary
{
    public partial class StarCitizenLibrarySettingsView : UserControl
    {
        public StarCitizenLibrarySettingsView()
        {
            InitializeComponent();
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is StarCitizenLibrarySettingsViewModel vm)
            {
                var selected = vm.Settings.CustomInstallPath;
                var res = API.Instance.Dialogs.SelectFolder();
                if (!string.IsNullOrEmpty(res))
                {
                    vm.Settings.CustomInstallPath = res;
                }
            }
        }
    }
}
