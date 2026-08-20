using System;
using System.Collections;
using BeNice.HexaSort.Services;
using BeNice.HexaSort.Views;
using UnityEngine;
#if !LUNA_PLAYABLE
using VContainer.Unity;
#endif

namespace BeNice.HexaSort
{
#if LUNA_PLAYABLE
    public sealed class HexGameplayTimerController : IDisposable
#else
    public sealed class HexGameplayTimerController : IStartable, IDisposable
#endif
    {
        private readonly HexGameplayController _gameplayController;
        private readonly HexGameplayTimerView _view;
        private readonly IHexCoroutineRunner _runner;

        private Coroutine _routine;

        public HexGameplayTimerController(
            HexGameplayController gameplayController,
            HexGameplayTimerView view,
            IHexCoroutineRunner runner)
        {
            _gameplayController = gameplayController;
            _view = view;
            _runner = runner;
        }

        public void Start()
        {
            _view.ValidateOrThrow();
            _view.Initialize();
            _routine = _runner.Run(RunTimer());
        }

        public void Dispose()
        {
            if (_routine != null)
                _runner.Stop(_routine);
        }

        private IEnumerator RunTimer()
        {
            var elapsed = 0f;
            var duration = _view.DurationSeconds;
            while (!_gameplayController.IsCompleted && elapsed < duration)
            {
                var deltaTime = Time.unscaledDeltaTime;
                elapsed = Mathf.Min(duration, elapsed + deltaTime);
                _view.Advance(deltaTime, 1f - elapsed / duration);
                yield return null;
            }

            _routine = null;
            if (!_gameplayController.IsCompleted)
                _gameplayController.CompleteGameplay();
        }
    }
}
