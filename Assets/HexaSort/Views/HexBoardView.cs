using System.Collections;
using System.Collections.Generic;
using BeNice.HexaSort.Configs;
using BeNice.HexaSort.Models;
using BeNice.HexaSort.Services;
using UnityEngine;

namespace BeNice.HexaSort.Views
{
    public sealed class HexBoardView : MonoBehaviour, IHexReactionAnimator
    {
        [SerializeField] private Transform _cellsRoot;
        [SerializeField] private Transform _stacksRoot;
        [SerializeField] private HexCellView[] _sceneCells;

        private readonly Dictionary<HexCoordinates, HexCellView> _cells = new Dictionary<HexCoordinates, HexCellView>();
        private readonly Dictionary<HexCoordinates, HexStackView> _stacks = new Dictionary<HexCoordinates, HexStackView>();
        private HexGameplayConfig _config;
        private HexStackFactory _stackFactory;
        private HexCoordinates? _highlighted;

        public Transform BoardPlane => transform;
        public bool HasSceneCells => _sceneCells != null && _sceneCells.Length > 0;

        public void Initialize(HexGameplayConfig config, HexStackFactory stackFactory)
        {
            _config = config;
            _stackFactory = stackFactory;
            if (_cellsRoot == null)
                _cellsRoot = transform;
            if (_stacksRoot == null)
                _stacksRoot = transform;
        }

        public void BuildBoard(HexBoardModel board)
        {
            _cells.Clear();
            if (HasSceneCells)
            {
                for (var i = 0; i < _sceneCells.Length; i++)
                {
                    if (_sceneCells[i] == null)
                        continue;

                    _sceneCells[i].InitializeFromSerialized();
                    _cells.Add(_sceneCells[i].Coordinates, _sceneCells[i]);
                }

                foreach (var cell in board.Cells)
                {
                    if (cell.HasStack)
                        AttachStack(cell.Coordinates, _stackFactory.CreateView(cell.Stack, _stacksRoot, false));
                }

                return;
            }

            foreach (var cell in board.Cells)
            {
                var view = Instantiate(_config.CellPrefab, _cellsRoot);
                view.Initialize(cell.Coordinates);
                view.transform.localPosition = cell.Coordinates.ToLocalPosition(_config.CellSize, _config.CellSpacing);
                _cells.Add(cell.Coordinates, view);

                if (cell.HasStack)
                    AttachStack(cell.Coordinates, _stackFactory.CreateView(cell.Stack, _stacksRoot, false));
            }
        }

        public void AttachStack(HexCoordinates coordinates, HexStackView stackView)
        {
            stackView.transform.SetParent(_stacksRoot, true);
            stackView.transform.localPosition = GetLocalPosition(coordinates);
            stackView.transform.localScale = Vector3.one;
            _stacks[coordinates] = stackView;
        }

        public Vector3 GetWorldPosition(HexCoordinates coordinates) =>
            transform.TransformPoint(GetLocalPosition(coordinates));

        public Vector3 GetLocalPosition(HexCoordinates coordinates)
        {
            if (_cells.TryGetValue(coordinates, out var cell))
                return transform.InverseTransformPoint(cell.transform.position);

            return coordinates.ToLocalPosition(_config.CellSize, _config.CellSpacing);
        }

        public IReadOnlyList<HexCoordinates> BuildSceneCoordinates()
        {
            var result = new List<HexCoordinates>();
            if (!HasSceneCells)
                return result;

            for (var i = 0; i < _sceneCells.Length; i++)
            {
                if (_sceneCells[i] == null)
                    continue;

                result.Add(_sceneCells[i].ConfiguredCoordinates);
            }

            return result;
        }

        public bool TryGetNearestFreeCell(Vector3 worldPosition, HexBoardModel board, out HexCoordinates coordinates)
        {
            coordinates = default;
            var local = transform.InverseTransformPoint(worldPosition);
            var bestDistance = float.MaxValue;
            var found = false;

            foreach (var cell in board.Cells)
            {
                if (cell.HasStack)
                    continue;

                var distance = Vector3.Distance(local, GetLocalPosition(cell.Coordinates));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    coordinates = cell.Coordinates;
                    found = true;
                }
            }

            return found && bestDistance <= _config.PlacementRadius;
        }

        public void Highlight(HexCoordinates? coordinates)
        {
            if (_highlighted.HasValue && _cells.TryGetValue(_highlighted.Value, out var previous))
                previous.SetHighlighted(false);

            _highlighted = coordinates;

            if (_highlighted.HasValue && _cells.TryGetValue(_highlighted.Value, out var next))
                next.SetHighlighted(true);
        }

        public IEnumerator MoveStackToCell(HexStackView stack, HexCoordinates coordinates)
        {
            yield return MoveTransform(stack.transform, stack.transform.position, GetWorldPosition(coordinates), _config.PlaceDuration);
            AttachStack(coordinates, stack);
        }

        public IEnumerator PlayOperation(HexReactionOperation operation, float speedMultiplier)
        {
            if (operation.Type == HexReactionOperationType.Merge)
                yield return PlayMerge(operation, speedMultiplier);
            else
                yield return PlayClear(operation, speedMultiplier);
        }

        private IEnumerator PlayMerge(HexReactionOperation operation, float speedMultiplier)
        {
            if (!_stacks.TryGetValue(operation.Source, out var source) || !_stacks.TryGetValue(operation.Target, out var target))
                yield break;

            var movedTiles = source.RemoveTopTiles(operation.Count);
            var targetPosition = target.transform.position;
            yield return MoveTilesAsStream(
                movedTiles,
                targetPosition,
                target.Model.Count - operation.Count,
                transform.up,
                _config.TileVerticalStep,
                _config.MergeArcHeight,
                _config.MergeTileDuration / speedMultiplier,
                _config.MergeTileStagger / speedMultiplier);

            target.AttachMovedTiles(movedTiles);
            if (source.Model.IsEmpty)
            {
                _stacks.Remove(operation.Source);
                Destroy(source.gameObject);
            }
        }

        private IEnumerator PlayClear(HexReactionOperation operation, float speedMultiplier)
        {
            if (!_stacks.TryGetValue(operation.Target, out var stack))
                yield break;

            var removed = stack.RemoveTopForClear(operation.Count);
            var tileDuration = _config.ClearDuration / speedMultiplier;
            var tileStagger = _config.MergeTileStagger / speedMultiplier;
            var startScales = new Vector3[removed.Count];
            for (var streamIndex = 0; streamIndex < removed.Count; streamIndex++)
            {
                var tileIndex = removed.Count - 1 - streamIndex;
                startScales[streamIndex] = removed[tileIndex].transform.localScale;
            }

            var totalDuration = tileDuration + tileStagger * Mathf.Max(0, removed.Count - 1);
            var elapsed = 0f;
            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                for (var streamIndex = 0; streamIndex < removed.Count; streamIndex++)
                {
                    var localElapsed = elapsed - streamIndex * tileStagger;
                    if (localElapsed <= 0f)
                        continue;

                    var tileIndex = removed.Count - 1 - streamIndex;
                    var tile = removed[tileIndex];
                    tile.transform.localScale = Vector3.Lerp(
                        startScales[streamIndex],
                        Vector3.zero,
                        Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(localElapsed / tileDuration)));
                }

                yield return null;
            }

            for (var tileIndex = 0; tileIndex < removed.Count; tileIndex++)
                removed[tileIndex].transform.localScale = Vector3.zero;

            stack.ReleaseTiles(removed);
            if (stack.Model.IsEmpty)
            {
                _stacks.Remove(operation.Target);
                Destroy(stack.gameObject);
            }
        }

        private static IEnumerator MoveTransform(Transform target, Vector3 start, Vector3 end, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                target.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            target.position = end;
        }

        private static IEnumerator MoveTilesAsStream(
            IReadOnlyList<HexTileView> tiles,
            Vector3 targetPosition,
            int targetStartIndex,
            Vector3 arcDirection,
            float verticalStep,
            float arcHeight,
            float tileDuration,
            float stagger)
        {
            var count = tiles.Count;
            var starts = new Vector3[count];
            var ends = new Vector3[count];
            var rotations = new Quaternion[count];
            var flipAxes = new Vector3[count];
            for (var streamIndex = 0; streamIndex < count; streamIndex++)
            {
                var tileIndex = count - 1 - streamIndex;
                var tile = tiles[tileIndex].transform;
                starts[streamIndex] = tile.position;
                ends[streamIndex] = targetPosition + arcDirection * ((targetStartIndex + streamIndex) * verticalStep);
                rotations[streamIndex] = tile.rotation;
                var flightDirection = Vector3.ProjectOnPlane(ends[streamIndex] - starts[streamIndex], arcDirection).normalized;
                flipAxes[streamIndex] = Vector3.Cross(arcDirection, flightDirection).normalized;
            }

            var totalDuration = tileDuration + stagger * Mathf.Max(0, count - 1);
            var elapsed = 0f;
            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                for (var streamIndex = 0; streamIndex < count; streamIndex++)
                {
                    var localElapsed = elapsed - streamIndex * stagger;
                    if (localElapsed <= 0f)
                        continue;

                    var tileIndex = count - 1 - streamIndex;
                    var tile = tiles[tileIndex].transform;
                    var t = Mathf.Clamp01(localElapsed / tileDuration);
                    var easedT = Mathf.SmoothStep(0f, 1f, t);
                    var arcOffset = arcDirection * (Mathf.Sin(Mathf.PI * easedT) * arcHeight);
                    tile.position = Vector3.Lerp(starts[streamIndex], ends[streamIndex], easedT) + arcOffset;
                    tile.rotation = t < 1f
                        ? Quaternion.AngleAxis(180f * easedT, flipAxes[streamIndex]) * rotations[streamIndex]
                        : rotations[streamIndex];
                }

                yield return null;
            }

            for (var streamIndex = 0; streamIndex < count; streamIndex++)
            {
                var tileIndex = count - 1 - streamIndex;
                var tile = tiles[tileIndex].transform;
                tile.position = ends[streamIndex];
                tile.rotation = rotations[streamIndex];
            }
        }
    }
}
