using AnnoMapEditor.DataArchives.Assets.Models;
using AnnoMapEditor.MapTemplates.Models;
using AnnoMapEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Media;

namespace AnnoMapEditor.UI.Overlays.SelectSlots
{
    public class SlotAssignmentViewModel : ObservableBase
    {
        private const long GOLD_ORE_GUID = 1010507;

        private readonly SlotAsset _emptySlot;


        public SlotAssignment SlotAssignment { get; init; }

        public SlotAsset AssignedSlot
        {
            get => SlotAssignment.AssignedSlot ?? _emptySlot;
            set
            {
                SlotAssignment.AssignedSlot = value != _emptySlot ? value : null;
                OnPropertyChanged();

                UpdateBrushes();
            }
        }

        public ObservableCollection<SlotAsset> SlotItems { get; init; }

        public bool HasFocus
        {
            get => _hasFocus;
            private set => SetProperty(ref _hasFocus, value);
        }
        private bool _hasFocus;

        public int PathThickness
        {
            get => _pathThickness;
            private set => SetProperty(ref _pathThickness, value);
        }
        private int _pathThickness = 1;

        public Brush PinBrush
        {
            get => _pinBrush;
            private set => SetProperty(ref _pinBrush, value);
        }
        private Brush _pinBrush;

        public Brush BackgroundBrush
        {
            get => _backgroundBrush;
            private set => SetProperty(ref _backgroundBrush, value);
        }
        private Brush _backgroundBrush;

        public RegionAsset SelectedRegion
        {
            get => _selectedRegion;
            set
            {
                SetProperty(ref _selectedRegion, value);
                UpdateFilter();
            }
        }
        private RegionAsset _selectedRegion;

        private readonly HashSet<SlotAsset> _originalSlotItems;

        public bool NewWorldAllOres
        {
            get => _newWorldAllOres;
            set
            {
                SetProperty(ref _newWorldAllOres, value);
                UpdateFilter();
            }
        }
        private bool _newWorldAllOres;

        public bool OldWorldAllOres
        {
            get => _oldWorldAllOres;
            set
            {
                SetProperty(ref _oldWorldAllOres, value);
                UpdateFilter();
            }
        }
        private bool _oldWorldAllOres;

        public bool ArcticAllOres
        {
            get => _arcticAllOres;
            set
            {
                SetProperty(ref _arcticAllOres, value);
                UpdateFilter();
            }
        }
        private bool _arcticAllOres;


        public SlotAssignmentViewModel(SlotAssignment slotAssignment, RegionAsset selectedRegion)
        {
            SlotAssignment = slotAssignment;
            _selectedRegion = selectedRegion;
            _emptySlot = new SlotAsset
            {
                DisplayName = $"Empty - Slot {slotAssignment.Slot.ObjectGuid}"
            };

            ICollection<SlotAsset> originalSlotItems = slotAssignment.Slot.SlotAsset!.ReplacementSlotAssets;
            _originalSlotItems = new(originalSlotItems);

            IEnumerable<SlotAsset> additionalOptions = IsMineSlot
                ? GetOreOptions(RegionAsset.SouthAmerica)
                    .Concat(GetOreOptions(RegionAsset.Moderate))
                    .Concat(GetOreOptions(RegionAsset.Arctic))
                : Enumerable.Empty<SlotAsset>();
            SlotItems = new(originalSlotItems.Concat(additionalOptions).Distinct());
            SlotItems.Add(_emptySlot);

            CollectionView fertilitiesView = (CollectionView)CollectionViewSource.GetDefaultView(SlotItems);
            fertilitiesView.Filter = SlotAssetFilter;

            UpdateBrushes();
        }


        private bool SlotAssetFilter(object item)
        {
            if (item is not SlotAsset slotAsset)
                throw new ArgumentException();

            if (slotAsset == _emptySlot || slotAsset == AssignedSlot)
                return true;

            if (NewWorldAllOres && _selectedRegion == RegionAsset.SouthAmerica && IsMineSlot)
                return slotAsset.GUID == GOLD_ORE_GUID || slotAsset.AssociatedRegions.Contains(RegionAsset.SouthAmerica);

            if (OldWorldAllOres && _selectedRegion == RegionAsset.Moderate && IsMineSlot)
                return slotAsset.AssociatedRegions.Contains(RegionAsset.Moderate);

            if (ArcticAllOres && _selectedRegion == RegionAsset.Arctic && IsMineSlot)
                return slotAsset.AssociatedRegions.Contains(RegionAsset.Arctic);

            if (!_originalSlotItems.Contains(slotAsset))
                return false;

            // Warning: Hardcoding
            // SouthAmerica uses both random mine slots 614 and 1000029. However, none of the
            // replacements for 1000029 contain the New World as an AssociatedRegion. Despite of
            // this, it is possible to have both 1010502 Iron Deposit and 1010507 Gold Deposit in
            // the New World.
            if (_selectedRegion == RegionAsset.SouthAmerica
                && SlotAssignment.Slot.ObjectGuid == SlotAsset.RANDOM_MINE_OLD_WORLD_GUID
                && (slotAsset.GUID == 1010501 || slotAsset.GUID == GOLD_ORE_GUID))
                return true;

            if (slotAsset.AssociatedRegions.Contains(_selectedRegion))
                return true;

            return false;
        }

        private bool IsMineSlot => SlotAsset.IsRandomMineSlot(SlotAssignment.Slot.ObjectGuid);

        private static IEnumerable<SlotAsset> GetOreOptions(RegionAsset region)
        {
            return new[] { SlotAsset.RandomMineOldWorld, SlotAsset.RandomMineNewWorld, SlotAsset.RandomMineArctic }
                .Where(mineSlot => mineSlot != null)
                .SelectMany(mineSlot => mineSlot.ReplacementSlotAssets)
                .Where(slot => slot.AssociatedRegions.Contains(region)
                    || (region == RegionAsset.SouthAmerica && slot.GUID == GOLD_ORE_GUID))
                .DistinctBy(slot => slot.GUID);
        }

        private void UpdateFilter()
        {
            CollectionViewSource.GetDefaultView(SlotItems).Refresh();
        }

        public void OnGotFocus()
        {
            HasFocus = true;
            PathThickness = 3;
        }

        public void OnLostFocus()
        {
            HasFocus = false;
            PathThickness = AssignedSlot == _emptySlot ? 1 : 3;
        }

        private void UpdateBrushes()
        {
            PathThickness = AssignedSlot == _emptySlot && !HasFocus ? 1 : 3;

            Brush slotBrush = SlotAsset.GetRandomSlotType(SlotAssignment.Slot.ObjectGuid) switch
            {
                SlotAsset.RANDOM_MINE => Brushes.Gold,
                SlotAsset.RANDOM_CLAY => Brushes.Orange,
                SlotAsset.RANDOM_OIL => Brushes.LightBlue,
                SlotAsset.RANDOM_GAS => Brushes.Red,
                _ => Brushes.White
            };

            if (AssignedSlot == _emptySlot)
            {
                // Keep empty slots identifiable, but make selected resources visibly stronger.
                PinBrush = new SolidColorBrush(((SolidColorBrush)slotBrush).Color)
                {
                    Opacity = 0.7
                };
                BackgroundBrush = Brushes.Transparent;
            }
            else
            {
                PinBrush = slotBrush;
                BackgroundBrush = slotBrush;
            }
        }
    }
}
