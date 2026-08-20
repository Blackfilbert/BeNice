using System;
using System.Collections.Generic;

namespace BeNice.HexaSort.Models
{
    public sealed class HexBoardModel
    {
        private readonly Dictionary<HexCoordinates, HexCellModel> _cells = new Dictionary<HexCoordinates, HexCellModel>();

        public IEnumerable<HexCellModel> Cells => _cells.Values;
        public bool HasStacks
        {
            get
            {
                foreach (var cell in _cells.Values)
                {
                    if (cell.HasStack)
                        return true;
                }

                return false;
            }
        }

        public void Initialize(IReadOnlyList<HexCoordinates> coordinates)
        {
            if (coordinates == null)
                throw new ArgumentNullException(nameof(coordinates));

            _cells.Clear();
            for (var i = 0; i < coordinates.Count; i++)
            {
                if (_cells.ContainsKey(coordinates[i]))
                    throw new InvalidOperationException($"Duplicate cell coordinate {coordinates[i]}.");

                _cells.Add(coordinates[i], new HexCellModel(coordinates[i]));
            }
        }

        public bool HasCell(HexCoordinates coordinates) => _cells.ContainsKey(coordinates);

        public bool TryGetCell(HexCoordinates coordinates, out HexCellModel cell) => _cells.TryGetValue(coordinates, out cell);

        public HexCellModel GetCell(HexCoordinates coordinates)
        {
            if (!_cells.TryGetValue(coordinates, out var cell))
                throw new InvalidOperationException($"Cell {coordinates} does not exist.");

            return cell;
        }

        public bool IsCellFree(HexCoordinates coordinates) =>
            _cells.TryGetValue(coordinates, out var cell) && !cell.HasStack;

        public bool TryPlaceStack(HexCoordinates coordinates, HexStackModel stack)
        {
            if (stack == null || stack.IsEmpty || !IsCellFree(coordinates))
                return false;

            _cells[coordinates].SetStack(stack);
            return true;
        }

        public void RemoveEmptyStack(HexCoordinates coordinates)
        {
            var cell = GetCell(coordinates);
            if (cell.Stack != null && cell.Stack.IsEmpty)
                cell.RemoveStack();
        }

        public bool TryGetNeighbor(HexCoordinates origin, int directionIndex, out HexCellModel neighbor)
        {
            var coordinates = origin.GetNeighbor(directionIndex);
            return _cells.TryGetValue(coordinates, out neighbor);
        }
    }
}
