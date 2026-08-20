using System;
using System.Collections.Generic;

namespace BeNice.HexaSort.Models
{
    public sealed class HexStackModel
    {
        private readonly List<HexTileColor> _tiles;

        public int Count => _tiles.Count;
        public bool IsEmpty => _tiles.Count == 0;

        public HexStackModel(IReadOnlyList<HexTileColor> colorsBottomToTop)
        {
            if (colorsBottomToTop == null)
                throw new ArgumentNullException(nameof(colorsBottomToTop));

            _tiles = new List<HexTileColor>(colorsBottomToTop);
        }

        public HexTileColor TopColor
        {
            get
            {
                if (IsEmpty)
                    throw new InvalidOperationException("Empty stack has no top color.");

                return _tiles[_tiles.Count - 1];
            }
        }

        public int CountTopGroup()
        {
            if (IsEmpty)
                return 0;

            return CountTopGroup(TopColor);
        }

        public int CountTopGroup(HexTileColor color)
        {
            var count = 0;
            for (var i = _tiles.Count - 1; i >= 0; i--)
            {
                if (_tiles[i] != color)
                    break;
                count++;
            }

            return count;
        }

        public IReadOnlyList<HexTileColor> ReadTopGroup()
        {
            var count = CountTopGroup();
            var group = new HexTileColor[count];
            for (var i = 0; i < count; i++)
                group[i] = _tiles[_tiles.Count - count + i];

            return group;
        }

        public IReadOnlyList<HexTileColor> ExtractTopGroup()
        {
            return ExtractTopTiles(CountTopGroup());
        }

        public IReadOnlyList<HexTileColor> ExtractTopTiles(int count)
        {
            if (count < 0 || count > _tiles.Count)
                throw new ArgumentOutOfRangeException(nameof(count));

            var result = new HexTileColor[count];
            var start = _tiles.Count - count;
            for (var i = 0; i < count; i++)
                result[i] = _tiles[start + i];

            _tiles.RemoveRange(start, count);
            return result;
        }

        public void AddGroupOnTop(IReadOnlyList<HexTileColor> colorsBottomToTop)
        {
            if (colorsBottomToTop == null)
                throw new ArgumentNullException(nameof(colorsBottomToTop));

            for (var i = 0; i < colorsBottomToTop.Count; i++)
                _tiles.Add(colorsBottomToTop[i]);
        }

        public bool CanClearTopGroup(int clearThreshold) => CountTopGroup() >= clearThreshold;

        public IReadOnlyList<HexTileColor> SnapshotBottomToTop() => _tiles.ToArray();
    }
}
