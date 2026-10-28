using AnnoMapEditor.DataArchives.Assets;
using AnnoMapEditor.DataArchives.Assets.Models;
using AnnoMapEditor.UI.Overlays.SelectIsland;
using AnnoMapEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnnoMapEditor.UI.Overlays.SelectCustomIsland
{
    public class SelectCustomIslandViewModel : ObservableBase, IOverlayViewModel
    {
        public event IslandSelectedEventHandler? IslandSelected;

        private readonly IReadOnlyList<IslandAsset> _allIslands;

        public IReadOnlyList<string> PackageNames { get; }

        public string? SelectedPackageName
        {
            get => _selectedPackageName;
            set
            {
                if (SetProperty(ref _selectedPackageName, value))
                    UpdateIslands();
            }
        }
        private string? _selectedPackageName;

        public IReadOnlyList<IslandAsset> Islands
        {
            get => _islands;
            private set => SetProperty(ref _islands, value);
        }
        private IReadOnlyList<IslandAsset> _islands = Array.Empty<IslandAsset>();

        public bool HasIslands => Islands.Count > 0;

        public IslandAsset? SelectedIsland
        {
            get => _selectedIsland;
            set
            {
                if (value == null)
                    return;

                _selectedIsland = value;
                OverlayService.Instance.Close(this);
                IslandSelected?.Invoke(this, new(value));
            }
        }
        private IslandAsset? _selectedIsland;

        public SelectCustomIslandViewModel()
        {
            _allIslands = CustomIslandCatalog.Instance.Islands
                .Where(island => !string.IsNullOrWhiteSpace(island.CustomPackageName))
                .OrderBy(island => island.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            PackageNames = _allIslands
                .Select(island => island.CustomPackageName!)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            SelectedPackageName = PackageNames.FirstOrDefault();
            if (SelectedPackageName == null)
                UpdateIslands();
        }

        private void UpdateIslands()
        {
            Islands = string.IsNullOrWhiteSpace(SelectedPackageName)
                ? Array.Empty<IslandAsset>()
                : _allIslands
                    .Where(island => string.Equals(
                        island.CustomPackageName,
                        SelectedPackageName,
                        StringComparison.CurrentCultureIgnoreCase))
                    .OrderBy(island => island.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

            _selectedIsland = null;
            OnPropertyChanged(nameof(SelectedIsland));
            OnPropertyChanged(nameof(HasIslands));
        }

        public void OnCancel()
        {
            OverlayService.Instance.Close(this);
        }
    }
}
