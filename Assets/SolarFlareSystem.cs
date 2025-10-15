using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages dramatic, large-scale solar flare eruptions with massive particle effects and plasma arcs
/// Designed for visibility from 50-100+ units away
/// </summary>
public class SolarFlareSystem : MonoBehaviour
{
    [Header("Flare Timing")]
    [SerializeField] private float minFlareInterval = 1.5f;
    [SerializeField] private float maxFlareInterval = 4f;

    [Header("Massive Flare Properties")]
    [SerializeField] private int minParticles = 500;
    [SerializeField] private int maxParticles = 2000;
    [SerializeField] private float flareDistance = 15f; // How far flares shoot out
    [SerializeField] private float flareSpeed = 12f;
    [SerializeField] private float flareLifetime = 4f;
    [SerializeField] private float particleSize = 2f;

    [Header("Plasma Arc Settings")]
    [SerializeField] private bool enablePlasmaArcs = true;
    [SerializeField] private int plasmaArcsPerFlare = 2;
    [SerializeField] private float arcWidth = 0.8f;

    [Header("Visual Settings")]
    [SerializeField] private Gradient flareColorGradient;
    [SerializeField] private Color flareGlowColor = new Color(1f, 0.7f, 0.2f);
    [SerializeField] private float flareEmissionIntensity = 8f;

    private ParticleSystem flareParticleSystem;
    private List<GameObject> plasmaArcs = new List<GameObject>();
    private float sunRadius;

    void Start()
    {
        // Get sun radius
        SphereCollider col = GetComponent<SphereCollider>();
        if (col != null)
        {
            sunRadius = col.radius * transform.localScale.x;
        }
        else
        {
            sunRadius = transform.localScale.x * 0.5f;
        }

        SetupParticleSystem();
        SetupDefaultGradient();
        StartCoroutine(FlareCoroutine());
    }

    private void SetupDefaultGradient()
    {
        if (flareColorGradient == null || flareColorGradient.colorKeys.Length == 0)
        {
            flareColorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[4];
            colorKeys[0] = new GradientColorKey(new Color(1f, 1f, 0.8f), 0f);      // Bright white-yellow
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.6f, 0.1f), 0.3f);  // Orange
            colorKeys[2] = new GradientColorKey(new Color(1f, 0.3f, 0.5f), 0.6f);  // Pink
            colorKeys[3] = new GradientColorKey(new Color(0.6f, 0.2f, 1f), 1f);    // Purple

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(0.8f, 0.5f);
            alphaKeys[2] = new GradientAlphaKey(0f, 1f);

            flareColorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }

    private void SetupParticleSystem()
    {
        GameObject psObj = new GameObject("MassiveFlareParticles");
        psObj.transform.SetParent(transform);
        psObj.transform.localPosition = Vector3.zero;

        flareParticleSystem = psObj.AddComponent<ParticleSystem>();

        var main = flareParticleSystem.main;
        main.startLifetime = flareLifetime;
        main.startSpeed = flareSpeed;
        main.startSize = particleSize;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 5000;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = flareParticleSystem.emission;
        emission.enabled = false;

        var shape = flareParticleSystem.shape;
        shape.enabled = false;

        var colorOverLifetime = flareParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(flareColorGradient);

        var sizeOverLifetime = flareParticleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.2f, 2f);
        sizeCurve.AddKey(0.5f, 1.5f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Add velocity over lifetime for arc effect
        var velocityOverLifetime = flareParticleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;

        var renderer = flareParticleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Unlit/Transparent"));
        renderer.material.color = flareGlowColor * flareEmissionIntensity;

        // Add trail module for extra visual impact
        var trails = flareParticleSystem.trails;
        trails.enabled = true;
        trails.ratio = 0.3f;
        trails.lifetime = 0.5f;
        trails.minVertexDistance = 0.2f;
        trails.colorOverLifetime = colorOverLifetime.color;
    }

    private IEnumerator FlareCoroutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minFlareInterval, maxFlareInterval);
            yield return new WaitForSeconds(waitTime);

            EmitMassiveFlare();
        }
    }

    private void EmitMassiveFlare()
    {
        // Random eruption point on sun surface
        Vector3 flareDirection = Random.onUnitSphere;
        Vector3 flareOrigin = transform.position + flareDirection * sunRadius;

        int particleCount = Random.Range(minParticles, maxParticles);

        // Main particle burst
        for (int i = 0; i < particleCount; i++)
        {
            // Create wide cone of particles
            Vector3 tangent1 = Vector3.Cross(flareDirection, Random.onUnitSphere).normalized;
            Vector3 tangent2 = Vector3.Cross(flareDirection, tangent1).normalized;

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float spread = Random.Range(0f, 45f) * Mathf.Deg2Rad;

            Vector3 spreadDirection = flareDirection;
            spreadDirection += tangent1 * Mathf.Cos(angle) * Mathf.Sin(spread);
            spreadDirection += tangent2 * Mathf.Sin(angle) * Mathf.Sin(spread);
            spreadDirection.Normalize();

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            emitParams.position = flareOrigin;
            emitParams.velocity = spreadDirection * flareSpeed * Random.Range(0.5f, 1.5f);
            emitParams.startSize = particleSize * Random.Range(0.8f, 2f);
            emitParams.startLifetime = flareLifetime * Random.Range(0.7f, 1.3f);
            emitParams.startColor = flareGlowColor * flareEmissionIntensity;

            flareParticleSystem.Emit(emitParams, 1);
        }

        // Create plasma arcs
        if (enablePlasmaArcs)
        {
            for (int i = 0; i < plasmaArcsPerFlare; i++)
            {
                CreatePlasmaArc(flareOrigin, flareDirection);
            }
        }
    }

    private void CreatePlasmaArc(Vector3 origin, Vector3 baseDirection)
    {
        GameObject arcObj = new GameObject("PlasmaArc");
        arcObj.transform.position = origin;

        LineRenderer line = arcObj.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Unlit/Color"));
        line.startColor = flareGlowColor * flareEmissionIntensity;
        line.endColor = flareGlowColor * flareEmissionIntensity * 0.5f;
        line.startWidth = arcWidth;
        line.endWidth = arcWidth * 0.3f;
        line.numCapVertices = 5;
        line.alignment = LineAlignment.View;

        // Create arc path that loops back
        int segments = 20;
        line.positionCount = segments;

        Vector3 tangent = Vector3.Cross(baseDirection, Random.onUnitSphere).normalized;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            float arcHeight = Mathf.Sin(t * Mathf.PI) * flareDistance;

            Vector3 pos = origin + baseDirection * (t * flareDistance * 0.5f) + tangent * arcHeight;

            // Add some randomness
            pos += Random.onUnitSphere * (flareDistance * 0.1f);

            line.SetPosition(i, pos);
        }

        // Animate and destroy
        StartCoroutine(AnimateAndDestroyArc(arcObj, line));
    }

    private IEnumerator AnimateAndDestroyArc(GameObject arcObj, LineRenderer line)
    {
        float lifetime = flareLifetime * 0.8f;
        float elapsed = 0f;

        Color startColor = line.startColor;
        Color endColor = line.endColor;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;

            // Fade out
            float alpha = 1f - t;
            line.startColor = startColor * alpha;
            line.endColor = endColor * alpha;

            // Shrink
            line.startWidth = arcWidth * alpha;
            line.endWidth = arcWidth * 0.3f * alpha;

            yield return null;
        }

        Destroy(arcObj);
    }

    void OnDestroy()
    {
        // Clean up any remaining arcs
        foreach (var arc in plasmaArcs)
        {
            if (arc != null) Destroy(arc);
        }
    }
}