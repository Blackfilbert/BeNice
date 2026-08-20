using System.Collections.Generic;
using BeNice.HexaSort.Models;

namespace BeNice.HexaSort.Services
{
    public enum HexReactionOperationType
    {
        Merge,
        Clear
    }

    public readonly struct HexReactionOperation
    {
        public HexReactionOperationType Type { get; }
        public HexCoordinates Source { get; }
        public HexCoordinates Target { get; }
        public HexTileColor Color { get; }
        public int Count { get; }
        public IReadOnlyList<HexTileColor> ColorsBottomToTop { get; }

        private HexReactionOperation(
            HexReactionOperationType type,
            HexCoordinates source,
            HexCoordinates target,
            HexTileColor color,
            int count,
            IReadOnlyList<HexTileColor> colorsBottomToTop)
        {
            Type = type;
            Source = source;
            Target = target;
            Color = color;
            Count = count;
            ColorsBottomToTop = colorsBottomToTop;
        }

        public static HexReactionOperation Merge(
            HexCoordinates source,
            HexCoordinates target,
            HexTileColor color,
            IReadOnlyList<HexTileColor> colorsBottomToTop)
        {
            return new HexReactionOperation(
                HexReactionOperationType.Merge,
                source,
                target,
                color,
                colorsBottomToTop.Count,
                colorsBottomToTop);
        }

        public static HexReactionOperation Clear(HexCoordinates target, HexTileColor color, int count)
        {
            return new HexReactionOperation(
                HexReactionOperationType.Clear,
                target,
                target,
                color,
                count,
                null);
        }
    }

    public enum HexReactionResult
    {
        Completed,
        StepLimitExceeded,
        Cancelled
    }
}
