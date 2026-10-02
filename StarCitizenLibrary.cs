using Playnite.SDK;
using Playnite.SDK.Events;
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
                    Name = "Tracking",
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

            // Prüfen, ob bereits ein manueller Star Citizen Eintrag mit Spielzeit/Notizen in Playnite existiert
            var manualGame = PlayniteApi.Database.Games.FirstOrDefault(g =>
                g.PluginId == Guid.Empty &&
                !string.IsNullOrEmpty(g.Name) &&
                g.Name.IndexOf("Star Citizen", StringComparison.OrdinalIgnoreCase) >= 0);

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

                // Wenn ein manueller Eintrag existiert, Spielzeit & Statistiken für den Hauptchannel (LIVE) automatisch übernehmen
                if (manualGame != null && install.Channel.Equals("LIVE", StringComparison.OrdinalIgnoreCase))
                {
                    if (manualGame.Playtime > 0) game.Playtime = manualGame.Playtime;
                    if (manualGame.PlayCount > 0) game.PlayCount = manualGame.PlayCount;
                    if (manualGame.LastActivity.HasValue) game.LastActivity = manualGame.LastActivity;
                    game.Favorite = manualGame.Favorite;
                    if (manualGame.UserScore.HasValue) game.UserScore = manualGame.UserScore;
                    if (!string.IsNullOrWhiteSpace(manualGame.Notes)) 
                    // Falls der Nutzer bereits eigene Bilder gewählt hatte, diese übernehmen
                    if (!string.IsNullOrEmpty(manualGame.CoverImage)) game.CoverImage = new MetadataFile(manualGame.CoverImage);
                    if (!string.IsNullOrEmpty(manualGame.BackgroundImage)) game.BackgroundImage = new MetadataFile(manualGame.BackgroundImage);
                    if (!string.IsNullOrEmpty(manualGame.Icon)) game.Icon = new MetadataFile(manualGame.Icon);
                }

                // Standard-Artworks falls keine manuellen vorhanden
                if (game.Icon == null)
                {
                    var iconPath = Path.Combine(pluginFolder, "icon.png");
                    if (File.Exists(iconPath)) game.Icon = new MetadataFile(iconPath);
                }

                if (game.CoverImage == null)
                {
                    game.CoverImage = new MetadataFile("https://media.robertsspaceindustries.com/o2x5s5x7omj1g/source.jpg");
                }

                if (game.BackgroundImage == null)
                {
                    game.BackgroundImage = new MetadataFile("https://media.robertsspaceindustries.com/y4pve9y7y9p3y/source.jpg");
                }

                games.Add(game);
            }

            return games;
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            try
            {
                // Nach dem Bibliotheks-Update: Prüfen ob ein manueller Eintrag existiert und die Spielzeit übertragen
                var libraryLiveGame = PlayniteApi.Database.Games.FirstOrDefault(g => g.PluginId == Id && g.GameId.Equals("RSI_SC_LIVE", StringComparison.OrdinalIgnoreCase));
                var manualGame = PlayniteApi.Database.Games.FirstOrDefault(g =>
                    g.PluginId == Guid.Empty &&
                    !string.IsNullOrEmpty(g.Name) &&
                    g.Name.IndexOf("Star Citizen", StringComparison.OrdinalIgnoreCase) >= 0);

                if (libraryLiveGame != null && manualGame != null && manualGame.Playtime > 0 && libraryLiveGame.Playtime == 0)
                {
                    libraryLiveGame.Playtime = manualGame.Playtime;
                    libraryLiveGame.PlayCount = manualGame.PlayCount;
                    libraryLiveGame.LastActivity = manualGame.LastActivity;
                    libraryLiveGame.Favorite = manualGame.Favorite;
                    if (!string.IsNullOrWhiteSpace(manualGame.Notes)) libraryLiveGame.Notes = manualGame.Notes;

                    PlayniteApi.Database.Games.Update(libraryLiveGame);
                    logger.Info($"Successfully migrated {manualGame.Playtime} seconds of playtime from manual entry to library entry.");
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to migrate playtime from manual Star Citizen entry.");
            }
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
