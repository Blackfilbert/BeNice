using System;
using BeNice.HexaSort.Configs;
using UnityEngine;

namespace BeNice.HexaSort.Views
{
    public sealed class HexTileView : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;

        private Quaternion _defaultLocalRotation;
        private Vector3 _defaultLocalScale;

        public HexTileColor Color { get; private set; }

        private void Awake()
        {
            _defaultLocalRotation = transform.localRotation;
            _defaultLocalScale = transform.localScale;
        }

        public void SetColor(HexTileColor color, HexGameplayConfig config)
        {
            Color = color;
            if (_renderer == null)
                return;

            _renderer.sharedMaterial = null;
            if (!config.TryGetMaterial(color, out var material))
                throw new InvalidOperationException($"No material configured for hex tile color {color}.");

            _renderer.sharedMaterial = material;
        }

        public void ResetVisual()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = _defaultLocalRotation;
            transform.localScale = _defaultLocalScale;
        }
    }
}
