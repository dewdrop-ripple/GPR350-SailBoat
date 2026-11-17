using UnityEngine;
using UnityEngine.Rendering;

// flat grid, moved by OceanSurface.shader
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanSurface : MonoBehaviour
{
    [SerializeField] private Shader mShader; // OceanSurface.shader
    [SerializeField] private float mSize = 1000f; // how far the ocean reaches
    [SerializeField] private int mResolution = 1000; // quads per side

    private void Start()
    {
        if (mShader == null)
        {
            Debug.LogWarning("Youre missing the shader");
            return;
        }

        GetComponent<MeshFilter>().mesh = BuildGrid();
        GetComponent<MeshRenderer>().material = new Material(mShader);
    }

    private Mesh BuildGrid()
    {
        int count = mResolution + 1;
        Vector3[] vertices = new Vector3[count * count];
        int[] triangles = new int[mResolution * mResolution * 6];

        for (int z = 0; z < count; z++)
        {
            for (int x = 0; x < count; x++)
            {
                float px = (x / (float)mResolution - 0.5f) * mSize;
                float pz = (z / (float)mResolution - 0.5f) * mSize;
                vertices[z * count + x] = new Vector3(px, 0f, pz);
            }
        }

        int t = 0;
        for (int z = 0; z < mResolution; z++)
        {
            for (int x = 0; x < mResolution; x++)
            {
                int i = z * count + x;
                triangles[t++] = i; triangles[t++] = i + count; triangles[t++] = i + 1;
                triangles[t++] = i + 1; triangles[t++] = i + count; triangles[t++] = i + count + 1;
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32; // more than 65535 vertices
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // the shader moves vertices, so widen the bounds to avoid culling
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(mSize, 100f, mSize));
        return mesh;
    }
}
