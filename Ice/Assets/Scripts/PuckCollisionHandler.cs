using UnityEngine;
using System;

namespace IcePuck
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class PuckCollisionHandler : MonoBehaviour
    {
        [Header("Bounce Settings")]
        [SerializeField] private int maxAllowedBounces = 3;
        [SerializeField] private bool manualReflectionOverride = false;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Rigidbody rb;
        private int currentBounceCount = 0;

        public int CurrentBounceCount => currentBounceCount;
        public int MaxAllowedBounces => maxAllowedBounces;

        public event Action<int> OnBounceCountChanged;
        public event Action OnPuckReset;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            SaveInitialTransform();
        }

        private void Start()
        {
            SaveInitialTransform();
        }

        public void SaveInitialTransform()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        public void ResetBounceCount()
        {
            currentBounceCount = 0;
            OnBounceCountChanged?.Invoke(currentBounceCount);
        }

        public void ResetToInitialPosition()
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            currentBounceCount = 0;
            OnBounceCountChanged?.Invoke(currentBounceCount);
            OnPuckReset?.Invoke();
        }

        private void OnCollisionEnter(Collision collision)
        {
            currentBounceCount++;
            OnBounceCountChanged?.Invoke(currentBounceCount);

            ContactPoint contact = collision.contacts[0];

            Vector3 incomingVelocity = collision.relativeVelocity;
            Vector3 reflectionVector = VectorMathUtils.CalculateReflection(-incomingVelocity, contact.normal);

            if (manualReflectionOverride && rb != null)
            {
                rb.linearVelocity = reflectionVector;
            }

            if (currentBounceCount > maxAllowedBounces)
            {
                ResetToInitialPosition();
            }
        }
    }
}
