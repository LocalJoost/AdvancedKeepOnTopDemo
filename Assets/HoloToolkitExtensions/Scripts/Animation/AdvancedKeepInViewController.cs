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
        public float MaxDistance = 2f;

        public float MinDistance = 0.2f;

        public float DistanceMoveTrigger = 0.4f;

        public float DeltaRotationTrigger = 15.0f;

        public float DistanceBeforeObstruction = 0.02f;

        public bool HideWhenMoving = true;

        public float HideSpeed = 2.5f;

        public int LayerMask = Physics.DefaultRaycastLayers;

        private const float FadeTime = 0.4f;

        public bool ScaleByDistance = true;

        [SerializeField]
        private bool _appearInView = true;

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

        private float _initialTransparancy;

        // Use this for initialization
        void Start()
        {
            _objectMaterial = GetComponentInChildren<Renderer>().material;
            _initialTransparancy = _objectMaterial.color.a;
        }

        void OnEnable()
        {
            _startTime = Time.time + _delay;
            DoInitialAppearance();
            _isJustEnabled = true;
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

        private void DoInitialAppearance()
        {
            if (!_appearInView)
            {
                return;
            }

            _lastMoveToLocation = GetNewPosition();
            transform.position = _lastMoveToLocation;
            DoScaleByDistance();
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
            if (Vector3.Distance(newPos, CameraCache.Main.transform.position) < MinDistance)
            {
                newPos = LookingDirectionHelpers.CalculatePositionDeadAhead(MinDistance);
            }
            return newPos;
        }

        private bool CheckHideWhenMoving()
        {
            if (!HideWhenMoving || _isFading)
            {
                return true;
            }
            if (CameraMovementTracker.Instance.Speed > HideSpeed && _objectMaterial.color.a != 0.0f)
            {
                StartCoroutine(SetFading());
                LeanTween.alpha(gameObject, 0, FadeTime);
            }
            else if (CameraMovementTracker.Instance.Speed <= HideSpeed && _objectMaterial.color.a != _initialTransparancy)
            {
                StartCoroutine(SetFading());
                LeanTween.alpha(gameObject, _initialTransparancy, FadeTime);
                MoveIntoView();
            }

            return _objectMaterial.color.a != 0.0f;
        }

        private IEnumerator SetFading()
        {
            _isFading = true;
            yield return new WaitForSeconds(FadeTime + 0.1f);
            _isFading = false;
        }
    }
}

