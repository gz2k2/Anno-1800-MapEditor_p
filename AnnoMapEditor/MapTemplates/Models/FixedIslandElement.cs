using Anno.FileDBModels.Anno1800.MapTemplate;
using AnnoMapEditor.DataArchives;
using AnnoMapEditor.DataArchives.Assets;
using AnnoMapEditor.DataArchives.Assets.Models;
using AnnoMapEditor.DataArchives.Assets.Repositories;
using AnnoMapEditor.MapTemplates.Enums;
using AnnoMapEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using IslandType = AnnoMapEditor.MapTemplates.Enums.IslandType;

namespace AnnoMapEditor.MapTemplates.Models
{
    public class FixedIslandElement : IslandElement
    {
        private static readonly Logger<FixedIslandElement> _logger = new();


        // TODO: Deserialize Fertilities and MineSlotMappings instead of copying from the source template.
        // TODO: Remove _sourceTemplate alltogether
        private readonly Element? _sourceElement;

        public IslandAsset IslandAsset 
        { 
            get => _islandAsset;
            [MemberNotNull(nameof(_islandAsset))]
            private set
            {
                SetProperty(ref _islandAsset!, value);
                SizeInTiles = _islandAsset.SizeInTiles;
            }
        }
        private IslandAsset _islandAsset;


        public bool RandomizeRotation
        {
            get => _randomizeRotation;
            set {
                SetProperty(ref _randomizeRotation, value);
                if (!value && Rotation == null) Rotation = 0;
            }
        }
        private bool _randomizeRotation = true;

        public byte? Rotation
        {
            get => _rotation;
            set 
            {
                SetProperty(ref _rotation, value != null ? (byte)(value % 4) : null);
                SetProperty(ref _randomizeRotation, value == null);
            }
        }
        private byte? _rotation;

        public bool RandomizeFertilities
        {
            get => _randomizeFertilities;
            set => SetProperty(ref _randomizeFertilities, value);
        }
        private bool _randomizeFertilities = true;

        public ObservableCollection<FertilityAsset> Fertilities { get; init; } = new();

        /// <summary>
        /// Uses the editor to choose fertilities when the game's generator cannot do so for a
        /// fixed island from another region.
        /// </summary>
        public bool AppRandomizeFertilities
        {
            get => _appRandomizeFertilities;
            set => SetProperty(ref _appRandomizeFertilities, value);
        }
        private bool _appRandomizeFertilities;

        public int AppRandomFertilitiesMin
        {
            get => _appRandomFertilitiesMin;
            set => SetProperty(ref _appRandomFertilitiesMin, Math.Max(0, value));
        }
        private int _appRandomFertilitiesMin = 1;

        public int AppRandomFertilitiesMax
        {
            get => _appRandomFertilitiesMax;
            set => SetProperty(ref _appRandomFertilitiesMax, Math.Max(0, value));
        }
        private int _appRandomFertilitiesMax = 3;

        public bool RandomizeSlots
        {
            get => _randomizeSlots;
            set => SetProperty( ref _randomizeSlots, value);
        }
        private bool _randomizeSlots = true;

        /// <summary>
        /// Uses the editor to assign region-compatible resources when the game's generator
        /// cannot reliably randomize slots on a foreign or custom island.
        /// </summary>
        public bool AppRandomizeSlots
        {
            get => _appRandomizeSlots;
            set => SetProperty(ref _appRandomizeSlots, value);
        }
        private bool _appRandomizeSlots;

        public int AppRandomOreMin { get => _appRandomOreMin; set => SetProperty(ref _appRandomOreMin, Math.Max(0, value)); }
        private int _appRandomOreMin = 1;
        public int AppRandomOreMax { get => _appRandomOreMax; set => SetProperty(ref _appRandomOreMax, Math.Max(0, value)); }
        private int _appRandomOreMax = 3;
        public int AppRandomClayMin { get => _appRandomClayMin; set => SetProperty(ref _appRandomClayMin, Math.Max(0, value)); }
        private int _appRandomClayMin = 1;
        public int AppRandomClayMax { get => _appRandomClayMax; set => SetProperty(ref _appRandomClayMax, Math.Max(0, value)); }
        private int _appRandomClayMax = 3;
        public int AppRandomOilMin { get => _appRandomOilMin; set => SetProperty(ref _appRandomOilMin, Math.Max(0, value)); }
        private int _appRandomOilMin = 1;
        public int AppRandomOilMax { get => _appRandomOilMax; set => SetProperty(ref _appRandomOilMax, Math.Max(0, value)); }
        private int _appRandomOilMax = 3;

        public Dictionary<long, SlotAssignment> SlotAssignments { get; init; } = new();


        public FixedIslandElement(IslandAsset islandAsset, IslandType islandType)
            : base(islandType)
        {
            IslandAsset = islandAsset;

            foreach (Slot slot in islandAsset.Slots.Values)
            {
                SlotAssignments.Add(slot.ObjectId, new()
                {
                    Slot = slot,
                    AssignedSlot = null
                });
            }
        }


        // ---- Serialization ----

        public FixedIslandElement(Element sourceElement) : base(sourceElement)
        {
            _sourceElement = sourceElement;

            string islandFilePath = sourceElement.MapFilePath
                ?? throw new ArgumentException($"Missing property '{nameof(Element.MapFilePath)}'.");

            _randomizeRotation = sourceElement.Rotation90 == null;
            _rotation = sourceElement.Rotation90;

            System.Diagnostics.Debug.WriteLineIf(sourceElement.Rotation90 != null, $"Reading source data {sourceElement.Rotation90} to rotation value of {_rotation} on path {islandFilePath}.");

            _randomizeFertilities = sourceElement.RandomizeFertilities != false;
            _randomizeSlots = sourceElement.MineSlotMapping == null || sourceElement.MineSlotMapping.Count == 0;

            LoadIslandDataFromRepository(sourceElement);
        }

        /// <summary>
        /// Loads the Asset data from the IslandRepository. 
        /// Should only be called when the IslandRepository is actually loaded, errors otherwise.
        /// </summary>
        /// <param name="sourceElement">The element from the template.</param>
        /// <exception cref="ArgumentException">The sourceElement does not have a MapFilePath. This is forbidden on FixedIslands.</exception>
        /// <exception cref="NullReferenceException">The given MapFilePath does not match any islands in the IslandRepository.</exception>
        [MemberNotNull(nameof(_islandAsset))]
        private void LoadIslandDataFromRepository(Element sourceElement)
        {
            string islandFilePath = sourceElement.MapFilePath
                ?? throw new ArgumentException($"Missing property '{nameof(Element.MapFilePath)}'.");

            IslandAsset? islandAsset = CustomIslandCatalog.Instance.Islands
                .FirstOrDefault(i => i.FilePath == islandFilePath);

            if (islandAsset == null && DataManager.Instance.IsInitialized
                && DataManager.Instance.IslandRepository.TryGetByFilePath(islandFilePath, out IslandAsset? repositoryIsland))
                islandAsset = repositoryIsland;

            if (islandAsset == null)
                throw new NullReferenceException($"Unknown island '{islandFilePath}'.");

            IslandAsset = islandAsset;
            //Rotation is not asset bound, thus loaded in constructor

            AssetRepository? assetRepository = DataManager.Instance.IsInitialized
                ? DataManager.Instance.AssetRepository
                : null;

            // fertilities
            //  _randomizeFertilities is loaded in constructor.
            if (sourceElement.FertilityGuids != null && assetRepository != null)
            {
                foreach (int guid in sourceElement.FertilityGuids)
                {
                    if (assetRepository.TryGet(guid, out FertilityAsset? fertility) && fertility != null)
                        Fertilities.Add(fertility);
                    else
                        _logger.LogWarning($"Unrecognized {nameof(FertilityAsset)} for GUID {guid}; game data is unavailable.");
                }
            }

            // fixed slots
            // _randomizeSlots is loaded in constructor.
            if (sourceElement.MineSlotMapping != null)
            {
                foreach ((long objectId, int slotGuid) in sourceElement.MineSlotMapping)
                {
                    // skip unsupported slots
                    if (!islandAsset.Slots.TryGetValue(objectId, out Slot? slot))
                    {
                        _logger.LogWarning($"Unrecognized {nameof(Slot)} id {objectId} on instance of '{islandFilePath}'. The slot will be skipped and the map may be corrupted.");
                        continue;
                    }

                    SlotAsset? slotAsset;

                    // 0 denotes an empty slot
                    if (slotGuid == 0)
                        slotAsset = null;

                    else if (assetRepository == null || !assetRepository.TryGet(slotGuid, out slotAsset))
                    {
                        _logger.LogWarning($"Unrecognized {nameof(SlotAsset)} GUID {slotGuid} on instance of '{islandFilePath}'. The slot will be skipped and the map may be corrupted.");
                        continue;
                    }

                    SlotAssignments.Add(objectId, new()
                    {
                        Slot = slot,
                        AssignedSlot = slotAsset
                    });
                }
            }

            // add remaining assignable slots
            foreach (Slot slot in islandAsset.Slots.Values)
            {
                if (!SlotAssignments.ContainsKey(slot.ObjectId) && slot.SlotAsset != null)
                {
                    SlotAssignments.Add(slot.ObjectId, new()
                    {
                        Slot = slot,
                        AssignedSlot = null
                    });
                }
            }
        }

        /// <summary>
        /// Creates an empty IslandAsset that gets replaced with the correct one as soon as the Repository is loaded.
        /// </summary>
        /// <param name="sourceElement">The element from the template.</param>
        /// <exception cref="ArgumentException">The sourceElement does not have a MapFilePath. This is forbidden on FixedIslands.</exception>
        [MemberNotNull(nameof(_islandAsset))]
        private void SetDummyAsset(Element sourceElement)
        {
            string islandFilePath = sourceElement.MapFilePath
                ?? throw new ArgumentException($"Missing property '{nameof(Element.MapFilePath)}'.");

            IslandSize islandSize = IslandRepository.DetectDefaultIslandSizeFromPath(islandFilePath);
            IslandDifficulty? islandDifficulty = IslandDifficulty.FromElementValue(sourceElement.Difficulty?.id);


            IslandAsset dummyAsset = new IslandAsset()
            {
                FilePath = islandFilePath,
                DisplayName = System.IO.Path.GetFileNameWithoutExtension(islandFilePath),
                Thumbnail = null,
                Region = RegionAsset.DetectFromPath(islandFilePath),
                IslandDifficulty =  new[] { islandDifficulty },
                IslandType = new[] { IslandRepository.DetectIslandTypeFromPath(islandFilePath) },
                IslandSize = new[] { islandSize },
                SizeInTiles = Math.Min(islandSize.DefaultSizeInTiles, 768),
                Slots = new Dictionary<long, Slot>()
            };

            IslandAsset = dummyAsset;
        }

        protected override void ToTemplate(Element resultElement)
        {
            base.ToTemplate(resultElement);

            resultElement.MapFilePath = _islandAsset.FilePath;
            resultElement.Rotation90  = _randomizeRotation ? null : Rotation;

            //
            // Randomization of fertilities is controlled by the flag RandomizeFertilities. Instead
            // of using true/false, true is replaced by null.
            //
            // Randomized fertilities:
            //   RandomizeFertilities = null
            //   FertilityGuids = []
            //
            // Fixed Fertilities:
            //   RandomizedFertilities = false
            //   FertilityGuids = [123456, ...]
            //
            resultElement.RandomizeFertilities = _randomizeFertilities ? null : false;
            if (_randomizeFertilities)
                resultElement.FertilityGuids = Array.Empty<int>();
            else
                resultElement.FertilityGuids = Fertilities.Select(f => (int)f.GUID).ToArray();

            //
            // There exists no additional "RandomizeSlots" flag in the templates. Instead slots
            // will be randomized if MineSlotMapping is an empty list. If it contains any elements,
            // all slots will be fixed.
            //
            // 0 is used to set empty slots.
            //
            // Randomized slots:
            //   MineSlotMapping = []
            // Fixed slots
            //   MineSlotMapping = [(8252351, 1000063), (162612, 0), ...]
            //
            if (_randomizeSlots)
                resultElement.MineSlotMapping = new();
            else
                resultElement.MineSlotMapping = IslandAsset.Slots.Values
                    .Select(s =>
                    {
                        int slotGuid = 0;
                        if (SlotAssignments.TryGetValue(s.ObjectId, out SlotAssignment? assignment))
                            slotGuid = (int)(assignment.AssignedSlot?.GUID ?? 0);

                        return new Tuple<long, int>(s.ObjectId, slotGuid);
                    })
                    .ToList();

            // despite its name, all fixed islands must have a RandomIslandConfig.
            resultElement.RandomIslandConfig = new()
            {
                value = new()
                {
                    Type = new() { id = IslandType.ElementValue },
                    Difficulty = new() { id = IslandDifficulty?.ElementValue }
                }
            };
        }

        /// <summary>
        /// Serializes this island with access to the map region. This is required for the
        /// editor-side fertility fallback used by islands from a different region.
        /// </summary>
        public TemplateElement ToTemplate(RegionAsset mapRegion)
        {
            TemplateElement templateElement = base.ToTemplate();

            if (AppRandomizeFertilities && _randomizeFertilities && IslandAsset.Region.GUID != mapRegion.GUID)
            {
                List<FertilityAsset> candidates = mapRegion.AllowedFertilities.Distinct().ToList();
                int minimum = Math.Min(AppRandomFertilitiesMin, candidates.Count);
                int maximum = Math.Clamp(AppRandomFertilitiesMax, minimum, candidates.Count);
                int count = Random.Shared.Next(minimum, maximum + 1);

                int[] fertilityGuids = candidates
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(count)
                    .Select(fertility => (int)fertility.GUID)
                    .ToArray();

                templateElement.Element!.RandomizeFertilities = false;
                templateElement.Element.FertilityGuids = fertilityGuids;
            }

            if (AppRandomizeSlots && _randomizeSlots
                && (IslandAsset.CustomSourceDirectory != null || IslandAsset.Region.GUID != mapRegion.GUID))
            {
                templateElement.Element.MineSlotMapping = CreateAppRandomSlotMapping(mapRegion);
            }
            return templateElement;
        }

        private List<Tuple<long, int>> CreateAppRandomSlotMapping(RegionAsset mapRegion)
        {
            Dictionary<string, List<Slot>> slotsByType = IslandAsset.Slots.Values
                .Where(slot => slot.SlotAsset != null)
                .GroupBy(slot => SlotAsset.GetRandomSlotType(slot.ObjectGuid))
                .ToDictionary(group => group.Key, group => group.ToList());
            Dictionary<long, SlotAsset?> assignments = IslandAsset.Slots.Values
                .Where(slot => slot.SlotAsset != null)
                .ToDictionary(slot => slot.ObjectId, _ => (SlotAsset?)null);

            foreach ((string slotType, List<Slot> slots) in slotsByType)
            {
                if (slotType == SlotAsset.RANDOM_MINE)
                    AssignEachLocalResource(slots, assignments, mapRegion, AppRandomOreMin, AppRandomOreMax);
                else if (slotType == SlotAsset.RANDOM_CLAY)
                    AssignLocalResourceSlots(slots, assignments, mapRegion, AppRandomClayMin, AppRandomClayMax);
                else if (slotType == SlotAsset.RANDOM_OIL)
                    AssignLocalResourceSlots(slots, assignments, mapRegion, AppRandomOilMin, AppRandomOilMax);
                else
                    AssignOtherSlots(slots, assignments, mapRegion);
            }

            return IslandAsset.Slots.Values
                .Where(slot => slot.SlotAsset != null)
                .Select(slot => new Tuple<long, int>(slot.ObjectId, (int)(assignments[slot.ObjectId]?.GUID ?? 0)))
                .ToList();
        }

        /// <summary>
        /// Assigns every locally available ore type independently. For example, Ore 3/5 gives
        /// every ore type a target between three and five slots (where that many slots exist).
        /// </summary>
        private static void AssignEachLocalResource(List<Slot> slots, Dictionary<long, SlotAsset?> assignments,
            RegionAsset mapRegion, int minimum, int maximum)
        {
            List<SlotAsset> localResources = slots
                .SelectMany(slot => GetLocalSlotOptions(slot, mapRegion))
                .DistinctBy(asset => asset.GUID)
                .OrderBy(_ => Random.Shared.Next())
                .ToList();

            int normalizedMinimum = Math.Max(0, minimum);
            int normalizedMaximum = Math.Max(normalizedMinimum, maximum);
            Dictionary<SlotAsset, int> targets = localResources.ToDictionary(
                resource => resource,
                _ => Random.Shared.Next(normalizedMinimum, normalizedMaximum + 1));

            // Fill the minimum for every resource first. This prevents an early resource from
            // consuming slots that are required to satisfy another ore's minimum.
            for (int round = 0; round < normalizedMinimum; round++)
            {
                foreach (SlotAsset resource in localResources.OrderBy(_ => Random.Shared.Next()))
                {
                    Slot? availableSlot = slots
                        .Where(slot => assignments[slot.ObjectId] == null && GetLocalSlotOptions(slot, mapRegion).Contains(resource))
                        .OrderBy(_ => Random.Shared.Next())
                        .FirstOrDefault();
                    if (availableSlot != null)
                        assignments[availableSlot.ObjectId] = resource;
                }
            }

            foreach (SlotAsset resource in localResources.OrderBy(_ => Random.Shared.Next()))
            {
                int alreadyAssigned = assignments.Values.Count(assignment => assignment == resource);
                int remaining = targets[resource] - alreadyAssigned;
                foreach (Slot slot in slots
                    .Where(slot => assignments[slot.ObjectId] == null && GetLocalSlotOptions(slot, mapRegion).Contains(resource))
                    .OrderBy(_ => Random.Shared.Next())
                    .Take(Math.Max(0, remaining)))
                {
                    assignments[slot.ObjectId] = resource;
                }
            }
        }

        /// <summary>Assigns one local resource type to a total number of slots in the Min/Max range.</summary>
        private static void AssignLocalResourceSlots(List<Slot> slots, Dictionary<long, SlotAsset?> assignments,
            RegionAsset mapRegion, int minimum, int maximum)
        {
            int count = GetRandomSlotCount(minimum, maximum);
            foreach (Slot slot in slots
                .Where(slot => GetLocalSlotOptions(slot, mapRegion).Count > 0)
                .OrderBy(_ => Random.Shared.Next())
                .Take(count))
            {
                List<SlotAsset> options = GetLocalSlotOptions(slot, mapRegion);
                assignments[slot.ObjectId] = options[Random.Shared.Next(options.Count)];
            }
        }

        private static void AssignOtherSlots(List<Slot> slots, Dictionary<long, SlotAsset?> assignments, RegionAsset mapRegion)
        {
            foreach (Slot slot in slots)
            {
                List<SlotAsset> options = GetLocalSlotOptions(slot, mapRegion);
                if (options.Count > 0)
                    assignments[slot.ObjectId] = options[Random.Shared.Next(options.Count)];
            }
        }

        private static int GetRandomSlotCount(int minimum, int maximum)
        {
            minimum = Math.Max(0, minimum);
            maximum = Math.Max(minimum, maximum);
            return Random.Shared.Next(minimum, maximum + 1);
        }

        private static List<SlotAsset> GetLocalSlotOptions(Slot slot, RegionAsset mapRegion)
        {
            return slot.SlotAsset!.ReplacementSlotAssets
                .Where(asset => asset.AssociatedRegions.Contains(mapRegion))
                .ToList();
        }
    }
}
