using UnityEngine;

/// <summary>
/// Controls the animated sun sphere with dramatic pulsing, color shifts, and corona effects
/// Enhanced for massive scale and long-distance visibility
/// </summary>
public class SunController : MonoBehaviour
{
    [Header("Sun Scale Settings")]
    [SerializeField] private float sunBaseScale = 25f;
    [SerializeField] private float pulseScaleAmount = 0.15f;

    [Header("Animation Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float colorShiftSpeed = 1.5f;
    [SerializeField] private float surfaceRotationSpeed = 5f;
    [SerializeField] private float turbulenceIntensity = 0.4f;

    [Header("Color Palette")]
    [SerializeField] private Color deepOrange = new Color(1f, 0.4f, 0.1f);
    [SerializeField] private Color brightYellow = new Color(1f, 0.95f, 0.3f);
    [SerializeField] private Color electricBlue = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color mysticalPurple = new Color(0.8f, 0.2f, 1f);
    [SerializeField] private float emissionIntensity = 5f;

    [Header("Corona Settings")]
    [SerializeField] private bool enableCorona = true;
    [SerializeField] private float coronaScale = 1.4f;
    [SerializeField] private float coronaPulseSpeed = 3f;

    private Material sunMaterial;
    private Light sunLight;
    private GameObject coronaObject;
    private Material coronaMaterial;
    private float time;
    private Vector3 baseScale;
    private Color[] colorPalette;

    void Start()
    {
        // Set up color palette
        colorPalette = new Color[] { deepOrange, brightYellow, electricBlue, mysticalPurple };

        // Set base scale
        baseScale = Vector3.one * sunBaseScale;
        transform.localScale = baseScale;

        // Get or create material
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            sunMaterial = renderer.material;
            SetupMaterial();
        }

        // Setup light component
        sunLight = GetComponent<Light>();
        if (sunLight == null)
        {
            sunLight = gameObject.AddComponent<Light>();
        }
        sunLight.type = LightType.Point;
        sunLight.range = sunBaseScale * 8f;
        sunLight.intensity = 3f;

        // Create corona
        if (enableCorona)
        {
            CreateCorona();
        }
    }

    void Update()
    {
        time += Time.deltaTime;
        AnimateSun();
        AnimateCorona();
    }

    private void SetupMaterial()
    {
        if (sunMaterial != null)
        {
            // Use Unlit shader for maximum brightness without lighting dependency
            sunMaterial.shader = Shader.Find("Unlit/Color");
            sunMaterial.EnableKeyword("_EMISSION");
            sunMaterial.SetColor("_Color", deepOrange);

            // Try to set emission if available (Standard shader)
            if (sunMaterial.HasProperty("_EmissionColor"))
            {
                sunMaterial.SetColor("_EmissionColor", deepOrange * emissionIntensity);
            }
        }
    }

    private void CreateCorona()
    {
        coronaObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coronaObject.name = "SunCorona";
        coronaObject.transform.SetParent(transform);
        coronaObject.transform.localPosition = Vector3.zero;
        coronaObject.transform.localScale = Vector3.one * coronaScale;

        // Remove collider
        Destroy(coronaObject.GetComponent<Collider>());

        // Create transparent glowing material
        coronaMaterial = new Material(Shader.Find("Unlit/Transparent"));
        coronaMaterial.color = new Color(1f, 0.6f, 0.2f, 0.3f);

        Renderer coronaRenderer = coronaObject.GetComponent<Renderer>();
        coronaRenderer.material = coronaMaterial;
        coronaRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void AnimateSun()
    {
        // Dramatic scale pulsing
        float pulse = Mathf.Sin(time * pulseSpeed) * pulseScaleAmount;
        transform.localScale = baseScale * (1f + pulse);

        // Multi-color shifting through palette
        float colorCycle = (time * colorShiftSpeed) % (colorPalette.Length * 2f);
        int colorIndex = Mathf.FloorToInt(colorCycle / 2f) % colorPalette.Length;
        int nextColorIndex = (colorIndex + 1) % colorPalette.Length;
        float lerpFactor = (colorCycle % 2f) / 2f;

        // Add turbulence to color transitions
        float turbulence = Mathf.PerlinNoise(time * 0.5f, time * 0.3f) * turbulenceIntensity;
        lerpFactor = Mathf.Clamp01(lerpFactor + turbulence);

        Color currentColor = Color.Lerp(colorPalette[colorIndex], colorPalette[nextColorIndex], lerpFactor);

        // Apply super saturated colors
        currentColor = Color.Lerp(currentColor, Color.white, 0.2f); // Add brightness
        currentColor.r = Mathf.Pow(currentColor.r, 0.8f); // Increase saturation
        currentColor.g = Mathf.Pow(currentColor.g, 0.8f);
        currentColor.b = Mathf.Pow(currentColor.b, 0.8f);

        // Update material
        if (sunMaterial != null)
        {
            sunMaterial.SetColor("_Color", currentColor * emissionIntensity);
            if (sunMaterial.HasProperty("_EmissionColor"))
            {
                sunMaterial.SetColor("_EmissionColor", currentColor * emissionIntensity * 2f);
            }
        }

        // Update light
        if (sunLight != null)
        {
            sunLight.intensity = 3f * (1f + pulse * 2f);
            sunLight.color = currentColor;
        }

        // Surface rotation
        transform.Rotate(Vector3.up, surfaceRotationSpeed * Time.deltaTime, Space.Self);
        transform.Rotate(Vector3.right, surfaceRotationSpeed * 0.3f * Time.deltaTime, Space.Self);
    }

    private void AnimateCorona()
    {
        if (!enableCorona || coronaObject == null) return;

        // Pulsing corona
        float coronaPulse = (Mathf.Sin(time * coronaPulseSpeed) + 1f) * 0.5f;
        float scale = coronaScale * Mathf.Lerp(0.9f, 1.2f, coronaPulse);
        coronaObject.transform.localScale = Vector3.one * scale;

        // Color sync with sun but more transparent
        if (coronaMaterial != null && sunMaterial != null)
        {
            Color sunColor = sunMaterial.GetColor("_Color");
            Color coronaColor = sunColor;
            coronaColor.a = Mathf.Lerp(0.2f, 0.5f, coronaPulse);
            coronaMaterial.color = coronaColor;
        }

        // Counter-rotate corona for visual interest
        coronaObject.transform.Rotate(Vector3.up, -surfaceRotationSpeed * 1.5f * Time.deltaTime, Space.Self);
    }

    // Public method to trigger manual color shift
    public void TriggerColorShift()
    {
        time += 1f / colorShiftSpeed;
    }
}