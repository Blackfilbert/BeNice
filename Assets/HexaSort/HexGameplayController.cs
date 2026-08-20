using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using BeNice.HexaSort.Configs;
using BeNice.HexaSort.Models;
using BeNice.HexaSort.Services;
using BeNice.HexaSort.Views;
using UnityEngine;

namespace BeNice.HexaSort
{
    public sealed class HexGameplayController : IDisposable
    {
        private readonly HexGameplayConfig _gameplayConfig;
        private readonly HexLevelConfig _levelConfig;
        private readonly HexBoardModel _board;
        private readonly HexPlacementService _placementService;
        private readonly HexReactionService _reactionService;
        private readonly HexStackFactory _stackFactory;
        private readonly HexBoardView _boardView;
        private readonly BottomStacksView _bottomStacksView;
        private readonly HexPackshotView _packshotView;
        private readonly IHexCoroutineRunner _runner;
        private readonly Camera _camera;
        private readonly CancellationTokenSource _destroyCts = new CancellationTokenSource();

        private HexGameplayState _state = HexGameplayState.Initializing;
        private HexStackView _draggedStack;
        private int _draggedSlot = -1;
        private HexCoordinates? _selectedCell;
        private int _nextBottomSetIndex;
        private Coroutine _activeRoutine;
        private Coroutine _packshotRoutine;
        private bool _isCompleted;

        public event Action DragStarted;
        public event Action<HexCoordinates> PlacementCompleted;
        public event Action ReactionStarted;
        public event Action<HexReactionOperation> MergeCompleted;
        public event Action<HexReactionOperation> ClearCompleted;
        public event Action ReactionCompleted;
        public event Action CurrentBottomSetConsumed;
        public event Action GameplayCompleted;

        public HexGameplayState State => _state;
        public bool IsCompleted => _isCompleted;

        public HexGameplayController(
            HexGameplayConfig gameplayConfig,
            HexLevelConfig levelConfig,
            HexBoardModel board,
            HexPlacementService placementService,
            HexReactionService reactionService,
            HexStackFactory stackFactory,
            HexBoardView boardView,
            BottomStacksView bottomStacksView,
            HexPackshotView packshotView,
            IHexCoroutineRunner runner,
            Camera camera)
        {
            _gameplayConfig = gameplayConfig;
            _levelConfig = levelConfig;
            _board = board;
            _placementService = placementService;
            _reactionService = reactionService;
            _stackFactory = stackFactory;
            _boardView = boardView;
            _bottomStacksView = bottomStacksView;
            _packshotView = packshotView;
            _runner = runner;
            _camera = camera;
        }

        public void Initialize()
        {
            _isCompleted = false;
            _state = HexGameplayState.Initializing;
            _packshotView.ValidateOrThrow();
            _packshotView.InstallRequested += OnInstallRequested;
            _packshotView.Initialize();
            _bottomStacksView.Bind(this);
            _gameplayConfig.ValidateOrThrow();
            _boardView.Initialize(_gameplayConfig, _stackFactory);
            _bottomStacksView.InitializeResponsiveLayout(_boardView.transform);
            var coordinates = _boardView.HasSceneCells
                ? _boardView.BuildSceneCoordinates()
                : _levelConfig.BuildCoordinates();

            if (!_boardView.HasSceneCells && _gameplayConfig.CellPrefab == null)
                throw new InvalidOperationException("HexGameplayConfig requires a cell prefab when scene cells are not assigned.");

            _levelConfig.ValidateOrThrow(coordinates);
            _board.Initialize(coordinates);
            if (_levelConfig.InitialStacks != null)
            {
                foreach (var initialStack in _levelConfig.InitialStacks)
                {
                    var model = _stackFactory.CreateModel(initialStack.ColorsBottomToTop);
                    if (!_board.TryPlaceStack(initialStack.Coordinates, model))
                        throw new InvalidOperationException($"Could not place initial stack at {initialStack.Coordinates}.");
                }
            }

            _boardView.BuildBoard(_board);
            ShowInitialBottomStacks();
            _state = HexGameplayState.WaitingForInput;
        }

        public void BeginDrag(int slot, HexStackView stack, Vector3 originWorldPosition, Vector2 pointerScreenPosition)
        {
            if (_state != HexGameplayState.WaitingForInput || _draggedStack != null || stack == null)
                return;

            _state = HexGameplayState.Dragging;
            _draggedSlot = slot;
            _draggedStack = stack;
            _draggedStack.transform.SetParent(_boardView.transform, true);
            _draggedStack.transform.localScale = Vector3.one * _gameplayConfig.DragScale;
            _draggedStack.ShowShadow(true);
            DragStarted?.Invoke();
            UpdateDrag(pointerScreenPosition);
        }

        public void UpdateDrag(Vector2 pointerScreenPosition)
        {
            if (_state != HexGameplayState.Dragging || _draggedStack == null)
                return;

            if (!TryGetPointerOnBoardPlane(pointerScreenPosition, out var world))
                return;

            world += _boardView.BoardPlane.up * _gameplayConfig.DragHeight;
            _draggedStack.transform.position = world;

            var boardPoint = world - _boardView.BoardPlane.up * _gameplayConfig.DragHeight;
            if (_boardView.TryGetNearestFreeCell(boardPoint, _board, out var coordinates) && _placementService.CanPlace(coordinates))
            {
                _selectedCell = coordinates;
                _boardView.Highlight(coordinates);
            }
            else
            {
                _selectedCell = null;
                _boardView.Highlight(null);
            }
        }

        public void EndDrag(Vector2 pointerScreenPosition)
        {
            if (_state != HexGameplayState.Dragging || _draggedStack == null)
                return;

            UpdateDrag(pointerScreenPosition);
            _boardView.Highlight(null);
            if (_selectedCell.HasValue)
                _activeRoutine = _runner.Run(PlaceAndResolve(_selectedCell.Value));
            else
                _activeRoutine = _runner.Run(ReturnDraggedStack());
        }

        public void Dispose()
        {
            _bottomStacksView.Unbind(this);
            _packshotView.InstallRequested -= OnInstallRequested;
            _packshotView.Dispose();
            _destroyCts.Cancel();
            _destroyCts.Dispose();
            if (_activeRoutine != null)
                _runner.Stop(_activeRoutine);
            if (_packshotRoutine != null)
                _runner.Stop(_packshotRoutine);
        }

        private IEnumerator PlaceAndResolve(HexCoordinates coordinates)
        {
            _state = HexGameplayState.Placing;
            var stack = _draggedStack;
            var slot = _draggedSlot;
            ClearDragState();

            yield return _boardView.MoveStackToCell(stack, coordinates);
            if (!_placementService.TryPlace(coordinates, stack.Model))
            {
                _draggedStack = stack;
                _draggedSlot = slot;
                yield return ReturnDraggedStack();
                yield break;
            }

            _bottomStacksView.ConsumeSlot(slot);
            PlacementCompleted?.Invoke(coordinates);
            if (_isCompleted)
                yield break;

            _state = HexGameplayState.Resolving;
            ReactionStarted?.Invoke();

            var reactionResult = HexReactionResult.Cancelled;
            yield return _reactionService.Resolve(
                coordinates,
                _boardView,
                _destroyCts.Token,
                OnReactionOperationApplied,
                result => reactionResult = result);

            if (_isCompleted)
                yield break;

            ReactionCompleted?.Invoke();
            if (reactionResult == HexReactionResult.StepLimitExceeded)
                Debug.LogError("Hex reaction step limit exceeded.");

            if (!_board.HasStacks)
            {
                CompleteGameplay();
                yield break;
            }

            _state = HexGameplayState.ShowingNextSet;
            if (!_bottomStacksView.HasStacks)
            {
                CurrentBottomSetConsumed?.Invoke();
                ShowNextBottomSet();
            }

            _state = HexGameplayState.WaitingForInput;
        }

        public void CompleteGameplay()
        {
            if (_isCompleted)
                return;

            _isCompleted = true;
            _destroyCts.Cancel();
            _boardView.Highlight(null);
            if (_draggedStack != null)
            {
                var stack = _draggedStack;
                var slot = _draggedSlot;
                ClearDragState();
                _bottomStacksView.RestoreSlot(slot, stack);
            }

            _state = HexGameplayState.Completing;
            GameplayCompleted?.Invoke();
            _packshotRoutine = _runner.Run(ShowPackshot());
        }

        private IEnumerator ShowPackshot()
        {
            yield return _packshotView.Show();
            _packshotRoutine = null;
            Luna.Unity.LifeCycle.GameEnded();
            _state = HexGameplayState.Packshot;
        }

        private void OnInstallRequested()
        {
            if (_isCompleted)
                Luna.Unity.Playable.InstallFullGame();
        }

        private IEnumerator ReturnDraggedStack()
        {
            _state = HexGameplayState.Returning;
            var stack = _draggedStack;
            var slot = _draggedSlot;
            ClearDragState();
            stack.ShowShadow(false);
            _bottomStacksView.RestoreSlot(slot, stack);
            _state = HexGameplayState.WaitingForInput;
            yield break;
        }

        private void ShowInitialBottomStacks()
        {
            _nextBottomSetIndex = 0;
            ShowNextBottomSet();
        }

        private void ShowNextBottomSet()
        {
            var configuredSet = _levelConfig.GetBottomSet(_nextBottomSetIndex);
            if (configuredSet != null)
                _nextBottomSetIndex++;

            var views = new HexStackView[_bottomStacksView.SlotCount];
            for (var slot = 0; slot < views.Length; slot++)
            {
                if (configuredSet != null)
                {
                    if (configuredSet.Stacks != null && slot < configuredSet.Stacks.Count)
                        views[slot] = CreateBottomStackView(configuredSet.Stacks[slot].ColorsBottomToTop);
                }
                else
                {
                    views[slot] = CreateRandomBottomStackView();
                }
            }

            _bottomStacksView.SetStacks(views);
        }

        private HexStackView CreateRandomBottomStackView()
        {
            var groupCount = UnityEngine.Random.Range(1, 4);
            var colors = new HexTileColor[groupCount];
            var availableColors = _gameplayConfig.Materials;
            for (var i = 0; i < colors.Length; i++)
            {
                var colorIndex = UnityEngine.Random.Range(0, availableColors.Count);
                var color = availableColors[colorIndex].Color;
                if (i > 0 && color == colors[i - 1] && availableColors.Count > 1)
                {
                    var offset = UnityEngine.Random.Range(1, availableColors.Count);
                    color = availableColors[(colorIndex + offset) % availableColors.Count].Color;
                }

                colors[i] = color;
            }

            return CreateBottomStackView(colors);
        }

        private HexStackView CreateBottomStackView(IReadOnlyList<HexTileColor> colorsBottomToTop)
        {
            var model = _stackFactory.CreateModel(colorsBottomToTop);
            return _stackFactory.CreateView(model, _bottomStacksView.transform, true);
        }

        private void OnReactionOperationApplied(HexReactionOperation operation)
        {
            if (operation.Type == HexReactionOperationType.Merge)
                MergeCompleted?.Invoke(operation);
            else
                ClearCompleted?.Invoke(operation);
        }

        private bool TryGetPointerOnBoardPlane(Vector2 screenPosition, out Vector3 world)
        {
            var ray = _camera.ScreenPointToRay(screenPosition);
            var plane = new Plane(_boardView.BoardPlane.up, _boardView.BoardPlane.position);
            if (plane.Raycast(ray, out var distance))
            {
                world = ray.GetPoint(distance);
                return true;
            }

            world = default;
            return false;
        }

        private void ClearDragState()
        {
            if (_draggedStack != null)
                _draggedStack.ShowShadow(false);
            _draggedStack = null;
            _draggedSlot = -1;
            _selectedCell = null;
        }

    }
}
