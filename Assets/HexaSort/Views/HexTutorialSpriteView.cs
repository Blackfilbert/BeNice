using System;
using UnityEngine;
using UnityEngine.UI;

namespace BeNice.HexaSort.Views
{
    public sealed class HexTutorialSpriteView : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Sprite _defaultSprite;
        [SerializeField] private Sprite _activeSprite;

        public void ValidateOrThrow()
        {
            if (_image == null)
                throw new InvalidOperationException("HexTutorialSpriteView requires an Image.");
            if (_defaultSprite == null)
                throw new InvalidOperationException("HexTutorialSpriteView requires a default sprite.");
            if (_activeSprite == null)
                throw new InvalidOperationException("HexTutorialSpriteView requires an active sprite.");
        }

        public void ShowDefault()
        {
            _image.sprite = _defaultSprite;
        }

        public void ShowActive()
        {
            _image.sprite = _activeSprite;
        }
    }
}
