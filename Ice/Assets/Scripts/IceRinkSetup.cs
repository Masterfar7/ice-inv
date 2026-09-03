using UnityEngine;
using UnityEngine.UI;

namespace IcePuck
{
    [DefaultExecutionOrder(-100)]
    public class IceRinkSetup : MonoBehaviour
    {
        [Header("Setup Options")]
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private Vector2 rinkSize = new Vector2(18f, 12f);
        [SerializeField] private float wallHeight = 1.8f;

        private PhysicsMaterial icePhysicsMaterial;

        private void Awake()
        {
            if (buildOnStart)
            {
                CreateEnvironment();
            }
        }

        [ContextMenu("Build Ice Rink Scene")]
        public void CreateEnvironment()
        {
            CreatePhysicsMaterial();

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "IceFloor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(rinkSize.x / 10f, 1f, rinkSize.y / 10f);

            Renderer floorRenderer = floor.GetComponent<Renderer>();
            if (floorRenderer != null)
            {
                floorRenderer.material = CreateMaterial(new Color(0.85f, 0.94f, 0.98f, 1f));
            }

            Collider floorCollider = floor.GetComponent<Collider>();
            if (floorCollider != null)
            {
                floorCollider.material = icePhysicsMaterial;
            }

            CreateWalls();
            CreatePuck();
            SetupCamera();
            SetupUI();
        }

        public static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            Material mat = new Material(shader ?? Shader.Find("Hidden/InternalErrorShader"));
            mat.color = color;

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            return mat;
        }

        private void CreatePhysicsMaterial()
        {
            icePhysicsMaterial = new PhysicsMaterial("ZeroFrictionElasticIce")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 1f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Maximum
            };
        }

        private void CreateWalls()
        {
            GameObject oldWalls = GameObject.Find("Walls");
            if (oldWalls != null) DestroyImmediate(oldWalls);

            GameObject wallsRoot = new GameObject("Walls");

            float halfX = rinkSize.x * 0.5f;
            float halfZ = rinkSize.y * 0.5f;
            float thickness = 0.6f;

            Color wallColor = new Color(0.18f, 0.35f, 0.55f);

            CreateWall("Wall_North", new Vector3(0, wallHeight * 0.5f, halfZ + thickness * 0.5f), new Vector3(rinkSize.x + thickness * 2, wallHeight, thickness), wallColor, wallsRoot.transform);
            CreateWall("Wall_South", new Vector3(0, wallHeight * 0.5f, -halfZ - thickness * 0.5f), new Vector3(rinkSize.x + thickness * 2, wallHeight, thickness), wallColor, wallsRoot.transform);
            CreateWall("Wall_East", new Vector3(halfX + thickness * 0.5f, wallHeight * 0.5f, 0), new Vector3(thickness, wallHeight, rinkSize.y), wallColor, wallsRoot.transform);
            CreateWall("Wall_West", new Vector3(-halfX - thickness * 0.5f, wallHeight * 0.5f, 0), new Vector3(thickness, wallHeight, rinkSize.y), wallColor, wallsRoot.transform);
        }

        private void CreateWall(string name, Vector3 pos, Vector3 scale, Color color, Transform parent)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;

            Renderer r = wall.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = CreateMaterial(color);
            }

            Collider col = wall.GetComponent<Collider>();
            if (col != null)
            {
                col.material = icePhysicsMaterial;
            }
        }

        private void CreatePuck()
        {
            GameObject existingPuck = GameObject.Find("Puck");
            if (existingPuck != null)
            {
                DestroyImmediate(existingPuck);
            }

            GameObject puck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puck.name = "Puck";

            puck.transform.position = new Vector3(0, 0.3f, 0);
            puck.transform.localScale = new Vector3(1.2f, 0.25f, 1.2f);

            Renderer r = puck.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = CreateMaterial(new Color(0.12f, 0.12f, 0.14f, 1f));
            }

            Collider col = puck.GetComponent<Collider>();
            if (col != null)
            {
                col.material = icePhysicsMaterial;
            }

            Rigidbody rb = puck.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.linearDamping = 0.15f;
            rb.angularDamping = 0.15f;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            puck.AddComponent<TrajectoryVisualizer>();
            puck.AddComponent<PuckCollisionHandler>();
            puck.AddComponent<PuckController>();
        }

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            cam.transform.position = new Vector3(0, 14f, -2f);
            cam.transform.rotation = Quaternion.Euler(75f, 0, 0);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.14f, 0.2f);
        }

        private void SetupUI()
        {
            GameObject existingCanvas = GameObject.Find("UI Canvas");
            if (existingCanvas != null) return;

            GameObject canvasObj = new GameObject("UI Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject panel = new GameObject("UIPanel");
            panel.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.02f, 0.72f);
            panelRect.anchorMax = new Vector2(0.40f, 0.98f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);

            Text bounceText = CreateUIText("BounceText", panel.transform, "Отскоки: 0 / 3", 22, TextAnchor.UpperLeft);
            RectTransform btRect = bounceText.rectTransform;
            btRect.anchorMin = new Vector2(0.05f, 0.65f);
            btRect.anchorMax = new Vector2(0.95f, 0.95f);

            Text forceText = CreateUIText("ForceText", panel.transform, "Сила impulse: 0.0", 18, TextAnchor.MiddleLeft);
            RectTransform ftRect = forceText.rectTransform;
            ftRect.anchorMin = new Vector2(0.05f, 0.35f);
            ftRect.anchorMax = new Vector2(0.95f, 0.65f);

            Text instText = CreateUIText("InstructionsText", panel.transform, "Управление...", 13, TextAnchor.LowerLeft);
            RectTransform itRect = instText.rectTransform;
            itRect.anchorMin = new Vector2(0.05f, 0.05f);
            itRect.anchorMax = new Vector2(0.95f, 0.35f);

            GameObject btnObj = new GameObject("ResetButton");
            btnObj.transform.SetParent(canvasObj.transform, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.82f, 0.88f);
            btnRect.anchorMax = new Vector2(0.98f, 0.96f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.6f, 0.9f);
            btnObj.AddComponent<Button>();

            Text btnTxt = CreateUIText("BtnText", btnObj.transform, "Сброс шайбы", 16, TextAnchor.MiddleCenter);
            btnTxt.rectTransform.anchorMin = Vector2.zero;
            btnTxt.rectTransform.anchorMax = Vector2.one;

            canvasObj.AddComponent<IceRinkUI>();
        }

        private Text CreateUIText(string name, Transform parent, string text, int fontSize, TextAnchor alignment)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Text t = obj.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = Color.white;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            return t;
        }
    }
}
