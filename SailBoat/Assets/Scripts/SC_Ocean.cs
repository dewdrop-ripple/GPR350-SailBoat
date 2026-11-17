using UnityEngine;

public class Ocean : MonoBehaviour
{
    private const float Gravity = 9.81f;

    [SerializeField] private float mWindSpeed = 5f;
    [SerializeField] private float mFetch = 250000f; // how far the wind has blown over open water
    [SerializeField] private Vector2 mWindDirection = new Vector2(0f, 0f);
    [SerializeField] private float mPeakSharpness = 3.3f; // JONSWAP gamma
    [SerializeField] private float mSpread = 5f; // how much waves line up with wind
    [SerializeField] private int mSeed = 1;
    [SerializeField] private int mResolution = 256;
    [SerializeField] private float mPatchSize = 500f;
    [SerializeField] private float mRipplePatchSize = 40f;
    [SerializeField] private float mWindResponseTime = 5f;
    [SerializeField] private float mHeightScale = 1f;
    [SerializeField] private float mChoppiness = 1.5f;

    [SerializeField] private ComputeShader mFFT;

    private class Cascade
    {
        public float patchSize;
        public float kMin, kMax; // the wave sizes this tile makes
        public Texture2D gauss;
        public RenderTexture displacement;
    }

    private WindManager mWindManager;
    private Cascade[] mCascades;
    private RenderTexture mH0, mA, mB, mC, mD;
    private int mSpectrumKernel, mEvolveKernel, mStageKernel, mFinishKernel;

    private void Start()
    {
        mWindManager = FindAnyObjectByType<WindManager>();

        if (mFFT == null)
        {
            Debug.LogWarning("Youre missing this shader");
            return;
        }

        mSpectrumKernel = mFFT.FindKernel("Spectrum");
        mEvolveKernel = mFFT.FindKernel("Evolve");
        mStageKernel = mFFT.FindKernel("FFTStage");
        mFinishKernel = mFFT.FindKernel("Finish");

        mH0 = MakeTexture(RenderTextureFormat.RGFloat);
        mA = MakeTexture(RenderTextureFormat.RGFloat);
        mB = MakeTexture(RenderTextureFormat.RGFloat);
        mC = MakeTexture(RenderTextureFormat.RGFloat);
        mD = MakeTexture(RenderTextureFormat.RGFloat);

        // the big tile stops where the small one takes over
        float splitK = Mathf.PI * mResolution / mPatchSize;
        mCascades = new Cascade[]
        {
            MakeCascade(mPatchSize, 0f, splitK, mSeed),
            MakeCascade(mRipplePatchSize, splitK, 1e9f, mSeed + 1)
        };

        // read by OceanSurface.shader
        for (int i = 0; i < mCascades.Length; i++)
        {
            Shader.SetGlobalTexture("_OceanDisplacement" + i, mCascades[i].displacement);
            Shader.SetGlobalFloat("_OceanPatch" + i, mCascades[i].patchSize);
            Shader.SetGlobalFloat("_OceanTexel" + i, mCascades[i].patchSize / mResolution);
        }
    }

    private void Update()
    {
        FollowWind();
        RunFFT();
    }

    // the sea follows the wind slowly
    private void FollowWind()
    {
        if (mWindManager == null)
        {
            return;
        }

        Vector3 target = mWindManager.GetWind();
        Vector2 wind = mWindDirection.normalized * mWindSpeed;
        wind += (new Vector2(target.x, target.z) - wind) * (1f - Mathf.Exp(-Time.deltaTime / mWindResponseTime));

        mWindSpeed = Mathf.Max(wind.magnitude, 1f);
        mWindDirection = wind.normalized;
    }

    private RenderTexture MakeTexture(RenderTextureFormat format)
    {
        RenderTexture texture = new RenderTexture(mResolution, mResolution, 0, format);
        texture.enableRandomWrite = true;
        texture.Create();
        return texture;
    }

    private Cascade MakeCascade(float patchSize, float kMin, float kMax, int seed)
    {
        int n = mResolution;
        Cascade cascade = new Cascade();
        cascade.patchSize = patchSize;
        cascade.kMin = kMin;
        cascade.kMax = kMax;

        cascade.gauss = new Texture2D(n, n, TextureFormat.RGFloat, false);
        cascade.gauss.filterMode = FilterMode.Point;
        cascade.gauss.SetPixelData(MakeGaussians(seed), 0);
        cascade.gauss.Apply();

        // sampled by world position, so it repeats and blends
        cascade.displacement = MakeTexture(RenderTextureFormat.ARGBFloat);
        cascade.displacement.wrapMode = TextureWrapMode.Repeat;
        cascade.displacement.filterMode = FilterMode.Bilinear;

        return cascade;
    }

    private void RunFFT()
    {
        if (mCascades == null)
        {
            return;
        }

        mFFT.SetInt("N", mResolution);
        mFFT.SetFloat("SimTime", Time.time);
        mFFT.SetVector("WindDirection", mWindDirection.normalized);
        mFFT.SetFloat("Alpha", Alpha());
        mFFT.SetFloat("PeakW", PeakFrequency());
        mFFT.SetFloat("PeakSharpness", mPeakSharpness);
        mFFT.SetFloat("Spread", mSpread);
        mFFT.SetFloat("HeightScale", mHeightScale);
        mFFT.SetFloat("SpreadNorm", SpreadNormalization());
        mFFT.SetFloat("Choppiness", mChoppiness);

        foreach (Cascade cascade in mCascades)
        {
            RunCascade(cascade);
        }
    }

    private void RunCascade(Cascade cascade)
    {
        int groups = mResolution / 8;

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
        int n = mResolution;
        int groups = n / 8;

        for (int vertical = 0; vertical < 2; vertical++)
        {
            mFFT.SetInt("Vertical", vertical);

            for (int ns = 1; ns < n; ns *= 2)
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

    private void OnDestroy()
    {
        if (mCascades == null)
        {
            return;
        }

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


    private float Alpha()
    {
        return 0.076f * Mathf.Pow(mWindSpeed * mWindSpeed / (mFetch * Gravity), 0.22f);
    }

    private float PeakFrequency()
    {
        return 22f * Mathf.Pow(Gravity * Gravity / (mWindSpeed * mFetch), 1f / 3f);
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

    private static float Gaussian(System.Random rng)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = rng.NextDouble();
        return (float)(System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Cos(2.0 * System.Math.PI * u2));
    }

    // real and imaginary parts, index = z * N + x
    private Vector2[] MakeGaussians(int seed)
    {
        System.Random rng = new System.Random(seed);
        Vector2[] gauss = new Vector2[mResolution * mResolution];

        for (int i = 0; i < gauss.Length; i++)
        {
            gauss[i] = new Vector2(Gaussian(rng), Gaussian(rng));
        }

        return gauss;
    }
}
