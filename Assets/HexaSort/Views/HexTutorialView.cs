using System;
using UnityEngine;

namespace BeNice.HexaSort.Views
{
    public sealed class HexTutorialView : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _canvasRoot;
        [SerializeField] private RectTransform _movingRoot;
        [SerializeField] private HexTutorialSpriteView _spriteView;

        public void ValidateOrThrow()
        {
            if (_canvas == null)
                throw new InvalidOperationException("HexTutorialView requires a Canvas.");
            if (_canvasRoot == null)
                throw new InvalidOperationException("HexTutorialView requires a canvas root RectTransform.");
            if (_movingRoot == null)
                throw new InvalidOperationException("HexTutorialView requires a moving root RectTransform.");

            if (_spriteView == null)
                throw new InvalidOperationException("HexTutorialView requires a tutorial sprite view.");

            _spriteView.ValidateOrThrow();
        }

        public void SetVisible(bool visible)
        {
            if (!visible && _spriteView != null)
                _spriteView.ShowDefault();

            if (_movingRoot != null)
                _movingRoot.gameObject.SetActive(visible);
        }

        public void ShowDefaultSprite()
        {
            _spriteView.ShowDefault();
        }

        public void ShowActiveSprite()
        {
            _spriteView.ShowActive();
        }

        public void SetScreenPosition(Vector2 screenPosition)
        {
            var eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _canvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRoot,
                    screenPosition,
                    eventCamera,
                    out var localPosition))
            {
                _movingRoot.anchoredPosition = localPosition;
            }
        }
    }
}
