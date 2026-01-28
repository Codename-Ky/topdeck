using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.9f, 0f);
    [SerializeField] private Vector3 barSize = new Vector3(0.8f, 0.1f, 0.1f);
    [SerializeField] private bool billboard = true;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.8f);
    [SerializeField] private Color healthyColor = new Color(0.2f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color lowHealthColor = new Color(0.95f, 0.25f, 0.2f, 1f);
    [SerializeField, Range(0.05f, 1f)] private float lowHealthThreshold = 0.25f;

    private Transform fillTransform;
    private Renderer fillRenderer;
    private float maxHealth = 1f;
    private Camera cachedCamera;

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
        ApplyOffset();
        SetHealth(maxHealthValue);
    }

    public void SetHealth(float currentHealth)
    {
        float normalized = Mathf.Clamp01(currentHealth / maxHealth);

        if (fillTransform != null)
        {
            Vector3 scale = fillTransform.localScale;
            scale.x = Mathf.Max(0.001f, normalized);
            fillTransform.localScale = scale;

            Vector3 position = fillTransform.localPosition;
            position.x = (normalized - 1f) * 0.5f * barSize.x;
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
        if (transform.childCount == 0)
        {
            GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
            background.name = "Background";
            background.transform.SetParent(transform, false);
            background.transform.localScale = barSize;
            background.transform.localPosition = Vector3.zero;
            RemoveCollider(background);
            var backgroundRenderer = background.GetComponent<Renderer>();
            RendererUtils.SetColor(backgroundRenderer, backgroundColor);

            GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "Fill";
            fill.transform.SetParent(transform, false);
            fill.transform.localScale = new Vector3(barSize.x, barSize.y * 0.9f, barSize.z * 0.9f);
            fill.transform.localPosition = new Vector3(-barSize.x * 0.5f, 0f, 0f);
            RemoveCollider(fill);
            fillRenderer = fill.GetComponent<Renderer>();
            fillTransform = fill.transform;
        }
        else
        {
            Transform fill = transform.Find("Fill");
            if (fill != null)
            {
                fillTransform = fill;
                fillRenderer = fill.GetComponent<Renderer>();
            }

            Transform background = transform.Find("Background");
            if (background != null)
            {
                background.localScale = barSize;
            }
        }
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
