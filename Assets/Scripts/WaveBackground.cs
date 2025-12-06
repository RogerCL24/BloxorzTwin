using System.Collections.Generic;
using UnityEngine;

public class WaveBackground : MonoBehaviour
{
    [Header("Grid Settings")]
    public int linesCount = 50;
    public int pointsPerLine = 50;
    public float width = 100f;
    public float depth = 100f;
    public float yPosition = -10f;

    [Header("Wave Settings")]
    public float waveSpeedX = 2f;
    public float waveSpeedZ = 1f;
    public float waveAmpX = 1f;
    public float waveAmpZ = 0.5f;
    public float noiseScale = 0.02f;
    public float noiseStrength = 5f;

    [Header("Visuals")]
    public Color lineColor = new Color(0f, 0.5f, 1f, 0.3f); // Translucent blue
    public float lineWidth = 0.1f;

    private List<LineRenderer> lineRenderers = new List<LineRenderer>();
    private Vector3[,] basePositions;
    private GameObject linesParent;
    private Material lineMat;

    void Start()
    {
        InitializeLines();
    }

    public void InitializeLines()
    {
        // Clean up existing
        if (linesParent != null) Destroy(linesParent);
        
        linesParent = new GameObject("WaveLinesContainer");
        linesParent.transform.SetParent(transform);
        linesParent.transform.localPosition = Vector3.zero;

        lineRenderers.Clear();
        basePositions = new Vector3[linesCount, pointsPerLine];

        // Create a simple material for the lines
        if (lineMat == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                lineMat = new Material(shader);
            }
            else
            {
                // Fallback if Sprites/Default isn't found (e.g. URP/HDRP might differ, but usually exists)
                lineMat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            }
        }

        float xStart = -width / 2f;
        float zStart = -depth / 2f;
        float xGap = width / (linesCount - 1);
        float zGap = depth / (pointsPerLine - 1);

        for (int i = 0; i < linesCount; i++)
        {
            GameObject lineObj = new GameObject($"Line_{i}");
            lineObj.transform.SetParent(linesParent.transform);
            lineObj.transform.localPosition = Vector3.zero;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = lineMat;
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = pointsPerLine;
            lr.useWorldSpace = false; 
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            lineRenderers.Add(lr);

            for (int j = 0; j < pointsPerLine; j++)
            {
                // Lines run along Z, spaced along X
                float x = xStart + i * xGap;
                float z = zStart + j * zGap;
                basePositions[i, j] = new Vector3(x, yPosition, z);
                lr.SetPosition(j, basePositions[i, j]);
            }
        }
    }

    void Update()
    {
        if (lineRenderers.Count == 0) return;

        float time = Time.time;

        for (int i = 0; i < linesCount; i++)
        {
            LineRenderer lr = lineRenderers[i];
            // Optimization: Update positions array directly if possible, but SetPosition is fine for < 5k points
            for (int j = 0; j < pointsPerLine; j++)
            {
                Vector3 basePos = basePositions[i, j];

                // Algorithm adapted from React code:
                // const move = noise.perlin2((p.x + time * waveSpeedX) * 0.002, (p.y + time * waveSpeedY) * 0.0015) * 12;
                // p.wave.x = Math.cos(move) * waveAmpX;
                // p.wave.y = Math.sin(move) * waveAmpY;

                // We use world coordinates for noise continuity
                // Note: Mathf.PerlinNoise mirrors at integer values, so we use a larger scale or offset if needed.
                float noiseVal = Mathf.PerlinNoise(
                    (basePos.x + transform.position.x + time * waveSpeedX) * noiseScale, 
                    (basePos.z + transform.position.z + time * waveSpeedZ) * noiseScale
                ) * noiseStrength;

                float offsetX = Mathf.Cos(noiseVal) * waveAmpX;
                float offsetZ = Mathf.Sin(noiseVal) * waveAmpZ;

                // We can also add some Y height variation for a "sea" feel
                float offsetY = Mathf.Sin(noiseVal * 0.5f) * (waveAmpX * 0.5f);

                Vector3 newPos = new Vector3(
                    basePos.x + offsetX,
                    basePos.y + offsetY,
                    basePos.z + offsetZ
                );

                lr.SetPosition(j, newPos);
            }
        }
    }
}
