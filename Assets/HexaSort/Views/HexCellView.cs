using BeNice.HexaSort.Configs;
using BeNice.HexaSort.Models;
using UnityEngine;

namespace BeNice.HexaSort.Views
{
    public sealed class HexCellView : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _highlightScale = 1.04f;
        [SerializeField] private SerializableHexCoordinates _coordinates;

        private Vector3 _baseScale;
        private bool _hasBaseScale;

        public HexCoordinates Coordinates { get; private set; }
        public HexCoordinates ConfiguredCoordinates => _coordinates.ToCoordinates();

        private void Awake()
        {
            CacheBaseScale();
        }

        public void Initialize(HexCoordinates coordinates)
        {
            Coordinates = coordinates;
            SetHighlighted(false);
        }

        public void InitializeFromSerialized()
        {
            Initialize(ConfiguredCoordinates);
        }

        public void SetHighlighted(bool highlighted)
        {
            CacheBaseScale();
            transform.localScale = highlighted ? _baseScale * _highlightScale : _baseScale;
        }

        private void CacheBaseScale()
        {
            if (_hasBaseScale)
                return;

            _baseScale = transform.localScale;
            _hasBaseScale = true;
        }
    }
}
