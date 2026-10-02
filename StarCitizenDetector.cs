using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Playnite.SDK;

namespace StarCitizenLibrary
{
    public class StarCitizenInstallation
    {
        public string Channel { get; set; }
        public string ChannelName { get; set; }
        public string InstallDirectory { get; set; }
        public string ExecutablePath { get; set; }
        public string LauncherExePath { get; set; }
        public string Version { get; set; }
        public string Branch { get; set; }
        public string BuildId { get; set; }
        public bool IsInstalled => Directory.Exists(InstallDirectory) && (File.Exists(ExecutablePath) || File.Exists(LauncherExePath));
    }

    public class VersionInfo
    {
        public string Version { get; set; }
        public string Branch { get; set; }
        public string BuildId { get; set; }
    }

    public static class StarCitizenDetector
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public static readonly string[] KnownChannels = { "LIVE", "PTU", "EPTU", "HOTFIX", "TECH-PREVIEW" };

        public static string FindRsiLauncher()
        {
            var candidates = new[]
            {
                @"C:\Program Files\Roberts Space Industries\RSI Launcher\RSI Launcher.exe",
                @"C:\Program Files (x86)\Roberts Space Industries\RSI Launcher\RSI Launcher.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\RSI Launcher\RSI Launcher.exe")
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            try
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    var p = Path.Combine(drive.RootDirectory.FullName, @"Program Files\Roberts Space Industries\RSI Launcher\RSI Launcher.exe");
                    if (File.Exists(p)) return p;
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Error scanning drives for RSI Launcher");
            }

            return null;
        }

        public static IEnumerable<StarCitizenInstallation> DetectInstallations(
            string customPath = null,
            bool includePtu = true,
            bool includeEptu = true,
            bool includeTechPreview = true)
        {
            var installations = new List<StarCitizenInstallation>();
            var rsiLauncherPath = FindRsiLauncher();

            var searchRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(customPath) && Directory.Exists(customPath))
            {
                searchRoots.Add(customPath.TrimEnd('\\', '/'));
            }

            try
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    var root = drive.RootDirectory.FullName;
                    searchRoots.Add(Path.Combine(root, "StarCitizen"));
                    searchRoots.Add(Path.Combine(root, @"Program Files\Roberts Space Industries\StarCitizen"));
                    searchRoots.Add(Path.Combine(root, @"Roberts Space Industries\StarCitizen"));
                    searchRoots.Add(Path.Combine(root, @"Games\Roberts Space Industries\StarCitizen"));
                    searchRoots.Add(Path.Combine(root, @"Games\StarCitizen"));
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Error querying drive roots for Star Citizen");
            }

            foreach (var root in searchRoots)
            {
                if (!Directory.Exists(root)) continue;

                foreach (var channel in KnownChannels)
                {
                    if (channel == "PTU" && !includePtu) continue;
                    if (channel == "EPTU" && !includeEptu) continue;
                    if (channel == "TECH-PREVIEW" && !includeTechPreview) continue;

                    var channelDir = Path.Combine(root, channel);
                    if (!Directory.Exists(channelDir)) continue;

                    var exePath = Path.Combine(channelDir, "Bin64", "StarCitizen.exe");
                    var launcherExe = Path.Combine(channelDir, "StarCitizen_Launcher.exe");

                    if (!File.Exists(exePath) && !File.Exists(launcherExe)) continue;

                    var chosenExe = File.Exists(launcherExe) ? launcherExe : exePath;
                    var versionInfo = ReadVersion(channelDir, exePath);

                    installations.Add(new StarCitizenInstallation
                    {
                        Channel = channel,
                        ChannelName = FormatChannelName(channel),
                        InstallDirectory = channelDir,
                        ExecutablePath = chosenExe,
                        LauncherExePath = rsiLauncherPath,
                        Version = versionInfo.Version,
                        Branch = versionInfo.Branch,
                        BuildId = versionInfo.BuildId
                    });
                }
            }

            return installations;
        }

        private static string FormatChannelName(string channel)
        {
            switch (channel.ToUpperInvariant())
            {
                case "LIVE": return "Star Citizen (LIVE)";
                case "PTU": return "Star Citizen (PTU)";
                case "EPTU": return "Star Citizen (EPTU)";
                case "TECH-PREVIEW": return "Star Citizen (Tech Preview)";
                case "HOTFIX": return "Star Citizen (Hotfix)";
                default: return string.Format("Star Citizen ({0})", channel);
            }
        }

        private static VersionInfo ReadVersion(string channelDir, string exePath)
        {
            var manifestPath = Path.Combine(channelDir, "build_manifest.id");
            if (File.Exists(manifestPath))
            {
                try
                {
                    var text = File.ReadAllText(manifestPath);
                    var versionMatch = Regex.Match(text, @"[""']Version[""']s*:s*[""']([^""']+)[""']");
                    var branchMatch = Regex.Match(text, @"[""']Branch[""']s*:s*[""']([^""']+)[""']");
                    var buildIdMatch = Regex.Match(text, @"[""']BuildId[""']s*:s*[""']([^""']+)[""']");

                    var version = versionMatch.Success ? versionMatch.Groups[1].Value : null;
                    var branch = branchMatch.Success ? branchMatch.Groups[1].Value : null;
                    var buildId = buildIdMatch.Success ? buildIdMatch.Groups[1].Value : null;

                    if (!string.IsNullOrEmpty(version))
                    {
                        return new VersionInfo { Version = version, Branch = branch, BuildId = buildId };
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, string.Format("Failed to parse build_manifest.id at {0}", manifestPath));
                }
            }

            if (File.Exists(exePath))
            {
                try
                {
                    var vi = FileVersionInfo.GetVersionInfo(exePath);
                    var v = vi.ProductVersion ?? vi.FileVersion;
                    if (!string.IsNullOrEmpty(v))
                    {
                        return new VersionInfo { Version = v.Replace(",", ".").Trim() };
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, string.Format("Failed to get FileVersionInfo for {0}", exePath));
                }
            }

            return new VersionInfo { Version = "Alpha" };
        }
    }
}
