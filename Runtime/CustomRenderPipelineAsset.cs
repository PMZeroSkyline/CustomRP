using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField] private bool useDynamicBatching = true, useGPUInstancing = true, userSRPBatcher = true, useLightsPerObject = true;
    [SerializeField] private ShadowSettings shadows = default;
    [SerializeField] private PostFXSettings postFXSettings = default;
    [SerializeField] private bool allowHDR = true;

    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(allowHDR, useDynamicBatching, useGPUInstancing, userSRPBatcher, useLightsPerObject, shadows, postFXSettings);
    }
}