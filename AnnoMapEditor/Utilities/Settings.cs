using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;

namespace AnnoMapEditor.Utilities
{
    public class Settings : ObservableBase
    {
        private const string ConfigFileName = "config.json";
        private readonly EditorConfiguration _configuration;
        public static Settings Instance { get; } = new();
        public ObservableCollection<MapPackage> MapPackages { get; } = new();
        public string ConfigPath => Path.Combine(AppContext.BaseDirectory, ConfigFileName);

        public bool Quickstart { get => _configuration.Quickstart; set => Set(value, nameof(Quickstart)); }
        public string? GamePath
        {
            get => _configuration.GamePath;
            set
            {
                if (value == GamePath) return;
                _configuration.GamePath = value;
                if (value != null)
                {
                    if (DataPath == null || !EnableExpertMode) _configuration.DataPath = Path.Combine(value, "maindata");
                    if (ModsPath == null || !EnableExpertMode) _configuration.ModsPath = Path.Combine(value, "mods");
                }
                Save();
                OnPropertyChanged(nameof(GamePath));
                OnPropertyChanged(nameof(DataPath));
                OnPropertyChanged(nameof(ModsPath));
            }
        }
        public string? DataPath { get => _configuration.DataPath; set => Set(value, nameof(DataPath)); }
        public string? ModsPath { get => _configuration.ModsPath; set => Set(value, nameof(ModsPath)); }
        public bool EnableExpertMode { get => _configuration.EnableExpertMode; set => Set(value, nameof(EnableExpertMode)); }
        public string? CustomIslandsPath { get => _configuration.CustomIslandsPath; set => Set(value, nameof(CustomIslandsPath)); }
        public string? TemplateFolderPath { get => _configuration.TemplateFolderPath; set => Set(value, nameof(TemplateFolderPath)); }
        public string? CustomIslandGuid { get => _configuration.CustomIslandGuid; set => Set(value, nameof(CustomIslandGuid)); }
        public string? AssetsHash { get => _configuration.AssetsHash; set => Set(value, nameof(AssetsHash)); }
        public string? Xpath { get => _configuration.Xpath; set => Set(value, nameof(Xpath)); }

        private Settings()
        {
            _configuration = LoadConfiguration();
            foreach (MapPackage package in _configuration.MapPackages) MapPackages.Add(package);
            if (MapPackages.Count == 0 && !string.IsNullOrWhiteSpace(CustomIslandsPath))
                MapPackages.Add(new MapPackage { Name = "Migrated package", CustomMapFolder = CustomIslandsPath });
            EnsureTwoDigitPackageIds();
            MapPackages.CollectionChanged += MapPackages_CollectionChanged;
            foreach (MapPackage package in MapPackages) package.PropertyChanged += MapPackage_PropertyChanged;
            if (GamePath == null) _configuration.GamePath = GetInstallDirFromRegistry();
            Save();
        }

        private EditorConfiguration LoadConfiguration()
        {
            try
            {
                if (File.Exists(ConfigPath))
                    return JsonConvert.DeserializeObject<EditorConfiguration>(File.ReadAllText(ConfigPath)) ?? new EditorConfiguration();
            }
            catch (IOException) { }
            catch (JsonException) { }

            return new EditorConfiguration
            {
                Quickstart = UserSettings.Default.Quickstart,
                GamePath = UserSettings.Default.GamePath,
                DataPath = UserSettings.Default.DataPath,
                ModsPath = UserSettings.Default.ModsPath,
                EnableExpertMode = UserSettings.Default.EnableExpertMode,
                CustomIslandsPath = UserSettings.Default.CustomIslandsPath,
                TemplateFolderPath = UserSettings.Default.TemplateFolderPath,
                AssetsHash = UserSettings.Default.AssetsHash,
                Xpath = UserSettings.Default.Xpath,
                MapPackages = DeserializeLegacyPackages(UserSettings.Default.MapPackages)
            };
        }

        private static Collection<MapPackage> DeserializeLegacyPackages(string json)
        {
            try { return new Collection<MapPackage>(JsonConvert.DeserializeObject<MapPackage[]>(json ?? "[]") ?? Array.Empty<MapPackage>()); }
            catch (JsonException) { return new Collection<MapPackage>(); }
        }

        private void Set<T>(T value, string propertyName)
        {
            var property = typeof(EditorConfiguration).GetProperty(propertyName)!;
            if (Equals(property.GetValue(_configuration), value)) return;
            property.SetValue(_configuration, value);
            Save();
            OnPropertyChanged(propertyName);
        }

        private void MapPackages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null) foreach (MapPackage package in e.OldItems) package.PropertyChanged -= MapPackage_PropertyChanged;
            if (e.NewItems != null) foreach (MapPackage package in e.NewItems) package.PropertyChanged += MapPackage_PropertyChanged;
            EnsureTwoDigitPackageIds();
            Save();
        }

        private void EnsureTwoDigitPackageIds()
        {
            if (MapPackages.Count > 99)
                throw new InvalidOperationException("A maximum of 99 map packages is supported.");

            HashSet<string> usedIds = new(StringComparer.Ordinal);
            Queue<MapPackage> packagesNeedingId = new();

            foreach (MapPackage package in MapPackages)
            {
                string id = package.Id?.Trim() ?? string.Empty;
                bool isValid = id.Length == 2
                    && int.TryParse(id, out int numericId)
                    && numericId >= 1
                    && numericId <= 99
                    && usedIds.Add(id);

                if (!isValid)
                    packagesNeedingId.Enqueue(package);
            }

            for (int numericId = 1; numericId <= 99 && packagesNeedingId.Count > 0; numericId++)
            {
                string candidate = numericId.ToString("D2");
                if (usedIds.Add(candidate))
                    packagesNeedingId.Dequeue().Id = candidate;
            }

            if (packagesNeedingId.Count > 0)
                throw new InvalidOperationException("No free two-digit map package ID is available.");
        }
        private void MapPackage_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Save();
        public void SaveMapPackages() => Save();
        private void Save()
        {
            _configuration.MapPackages = new Collection<MapPackage>(MapPackages);
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(_configuration, Formatting.Indented));
        }

        public static string? GetInstallDirFromRegistry()
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Ubisoft\Anno 1800");
            string? installDir = key?.GetValue("InstallDir") as string;
            return installDir?.Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar);
        }
    }

    public class EditorConfiguration
    {
        public bool Quickstart { get; set; }
        public string? GamePath { get; set; }
        public string? DataPath { get; set; }
        public string? ModsPath { get; set; }
        public bool EnableExpertMode { get; set; }
        public string? CustomIslandsPath { get; set; }
        public string? TemplateFolderPath { get; set; }
        [JsonProperty("Guid")]
        public string? CustomIslandGuid { get; set; }
        public string? AssetsHash { get; set; }
        public string? Xpath { get; set; }
        public Collection<MapPackage> MapPackages { get; set; } = new();
    }
}
