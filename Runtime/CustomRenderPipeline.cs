using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{
    CameraRenderer renderer;

    private bool useDynamicBatching, useGPUInstancing, useLightsPerObject;

    private ShadowSettings shadowSettings;

    private PostFXSettings postFXSettings;

    private int colorLUTResolution;

    private CameraBufferSettings cameraBufferSettings;

    public CustomRenderPipeline(CameraBufferSettings cameraBufferSettings, bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, bool useLightsPerObject, ShadowSettings shadowSettings, PostFXSettings postFXSettings, int colorLUTResolution, Shader cameraRendererShader)
    {
        this.colorLUTResolution = colorLUTResolution;
        this.cameraBufferSettings = cameraBufferSettings;
        this.postFXSettings = postFXSettings;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.shadowSettings = shadowSettings;
        this.useLightsPerObject = useLightsPerObject;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        InitializeForEditor();
        renderer = new CameraRenderer(cameraRendererShader);
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera, cameraBufferSettings, useDynamicBatching, useGPUInstancing, useLightsPerObject, shadowSettings, postFXSettings, colorLUTResolution);
        }
    }
}
