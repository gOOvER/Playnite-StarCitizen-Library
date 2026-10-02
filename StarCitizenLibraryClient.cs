using Playnite.SDK;
using System.Diagnostics;
using System.IO;

namespace StarCitizenLibrary
{
    public class StarCitizenLibraryClient : LibraryClient
    {
        private readonly StarCitizenLibrary plugin;

        public StarCitizenLibraryClient(StarCitizenLibrary plugin)
        {
            this.plugin = plugin;
        }

        public override string Icon => Path.Combine(plugin.GetPluginFolder(), "icon.png");

        public override bool IsInstalled
        {
            get
            {
                var launcher = StarCitizenDetector.FindRsiLauncher();
                return !string.IsNullOrEmpty(launcher) && File.Exists(launcher);
            }
        }

        public override void Open()
        {
            var launcher = StarCitizenDetector.FindRsiLauncher();
            if (!string.IsNullOrEmpty(launcher) && File.Exists(launcher))
            {
                Process.Start(new ProcessStartInfo(launcher) { UseShellExecute = true });
            }
        }
    }
}
