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

float RelativeDepthDelta(float centerDepth, float sampleDepth)
{
    float nearestDepth = max(min(centerDepth, sampleDepth), 0.0001);
    return abs(centerDepth - sampleDepth) / nearestDepth;
}

void DepthBasedOutlines_float(float2 screenUV, float2 pixelSize, out float outlines)
{
    float2 uv = saturate(screenUV);
    float centerDepth = SampleLinearEyeDepth(uv);
    float maxDepthDelta = 0.0;

    maxDepthDelta = max(maxDepthDelta, RelativeDepthDelta(centerDepth, SampleLinearEyeDepth(saturate(uv + float2(-pixelSize.x, 0.0)))));
    maxDepthDelta = max(maxDepthDelta, RelativeDepthDelta(centerDepth, SampleLinearEyeDepth(saturate(uv + float2(pixelSize.x, 0.0)))));
    maxDepthDelta = max(maxDepthDelta, RelativeDepthDelta(centerDepth, SampleLinearEyeDepth(saturate(uv + float2(0.0, pixelSize.y)))));
    maxDepthDelta = max(maxDepthDelta, RelativeDepthDelta(centerDepth, SampleLinearEyeDepth(saturate(uv + float2(0.0, -pixelSize.y)))));
    maxDepthDelta = max(maxDepthDelta, RelativeDepthDelta(centerDepth, SampleLinearEyeDepth(saturate(uv + float2(-pixelSize.x, pixelSize.y)))));
    maxDepthDelta = max(maxDepthDelta, RelativeDepthDelta(centerDepth, SampleLinearEyeDepth(saturate(uv + float2(pixelSize.x, pixelSize.y)))));
    maxDepthDelta = max(maxDepthDelta, RelativeDepthDelta(centerDepth, SampleLinearEyeDepth(saturate(uv + float2(-pixelSize.x, -pixelSize.y)))));
    maxDepthDelta = max(maxDepthDelta, RelativeDepthDelta(centerDepth, SampleLinearEyeDepth(saturate(uv + float2(pixelSize.x, -pixelSize.y)))));

    const float depthThreshold = 0.015;
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
    float2 uv = saturate(screenUV);
    float3 centerNormal = normalize(SampleSceneNormals(uv));
    float maxNormalDelta = 0.0;

    maxNormalDelta = max(maxNormalDelta, 1.0 - dot(centerNormal, normalize(SampleSceneNormals(saturate(uv + float2(-pixelSize.x, 0.0))))));
    maxNormalDelta = max(maxNormalDelta, 1.0 - dot(centerNormal, normalize(SampleSceneNormals(saturate(uv + float2(pixelSize.x, 0.0))))));
    maxNormalDelta = max(maxNormalDelta, 1.0 - dot(centerNormal, normalize(SampleSceneNormals(saturate(uv + float2(0.0, pixelSize.y))))));
    maxNormalDelta = max(maxNormalDelta, 1.0 - dot(centerNormal, normalize(SampleSceneNormals(saturate(uv + float2(0.0, -pixelSize.y))))));
    maxNormalDelta = max(maxNormalDelta, 1.0 - dot(centerNormal, normalize(SampleSceneNormals(saturate(uv + float2(-pixelSize.x, pixelSize.y))))));
    maxNormalDelta = max(maxNormalDelta, 1.0 - dot(centerNormal, normalize(SampleSceneNormals(saturate(uv + float2(pixelSize.x, pixelSize.y))))));
    maxNormalDelta = max(maxNormalDelta, 1.0 - dot(centerNormal, normalize(SampleSceneNormals(saturate(uv + float2(-pixelSize.x, -pixelSize.y))))));
    maxNormalDelta = max(maxNormalDelta, 1.0 - dot(centerNormal, normalize(SampleSceneNormals(saturate(uv + float2(pixelSize.x, -pixelSize.y))))));

    outlines = smoothstep(0.05, 0.2, maxNormalDelta);
}

#else

void NormalBasedOutlines_float(float2 screenUV, float2 pixelSize, out float outlines)
{
    outlines = 0.0;
}

#endif
