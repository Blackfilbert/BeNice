using System;
using BeNice.HexaSort.Views;
using System.Collections.Generic;
using UnityEngine;

namespace BeNice.HexaSort.Configs
{
    [CreateAssetMenu(menuName = "BeNice/Hexa Sort/Gameplay Config")]
    public sealed class HexGameplayConfig : ScriptableObject
    {
        [Header("Grid")]
        [SerializeField] private float _cellSize = 0.65f;
        [SerializeField] private float _cellSpacing = 0.08f;
        [SerializeField] private float _tileHeight = 0.14f;
        [SerializeField] private float _tileVerticalStep = 0.12f;
        [SerializeField] private int _clearThreshold = 10;
        [SerializeField] private int _maxReactionSteps = 128;
        [SerializeField] private float _placementRadius = 0.75f;

        [Header("Drag")]
        [SerializeField] private float _dragHeight = 0.75f;
        [SerializeField] private float _dragScale = 1.08f;
        [SerializeField] private Vector3 _bottomStackScale = Vector3.one;

        [Header("Animation")]
        [SerializeField] private float _animationDurationMultiplier = 3f;
        [SerializeField] private float _returnDuration = 0.18f;
        [SerializeField] private float _placeDuration = 0.12f;
        [SerializeField] private float _mergeTileDuration = 0.06f;
        [SerializeField] private float _mergeTileStagger = 0.025f;
        [SerializeField] private float _mergeArcHeight = 0.35f;
        [SerializeField] private float _clearDuration = 0.12f;
        [SerializeField] private float _reactionAcceleration = 1.3f;
        [SerializeField] private float _maxReactionSpeedMultiplier = 4f;

        [Header("Tutorial")]
        [SerializeField] private float _tutorialMoveDuration = 0.8f;
        [SerializeField] private float _tutorialTargetPause = 0.35f;
        [SerializeField] private float _tutorialReturnDuration = 0.18f;
        [SerializeField] private float _tutorialRetryDelay = 2f;

        [Header("Prefabs")]
        [SerializeField] private HexCellView _cellPrefab;
        [SerializeField] private HexTileView _tilePrefab;
        [SerializeField] private HexStackView _stackPrefab;
        [SerializeField] private HexTileColorMaterial[] _materials;

        public float CellSize => _cellSize;
        public float CellSpacing => _cellSpacing;
        public float TileHeight => _tileHeight;
        public float TileVerticalStep => _tileVerticalStep;
        public int ClearThreshold => Mathf.Max(1, _clearThreshold);
        public int TilesPerColorSelection => Mathf.Max(1, ClearThreshold / 3);
        public int MaxReactionSteps => Mathf.Max(1, _maxReactionSteps);
        public float PlacementRadius => Mathf.Max(0.01f, _placementRadius);
        public float DragHeight => _dragHeight;
        public float DragScale => _dragScale;
        public Vector3 BottomStackScale => _bottomStackScale;
        public float AnimationDurationMultiplier => Mathf.Max(0.01f, _animationDurationMultiplier);
        public float ReturnDuration => Mathf.Max(0.01f, _returnDuration) * AnimationDurationMultiplier;
        public float PlaceDuration => Mathf.Max(0.01f, _placeDuration) * AnimationDurationMultiplier;
        public float MergeTileDuration => Mathf.Max(0.01f, _mergeTileDuration) * AnimationDurationMultiplier;
        public float MergeTileStagger => Mathf.Max(0f, _mergeTileStagger) * AnimationDurationMultiplier;
        public float MergeArcHeight => Mathf.Max(0f, _mergeArcHeight);
        public float ClearDuration => Mathf.Max(0.01f, _clearDuration) * AnimationDurationMultiplier;
        public float ReactionAcceleration => Mathf.Max(1f, _reactionAcceleration);
        public float MaxReactionSpeedMultiplier => Mathf.Max(1f, _maxReactionSpeedMultiplier);
        public float TutorialMoveDuration => Mathf.Max(0.01f, _tutorialMoveDuration);
        public float TutorialTargetPause => Mathf.Max(0f, _tutorialTargetPause);
        public float TutorialReturnDuration => Mathf.Max(0.01f, _tutorialReturnDuration);
        public float TutorialRetryDelay => Mathf.Max(0f, _tutorialRetryDelay);
        public HexCellView CellPrefab => _cellPrefab;
        public HexTileView TilePrefab => _tilePrefab;
        public HexStackView StackPrefab => _stackPrefab;
        public IReadOnlyList<HexTileColorMaterial> Materials => _materials;

        public bool TryGetMaterial(HexTileColor color, out Material material)
        {
            if (_materials != null)
            {
                for (var i = 0; i < _materials.Length; i++)
                {
                    if (_materials[i].Color == color)
                    {
                        material = _materials[i].Material;
                        return material != null;
                    }
                }
            }

            material = null;
            return false;
        }

        public void ValidateOrThrow()
        {
            if (_tilePrefab == null)
                throw new InvalidOperationException("HexGameplayConfig requires a tile prefab.");
            if (_stackPrefab == null)
                throw new InvalidOperationException("HexGameplayConfig requires a stack prefab.");
            if (_materials == null || _materials.Length == 0)
                throw new InvalidOperationException("HexGameplayConfig requires color materials.");

            var configuredColors = new HashSet<HexTileColor>();
            for (var i = 0; i < _materials.Length; i++)
            {
                if (_materials[i].Material == null)
                    throw new InvalidOperationException("HexGameplayConfig has an empty color material reference.");
                if (!configuredColors.Add(_materials[i].Color))
                    throw new InvalidOperationException($"HexGameplayConfig contains duplicate material for {_materials[i].Color}.");
            }

            foreach (HexTileColor color in Enum.GetValues(typeof(HexTileColor)))
            {
                if (!configuredColors.Contains(color))
                    throw new InvalidOperationException($"HexGameplayConfig requires a material for {color}.");
            }
        }
    }
}
