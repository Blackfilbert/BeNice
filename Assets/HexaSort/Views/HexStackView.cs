using System.Collections.Generic;
using BeNice.HexaSort.Configs;
using BeNice.HexaSort.Models;
using BeNice.HexaSort.Services;
using UnityEngine;

namespace BeNice.HexaSort.Views
{
    public sealed class HexStackView : MonoBehaviour
    {
        [SerializeField] private Transform _tilesRoot;
        [SerializeField] private GameObject _shadow;

        private readonly List<HexTileView> _tiles = new List<HexTileView>();
        private HexGameplayConfig _config;
        private HexTilePool _tilePool;

        public HexStackModel Model { get; private set; }
        public bool IsDraggable { get; private set; }

        public void Initialize(HexStackModel model, HexGameplayConfig config, HexTilePool tilePool, bool draggable)
        {
            Model = model;
            _config = config;
            _tilePool = tilePool;
            IsDraggable = draggable;
            if (_tilesRoot == null)
                _tilesRoot = transform;

            RebuildFromModel();
            ShowShadow(false);
        }

        public void RebuildFromModel()
        {
            for (var i = _tiles.Count - 1; i >= 0; i--)
                _tilePool.Release(_tiles[i]);
            _tiles.Clear();

            var colors = Model.SnapshotBottomToTop();
            for (var i = 0; i < colors.Count; i++)
                AddTileVisual(colors[i]);
        }

        public IReadOnlyList<HexTileView> RemoveTopTiles(int count)
        {
            var removed = new HexTileView[count];
            for (var i = count - 1; i >= 0; i--)
            {
                var index = _tiles.Count - 1;
                removed[i] = _tiles[index];
                _tiles.RemoveAt(index);
            }

            RefreshTilePositions();
            return removed;
        }

        public void AttachMovedTiles(IReadOnlyList<HexTileView> movedTiles)
        {
            for (var i = 0; i < movedTiles.Count; i++)
            {
                movedTiles[i].transform.SetParent(_tilesRoot, true);
                _tiles.Add(movedTiles[i]);
            }

            RefreshTilePositions();
        }

        public IReadOnlyList<HexTileView> RemoveTopForClear(int count) => RemoveTopTiles(count);

        public void ReleaseTiles(IReadOnlyList<HexTileView> tiles)
        {
            for (var i = 0; i < tiles.Count; i++)
                _tilePool.Release(tiles[i]);
        }

        public void ShowShadow(bool visible)
        {
            if (_shadow != null)
                _shadow.SetActive(visible);
        }

        private void AddTileVisual(HexTileColor color)
        {
            var tile = _tilePool.Get(color, _tilesRoot);
            _tiles.Add(tile);
            tile.transform.localPosition = new Vector3(0f, _tiles.Count * _config.TileVerticalStep, 0f);
        }

        private void RefreshTilePositions()
        {
            for (var i = 0; i < _tiles.Count; i++)
                _tiles[i].transform.localPosition = new Vector3(0f, i * _config.TileVerticalStep, 0f);
        }
    }
}
