using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
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
                    GameActions = new List<GameAction>()
                };

                // Primary Play Action
                if (settings.Settings.LaunchViaLauncher && !string.IsNullOrEmpty(install.LauncherExePath) && File.Exists(install.LauncherExePath))
                {
                    game.GameActions.Add(new GameAction
                    {
                        Name = "Play via RSI Launcher",
                        Type = GameActionType.File,
                        Path = install.LauncherExePath,
                        IsPlayAction = true
                    });

                    game.GameActions.Add(new GameAction
                    {
                        Name = "Direct Launch (EAC)",
                        Type = GameActionType.File,
                        Path = install.ExecutablePath,
                        WorkingDir = install.InstallDirectory,
                        IsPlayAction = false
                    });
                }
                else
                {
                    game.GameActions.Add(new GameAction
                    {
                        Name = "Play Star Citizen",
                        Type = GameActionType.File,
                        Path = install.ExecutablePath,
                        WorkingDir = install.InstallDirectory,
                        IsPlayAction = true
                    });

                    if (!string.IsNullOrEmpty(install.LauncherExePath) && File.Exists(install.LauncherExePath))
                    {
                        game.GameActions.Add(new GameAction
                        {
                            Name = "Open RSI Launcher",
                            Type = GameActionType.File,
                            Path = install.LauncherExePath,
                            IsPlayAction = false
                        });
                    }
                }

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
