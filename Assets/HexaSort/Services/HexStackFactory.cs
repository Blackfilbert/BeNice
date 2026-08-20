using System.Collections.Generic;
using BeNice.HexaSort.Configs;
using BeNice.HexaSort.Models;
using BeNice.HexaSort.Views;
using UnityEngine;

namespace BeNice.HexaSort.Services
{
    public sealed class HexStackFactory
    {
        private readonly HexGameplayConfig _config;
        private readonly HexTilePool _tilePool;

        public HexStackFactory(HexGameplayConfig config, HexTilePool tilePool)
        {
            _config = config;
            _tilePool = tilePool;
        }

        public HexStackModel CreateModel(IReadOnlyList<HexTileColor> colorsBottomToTop)
        {
            var tilesPerSelection = _config.TilesPerColorSelection;
            var expandedColors = new List<HexTileColor>(colorsBottomToTop.Count * tilesPerSelection);
            for (var colorIndex = 0; colorIndex < colorsBottomToTop.Count; colorIndex++)
            {
                for (var tileIndex = 0; tileIndex < tilesPerSelection; tileIndex++)
                    expandedColors.Add(colorsBottomToTop[colorIndex]);
            }

            return new HexStackModel(expandedColors);
        }

        public HexStackView CreateView(HexStackModel model, Transform parent, bool draggable)
        {
            var view = Object.Instantiate(_config.StackPrefab, parent);
            view.Initialize(model, _config, _tilePool, draggable);
            return view;
        }
    }
}
