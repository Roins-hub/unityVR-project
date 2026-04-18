using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[Serializable]
public class LiquidCustomPass : CustomPass
{
    public float radius = 6.47f;
    public LayerMask maskLayer = 0;
    public RenderQueueType renderQueueType = RenderQueueType.All;

    private const int BlurSampleCount = 9;
    private static readonly ShaderTagId[] ShaderTags =
    {
        new ShaderTagId("Forward"),
        new ShaderTagId("ForwardOnly"),
        new ShaderTagId("SRPDefaultUnlit"),
        new ShaderTagId("FirstPass"),
    };

    private RTHandle blurBuffer;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        blurBuffer = RTHandles.Alloc(
            Vector2.one * 0.5f,
            TextureXR.slices,
            colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
            filterMode: FilterMode.Bilinear,
            dimension: TextureXR.dimension,
            useDynamicScale: true,
            name: "Liquid Custom Pass Blur Buffer");
    }

    protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
    {
        cullingParameters.cullingMask |= (uint)GetEffectiveMask();
    }

    protected override void Execute(CustomPassContext ctx)
    {
        int effectiveMask = GetEffectiveMask();
        if (effectiveMask == 0)
            return;

        CustomPassUtils.DrawRenderers(
            ctx,
            ShaderTags,
            effectiveMask,
            renderQueueType,
            sorting: SortingCriteria.CommonTransparent);

        if (radius > 0f && blurBuffer != null)
        {
            CustomPassUtils.GaussianBlur(
                ctx,
                ctx.customColorBuffer.Value,
                ctx.customColorBuffer.Value,
                blurBuffer,
                BlurSampleCount,
                radius,
                downSample: true);
        }
    }

    protected override void Cleanup()
    {
        if (blurBuffer != null)
            RTHandles.Release(blurBuffer);

        blurBuffer = null;
    }

    private int GetEffectiveMask()
    {
        int configuredMask = maskLayer;
        return configuredMask != 0 ? configuredMask : LayerMask.GetMask("Liquid");
    }
}
