using System;
using System.Collections.Generic;
using BeNice.HexaSort.Models;
using UnityEngine;

namespace BeNice.HexaSort.Configs
{
    [CreateAssetMenu(menuName = "BeNice/Hexa Sort/Level Config")]
    public sealed class HexLevelConfig : ScriptableObject
    {
        [Serializable]
        public sealed class InitialStack
        {
            [SerializeField] private SerializableHexCoordinates _coordinates;
            [InspectorName("Colors Bottom To Top (Max 3)")]
            [SerializeField] private HexTileColor[] _colorsBottomToTop;

            public HexCoordinates Coordinates => _coordinates.ToCoordinates();
            public IReadOnlyList<HexTileColor> ColorsBottomToTop => _colorsBottomToTop;
        }

        [Serializable]
        public sealed class BottomStack
        {
            [InspectorName("Colors Bottom To Top (Max 3)")]
            [SerializeField] private HexTileColor[] _colorsBottomToTop;

            public IReadOnlyList<HexTileColor> ColorsBottomToTop => _colorsBottomToTop;
        }

        [Serializable]
        public sealed class BottomStackSet
        {
            [SerializeField] private BottomStack[] _stacks;

            public IReadOnlyList<BottomStack> Stacks => _stacks;
        }

        [SerializeField] private SerializableHexCoordinates[] _cells;
        [SerializeField] private InitialStack[] _initialStacks;
        [SerializeField] private BottomStackSet[] _bottomStackSets;
        [SerializeField] private int _tutorialSourceSlot;
        [SerializeField] private SerializableHexCoordinates _tutorialTargetCoordinate;
        [SerializeField] private bool _completeWhenBottomSetsConsumed = true;

        public int TutorialSourceSlot => _tutorialSourceSlot;
        public HexCoordinates TutorialTargetCoordinate => _tutorialTargetCoordinate.ToCoordinates();
        public bool CompleteWhenBottomSetsConsumed => _completeWhenBottomSetsConsumed;
        public int BottomSetCount => _bottomStackSets == null ? 0 : _bottomStackSets.Length;
        public int TotalBottomStackCount
        {
            get
            {
                if (_bottomStackSets == null)
                    return 0;

                var count = 0;
                for (var i = 0; i < _bottomStackSets.Length; i++)
                {
                    if (_bottomStackSets[i]?.Stacks != null)
                        count += _bottomStackSets[i].Stacks.Count;
                }

                return count;
            }
        }

        public IReadOnlyList<HexCoordinates> BuildCoordinates()
        {
            var result = new HexCoordinates[_cells == null ? 0 : _cells.Length];
            for (var i = 0; i < result.Length; i++)
                result[i] = _cells[i].ToCoordinates();

            return result;
        }

        public IReadOnlyList<InitialStack> InitialStacks => _initialStacks;

        public BottomStackSet GetBottomSet(int index)
        {
            if (_bottomStackSets == null || index < 0 || index >= _bottomStackSets.Length)
                return null;

            return _bottomStackSets[index];
        }

        public BottomStack GetBottomStack(int sequenceIndex)
        {
            if (_bottomStackSets == null || sequenceIndex < 0)
                return null;

            for (var setIndex = 0; setIndex < _bottomStackSets.Length; setIndex++)
            {
                var stacks = _bottomStackSets[setIndex]?.Stacks;
                if (stacks == null)
                    continue;
                if (sequenceIndex < stacks.Count)
                    return stacks[sequenceIndex];

                sequenceIndex -= stacks.Count;
            }

            return null;
        }

        public void ValidateOrThrow()
        {
            ValidateOrThrow(BuildCoordinates());
        }

        public void ValidateOrThrow(IReadOnlyList<HexCoordinates> cells)
        {
            var existingCells = new HashSet<HexCoordinates>();
            if (cells == null || cells.Count == 0)
                throw new InvalidOperationException("Hex level requires at least one cell.");

            for (var i = 0; i < cells.Count; i++)
            {
                var coordinate = cells[i];
                if (!existingCells.Add(coordinate))
                    throw new InvalidOperationException($"Duplicate cell coordinate {coordinate}.");
            }

            var occupied = new HashSet<HexCoordinates>();
            if (_initialStacks != null)
            {
                for (var i = 0; i < _initialStacks.Length; i++)
                {
                    var stack = _initialStacks[i];
                    if (stack == null)
                        throw new InvalidOperationException("HexLevelConfig contains an empty initial stack entry.");

                    var coordinate = stack.Coordinates;
                    if (!existingCells.Contains(coordinate))
                        throw new InvalidOperationException($"Initial stack coordinate {coordinate} has no cell.");
                    if (!occupied.Add(coordinate))
                        throw new InvalidOperationException($"Two initial stacks use coordinate {coordinate}.");
                    ValidateColors(stack.ColorsBottomToTop, "Initial stack");
                }
            }

            if (_bottomStackSets == null || _bottomStackSets.Length == 0)
                throw new InvalidOperationException("HexLevelConfig requires bottom stack sets.");

            for (var setIndex = 0; setIndex < _bottomStackSets.Length; setIndex++)
            {
                var set = _bottomStackSets[setIndex];
                if (set == null || set.Stacks == null || set.Stacks.Count == 0 || set.Stacks.Count > 3)
                    throw new InvalidOperationException($"Bottom set {setIndex} must contain one to three stacks.");

                for (var stackIndex = 0; stackIndex < set.Stacks.Count; stackIndex++)
                {
                    if (set.Stacks[stackIndex] == null)
                        throw new InvalidOperationException($"Bottom set {setIndex} contains an empty stack entry.");
                    ValidateColors(set.Stacks[stackIndex].ColorsBottomToTop, $"Bottom set {setIndex}");
                }
            }

            if (_tutorialSourceSlot < 0 || _tutorialSourceSlot > 2)
                throw new InvalidOperationException("Tutorial source slot must be between 0 and 2.");
            if (!existingCells.Contains(TutorialTargetCoordinate))
                throw new InvalidOperationException($"Tutorial target {TutorialTargetCoordinate} has no cell.");
            if (occupied.Contains(TutorialTargetCoordinate))
                throw new InvalidOperationException($"Tutorial target {TutorialTargetCoordinate} must be initially free.");
        }

        private static void ValidateColors(IReadOnlyList<HexTileColor> colors, string owner)
        {
            if (colors == null || colors.Count == 0)
                throw new InvalidOperationException($"{owner} has an empty color sequence.");
            if (colors.Count > 3)
                throw new InvalidOperationException($"{owner} can contain at most three colors.");
        }
    }
}
