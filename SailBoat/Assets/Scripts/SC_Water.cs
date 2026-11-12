using UnityEngine;

public class Water : MonoBehaviour
{
    [System.Serializable]
    private struct Wave
    {
        public Vector2 direction;
        [Range(0f, 1f)] public float steepness;
        public float wavelength; // m
    }

    [SerializeField] private float mSize = 200f;    // m
    [SerializeField] private int mResolution = 100; // quads per side

    [SerializeField] private bool mDebugVertices;   // draw a dot on every vertex
    [SerializeField] private Wave[] mWaves =
    {
        new Wave { direction = new Vector2(1f, 0f), steepness = 0.12f, wavelength = 36f },
        new Wave { direction = new Vector2(0.8f, -0.5f), steepness = 0.13f, wavelength = 26f },
        new Wave { direction = new Vector2(1f, 1f), steepness = 0.13f, wavelength = 18f },
        new Wave { direction = new Vector2(0.3f, 1f), steepness = 0.12f, wavelength = 13f },
        new Wave { direction = new Vector2(1f, -1f), steepness = 0.11f, wavelength = 10f },
        new Wave { direction = new Vector2(-1f, 0.5f), steepness = 0.09f, wavelength = 8f },
        new Wave { direction = new Vector2(1f, 0.6f), steepness = 0.08f, wavelength = 6.5f },
        new Wave { direction = new Vector2(-0.5f, -1f), steepness = 0.07f, wavelength = 5f },
    };

    private Mesh mMesh;
    private Vector3[] mGrid;     // flat positions, never changes
    private Vector3[] mVertices; // grid + wave displacement
    private Vector3[] mNormals;


    // Gerstner waves, see catlikecoding.com/unity/tutorials/flow/waves
    // Returns how far the waves push a flat grid point, and the surface normal there
    private Vector3 Displacement(Vector3 grid, out Vector3 normal)
    {
        Vector3 offset = Vector3.zero;
        Vector3 tangent = Vector3.right;
        Vector3 binormal = Vector3.forward;

        foreach (Wave w in mWaves)
        {
            Vector2 d = w.direction.normalized;
            float k = 2f * Mathf.PI / w.wavelength;
            float c = Mathf.Sqrt(9.8f / k);
            float f = k * (d.x * grid.x + d.y * grid.z - c * Time.time);
            float a = w.steepness / k;
            float sin = Mathf.Sin(f);
            float cos = Mathf.Cos(f);

            offset += new Vector3(d.x * a * cos, a * sin, d.y * a * cos);
            tangent += new Vector3(-d.x * d.x * w.steepness * sin, d.x * w.steepness * cos, -d.x * d.y * w.steepness * sin);
            binormal += new Vector3(-d.x * d.y * w.steepness * sin, d.y * w.steepness * cos, -d.y * d.y * w.steepness * sin);
        }

        normal = Vector3.Cross(binormal, tangent).normalized;
        return offset;
    }

    public float GetHeight(Vector3 worldPos)
    {
        Vector3 local = worldPos - transform.position;
        Vector3 normal;

        Vector3 grid = new Vector3(local.x, 0f, local.z);
        for (int i = 0; i < 4; i++)
        {
            Vector3 offset = Displacement(grid, out normal);
            grid.x = local.x - offset.x;
            grid.z = local.z - offset.z;
        }

        return transform.position.y + Displacement(grid, out normal).y;
    }

    public float GetDepth(Vector3 worldPos)
    {
        return Mathf.Max(GetHeight(worldPos) - worldPos.y, 0f);
    }



    // Rendering

    private void Awake()
    {
        BuildGrid();
    }

    private void Update()
    {
        UpdateVertices();

        mMesh.vertices = mVertices;
        mMesh.normals = mNormals;
        mMesh.RecalculateBounds();
    }

    private void UpdateVertices()
    {
        for (int i = 0; i < mVertices.Length; i++)
        {
            mVertices[i] = mGrid[i] + Displacement(mGrid[i], out mNormals[i]);
        }
    }

       private void OnDrawGizmos()
    {
        if (!mDebugVertices || mVertices == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        foreach (Vector3 v in mVertices)
        {
            Gizmos.DrawSphere(transform.position + v, 0.1f);
        }
    }

    private void BuildGrid()
    {
        int count = mResolution + 1;
        mGrid = new Vector3[count * count];
        mVertices = new Vector3[count * count];
        mNormals = new Vector3[count * count];
        int[] triangles = new int[mResolution * mResolution * 6];

        // Vertices: a flat square grid centered on the object
        for (int z = 0; z < count; z++)
        {
            for (int x = 0; x < count; x++)
            {
                float px = (x / (float)mResolution - 0.5f) * mSize;
                float pz = (z / (float)mResolution - 0.5f) * mSize;
                mGrid[z * count + x] = new Vector3(px, 0f, pz);
            }
        }

        // Triangles: two per grid square
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

        mMesh = new Mesh();
        mMesh.MarkDynamic(); // tells Unity the vertices will change often
        mMesh.vertices = mGrid;

        mMesh.MarkDynamic(); // tells Unity the vertices will change often
        mMesh.vertices = mVertices;
        mMesh.triangles = triangles;
        mMesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mMesh;
    }
}
