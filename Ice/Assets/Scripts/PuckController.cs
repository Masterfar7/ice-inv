using UnityEngine;

namespace IcePuck
{
    [RequireComponent(typeof(Rigidbody), typeof(PuckCollisionHandler))]
    public class PuckController : MonoBehaviour
    {
        [Header("Force Settings")]
        [SerializeField] private float forceMultiplier = 8f;
        [SerializeField] private float minForce = 1f;
        [SerializeField] private float maxForce = 25f;
        [SerializeField] private bool slingshotMode = true;

        [Header("Drag Detection")]
        [SerializeField] private float maxPickupRadius = 3f;
        [SerializeField] private Camera mainCamera;

        [Header("Visualizer Reference")]
        [SerializeField] private TrajectoryVisualizer visualizer;

        private Rigidbody rb;
        private PuckCollisionHandler collisionHandler;
        private bool isDragging = false;
        private Vector3 dragWorldPosition;
        private Plane groundPlane;

        public bool IsDragging => isDragging;
        public float CurrentForceMagnitude { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            collisionHandler = GetComponent<PuckCollisionHandler>();

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (visualizer == null)
            {
                visualizer = GetComponent<TrajectoryVisualizer>();
                if (visualizer == null)
                {
                    visualizer = gameObject.AddComponent<TrajectoryVisualizer>();
                }
            }
        }

        private void Update()
        {
            HandleMouseInput();
        }

        private void HandleMouseInput()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            groundPlane = new Plane(Vector3.up, transform.position);

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (groundPlane.Raycast(ray, out float enter))
            {
                dragWorldPosition = ray.GetPoint(enter);
            }

            if (Input.GetMouseButtonDown(0))
            {
                float distToPuck = VectorMathUtils.CalculateMagnitude(dragWorldPosition - transform.position);
                if (distToPuck <= maxPickupRadius)
                {
                    isDragging = true;
                }
            }

            if (Input.GetMouseButton(0) && isDragging)
            {
                UpdateDragState();
            }

            if (Input.GetMouseButtonUp(0) && isDragging)
            {
                ApplyImpulseForce();
                isDragging = false;
                if (visualizer != null) visualizer.HideVisuals();
            }
        }

        private void UpdateDragState()
        {
            Vector3 rawVector;
            if (slingshotMode)
            {
                rawVector = transform.position - dragWorldPosition;
            }
            else
            {
                rawVector = dragWorldPosition - transform.position;
            }

            rawVector.y = 0;

            float rawMagnitude = VectorMathUtils.CalculateMagnitude(rawVector);
            Vector3 forceDirection = VectorMathUtils.CalculateDirection(Vector3.zero, rawVector);

            float scaledMagnitude = rawMagnitude * forceMultiplier;
            CurrentForceMagnitude = VectorMathUtils.ClampMagnitude(scaledMagnitude, minForce, maxForce);

            Vector3 forceVector = forceDirection * CurrentForceMagnitude;

            if (visualizer != null)
            {
                visualizer.DrawVisuals(transform.position, forceVector, CurrentForceMagnitude, minForce, maxForce, rb.mass);
            }
        }

        private void ApplyImpulseForce()
        {
            Vector3 rawVector = slingshotMode
                ? (transform.position - dragWorldPosition)
                : (dragWorldPosition - transform.position);

            rawVector.y = 0;

            float rawMagnitude = VectorMathUtils.CalculateMagnitude(rawVector);
            Vector3 forceDirection = VectorMathUtils.CalculateDirection(Vector3.zero, rawVector);

            float scaledMagnitude = rawMagnitude * forceMultiplier;
            float finalMagnitude = VectorMathUtils.ClampMagnitude(scaledMagnitude, minForce, maxForce);

            Vector3 impulseVector = forceDirection * finalMagnitude;

            if (finalMagnitude >= minForce && rb != null)
            {
                rb.AddForce(impulseVector, ForceMode.Impulse);

                if (collisionHandler != null)
                {
                    collisionHandler.ResetBounceCount();
                }
            }
        }
    }
}
