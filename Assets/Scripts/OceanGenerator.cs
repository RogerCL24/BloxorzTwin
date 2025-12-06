using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int xSize = 100;
    public int zSize = 100;
    public float gridSize = 1.5f; // Larger grid for bigger ocean

    [Header("Wave Settings")]
    public float waveHeight = 1.2f;
    public float waveSpeed = 1.5f;
    public float waveScale = 0.15f;

    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] baseVertices;

    void Start()
    {
        GenerateMesh();
    }

    void GenerateMesh()
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Ocean Mesh";

        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        Vector4[] tangents = new Vector4[vertices.Length];
        Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);

        // Center the mesh
        float xOffset = (xSize * gridSize) / 2f;
        float zOffset = (zSize * gridSize) / 2f;

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++, i++)
            {
                vertices[i] = new Vector3(x * gridSize - xOffset, 0, z * gridSize - zOffset);
                uv[i] = new Vector2((float)x / xSize, (float)z / zSize);
                tangents[i] = tangent;
            }
        }
        
        baseVertices = (Vector3[])vertices.Clone();

        int[] triangles = new int[xSize * zSize * 6];
        for (int ti = 0, vi = 0, z = 0; z < zSize; z++, vi++)
        {
            for (int x = 0; x < xSize; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + xSize + 1;
                triangles[ti + 5] = vi + xSize + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.tangents = tangents;
        mesh.RecalculateNormals();
        
        // Create a nice ocean material
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer.material == null || renderer.material.name.StartsWith("Default"))
        {
             Material oceanMat = new Material(Shader.Find("Standard"));
             oceanMat.color = new Color(0, 0.3f, 0.6f, 0.9f); // Deep blue
             oceanMat.SetFloat("_Glossiness", 0.9f); // Very shiny
             oceanMat.SetFloat("_Metallic", 0.2f);
             renderer.material = oceanMat;
        }
    }

    void Update()
    {
        if (mesh == null) return;

        float time = Time.time * waveSpeed;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = baseVertices[i];
            
            // Combine multiple Perlin noise layers for more natural waves
            float y = Mathf.PerlinNoise((v.x + time) * waveScale, (v.z + time) * waveScale) * waveHeight;
            y += Mathf.PerlinNoise((v.x - time) * waveScale * 2f, (v.z - time * 0.5f) * waveScale * 2f) * waveHeight * 0.3f;
            y += Mathf.Sin(time + v.x * 0.5f) * 0.2f; // Add some rolling swell

            vertices[i].y = y;
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }
}
