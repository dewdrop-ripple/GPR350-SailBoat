using UnityEngine;

public class Water : MonoBehaviour
{
    [SerializeField] private float mSize = 200f;    // m
    [SerializeField] private int mResolution = 100; // quads per side

    private Mesh mMesh;
    private Vector3[] mVertices;
    private float[] mHeights;


    public float GetHeight(Vector3 worldPos)
    {
        Vector3 local = worldPos - transform.position;
        float gx = Mathf.Clamp((local.x / mSize + 0.5f) * mResolution, 0f, mResolution - 0.001f);
        float gz = Mathf.Clamp((local.z / mSize + 0.5f) * mResolution, 0f, mResolution - 0.001f);

        // The grid square it's in, and how far across that square
        int x = (int)gx;
        int z = (int)gz;
        float fx = gx - x;
        float fz = gz - z;

        // Blend the 4 corner heights
        float bottom = Mathf.Lerp(HeightAt(x, z), HeightAt(x + 1, z), fx);
        float top = Mathf.Lerp(HeightAt(x, z + 1), HeightAt(x + 1, z + 1), fx);

        return transform.position.y + Mathf.Lerp(bottom, top, fz);
    }

    private float HeightAt(int x, int z)
    {
        return mHeights[z * (mResolution + 1) + x];
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
        UpdateHeights();

        for (int i = 0; i < mVertices.Length; i++)
        {
            mVertices[i].y = mHeights[i];
        }

        mMesh.vertices = mVertices;
        mMesh.RecalculateNormals();
        mMesh.RecalculateBounds();
    }

    private void UpdateHeights()
    {
        for (int i = 0; i < mHeights.Length; i++)
        {
            mHeights[i] = Mathf.Sin(mVertices[i].x * 0.2f + Time.time - 1) * 0.2f +
                            Mathf.Cos(mVertices[i].z * 0.2f + Time.time - 2) * 0.2f +
                            Mathf.Sin(mVertices[i].x * 0.5f + Time.time - 3) * 0.1f +
                            Mathf.Cos(mVertices[i].z * 0.5f + Time.time - 4) * 0.1f; //+
                            //Mathf.Cos(mVertices[i].z * 0.0000001f + Time.time/3 + 1) * 15f + 5;
        }
    }

    private void BuildGrid()
    {
        int count = mResolution + 1;
        mVertices = new Vector3[count * count];
        mHeights = new float[mVertices.Length];
        int[] triangles = new int[mResolution * mResolution * 6];

        // Vertices: a flat square grid centered on the object
        for (int z = 0; z < count; z++)
        {
            for (int x = 0; x < count; x++)
            {
                float px = (x / (float)mResolution - 0.5f) * mSize;
                float pz = (z / (float)mResolution - 0.5f) * mSize;
                mVertices[z * count + x] = new Vector3(px, 0f, pz);
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
        mMesh.vertices = mVertices;
        mMesh.triangles = triangles;
        mMesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mMesh;
    }
}
