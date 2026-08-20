using System.Collections.Generic;
using BeNice.HexaSort.Configs;
using BeNice.HexaSort.Views;
using UnityEngine;

namespace BeNice.HexaSort.Services
{
    public sealed class HexTilePool
    {
        private readonly HexGameplayConfig _config;
        private readonly Transform _root;
        private readonly Stack<HexTileView> _pool = new Stack<HexTileView>();

        public HexTilePool(HexGameplayConfig config, Transform root)
        {
            _config = config;
            _root = root;
        }

        public HexTileView Get(HexTileColor color, Transform parent)
        {
            var tile = _pool.Count > 0 ? _pool.Pop() : Object.Instantiate(_config.TilePrefab);
            tile.transform.SetParent(parent, false);
            tile.ResetVisual();
            tile.SetColor(color, _config);
            tile.gameObject.SetActive(true);
            return tile;
        }

        public void Release(HexTileView tile)
        {
            if (tile == null)
                return;

            tile.ResetVisual();
            tile.gameObject.SetActive(false);
            tile.transform.SetParent(_root, false);
            _pool.Push(tile);
        }
    }
}
