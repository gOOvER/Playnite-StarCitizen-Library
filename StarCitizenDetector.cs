using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Playnite.SDK;

namespace StarCitizenLibrary
{
    public class PilotInfo
    {
        public string Name { get; set; }
        public string AccountId { get; set; }
        public string LastShard { get; set; }
    }

    public class StarCitizenInstallation
    {
        public string Channel { get; set; }
        public string ChannelName { get; set; }
        public string InstallDirectory { get; set; }
        public string ExecutablePath { get; set; }
        public string LauncherExePath { get; set; }
        public string Version { get; set; }
        public string RawVersion { get; set; }
        public string Branch { get; set; }
        public string BuildId { get; set; }
        public ulong? InstallSize { get; set; }
        public PilotInfo Pilot { get; set; }
        public bool IsInstalled => Directory.Exists(InstallDirectory) && (File.Exists(ExecutablePath) || File.Exists(LauncherExePath));
    }

    public class VersionInfo
    {
        public string Version { get; set; }
        public string RawVersion { get; set; }
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

        public static ulong? CalculateDirectorySize(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath)) return null;

                var dirInfo = new DirectoryInfo(directoryPath);
                ulong totalSize = 0;
                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    totalSize += (ulong)file.Length;
                }
                return totalSize;
            }
            catch (Exception ex)
            {
                logger.Warn(ex, string.Format("Failed to calculate directory size for {0}", directoryPath));
                return null;
            }
        }

        public static PilotInfo ReadPilotInfo(string channelDir)
        {
            // 1. Aus temporärer loginData.json prüfen
            var loginData = Path.Combine(channelDir, "loginData.json");
            if (File.Exists(loginData))
            {
                try
                {
                    var text = File.ReadAllText(loginData);
                    var displayMatch = Regex.Match(text, "displayname\"\\s*:\\s*\"([^\"]+)\"");
                    var nickMatch = Regex.Match(text, "nickname\"\\s*:\\s*\"([^\"]+)\"");
                    var accountMatch = Regex.Match(text, "account_id\"\\s*:\\s*\"([^\"]+)\"");
                    var shardMatch = Regex.Match(text, "pub-sc-[^\"]+");

                    var name = displayMatch.Success ? displayMatch.Groups[1].Value : (nickMatch.Success ? nickMatch.Groups[1].Value : null);
                    if (!string.IsNullOrEmpty(name))
                    {
                        return new PilotInfo
                        {
                            Name = name,
                            AccountId = accountMatch.Success ? accountMatch.Groups[1].Value : null,
                            LastShard = shardMatch.Success ? shardMatch.Value : null
                        };
                    }
                }
                catch { }
            }

            // 2. Aus Game.log auslesen
            var gameLog = Path.Combine(channelDir, "Game.log");
            if (File.Exists(gameLog))
            {
                try
                {
                    using (var fs = new FileStream(gameLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs))
                    {
                        string pilotName = null;
                        string accountId = null;
                        string lastShard = null;
                        int lines = 0;

                        while (!reader.EndOfStream && lines < 600)
                        {
                            var line = reader.ReadLine();
                            lines++;
                            if (line == null) continue;

                            if (pilotName == null && line.Contains("AccountLoginCharacterStatus_Character"))
                            {
                                var m = Regex.Match(line, "accountId\\s+(\\d+)\\s+-\\s+name\\s+([^\\s\\-]+)");
                                if (m.Success)
                                {
                                    accountId = m.Groups[1].Value;
                                    pilotName = m.Groups[2].Value;
                                }
                            }

                            if (lastShard == null && line.Contains("@env_session:"))
                            {
                                var m = Regex.Match(line, "@env_session:\\s*'([^']+)'");
                                if (m.Success)
                                {
                                    lastShard = m.Groups[1].Value;
                                }
                            }

                            if (pilotName != null && lastShard != null) break;
                        }

                        if (!string.IsNullOrEmpty(pilotName))
                        {
                            return new PilotInfo
                            {
                                Name = pilotName,
                                AccountId = accountId,
                                LastShard = lastShard
                            };
                        }
                    }
                }
                catch { }
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
                    var installSize = CalculateDirectorySize(channelDir);
                    var pilot = ReadPilotInfo(channelDir);

                    installations.Add(new StarCitizenInstallation
                    {
                        Channel = channel,
                        ChannelName = FormatChannelName(channel),
                        InstallDirectory = channelDir,
                        ExecutablePath = chosenExe,
                        LauncherExePath = rsiLauncherPath,
                        Version = versionInfo.Version,
                        RawVersion = versionInfo.RawVersion,
                        Branch = versionInfo.Branch,
                        BuildId = versionInfo.BuildId,
                        InstallSize = installSize,
                        Pilot = pilot
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
                    var versionMatch = Regex.Match(text, "Version\"\\s*:\\s*\"([^\"]+)\"");
                    var branchMatch = Regex.Match(text, "Branch\"\\s*:\\s*\"([^\"]+)\"");
                    var buildIdMatch = Regex.Match(text, "BuildId\"\\s*:\\s*\"([^\"]+)\"");

                    var rawVersion = versionMatch.Success ? versionMatch.Groups[1].Value : null;
                    var branch = branchMatch.Success ? branchMatch.Groups[1].Value : null;
                    var buildId = buildIdMatch.Success ? buildIdMatch.Groups[1].Value : null;

                    string displayVersion = null;
                    if (!string.IsNullOrEmpty(branch))
                    {
                        var semMatch = Regex.Match(branch, @"\b\d+\.\d+(?:\.\d+)?\b");
                        if (semMatch.Success)
                        {
                            displayVersion = semMatch.Value;
                        }
                    }

                    var finalVersion = displayVersion ?? rawVersion ?? "Alpha";

                    return new VersionInfo
                    {
                        Version = finalVersion,
                        RawVersion = rawVersion,
                        Branch = branch,
                        BuildId = buildId
                    };
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
                        var normalized = v.Replace(",", ".").Trim();
                        var semMatch = Regex.Match(normalized, @"\b\d+\.\d+(?:\.\d+)?\b");
                        return new VersionInfo
                        {
                            Version = semMatch.Success ? semMatch.Value : normalized,
                            RawVersion = normalized
                        };
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, string.Format("Failed to get FileVersionInfo for {0}", exePath));
                }
            }

            return new VersionInfo { Version = "Alpha", RawVersion = "Alpha" };
        }
    }
}
