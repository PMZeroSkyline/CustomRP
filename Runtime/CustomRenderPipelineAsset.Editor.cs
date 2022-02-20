using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

partial class CustomRenderPipelineAsset
{
    #if UNITY_EDITOR

    private static string[] renderingLayerNames = new string[31];

    static CustomRenderPipelineAsset()
    {
        renderingLayerNames = new string[31];
        for (int i = 0; i < renderingLayerNames.Length; i++)
        {
            renderingLayerNames[i] = "Layer" + (i + 1);
        }
    }

    public override string[] renderingLayerMaskNames => renderingLayerNames;
    

#endif
}
