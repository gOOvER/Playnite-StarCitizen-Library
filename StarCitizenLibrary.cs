using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;

namespace StarCitizenLibrary
{
    public class StarCitizenLibrary : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private StarCitizenLibrarySettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("d2146b15-4cfc-40cc-93dd-1297e2e0aa49");

        public override string Name => "Roberts Space Industries";

        public override string LibraryIcon => Path.Combine(GetPluginFolder(), "icon.png");

        public override LibraryClient Client { get; }

        public StarCitizenLibrary(IPlayniteAPI api) : base(api)
        {
            settings = new StarCitizenLibrarySettingsViewModel(this);
            Client = new StarCitizenLibraryClient(this);
            Properties = new LibraryPluginProperties
            {
                HasSettings = true
            };
        }

        public string GetPluginFolder()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
        {
            var installations = StarCitizenDetector.DetectInstallations(
                settings.Settings.CustomInstallPath,
                settings.Settings.ImportPtu,
                settings.Settings.ImportEptu,
                settings.Settings.ImportTechPreview
            );

            var install = installations.FirstOrDefault(i =>
                string.Equals($"RSI_SC_{i.Channel}", args.Game.GameId, StringComparison.OrdinalIgnoreCase));

            if (install != null)
            {
                var bin64Dir = Path.Combine(install.InstallDirectory, "Bin64");
                var gameRoot = Path.GetDirectoryName(install.InstallDirectory) ?? install.InstallDirectory;
                var launcherPath = !string.IsNullOrEmpty(install.LauncherExePath) && File.Exists(install.LauncherExePath)
                    ? install.LauncherExePath
                    : install.ExecutablePath;

                yield return new AutomaticPlayController(args.Game)
                {
                    Name = "Play Star Citizen",
                    Path = launcherPath,
                    WorkingDir = gameRoot,
                    TrackingMode = TrackingMode.Directory,
                    TrackingPath = bin64Dir,
                    InitialTrackingDelay = 0,
                    TrackingFrequency = 2000
                };
            }
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            var games = new List<GameMetadata>();
            var pluginFolder = GetPluginFolder();

            var installations = StarCitizenDetector.DetectInstallations(
                settings.Settings.CustomInstallPath,
                settings.Settings.ImportPtu,
                settings.Settings.ImportEptu,
                settings.Settings.ImportTechPreview
            );

            foreach (var install in installations)
            {
                var gameId = $"RSI_SC_{install.Channel}";
                var bin64Dir = Path.Combine(install.InstallDirectory, "Bin64");
                var gameRoot = Path.GetDirectoryName(install.InstallDirectory) ?? install.InstallDirectory;
                var launcherPath = !string.IsNullOrEmpty(install.LauncherExePath) && File.Exists(install.LauncherExePath)
                    ? install.LauncherExePath
                    : install.ExecutablePath;

                var game = new GameMetadata
                {
                    GameId = gameId,
                    Name = install.ChannelName,
                    InstallDirectory = install.InstallDirectory,
                    IsInstalled = true,
                    Version = install.Version,
                    Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_windows") },
                    Developers = new HashSet<MetadataProperty> { new MetadataNameProperty("Cloud Imperium Games") },
                    Publishers = new HashSet<MetadataProperty> { new MetadataNameProperty("Roberts Space Industries") },
                    Genres = new HashSet<MetadataProperty>
                    {
                        new MetadataNameProperty("Space Sim"),
                        new MetadataNameProperty("MMO"),
                        new MetadataNameProperty("First-Person Shooter")
                    },
                    Features = new HashSet<MetadataProperty>
                    {
                        new MetadataNameProperty("Multiplayer"),
                        new MetadataNameProperty("Co-op")
                    },
                    Description = "Star Citizen is an in-development multiplayer space trading and combat simulation game developed and published by Cloud Imperium Games.",
                    Links = new List<Link>
                    {
                        new Link("Official Website", "https://robertsspaceindustries.com/"),
                        new Link("Server Status", "https://status.robertsspaceindustries.com/"),
                        new Link("Comm-Link", "https://robertsspaceindustries.com/comm-link"),
                        new Link("Issue Council", "https://issue-council.robertsspaceindustries.com/"),
                        new Link("Erkul Ship Loadout", "https://www.erkul.games/live/calculator")
                    },
                    GameActions = new List<GameAction>
                    {
                        new GameAction
                        {
                            Name = "Tracking",
                            Type = GameActionType.File,
                            Path = launcherPath,
                            WorkingDir = gameRoot,
                            TrackingMode = TrackingMode.Directory,
                            TrackingPath = bin64Dir,
                            InitialTrackingDelay = 0,
                            TrackingFrequency = 2000,
                            IsPlayAction = true
                        }
                    }
                };

                // Default Artworks (Icons & Images)
                var iconPath = Path.Combine(pluginFolder, "icon.png");
                if (File.Exists(iconPath))
                {
                    game.Icon = new MetadataFile(iconPath);
                }

                // Official high-res promotional artwork from RSI CDN
                game.CoverImage = new MetadataFile("https://media.robertsspaceindustries.com/o2x5s5x7omj1g/source.jpg");
                game.BackgroundImage = new MetadataFile("https://media.robertsspaceindustries.com/y4pve9y7y9p3y/source.jpg");

                games.Add(game);
            }

            return games;
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new StarCitizenLibrarySettingsView();
        }
    }
}
