using AnnoMapEditor.DataArchives.Assets.Deserialization;
using AnnoMapEditor.DataArchives.Assets.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace AnnoMapEditor.DataArchives.Assets.Models
{
    [AssetTemplate(TEMPLATE_NAME)]
    public class SlotAsset : StandardAsset
    {
        public const string TEMPLATE_NAME = "Slot";

        public const long RANDOM_MINE_OLD_WORLD_GUID = 1000029;
        public const long RANDOM_MINE_NEW_WORLD_GUID = 614;
        public const long RANDOM_MINE_ARCTIC_GUID = 116037;
        public const long RANDOM_CLAY_GUID = 100417;
        public const long RANDOM_OIL_GUID = 100849;
        public const long RANDOM_GAS_GUID = 113750;

        // Additional random slot GUIDs can be added here by mods. The original
        // constants remain available for backwards compatibility.
        // seperate GUIDs with commas, e.g. 123456,789012,345678
        public static readonly IReadOnlyCollection<long> RANDOM_MINE_GUIDS = new[]
        {
            RANDOM_MINE_OLD_WORLD_GUID,
            RANDOM_MINE_NEW_WORLD_GUID,
            RANDOM_MINE_ARCTIC_GUID,
            618
        };

        public static readonly IReadOnlyCollection<long> RANDOM_OIL_GUIDS = new[]
        {
            RANDOM_OIL_GUID
        };

        public static readonly IReadOnlyCollection<long> RANDOM_CLAY_GUIDS = new[]
        {
            RANDOM_CLAY_GUID
        };

        public static readonly IReadOnlyCollection<long> RANDOM_GAS_GUIDS = new[]
        {
            RANDOM_GAS_GUID
        };

        public const string RANDOM_MINE = "RANDOM_MINE";
        public const string RANDOM_CLAY = "RANDOM_CLAY";
        public const string RANDOM_OIL = "RANDOM_OIL";
        public const string RANDOM_GAS = "RANDOM_GAS";
        public const string RANDOM_CUSTOM = "RANDOM_CUSTOM";

        public static string GetRandomSlotType(long slotGuid)
        {
            if (RANDOM_MINE_GUIDS.Contains(slotGuid))
                return RANDOM_MINE;
            if (RANDOM_CLAY_GUIDS.Contains(slotGuid))
                return RANDOM_CLAY;
            if (RANDOM_OIL_GUIDS.Contains(slotGuid))
                return RANDOM_OIL;
            if (RANDOM_GAS_GUIDS.Contains(slotGuid))
                return RANDOM_GAS;

            return RANDOM_CUSTOM;
        }

        public static bool IsRandomMineSlot(long slotGuid) => RANDOM_MINE_GUIDS.Contains(slotGuid);


        [StaticAsset(RANDOM_MINE_OLD_WORLD_GUID)]
        public static SlotAsset RandomMineOldWorld { get; private set; }

        [StaticAsset(RANDOM_MINE_NEW_WORLD_GUID)]
        public static SlotAsset RandomMineNewWorld { get; private set; }

        [StaticAsset(RANDOM_MINE_ARCTIC_GUID)]
        public static SlotAsset RandomMineArctic { get; private set; }

        [StaticAsset(RANDOM_CLAY_GUID)]
        public static SlotAsset RandomClay { get; private set; }

        [StaticAsset(RANDOM_OIL_GUID)]
        public static SlotAsset RandomOil { get; private set; }


        public string DisplayName { get; init; }

        public string DisplayNameWithGuid => GUID == 0 ? DisplayName : $"{DisplayName} - {GUID}";

        public string? SlotType { get; init; }

        public bool IsRandomSlot { get; init; }

        public IEnumerable<long> ReplacementGuids { get; init; }

        [GuidReference(nameof(ReplacementGuids))]
        public ICollection<SlotAsset> ReplacementSlotAssets { get; set; }

        public IEnumerable<string> AssociatedRegionIds { get; init; }

        [RegionIdReference(nameof(AssociatedRegionIds))]
        public ICollection<RegionAsset> AssociatedRegions { get; set; }


        public SlotAsset() : base()
        {
            DisplayName = "";
            ReplacementGuids = Enumerable.Empty<long>();
            ReplacementSlotAssets = Array.Empty<SlotAsset>();
            AssociatedRegions = Array.Empty<RegionAsset>();
        }


        // deserialization constructor
        public SlotAsset(XElement valuesXml) 
            : base(valuesXml)
        {
            DisplayName = valuesXml.Element("Text")!
                .Element("LocaText")!
                .Element("English")!
                .Element("Text")!
                .Value!;

            SlotType = valuesXml.Element(TEMPLATE_NAME)?
                .Element("SlotType")?
                .Value;
            IsRandomSlot = SlotType == "Random";

            IEnumerable<long>? replacementGuids = valuesXml.Element("RandomMapObject")?
                .Element("Replacements")?
                .Elements("Item")?
                .Select(x => long.Parse(x.Element("NewAsset")!.Value))
                .ToList();

            IEnumerable<long>? dlcReplacementGuids = valuesXml.Element("RandomMapObject")?
                .Element("DLCSlotReplacements")?
                .Elements("Item")?
                .Select(x => x.Element("Replacement"))
                .Where(x => x != null)
                .Select(x => long.Parse(x!.Value))
                .ToList();

            ReplacementGuids = replacementGuids ?? dlcReplacementGuids ?? Array.Empty<long>();

            AssociatedRegionIds = valuesXml.Element("Building")?
                .Element("AssociatedRegions")?
                .Value?
                .Split(';')
                .ToArray()
                ?? Array.Empty<string>();
        }
    }
}
