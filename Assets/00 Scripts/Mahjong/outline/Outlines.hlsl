// ==========================================
// File: Outlines.hlsl
// Unity URP Full Screen Outline Shader
// ==========================================

float SampleLinearEyeDepth(float2 uv)
{
    float rawDepth = SampleSceneDepth(uv);
    return LinearEyeDepth(rawDepth, _ZBufferParams);
}

// ------------------------------------------
// 1. Edge Detection dựa trên Depth (Độ sâu)
// ------------------------------------------
#if defined(UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED)

void DepthBasedOutlines_float(float2 screenUV, float2 pixelSize, out float outlines)
{
    outlines = 0.0;

    float centerDepth = SampleLinearEyeDepth(screenUV);
    float leftDepth = SampleLinearEyeDepth(screenUV + float2(-pixelSize.x, 0.0));
    float rightDepth = SampleLinearEyeDepth(screenUV + float2(pixelSize.x, 0.0));
    float upDepth = SampleLinearEyeDepth(screenUV + float2(0.0, pixelSize.y));
    float downDepth = SampleLinearEyeDepth(screenUV + float2(0.0, -pixelSize.y));

    float maxDepthDelta = 0.0;
    maxDepthDelta = max(maxDepthDelta, abs(centerDepth - leftDepth));
    maxDepthDelta = max(maxDepthDelta, abs(centerDepth - rightDepth));
    maxDepthDelta = max(maxDepthDelta, abs(centerDepth - upDepth));
    maxDepthDelta = max(maxDepthDelta, abs(centerDepth - downDepth));

    float depthThreshold = max(0.01, centerDepth * 0.015);
    outlines = smoothstep(depthThreshold, depthThreshold * 2.0, maxDepthDelta);
}

#else

void DepthBasedOutlines_float(float2 screenUV, float2 pixelSize, out float outlines)
{
    outlines = 0.0;
}

#endif

// ------------------------------------------
// 2. Edge Detection dựa trên Normals (Pháp tuyến)
// ------------------------------------------
#if defined(UNITY_DECLARE_NORMALS_TEXTURE_INCLUDED)

void NormalBasedOutlines_float(float2 screenUV, float2 pixelSize, out float outlines)
{
    outlines = 0.0;

    float3 centerNormal = normalize(SampleSceneNormals(screenUV));
    float3 leftNormal = normalize(SampleSceneNormals(screenUV + float2(-pixelSize.x, 0.0)));
    float3 rightNormal = normalize(SampleSceneNormals(screenUV + float2(pixelSize.x, 0.0)));
    float3 upNormal = normalize(SampleSceneNormals(screenUV + float2(0.0, pixelSize.y)));
    float3 downNormal = normalize(SampleSceneNormals(screenUV + float2(0.0, -pixelSize.y)));

    float maxNormalDelta = 0.0;
    maxNormalDelta = max(maxNormalDelta, 1.0 - dot(centerNormal, leftNormal));
    maxNormalDelta = max(maxNormalDelta, 1.0 - dot(centerNormal, rightNormal));
    maxNormalDelta = max(maxNormalDelta, 1.0 - dot(centerNormal, upNormal));
    maxNormalDelta = max(maxNormalDelta, 1.0 - dot(centerNormal, downNormal));

    outlines = smoothstep(0.05, 0.2, maxNormalDelta);
}

#else

void NormalBasedOutlines_float(float2 screenUV, float2 pixelSize, out float outlines)
{
    outlines = 0.0;
}

#endif
