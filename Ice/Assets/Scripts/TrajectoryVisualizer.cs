using System.Collections.Generic;
using UnityEngine;

namespace IcePuck
{
    public class TrajectoryVisualizer : MonoBehaviour
    {
        private LineRenderer forceArrowRenderer;
        private LineRenderer trajectoryRenderer;

        [Header("Visual Customization")]
        [SerializeField] private Color minForceColor = Color.green;
        [SerializeField] private Color midForceColor = Color.yellow;
        [SerializeField] private Color maxForceColor = Color.red;
        [SerializeField] private float arrowHeadSize = 0.4f;

        [Header("Trajectory Physics Simulation")]
        [SerializeField] private int maxSimulationSteps = 50;
        [SerializeField] private float timeStep = 0.05f;
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private float puckRadius = 0.35f;

        private void Awake()
        {
            EnsureLineRenderers();
        }

        private void EnsureLineRenderers()
        {
            if (forceArrowRenderer == null)
            {
                GameObject arrowObj = new GameObject("ForceArrowRenderer");
                arrowObj.transform.SetParent(transform);
                forceArrowRenderer = arrowObj.AddComponent<LineRenderer>();
                ConfigureLineRenderer(forceArrowRenderer, 0.2f, 0.08f);
            }

            if (trajectoryRenderer == null)
            {
                GameObject trajObj = new GameObject("TrajectoryRenderer");
                trajObj.transform.SetParent(transform);
                trajectoryRenderer = trajObj.AddComponent<LineRenderer>();
                ConfigureLineRenderer(trajectoryRenderer, 0.1f, 0.1f);
            }
        }

        private void ConfigureLineRenderer(LineRenderer lr, float startWidth, float endWidth)
        {
            lr.startWidth = startWidth;
            lr.endWidth = endWidth;
            lr.positionCount = 0;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Unlit/Color");

            if (shader != null)
            {
                lr.material = new Material(shader);
            }
        }

        public void DrawVisuals(Vector3 puckPosition, Vector3 forceVector, float magnitude, float minForce, float maxForce, float puckMass)
        {
            if (forceArrowRenderer == null || trajectoryRenderer == null)
                EnsureLineRenderers();

            float normalizedForce = Mathf.Clamp01((magnitude - minForce) / Mathf.Max(0.001f, maxForce - minForce));
            Color currentVisualColor = GetColorForForce(normalizedForce);

            DrawForceArrow(puckPosition, forceVector, currentVisualColor);
            DrawTrajectory(puckPosition, forceVector, puckMass, currentVisualColor);
        }

        private Color GetColorForForce(float t)
        {
            if (t <= 0.5f)
            {
                return Color.Lerp(minForceColor, midForceColor, t * 2f);
            }
            return Color.Lerp(midForceColor, maxForceColor, (t - 0.5f) * 2f);
        }

        private void DrawForceArrow(Vector3 puckPosition, Vector3 forceVector, Color arrowColor)
        {
            forceArrowRenderer.enabled = true;
            forceArrowRenderer.startColor = arrowColor;
            forceArrowRenderer.endColor = arrowColor;

            Vector3 endPoint = puckPosition + forceVector;

            Vector3 direction = VectorMathUtils.CalculateDirection(puckPosition, endPoint);
            Vector3 side = Vector3.Cross(direction, Vector3.up).normalized * (arrowHeadSize * 0.5f);
            Vector3 headBase = endPoint - direction * arrowHeadSize;

            forceArrowRenderer.positionCount = 5;
            forceArrowRenderer.SetPosition(0, puckPosition);
            forceArrowRenderer.SetPosition(1, endPoint);
            forceArrowRenderer.SetPosition(2, headBase + side);
            forceArrowRenderer.SetPosition(3, endPoint);
            forceArrowRenderer.SetPosition(4, headBase - side);
        }

        private void DrawTrajectory(Vector3 puckPosition, Vector3 forceVector, float mass, Color pathColor)
        {
            trajectoryRenderer.enabled = true;
            trajectoryRenderer.startColor = new Color(pathColor.r, pathColor.g, pathColor.b, 0.85f);
            trajectoryRenderer.endColor = new Color(pathColor.r, pathColor.g, pathColor.b, 0.25f);

            List<Vector3> points = new List<Vector3> { puckPosition };

            Vector3 velocity = forceVector / Mathf.Max(0.001f, mass);
            Vector3 currentPos = puckPosition;

            int simulatedBounces = 0;

            for (int i = 0; i < maxSimulationSteps; i++)
            {
                float stepDist = VectorMathUtils.CalculateMagnitude(velocity) * timeStep;
                Vector3 stepDirection = VectorMathUtils.CalculateDirection(currentPos, currentPos + velocity);

                if (stepDist < 0.001f) break;

                if (Physics.SphereCast(currentPos, puckRadius, stepDirection, out RaycastHit hit, stepDist, collisionMask))
                {
                    currentPos = hit.point + hit.normal * puckRadius;
                    points.Add(currentPos);

                    velocity = VectorMathUtils.CalculateReflection(velocity, hit.normal);
                    simulatedBounces++;

                    if (simulatedBounces > 4) break;
                }
                else
                {
                    currentPos += velocity * timeStep;
                    points.Add(currentPos);
                }
            }

            trajectoryRenderer.positionCount = points.Count;
            for (int p = 0; p < points.Count; p++)
            {
                trajectoryRenderer.SetPosition(p, points[p]);
            }
        }

        public void HideVisuals()
        {
            if (forceArrowRenderer != null) forceArrowRenderer.enabled = false;
            if (trajectoryRenderer != null) trajectoryRenderer.enabled = false;
        }
    }
}
