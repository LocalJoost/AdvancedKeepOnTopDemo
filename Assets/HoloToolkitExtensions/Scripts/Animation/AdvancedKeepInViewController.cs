using System.Collections;
using System.Linq;
using HoloToolkit.Unity;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using HoloToolkitExtensions.Utilities;

namespace HoloToolkitExtensions.Animation
{
    public class AdvancedKeepInViewController : MonoBehaviour
    {
        [Tooltip("The maximum distance to project the object before the camera")]
        public float MaxDistance = 2f;

        [Tooltip("The minimum distance to project the object before the camera")]
        public float MinDistance = 0.2f;

        [Tooltip("The minimum meters per second the user has to move to trigger moving the object to a new location")]
        public float DistanceMoveTrigger = 0.4f;

        [Tooltip("The minimum degrees per second the user has to rotate to trigger moving the object to a new location")]
        public float DeltaRotationTrigger = 15.0f;

        [Tooltip("The extra distance to keep between the object and the obstruction")]
        public float DistanceBeforeObstruction = 0.02f;

        [Tooltip("Enable hiding the object when exceeding HideSpeed m/s")]
        public bool HideWhenMoving = true;

        [Tooltip("The minimum meters per second the user has to move to trigger hiding the object completely")]
        public float HideSpeed = 2.5f;

        [Tooltip("The time used to fade out the object when hiding it (and fading it in when it should reappear")]
        private const float FadeTime = 0.4f;

        [Tooltip("The layers to check for obstructions")]
        public int LayerMask = Physics.DefaultRaycastLayers;

        [Tooltip("Make the object bigger or smaller depending on the distance to the camera")]
        public bool ScaleByDistance = true;

        [SerializeField]
        [Tooltip("When the object is enabled, move it in view immediately")]
        public bool AppearInView = true;

        [SerializeField]
        private BaseRayStabilizer _stabilizer;

        [SerializeField]
        private bool _showDebugBoxcastLines = true;

        public float MoveTime = 0.8f;

        private float _startTime;

        private float _delay = 0.1f;

        private Vector3 _lastMoveToLocation;

        private Material _objectMaterial;

        private Vector3? _originalScale;

        private bool _isFading;

        private bool _isMoving;

        private bool _isScaling;

        private bool _isJustEnabled = false;

        private bool _isHidden = false;

        private float _initialTransparency;

        // Use this for initialization
        void Start()
        {
            _objectMaterial = GetComponentInChildren<Renderer>().material;
            _initialTransparency = _objectMaterial.color.a;
        }

        void OnEnable()
        {
            _startTime = Time.time + _delay;
            DoInitialAppearance();
            _isJustEnabled = true;
        }

        private void DoInitialAppearance()
        {
            if (!AppearInView)
            {
                return;
            }

            _lastMoveToLocation = GetNewPosition();
            transform.position = _lastMoveToLocation;
        }

        void Update()
        {
            if (_startTime > Time.time)
                return;
            if (_originalScale == null)
            {
                _originalScale = transform.localScale;
            }

            if (!CheckHideWhenMoving())
            {
                return;
            }

            if (CameraMovementTracker.Instance.Distance > DistanceMoveTrigger ||
                CameraMovementTracker.Instance.RotationDelta > DeltaRotationTrigger ||
                _isJustEnabled)
            {
                _isJustEnabled = false;
                MoveIntoView();
            }
#if UNITY_EDITOR
            if (_showDebugBoxcastLines)
            {
                LookingDirectionHelpers.GetObjectBeforeObstruction(gameObject, MaxDistance,
                    DistanceBeforeObstruction, LayerMask, _stabilizer, true);
            }
#endif
        }

        private void MoveIntoView()
        {
            if (_isMoving)
            {
                return;
            }

            _isMoving = true;
            var newPos = GetNewPosition();
            MoveAndScale(newPos);
        }

        private void MoveAndScale(Vector3 newPos, bool isFinalAdjustment = false)
        {
            LeanTween.move(gameObject, newPos, MoveTime).setEaseInOutSine().setOnComplete(() =>
            {
                if (!isFinalAdjustment)
                {
                    newPos = GetNewPosition();
                    MoveAndScale(newPos, true);
                }
                else
                {
                    _isMoving = false;
                    DoScaleByDistance();
                }
            });
            _lastMoveToLocation = newPos;
        }

        private void DoScaleByDistance()
        {
            if (!ScaleByDistance || _originalScale == null || _isScaling)
            {
                return;
            }
            _isScaling = true;
            var distance = Vector3.Distance(_stabilizer ? _stabilizer.StablePosition : CameraCache.Main.transform.position,
                _lastMoveToLocation);
            var newScale = _originalScale.Value * distance / MaxDistance;
            LeanTween.scale(gameObject, newScale, MoveTime).setOnComplete(() => _isScaling = false);
        }

        private Vector3 GetNewPosition()
        {
            var newPos = LookingDirectionHelpers.GetObjectBeforeObstruction(gameObject, MaxDistance,
                DistanceBeforeObstruction, LayerMask, _stabilizer);
            return newPos;
        }

        private bool CheckHideWhenMoving()
        {
            if (!HideWhenMoving || _isFading)
            {
                return true;
            }
            if (CameraMovementTracker.Instance.Speed > HideSpeed &&
                !_isHidden)
            {
                _isHidden = true;
                StartCoroutine(SetFading());
                LeanTween.alpha(gameObject, 0, FadeTime);
            }
            else if (CameraMovementTracker.Instance.Speed <= HideSpeed && _isHidden)
            {
                _isHidden = false;
                StartCoroutine(SetFading());
                LeanTween.alpha(gameObject, _initialTransparency, FadeTime);
                MoveIntoView();
            }

            return !_isHidden;
        }

        private IEnumerator SetFading()
        {
            _isFading = true;
            yield return new WaitForSeconds(FadeTime + 0.1f);
            _isFading = false;
        }
    }
}

