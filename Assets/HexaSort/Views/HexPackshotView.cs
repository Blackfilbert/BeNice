using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BeNice.HexaSort.Views
{
    public sealed class HexPackshotView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _installButton;
        [SerializeField] private float _fadeDuration = 0.5f;

        public event Action InstallRequested;

        public void ValidateOrThrow()
        {
            if (_canvasGroup == null)
                throw new InvalidOperationException("HexPackshotView requires a CanvasGroup.");
            if (_installButton == null)
                throw new InvalidOperationException("HexPackshotView requires a full-screen install Button.");
        }

        public void Initialize()
        {
            _installButton.onClick.AddListener(OnInstallClicked);
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        public void Dispose()
        {
            _installButton.onClick.RemoveListener(OnInstallClicked);
        }

        public IEnumerator Show()
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            var duration = Mathf.Max(0.01f, _fadeDuration);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _canvasGroup.alpha = 1f;
        }

        private void OnInstallClicked()
        {
            InstallRequested?.Invoke();
        }
    }
}
