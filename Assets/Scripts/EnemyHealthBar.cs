using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.9f, 0f);
    [SerializeField] private Vector3 barSize = new Vector3(0.95f, 0.12f, 0.06f);
    [SerializeField, Range(0.6f, 1f)] private float fillWidthPercent = 0.9f;
    [SerializeField, Range(0.4f, 1f)] private float fillHeightPercent = 0.7f;
    [SerializeField, Range(0.2f, 1f)] private float fillDepthPercent = 0.6f;
    [SerializeField] private float fillDepthOffset = -0.03f;
    [SerializeField] private bool billboard = true;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.8f);
    [SerializeField] private Color healthyColor = new Color(0.2f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color lowHealthColor = new Color(0.95f, 0.25f, 0.2f, 1f);
    [SerializeField, Range(0.05f, 1f)] private float lowHealthThreshold = 0.25f;

    private Transform fillTransform;
    private Renderer fillRenderer;
    private Renderer backgroundRenderer;
    private float maxHealth = 1f;
    private Camera cachedCamera;
    private float fillMaxWidth;

    private static Material unlitMaterial;

    private void Awake()
    {
        BuildIfNeeded();
        cachedCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (!billboard)
        {
            return;
        }

        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
            if (cachedCamera == null)
            {
                return;
            }
        }

        transform.forward = cachedCamera.transform.forward;
    }

    public void Initialize(float maxHealthValue)
    {
        maxHealth = Mathf.Max(1f, maxHealthValue);
        if (fillTransform == null)
        {
            BuildIfNeeded();
        }
        ApplyOffset();
        SetHealth(maxHealthValue);
    }

    public void SetHealth(float currentHealth)
    {
        if (fillTransform == null)
        {
            BuildIfNeeded();
        }

        float normalized = Mathf.Clamp01(currentHealth / maxHealth);

        if (fillTransform != null)
        {
            float width = Mathf.Max(0.001f, normalized * fillMaxWidth);
            Vector3 scale = fillTransform.localScale;
            scale.x = width;
            fillTransform.localScale = scale;

            Vector3 position = fillTransform.localPosition;
            position.x = (width - fillMaxWidth) * 0.5f;
            fillTransform.localPosition = position;
        }

        if (fillRenderer != null)
        {
            Color targetColor = normalized <= lowHealthThreshold ? lowHealthColor : healthyColor;
            RendererUtils.SetColor(fillRenderer, targetColor);
        }
    }

    public void SetOffset(float height)
    {
        localOffset.y = height;
        ApplyOffset();
    }

    private void ApplyOffset()
    {
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private void BuildIfNeeded()
    {
        Transform background = transform.Find("Background");
        if (background == null)
        {
            GameObject backgroundObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backgroundObject.name = "Background";
            backgroundObject.transform.SetParent(transform, false);
            background = backgroundObject.transform;
        }
        RemoveCollider(background.gameObject);

        Transform fill = transform.Find("Fill");
        if (fill == null)
        {
            GameObject fillObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fillObject.name = "Fill";
            fillObject.transform.SetParent(transform, false);
            fill = fillObject.transform;
        }
        RemoveCollider(fill.gameObject);

        background.localPosition = Vector3.zero;
        background.localScale = barSize;
        backgroundRenderer = background.GetComponent<Renderer>();
        ApplyMaterial(backgroundRenderer);
        RendererUtils.SetColor(backgroundRenderer, backgroundColor);

        fillTransform = fill;
        fillRenderer = fill.GetComponent<Renderer>();
        ApplyMaterial(fillRenderer);

        float widthPercent = Mathf.Clamp(fillWidthPercent, 0.1f, 1f);
        float heightPercent = Mathf.Clamp(fillHeightPercent, 0.1f, 1f);
        float depthPercent = Mathf.Clamp(fillDepthPercent, 0.1f, 1f);
        fillMaxWidth = barSize.x * widthPercent;
        fillTransform.localScale = new Vector3(fillMaxWidth, barSize.y * heightPercent, barSize.z * depthPercent);
        fillTransform.localPosition = new Vector3(0f, 0f, fillDepthOffset);
    }

    private static void ApplyMaterial(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = GetUnlitMaterial();
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static Material GetUnlitMaterial()
    {
        if (unlitMaterial != null)
        {
            return unlitMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            return null;
        }

        unlitMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        return unlitMaterial;
    }

    private static void RemoveCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }
}
