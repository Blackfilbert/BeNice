using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using BeNice.HexaSort.Configs;
using BeNice.HexaSort.Models;

namespace BeNice.HexaSort.Services
{
    public sealed class HexReactionService
    {
        private readonly HexBoardModel _board;
        private readonly HexGameplayConfig _config;
        private bool _isRunning;

        public bool IsRunning => _isRunning;

        public HexReactionService(HexBoardModel board, HexGameplayConfig config)
        {
            _board = board;
            _config = config;
        }

        public IEnumerator Resolve(
            HexCoordinates activeCoordinates,
            IHexReactionAnimator animator,
            CancellationToken cancellationToken,
            Action<HexReactionOperation> operationApplied,
            Action<HexReactionResult> completed)
        {
            if (_isRunning)
                throw new InvalidOperationException("Reaction is already running.");

            _isRunning = true;
            var result = HexReactionResult.Completed;
            var speedMultiplier = 1f;
            var pendingCoordinates = new List<HexCoordinates> { activeCoordinates };
            var step = 0;

            try
            {
                while (pendingCoordinates.Count > 0 && step < _config.MaxReactionSteps)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result = HexReactionResult.Cancelled;
                        yield break;
                    }

                    var currentCoordinates = pendingCoordinates[0];
                    if (!TryBuildNextOperation(currentCoordinates, out var operation))
                    {
                        pendingCoordinates.RemoveAt(0);
                        continue;
                    }

                    ApplyOperation(operation);
                    pendingCoordinates[0] = operation.Target;
                    RemovePendingDuplicate(pendingCoordinates, operation.Target);
                    if (operation.Type == HexReactionOperationType.Merge &&
                        operation.Source != operation.Target &&
                        _board.TryGetCell(operation.Source, out var sourceCell) &&
                        sourceCell.HasStack)
                    {
                        AddPendingUnique(pendingCoordinates, operation.Source);
                    }

                    operationApplied?.Invoke(operation);
                    yield return animator.PlayOperation(operation, speedMultiplier);
                    step++;
                    speedMultiplier = Math.Min(_config.MaxReactionSpeedMultiplier, speedMultiplier * _config.ReactionAcceleration);
                }

                if (HasPendingOperation(pendingCoordinates))
                    result = HexReactionResult.StepLimitExceeded;
            }
            finally
            {
                _isRunning = false;
                completed?.Invoke(result);
            }
        }

        private bool HasPendingOperation(IReadOnlyList<HexCoordinates> pendingCoordinates)
        {
            for (var i = 0; i < pendingCoordinates.Count; i++)
            {
                if (TryBuildNextOperation(pendingCoordinates[i], out _))
                    return true;
            }

            return false;
        }

        private static void AddPendingUnique(List<HexCoordinates> pendingCoordinates, HexCoordinates coordinates)
        {
            if (!pendingCoordinates.Contains(coordinates))
                pendingCoordinates.Add(coordinates);
        }

        private static void RemovePendingDuplicate(List<HexCoordinates> pendingCoordinates, HexCoordinates coordinates)
        {
            for (var i = pendingCoordinates.Count - 1; i > 0; i--)
            {
                if (pendingCoordinates[i] == coordinates)
                    pendingCoordinates.RemoveAt(i);
            }
        }

        public bool TryBuildNextOperation(HexCoordinates activeCoordinates, out HexReactionOperation operation)
        {
            operation = default;
            if (!_board.TryGetCell(activeCoordinates, out var activeCell) || !activeCell.HasStack)
                return false;

            var activeStack = activeCell.Stack;
            if (activeStack.CanClearTopGroup(_config.ClearThreshold))
            {
                operation = HexReactionOperation.Clear(activeCoordinates, activeStack.TopColor, activeStack.CountTopGroup());
                return true;
            }

            var activeTopColor = activeStack.TopColor;
            for (var direction = 0; direction < HexCoordinates.Directions.Length; direction++)
            {
                if (!_board.TryGetNeighbor(activeCoordinates, direction, out var neighbor) || !neighbor.HasStack)
                    continue;

                var targetStack = neighbor.Stack;
                if (targetStack.TopColor != activeTopColor)
                    continue;

                if (activeStack.Count > targetStack.Count)
                {
                    operation = HexReactionOperation.Merge(
                        activeCoordinates,
                        neighbor.Coordinates,
                        activeTopColor,
                        activeStack.ReadTopGroup());
                }
                else
                {
                    operation = HexReactionOperation.Merge(
                        neighbor.Coordinates,
                        activeCoordinates,
                        activeTopColor,
                        targetStack.ReadTopGroup());
                }

                return true;
            }

            return false;
        }

        public void ApplyOperation(HexReactionOperation operation)
        {
            if (operation.Type == HexReactionOperationType.Merge)
            {
                var source = _board.GetCell(operation.Source);
                var target = _board.GetCell(operation.Target);
                var group = source.Stack.ExtractTopTiles(operation.Count);
                target.Stack.AddGroupOnTop(group);
                _board.RemoveEmptyStack(operation.Source);
                return;
            }

            var cell = _board.GetCell(operation.Target);
            cell.Stack.ExtractTopTiles(operation.Count);
            _board.RemoveEmptyStack(operation.Target);
        }
    }
}
