using UnityEngine;

namespace IcePuck
{
    public static class VectorMathUtils
    {
        public static float CalculateMagnitude(Vector3 v)
        {
            return Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        }

        public static float CalculateMagnitude(Vector2 v)
        {
            return Mathf.Sqrt(v.x * v.x + v.y * v.y);
        }

        public static Vector3 CalculateDirection(Vector3 from, Vector3 to)
        {
            Vector3 diff = new Vector3(to.x - from.x, to.y - from.y, to.z - from.z);
            float mag = CalculateMagnitude(diff);
            if (mag < 0.00001f)
                return Vector3.zero;

            return new Vector3(diff.x / mag, diff.y / mag, diff.z / mag);
        }

        public static float DotProduct(Vector3 a, Vector3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        public static Vector3 CalculateReflection(Vector3 velocity, Vector3 normal)
        {
            float dot = DotProduct(velocity, normal);
            return new Vector3(
                velocity.x - 2f * dot * normal.x,
                velocity.y - 2f * dot * normal.y,
                velocity.z - 2f * dot * normal.z
            );
        }

        public static float ClampMagnitude(float currentMagnitude, float minForce, float maxForce)
        {
            return Mathf.Clamp(currentMagnitude, minForce, maxForce);
        }
    }
}
