using BeNice.HexaSort.Configs;
using BeNice.HexaSort.Models;
using BeNice.HexaSort.Services;
using BeNice.HexaSort.Views;
using System.Collections.Generic;
using UnityEngine;
#if !LUNA_PLAYABLE
using VContainer;
using VContainer.Unity;
#endif

namespace BeNice.HexaSort.Scopes
{
#if LUNA_PLAYABLE
    public sealed class GameplayLifetimeScope : MonoBehaviour
#else
    public sealed class GameplayLifetimeScope : LifetimeScope
#endif
    {
#if LUNA_PLAYABLE
        [SerializeField] private VContainer.Unity.ParentReference parentReference;
        [SerializeField] private bool autoRun;
        [SerializeField] private List<GameObject> autoInjectGameObjects;
#endif

        [Header("Configs")]
        [SerializeField] private HexGameplayConfig _gameplayConfig;
        [SerializeField] private HexLevelConfig _levelConfig;

        [Header("Scene Views")]
        [SerializeField] private HexBoardView _boardView;
        [SerializeField] private BottomStacksView _bottomStacksView;
        [SerializeField] private HexGameplayRunner _runner;
        [SerializeField] private HexTutorialView _tutorialView;
        [SerializeField] private HexGameplayTimerView _timerView;
        [SerializeField] private HexPackshotView _packshotView;

        [Header("Scene References")]
        [SerializeField] private Transform _tilePoolRoot;
        [SerializeField] private Camera _gameplayCamera;

#if LUNA_PLAYABLE
        private HexGameplayController _gameplayController;
        private HexTutorialController _tutorialController;
        private HexGameplayTimerController _timerController;
        private bool _isInitialized;

        private void Awake()
        {
            InitializeForLuna();
        }
#else
        protected override void Awake()
        {
            base.Awake();
        }
#endif

        public void InitializeForLuna()
        {
#if LUNA_PLAYABLE
            if (_isInitialized)
                return;

            _isInitialized = true;

            var board = new HexBoardModel();
            var placementService = new HexPlacementService(board);
            var reactionService = new HexReactionService(board, _gameplayConfig);
            var tilePool = new HexTilePool(_gameplayConfig, _tilePoolRoot);
            var stackFactory = new HexStackFactory(_gameplayConfig, tilePool);

            _bottomStacksView.Construct(_gameplayConfig);
            _gameplayController = new HexGameplayController(
                _gameplayConfig,
                _levelConfig,
                board,
                placementService,
                reactionService,
                stackFactory,
                _boardView,
                _bottomStacksView,
                _packshotView,
                _runner,
                _gameplayCamera);
            _gameplayController.Initialize();

            if (_tutorialView != null)
            {
                _tutorialController = new HexTutorialController(
                    _gameplayConfig,
                    _levelConfig,
                    _gameplayController,
                    _boardView,
                    _bottomStacksView,
                    _tutorialView,
                    _runner,
                    _gameplayCamera);
                _tutorialController.Start();
            }

            if (_timerView != null)
            {
                _timerController = new HexGameplayTimerController(
                    _gameplayController,
                    _timerView,
                    _runner);
                _timerController.Start();
            }
#endif
        }

#if LUNA_PLAYABLE
        private void OnDestroy()
        {
            _timerController?.Dispose();
            _tutorialController?.Dispose();
            _gameplayController?.Dispose();
        }
#else
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_gameplayConfig).AsSelf();
            builder.RegisterInstance(_levelConfig).AsSelf();
            builder.RegisterInstance(_gameplayCamera).AsSelf();

            builder.RegisterComponent(_boardView).AsSelf();
            builder.RegisterComponent(_bottomStacksView).AsSelf();
            builder.RegisterComponent(_runner).As<IHexCoroutineRunner>();
            builder.RegisterComponent(_packshotView).AsSelf();

            builder.Register<HexBoardModel>(Lifetime.Scoped).AsSelf();
            builder.Register<HexPlacementService>(Lifetime.Scoped).AsSelf();
            builder.Register<HexReactionService>(Lifetime.Scoped).AsSelf();
            builder.Register<HexTilePool>(Lifetime.Scoped).WithParameter(_tilePoolRoot).AsSelf();
            builder.Register<HexStackFactory>(Lifetime.Scoped).AsSelf();
            builder.Register<HexGameplayController>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<HexGameplayEntryPoint>(Lifetime.Scoped);

            if (_tutorialView != null)
            {
                builder.RegisterComponent(_tutorialView).AsSelf();
                builder.RegisterEntryPoint<HexTutorialController>(Lifetime.Scoped);
            }

            if (_timerView != null)
            {
                builder.RegisterComponent(_timerView).AsSelf();
                builder.RegisterEntryPoint<HexGameplayTimerController>(Lifetime.Scoped);
            }
        }
#endif
    }
}
