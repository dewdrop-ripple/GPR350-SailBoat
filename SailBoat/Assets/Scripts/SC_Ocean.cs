using UnityEngine;
using UnityEngine.Rendering;

// GPU ocean: wind -> JONSWAP spectrum -> inverse FFT -> displacement textures.
// OceanSurface.shader draws them, and the boat reads the swell back with GetHeight, GetDepth and GetVelocity.
public class Ocean : MonoBehaviour
{
    // SETTINGS

    [SerializeField] private ComputeShader mFFT;
    [SerializeField] private float mFetch = 250000f; // m, how far the wind has blown over open water
    [SerializeField] private float mSpread = 5f; // how much waves line up with the wind
    [SerializeField] private float mHeightScale = 1.25f;
    [SerializeField] private float mChoppiness = 1.5f; // higher = sharper crests

    private const float Gravity = 9.81f;
    private const int Resolution = 256; // pixels per tile side
    private const float SwellTile = 500f; // m, waves longer than about 4 m: they move the mesh and the boat
    private const float RippleTile = 40f; // m, shorter waves: they only change the lighting
    private const float PeakSharpness = 3.3f; // JONSWAP gamma
    private const float ShortWaveFade = 0.1f; // fades waves too short for a tile to draw smoothly
    private const float WindResponseTime = 5f; // s, the sea follows the wind slowly


    // STATE

    private class Cascade
    {
        public float patchSize; // m
        public float kMin, kMax; // rad/m, the wave sizes this tile makes
        public Texture2D gauss; // the random numbers behind the waves
        public RenderTexture displacement; // m: x push, height, z push
    }

    private WindManager mWindManager;
    private Vector2 mWind = new Vector2(0f, 7f); // m/s, eases toward the WindManager's wind
    private Cascade[] mCascades;
    private RenderTexture mH0, mA, mB, mC, mD; // scratch textures every tile reuses
    private int mSpectrumKernel, mEvolveKernel, mStageKernel, mFinishKernel;

    // the swell tile on the CPU, from the latest readback and the one before it
    private Vector4[] mSwell, mSwellBefore;
    private float mSwellTime, mSwellBeforeTime, mRequestTime;
    private int mReadbacks;
    private bool mReadbackPending;


    // SETUP

    private void Start()
    {
        mWindManager = FindAnyObjectByType<WindManager>();

        mSpectrumKernel = mFFT.FindKernel("Spectrum");
        mEvolveKernel = mFFT.FindKernel("Evolve");
        mStageKernel = mFFT.FindKernel("FFTStage");
        mFinishKernel = mFFT.FindKernel("Finish");

        mH0 = MakeTexture(RenderTextureFormat.RGFloat);
        mA = MakeTexture(RenderTextureFormat.RGFloat);
        mB = MakeTexture(RenderTextureFormat.RGFloat);
        mC = MakeTexture(RenderTextureFormat.RGFloat);
        mD = MakeTexture(RenderTextureFormat.RGFloat);

        // the swell tile stops at the smallest wave it can show, the ripple tile takes over from there
        float splitK = Mathf.PI * Resolution / SwellTile;
        mCascades = new Cascade[]
        {
            MakeCascade(SwellTile, 0f, splitK, 1),
            MakeCascade(RippleTile, splitK, 1e9f, 2)
        };

        mSwell = new Vector4[Resolution * Resolution];
        mSwellBefore = new Vector4[Resolution * Resolution];

        // read by OceanSurface.shader
        for (int i = 0; i < mCascades.Length; i++)
        {
            Shader.SetGlobalTexture("_OceanDisplacement" + i, mCascades[i].displacement);
            Shader.SetGlobalFloat("_OceanPatch" + i, mCascades[i].patchSize);
            Shader.SetGlobalFloat("_OceanTexel" + i, mCascades[i].patchSize / Resolution);
        }
    }

    private void OnDestroy()
    {
        mH0.Release();
        mA.Release();
        mB.Release();
        mC.Release();
        mD.Release();

        foreach (Cascade cascade in mCascades)
        {
            cascade.displacement.Release();
            Destroy(cascade.gauss);
        }
    }

    private RenderTexture MakeTexture(RenderTextureFormat format)
    {
        RenderTexture texture = new RenderTexture(Resolution, Resolution, 0, format);
        texture.enableRandomWrite = true;
        texture.Create();
        return texture;
    }

    private Cascade MakeCascade(float patchSize, float kMin, float kMax, int seed)
    {
        Cascade cascade = new Cascade();
        cascade.patchSize = patchSize;
        cascade.kMin = kMin;
        cascade.kMax = kMax;

        cascade.gauss = new Texture2D(Resolution, Resolution, TextureFormat.RGFloat, false);
        cascade.gauss.filterMode = FilterMode.Point;
        cascade.gauss.SetPixelData(MakeGaussians(seed), 0);
        cascade.gauss.Apply();

        // sampled by world position, so it repeats and blends
        cascade.displacement = MakeTexture(RenderTextureFormat.ARGBFloat);
        cascade.displacement.wrapMode = TextureWrapMode.Repeat;
        cascade.displacement.filterMode = FilterMode.Bilinear;

        return cascade;
    }

    // real and imaginary parts, index = z * Resolution + x
    private Vector2[] MakeGaussians(int seed)
    {
        System.Random rng = new System.Random(seed);
        Vector2[] gauss = new Vector2[Resolution * Resolution];

        for (int i = 0; i < gauss.Length; i++)
        {
            gauss[i] = new Vector2(Gaussian(rng), Gaussian(rng));
        }

        return gauss;
    }

    private static float Gaussian(System.Random rng)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = rng.NextDouble();
        return (float)(System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Cos(2.0 * System.Math.PI * u2));
    }


    // EVERY FRAME

    private void Update()
    {
        FollowWind();
        RunFFT();
        RequestReadback();
    }

    private void FollowWind()
    {
        Vector3 target = mWindManager.GetWind();
        mWind += (new Vector2(target.x, target.z) - mWind) * (1f - Mathf.Exp(-Time.deltaTime / WindResponseTime));
    }

    private void RunFFT()
    {
        // JONSWAP constants from wind speed and fetch
        float windSpeed = mWind.magnitude;
        mFFT.SetFloat("Alpha", 0.076f * Mathf.Pow(windSpeed * windSpeed / (mFetch * Gravity), 0.22f));
        mFFT.SetFloat("PeakW", 22f * Mathf.Pow(Gravity * Gravity / (windSpeed * mFetch), 1f / 3f));

        mFFT.SetInt("N", Resolution);
        mFFT.SetFloat("SimTime", Time.time);
        mFFT.SetVector("WindDirection", mWind.normalized);
        mFFT.SetFloat("PeakSharpness", PeakSharpness);
        mFFT.SetFloat("Spread", mSpread);
        mFFT.SetFloat("SpreadNorm", SpreadNormalization());
        mFFT.SetFloat("ShortWaveFade", ShortWaveFade);
        mFFT.SetFloat("HeightScale", mHeightScale);
        mFFT.SetFloat("Choppiness", mChoppiness);

        foreach (Cascade cascade in mCascades)
        {
            RunCascade(cascade);
        }
    }

    private void RunCascade(Cascade cascade)
    {
        int groups = Resolution / 8;

        mFFT.SetFloat("PatchSize", cascade.patchSize);
        mFFT.SetFloat("KMin", cascade.kMin);
        mFFT.SetFloat("KMax", cascade.kMax);

        mFFT.SetTexture(mSpectrumKernel, "Gauss", cascade.gauss);
        mFFT.SetTexture(mSpectrumKernel, "Output", mH0);
        mFFT.Dispatch(mSpectrumKernel, groups, groups, 1);

        mFFT.SetTexture(mEvolveKernel, "Input", mH0);
        mFFT.SetTexture(mEvolveKernel, "Output", mA);
        mFFT.SetTexture(mEvolveKernel, "Output2", mC);
        mFFT.Dispatch(mEvolveKernel, groups, groups, 1);

        // two FFTs: height + x push, then z push
        RenderTexture first = InverseFFT(mA, mB);
        RenderTexture second = InverseFFT(mC, mD);

        mFFT.SetTexture(mFinishKernel, "Input", first);
        mFFT.SetTexture(mFinishKernel, "Input2", second);
        mFFT.SetTexture(mFinishKernel, "Displacement", cascade.displacement);
        mFFT.Dispatch(mFinishKernel, groups, groups, 1);
    }

    // returns whichever texture holds the result
    private RenderTexture InverseFFT(RenderTexture from, RenderTexture to)
    {
        int groups = Resolution / 8;

        for (int vertical = 0; vertical < 2; vertical++)
        {
            mFFT.SetInt("Vertical", vertical);

            for (int ns = 1; ns < Resolution; ns *= 2)
            {
                mFFT.SetInt("Ns", ns);
                mFFT.SetTexture(mStageKernel, "Input", from);
                mFFT.SetTexture(mStageKernel, "Output", to);
                mFFT.Dispatch(mStageKernel, groups / 2, groups, 1);

                RenderTexture swap = from;
                from = to;
                to = swap;
            }
        }

        return from;
    }

    // makes cos^spread add up to 1
    private float SpreadNormalization()
    {
        int steps = 720;
        float sum = 0f;
        for (int i = 0; i < steps; i++)
        {
            float angle = -Mathf.PI / 2f + (i + 0.5f) * Mathf.PI / steps;
            sum += Mathf.Pow(Mathf.Cos(angle), mSpread);
        }
        return sum * Mathf.PI / steps;
    }


    // BOAT QUERIES

    public float GetHeight(Vector3 worldPos)
    {
        return transform.position.y + Bilinear(mSwell, RestPoint(worldPos)).y;
    }

    public float GetDepth(Vector3 worldPos)
    {
        return Mathf.Max(GetHeight(worldPos) - worldPos.y, 0f);
    }

    // how much the push changed between the last two readbacks, per second
    public Vector3 GetVelocity(Vector3 worldPos)
    {
        if (mReadbacks < 2)
        {
            return Vector3.zero;
        }

        Vector2 rest = RestPoint(worldPos);
        Vector4 change = Bilinear(mSwell, rest) - Bilinear(mSwellBefore, rest);
        return new Vector3(change.x, change.y, change.z) / (mSwellTime - mSwellBeforeTime);
    }

    // one request at a time, so they never pile up
    private void RequestReadback()
    {
        if (mReadbackPending)
        {
            return;
        }

        mReadbackPending = true;
        mRequestTime = Time.unscaledTime; // keeps moving when the game is paused, so the velocity never divides by zero
        AsyncGPUReadback.Request(mCascades[0].displacement, 0, OnReadback);
    }

    private void OnReadback(AsyncGPUReadbackRequest request)
    {
        mReadbackPending = false;

        if (request.hasError)
        {
            return;
        }

        Vector4[] spare = mSwellBefore;
        mSwellBefore = mSwell;
        mSwellBeforeTime = mSwellTime;
        mSwell = spare;
        request.GetData<Vector4>().CopyTo(mSwell);
        mSwellTime = mRequestTime;
        mReadbacks++;
    }

    // the waves push points sideways, so find the flat grid point that ends up under this spot
    private Vector2 RestPoint(Vector3 worldPos)
    {
        Vector2 target = new Vector2(worldPos.x, worldPos.z);
        Vector2 rest = target;

        for (int i = 0; i < 4; i++)
        {
            Vector4 push = Bilinear(mSwell, rest);
            rest = target - new Vector2(push.x, push.z);
        }

        return rest;
    }

    // bilinear sample of the swell tile at a point of the flat grid
    private Vector4 Bilinear(Vector4[] data, Vector2 restXZ)
    {
        // pixel position in the repeating tile, pixel values sit at their centers
        float x = Mathf.Repeat(restXZ.x / SwellTile * Resolution - 0.5f, Resolution);
        float z = Mathf.Repeat(restXZ.y / SwellTile * Resolution - 0.5f, Resolution);

        int x0 = Mathf.FloorToInt(x) % Resolution;
        int z0 = Mathf.FloorToInt(z) % Resolution;
        int x1 = (x0 + 1) % Resolution;
        int z1 = (z0 + 1) % Resolution;
        float fx = x - Mathf.Floor(x);
        float fz = z - Mathf.Floor(z);

        Vector4 bottom = Vector4.Lerp(data[z0 * Resolution + x0], data[z0 * Resolution + x1], fx);
        Vector4 top = Vector4.Lerp(data[z1 * Resolution + x0], data[z1 * Resolution + x1], fx);
        return Vector4.Lerp(bottom, top, fz);
    }
}
