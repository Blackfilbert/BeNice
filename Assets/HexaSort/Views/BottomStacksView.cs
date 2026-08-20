using System;
using BeNice.HexaSort.Configs;
using BeNice.HexaSort.Models;
using UnityEngine;
#if !LUNA_PLAYABLE
using VContainer;
#endif

namespace BeNice.HexaSort.Views
{
    public sealed class BottomStacksView : MonoBehaviour
    {
        [SerializeField] private Transform[] _slots = new Transform[3];
        [SerializeField] private Camera _camera;
        [SerializeField] private float _screenPickRadius = 90f;

        [Header("Landscape Layout")]
        [SerializeField] private bool _useLandscapeLayout = true;
        [SerializeField, Min(0.1f)] private float _landscapeCameraSize = 5.8f;
        [SerializeField] private float _landscapeCameraY = 13.4f;
        [SerializeField] private Vector3 _landscapeBoardOffset = new Vector3(-2.4f, 0f, 0f);
        [SerializeField] private Vector3 _landscapeSlotsOffset = new Vector3(6.2f, 0f, 0f);
        [SerializeField, Min(0.1f)] private float _landscapeSlotSpacing = 2.2f;

        private HexGameplayController _controller;
        private HexGameplayConfig _config;
        private HexStackView[] _stacks;
        private int _activeSlot = -1;
        private Transform _boardTransform;
        private Vector3 _portraitBoardPosition;
        private Vector3 _portraitSlotsPosition;
        private Vector3[] _portraitSlotLocalPositions;
        private Vector3 _portraitCameraPosition;
        private float _portraitCameraSize;
        private bool _layoutInitialized;
        private bool _isLandscape;

        public event Action DragStarted;
        public event Action LayoutChanged;
        public int SlotCount => _slots == null ? 0 : _slots.Length;
        public bool HasStacks
        {
            get
            {
                if (_stacks == null)
                    return false;

                for (var i = 0; i < _stacks.Length; i++)
                {
                    if (_stacks[i] != null)
                        return true;
                }

                return false;
            }
        }

#if !LUNA_PLAYABLE
        [Inject]
#endif
        public void Construct(HexGameplayConfig config)
        {
            _config = config;
        }

        public void Bind(HexGameplayController controller)
        {
            _controller = controller;
        }

        public void InitializeResponsiveLayout(Transform boardTransform)
        {
            if (_layoutInitialized || boardTransform == null || _camera == null)
                return;

            _boardTransform = boardTransform;
            _portraitBoardPosition = _boardTransform.position;
            _portraitSlotsPosition = transform.position;
            _portraitCameraPosition = _camera.transform.position;
            _portraitCameraSize = _camera.orthographicSize;
            _portraitSlotLocalPositions = new Vector3[SlotCount];
            for (var i = 0; i < _portraitSlotLocalPositions.Length; i++)
            {
                if (_slots[i] != null)
                    _portraitSlotLocalPositions[i] = _slots[i].localPosition;
            }

            _layoutInitialized = true;
            ApplyResponsiveLayout(true);
        }

        public void Unbind(HexGameplayController controller)
        {
            if (_controller == controller)
                _controller = null;
        }

        public void SetStacks(HexStackView[] stacks)
        {
            Clear();
            _stacks = new HexStackView[SlotCount];
            for (var i = 0; i < stacks.Length && i < _stacks.Length; i++)
            {
                if (stacks[i] != null)
                    SetStack(i, stacks[i]);
            }
        }

        public void SetStack(int slot, HexStackView stack)
        {
            if (_stacks == null || slot < 0 || slot >= _stacks.Length || stack == null || _slots[slot] == null)
                return;

            if (_stacks[slot] != null && _stacks[slot] != stack)
                Destroy(_stacks[slot].gameObject);

            _stacks[slot] = stack;
            stack.transform.SetParent(_slots[slot], false);
            stack.transform.localPosition = Vector3.zero;
            stack.transform.localScale = _config.BottomStackScale;
        }

        public void Clear()
        {
            if (_stacks == null)
                return;

            for (var i = 0; i < _stacks.Length; i++)
            {
                if (_stacks[i] != null)
                    Destroy(_stacks[i].gameObject);
            }
        }

        public void ConsumeSlot(int slot)
        {
            if (_stacks == null || slot < 0 || slot >= _stacks.Length)
                return;

            _stacks[slot] = null;
        }

        public void RestoreSlot(int slot, HexStackView stack)
        {
            if (_stacks == null || slot < 0 || slot >= _stacks.Length || stack == null || _slots[slot] == null)
                return;

            _stacks[slot] = stack;
            SetStack(slot, stack);
        }

        private void Update()
        {
            ApplyResponsiveLayout(false);

            if (_camera == null || _controller == null || _stacks == null)
                return;

            if (TryGetPointerDown(out var downPosition) && TryPickSlot(downPosition, out _activeSlot))
            {
                DragStarted?.Invoke();
                _controller.BeginDrag(_activeSlot, _stacks[_activeSlot], GetSlotWorldPosition(_activeSlot), downPosition);
                return;
            }

            if (_activeSlot < 0)
                return;

            if (TryGetPointerPosition(out var position))
                _controller.UpdateDrag(position);

            if (TryGetPointerUp(out var upPosition))
            {
                _controller.EndDrag(upPosition);
                _activeSlot = -1;
            }
        }

        private void ApplyResponsiveLayout(bool force)
        {
            if (!_layoutInitialized)
                return;

            var landscape = _useLandscapeLayout && Screen.width > Screen.height;
            if (!force && landscape == _isLandscape)
                return;

            _isLandscape = landscape;
            if (landscape)
            {
                _camera.orthographicSize = _landscapeCameraSize;
                var cameraPosition = _portraitCameraPosition;
                cameraPosition.y = _landscapeCameraY;
                _camera.transform.position = cameraPosition;
                _boardTransform.position = _portraitBoardPosition + _landscapeBoardOffset;
                transform.position = _boardTransform.position + _landscapeSlotsOffset;

                var center = (_slots.Length - 1) * 0.5f;
                for (var i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i] != null)
                        _slots[i].localPosition = new Vector3(0f, 0f, (center - i) * _landscapeSlotSpacing);
                }
            }
            else
            {
                _camera.orthographicSize = _portraitCameraSize;
                _camera.transform.position = _portraitCameraPosition;
                _boardTransform.position = _portraitBoardPosition;
                transform.position = _portraitSlotsPosition;
                for (var i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i] != null)
                        _slots[i].localPosition = _portraitSlotLocalPositions[i];
                }
            }

            LayoutChanged?.Invoke();
        }

        private bool TryPickSlot(Vector2 screenPosition, out int slot)
        {
            slot = -1;
            for (var i = 0; i < _stacks.Length && i < _slots.Length; i++)
            {
                if (_stacks[i] == null || _slots[i] == null)
                    continue;

                var slotScreen = _camera.WorldToScreenPoint(_slots[i].position);
                if (Vector2.Distance(screenPosition, slotScreen) <= _screenPickRadius)
                {
                    slot = i;
                    return true;
                }
            }

            return false;
        }

        public Vector3 GetSlotWorldPosition(int slot)
        {
            if (_slots == null || slot < 0 || slot >= _slots.Length || _slots[slot] == null)
                throw new ArgumentOutOfRangeException(nameof(slot));

            return _slots[slot].position;
        }

        private static bool TryGetPointerDown(out Vector2 position)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                position = Input.GetTouch(0).position;
                return true;
            }

            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }

            position = default;
            return false;
        }

        private static bool TryGetPointerPosition(out Vector2 position)
        {
            if (Input.touchCount > 0)
            {
                position = Input.GetTouch(0).position;
                return true;
            }

            position = Input.mousePosition;
            return true;
        }

        private static bool TryGetPointerUp(out Vector2 position)
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    position = touch.position;
                    return true;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                position = Input.mousePosition;
                return true;
            }

            position = default;
            return false;
        }
    }
}
