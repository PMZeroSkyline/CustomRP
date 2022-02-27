using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipline")]
public partial class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField] private bool useDynamicBatching = true, useGPUInstancing = true, userSRPBatcher = true, useLightsPerObject = true;
    [SerializeField] private ShadowSettings shadows = default;
    [SerializeField] private CameraBufferSettings cameraBuffer = new CameraBufferSettings
    {
        allowHDR = true,
        renderScale = 1f
    };
    [SerializeField] private PostFXSettings postFXSettings = default;
    public enum ColorLUTResolution
    {
        _16 = 16,
        _32 = 32,
        _64 = 64
    }
    [SerializeField] ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;
    [SerializeField] private Shader cameraRendererShader = default;

    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(cameraBuffer, useDynamicBatching, useGPUInstancing, userSRPBatcher, useLightsPerObject, shadows, postFXSettings, (int)colorLUTResolution, cameraRendererShader);
    }
}

