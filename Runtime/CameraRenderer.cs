﻿using UnityEngine;
using UnityEngine.Rendering;

partial class CameraRenderer
{
    
    private ScriptableRenderContext context;
    
    private Camera camera;
    
    private const string bufferName = "Render Camera";
    
    private CommandBuffer buffer = new CommandBuffer {name = bufferName};
    
    private CullingResults cullingResults;
    
    private static ShaderTagId 
        unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
        litShaderTagId = new ShaderTagId("CustomLit");

    private Lighting lighting = new Lighting();

    private PostFXStack postFXStack = new PostFXStack();

    private static int 
        //frameBufferId = Shader.PropertyToID("_CameraFrameBuffer"),
        colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment"),
        depthAttachmentId = Shader.PropertyToID("_cameraDepthAttachment"),
        colorTextureId = Shader.PropertyToID("_CameraColorTexture"),
        depthTextureId = Shader.PropertyToID("_CameraDepthTexture"),
        sourceTextureId = Shader.PropertyToID("_SourceTexture"),
        srcBlendId = Shader.PropertyToID("_CameraSrcBlend"),
        dstBlendId = Shader.PropertyToID("_CameraDstBlend");

    private bool useColorTexture, useDepthTexture, useIntermediateBuffer;
    private bool useHDR;

    private static CameraSettings defaultCameraSettings = new CameraSettings();

    private Material material;

    private Texture2D missingTexture;

    private static bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
    public CameraRenderer(Shader shader)
    {
        material = CoreUtils.CreateEngineMaterial(shader);
        missingTexture = new Texture2D(1, 1)
        {
            hideFlags = HideFlags.HideAndDontSave,
            name = "Missing"
        };
        missingTexture.SetPixel(0, 0, Color.white * 0.5f);
        missingTexture.Apply(true, true);
    }

    public void Dispose()
    {
        CoreUtils.Destroy(material);
        CoreUtils.Destroy(missingTexture);
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, bool isDepth = false)
    {
        buffer.SetGlobalTexture(sourceTextureId, from);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
    }
    void DrawFinal(CameraSettings.FinalBlendMode finalBlendMode)
    {
        buffer.SetGlobalFloat(srcBlendId, (float)finalBlendMode.source);
        buffer.SetGlobalFloat(dstBlendId, (float)finalBlendMode.destination);
        buffer.SetGlobalTexture(sourceTextureId, colorAttachmentId);
        buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, finalBlendMode.destination == BlendMode.Zero ?  RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);

        buffer.SetViewport(camera.pixelRect);
        buffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
        buffer.SetGlobalFloat(srcBlendId, 1f);
        buffer.SetGlobalFloat(dstBlendId, 0f);
    }
    public void Render(ScriptableRenderContext context, Camera camera, CameraBufferSettings bufferSettings, bool useDynamicBathcing, bool useGPUInstancing, bool useLightsPerObject, ShadowSettings shadowSettings, PostFXSettings postFXSettings, int colorLUTResolution)
    {
        this.context = context;
        this.camera = camera;
        var crpCamera = camera.GetComponent<CustomRenderPipelineCamera>();
        CameraSettings cameraSettings = crpCamera ? crpCamera.Settings : defaultCameraSettings;
        useDepthTexture = true;
        if (cameraSettings.overridePostFX)
        {
            postFXSettings = cameraSettings.postFXSettings;
        }
        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull(shadowSettings.maxDistance)) { return; }

        if (camera.cameraType == CameraType.Reflection)
        {
            useColorTexture = bufferSettings.copyColorReflection;
            useDepthTexture = bufferSettings.copyDepthReflections;
        }
        else
        {
            useColorTexture = bufferSettings.copyColor && cameraSettings.copyColor;
            useDepthTexture = bufferSettings.copyDepth && cameraSettings.copyDepth;
        }
        useHDR = bufferSettings.allowHDR && camera.allowHDR;
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings, useLightsPerObject, cameraSettings.maskLights ? cameraSettings.renderingLayerMask : -1);
        postFXStack.Setup(context, camera, postFXSettings, useHDR, colorLUTResolution, cameraSettings.finalBlendMode);
        buffer.EndSample(SampleName);
        Setup();
        DrawVisibleGeometry(useDynamicBathcing, useGPUInstancing, useLightsPerObject, cameraSettings.renderingLayerMask);
        DrawUnsupportedShaders();
        DrawGizmosBeforeFX();
        if (postFXStack.IsActive)
        {
            postFXStack.Render(colorAttachmentId);
        }
        else if(useIntermediateBuffer)
        {
            DrawFinal(cameraSettings.finalBlendMode);
            ExecuteBuffer();
        }
        DrawGizmosAfterFX();
        Cleanup();
        Submit();
    }

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject, int renderingLayerMask)
    {
        PerObjectData lightsPerObjectFlags = useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
        
        var sortingSettings = new SortingSettings();
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            enableDynamicBatching = useDynamicBatching, 
            enableInstancing = useGPUInstancing,
            perObjectData = PerObjectData.ReflectionProbes |
                            PerObjectData.Lightmaps |
                            PerObjectData.ShadowMask |
                            PerObjectData.LightProbe |
                            PerObjectData .OcclusionProbe |
                            PerObjectData.LightProbeProxyVolume |
                            PerObjectData.OcclusionProbeProxyVolume |
                            lightsPerObjectFlags
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId);

        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask : (uint)renderingLayerMask);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        
        context.DrawSkybox(camera);

        if (useColorTexture || useDepthTexture)
        {
            CopyAttachments();
        }
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;

        useIntermediateBuffer = useColorTexture || useDepthTexture || postFXStack.IsActive;
        if (useIntermediateBuffer)
        {
            if (flags > CameraClearFlags.Color)
            {
                flags = CameraClearFlags.Color;
            }
            buffer.GetTemporaryRT(colorAttachmentId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            buffer.GetTemporaryRT(depthAttachmentId, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Point, RenderTextureFormat.Depth);
            buffer.SetRenderTarget(colorAttachmentId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                depthAttachmentId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        }
        
        buffer.ClearRenderTarget(
        flags <= CameraClearFlags.Depth,
        flags == CameraClearFlags.Color,
        flags == CameraClearFlags.Color ?
            camera.backgroundColor.linear : Color.clear
        );
        buffer.BeginSample(SampleName);
        buffer.SetGlobalTexture(colorTextureId, missingTexture);
        buffer.SetGlobalTexture(depthTextureId, missingTexture);
        ExecuteBuffer();
    }
    
    void Submit()
    { 
        buffer.EndSample(SampleName);
        ExecuteBuffer();
         
        context.Submit();
    } 

    void ExecuteBuffer() 
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    bool Cull(float maxShadowDistance)
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }

    void CopyAttachments()
    {
        if (useColorTexture)
        {
            buffer.GetTemporaryRT(colorTextureId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            if (copyTextureSupported)
            {
                buffer.CopyTexture(colorAttachmentId, colorTextureId);
            }
            else
            {
                Draw(colorAttachmentId, colorTextureId);
            }
        }
        if (useDepthTexture)
        {
            buffer.GetTemporaryRT(depthTextureId, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Point, RenderTextureFormat.Depth);
            if (copyTextureSupported)
            {
                buffer.CopyTexture(depthAttachmentId, depthTextureId);
            }
            else
            {
                Draw(depthAttachmentId, depthTextureId, true);
            }
        }

        if (!copyTextureSupported)
        {
            buffer.SetRenderTarget(colorAttachmentId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                depthAttachmentId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        
        }
        ExecuteBuffer();

    }
    void Cleanup()
    {
        lighting.Cleanup();
        if (useIntermediateBuffer)
        {
            buffer.ReleaseTemporaryRT(colorAttachmentId);
            buffer.ReleaseTemporaryRT(depthAttachmentId);

            if (useColorTexture)
            {
                buffer.ReleaseTemporaryRT(colorTextureId);
            }
            if (useDepthTexture)
            {
                buffer.ReleaseTemporaryRT(depthTextureId);
            }
        
        }

    }
}
