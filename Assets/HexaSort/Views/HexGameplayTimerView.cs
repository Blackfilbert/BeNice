using System;
using UnityEngine;

namespace BeNice.HexaSort.Views
{
    public sealed class HexGameplayTimerView : MonoBehaviour
    {
        [SerializeField] private Transform _rotatingObject;
        [SerializeField] private RectTransform _fillRect;
        [SerializeField] private float _durationSeconds = 120f;
        [SerializeField] private float _rotationDegreesPerSecond = -20f;

        public float DurationSeconds => Mathf.Max(0.01f, _durationSeconds);

        public void ValidateOrThrow()
        {
            if (_rotatingObject == null)
                throw new InvalidOperationException("HexGameplayTimerView requires a rotating object.");
            if (_fillRect == null)
                throw new InvalidOperationException("HexGameplayTimerView requires a fill RectTransform.");
        }

        public void Initialize()
        {
            SetFill(1f);
        }

        public void Advance(float deltaTime, float remainingNormalized)
        {
            _rotatingObject.Rotate(0f, 0f, _rotationDegreesPerSecond * deltaTime, Space.Self);
            SetFill(remainingNormalized);
        }

        private void SetFill(float normalized)
        {
            _fillRect.anchorMin = Vector2.zero;
            _fillRect.anchorMax = new Vector2(Mathf.Clamp01(normalized), 1f);
            _fillRect.offsetMin = Vector2.zero;
            _fillRect.offsetMax = Vector2.zero;
        }
    }
}
