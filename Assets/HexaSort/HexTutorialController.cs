using System;
using System.Collections;
using BeNice.HexaSort.Configs;
using BeNice.HexaSort.Services;
using BeNice.HexaSort.Views;
using UnityEngine;
#if !LUNA_PLAYABLE
using VContainer.Unity;
#endif

namespace BeNice.HexaSort
{
#if LUNA_PLAYABLE
    public sealed class HexTutorialController : IDisposable
#else
    public sealed class HexTutorialController : IStartable, IDisposable
#endif
    {
        private readonly HexGameplayConfig _gameplayConfig;
        private readonly HexLevelConfig _levelConfig;
        private readonly HexGameplayController _gameplayController;
        private readonly HexBoardView _boardView;
        private readonly BottomStacksView _bottomStacksView;
        private readonly HexTutorialView _tutorialView;
        private readonly IHexCoroutineRunner _runner;
        private readonly Camera _gameplayCamera;

        private Coroutine _routine;
        private bool _completed;

        public HexTutorialController(
            HexGameplayConfig gameplayConfig,
            HexLevelConfig levelConfig,
            HexGameplayController gameplayController,
            HexBoardView boardView,
            BottomStacksView bottomStacksView,
            HexTutorialView tutorialView,
            IHexCoroutineRunner runner,
            Camera gameplayCamera)
        {
            _gameplayConfig = gameplayConfig;
            _levelConfig = levelConfig;
            _gameplayController = gameplayController;
            _boardView = boardView;
            _bottomStacksView = bottomStacksView;
            _tutorialView = tutorialView;
            _runner = runner;
            _gameplayCamera = gameplayCamera;
        }

        public void Start()
        {
            _tutorialView.ValidateOrThrow();
            _tutorialView.SetVisible(false);
            _gameplayController.DragStarted += OnDragStarted;
            _gameplayController.PlacementCompleted += OnPlacementCompleted;
            _gameplayController.GameplayCompleted += OnGameplayCompleted;
            _bottomStacksView.LayoutChanged += OnLayoutChanged;
            StartRoutine(ShowAfterDelay(0f, true));
        }

        public void Dispose()
        {
            _gameplayController.DragStarted -= OnDragStarted;
            _gameplayController.PlacementCompleted -= OnPlacementCompleted;
            _gameplayController.GameplayCompleted -= OnGameplayCompleted;
            _bottomStacksView.LayoutChanged -= OnLayoutChanged;
            StopRoutine();
            _tutorialView.SetVisible(false);
        }

        private void OnDragStarted()
        {
            if (_completed)
                return;

            StopRoutine();
            _tutorialView.SetVisible(false);
            StartRoutine(ShowAfterDelay(_gameplayConfig.TutorialRetryDelay, false));
        }

        private void OnPlacementCompleted(Models.HexCoordinates coordinates)
        {
            _completed = true;
            StopRoutine();
            _tutorialView.SetVisible(false);
        }

        private void OnGameplayCompleted()
        {
            _completed = true;
            StopRoutine();
            _tutorialView.SetVisible(false);
        }

        private void OnLayoutChanged()
        {
            if (_completed)
                return;

            StopRoutine();
            _tutorialView.SetVisible(false);
            StartRoutine(ShowAfterDelay(0f, true));
        }

        private IEnumerator ShowAfterDelay(float delay, bool waitOneFrame)
        {
            if (waitOneFrame)
                yield return null;

            while (!_completed && _gameplayController.State != HexGameplayState.WaitingForInput)
                yield return null;

            var elapsed = 0f;
            while (!_completed && elapsed < delay)
            {
                if (_gameplayController.State != HexGameplayState.WaitingForInput)
                    elapsed = 0f;
                else
                    elapsed += Time.unscaledDeltaTime;

                yield return null;
            }

            if (!_completed)
                yield return PlayLoop();
        }

        private IEnumerator PlayLoop()
        {
            var sourceWorld = _bottomStacksView.GetSlotWorldPosition(_levelConfig.TutorialSourceSlot);
            var targetWorld = _boardView.GetWorldPosition(_levelConfig.TutorialTargetCoordinate);
            var sourceScreen = (Vector2)_gameplayCamera.WorldToScreenPoint(sourceWorld);
            var targetScreen = (Vector2)_gameplayCamera.WorldToScreenPoint(targetWorld);

            _tutorialView.ShowDefaultSprite();
            _tutorialView.SetVisible(true);
            while (!_completed)
            {
                _tutorialView.ShowActiveSprite();
                yield return Move(sourceScreen, targetScreen, _gameplayConfig.TutorialMoveDuration);
                _tutorialView.ShowDefaultSprite();
                yield return Wait(_gameplayConfig.TutorialTargetPause);
                yield return Move(targetScreen, sourceScreen, _gameplayConfig.TutorialReturnDuration);
            }
        }

        private IEnumerator Move(Vector2 start, Vector2 end, float duration)
        {
            var elapsed = 0f;
            while (!_completed && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                _tutorialView.SetScreenPosition(Vector2.Lerp(start, end, t));
                yield return null;
            }

            if (!_completed)
                _tutorialView.SetScreenPosition(end);
        }

        private IEnumerator Wait(float duration)
        {
            var elapsed = 0f;
            while (!_completed && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void StartRoutine(IEnumerator routine)
        {
            _routine = _runner.Run(routine);
        }

        private void StopRoutine()
        {
            if (_routine == null)
                return;

            _runner.Stop(_routine);
            _routine = null;
        }
    }
}
