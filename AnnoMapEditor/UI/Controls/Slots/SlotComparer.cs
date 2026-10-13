using AnnoMapEditor.DataArchives.Assets.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnnoMapEditor.UI.Controls.Slots
{
    /// <summary>
    /// Sorts the island's slots the same way as on the minimap in game.
    /// Feature request courtesy of Taubenangriff.
    /// </summary>
    public class SlotComparer : IComparer<SlotAsset>
    {
        public static readonly SlotComparer Instance = new();


        private static readonly Dictionary<string, int> _orderLookup;
        static SlotComparer()
        {
            int index = 0;
            // MinimapSceneAsset is loaded from the game's assets.xml. It is intentionally null
            // in custom-island test mode, where slots still need to be displayed and counted.
            _orderLookup = MinimapSceneAsset.Instance?.LodesOrderSlotTypes?
                .ToDictionary(f => f, f => index++)
                ?? new Dictionary<string, int>();
        }


        public int Compare(SlotAsset? x, SlotAsset? y)
        {
            if (x == null || y == null)
                throw new ArgumentNullException();

            if (x.SlotType == null || !_orderLookup.TryGetValue(x.SlotType, out int xIndex))
                return 1;

            if (y.SlotType == null || !_orderLookup.TryGetValue(y.SlotType, out int yIndex))
                return -1;

            return xIndex.CompareTo(yIndex);
        }
    }
}
