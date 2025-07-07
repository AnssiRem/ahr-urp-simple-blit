namespace AHR.Rendering
{
  using UnityEngine;
  using UnityEngine.Rendering;
  using UnityEngine.Rendering.RenderGraphModule;
  using UnityEngine.Rendering.RenderGraphModule.Util;
  using UnityEngine.Rendering.Universal;

  public class SimpleBlitRendererFeature : ScriptableRendererFeature
  {
    [SerializeField]
    private Material m_blitMaterial;
    [SerializeField]
    private RenderPassEvent m_injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
#if UNITY_EDITOR
    [SerializeField]
    private bool m_enableInSceneView;
#endif

    private ScriptableRenderPass m_pass;

    public override void Create()
    {
      if (m_blitMaterial == null)
      {
        return;
      }

      m_pass = new SimpleBlitPass(m_blitMaterial) { renderPassEvent = m_injectionPoint };
    }

    public override void AddRenderPasses(
      ScriptableRenderer renderer,
      ref RenderingData renderingData)
    {
      if (m_pass == null)
      {
        return;
      }

#if UNITY_EDITOR
      if (!m_enableInSceneView && (renderingData.cameraData.cameraType & CameraType.SceneView) == CameraType.SceneView)
      {
        return;
      }
#endif

      renderer.EnqueuePass(m_pass);
    }

    private class SimpleBlitPass : ScriptableRenderPass
    {
      private readonly Material m_material;

      public SimpleBlitPass(Material material)
      {
        m_material = material;
        requiresIntermediateTexture = true;
      }

      public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
      {
        var resourceData = frameData.Get<UniversalResourceData>();
        if (resourceData.isActiveTargetBackBuffer)
        {
          return;
        }

        var source = resourceData.activeColorTexture;

        var destinationDesc = renderGraph.GetTextureDesc(source);
        destinationDesc.name = $"CameraColor-{passName}";
        destinationDesc.clearBuffer = false;
        destinationDesc.msaaSamples = MSAASamples.None;
        destinationDesc.depthBufferBits = 0;
        var destination = renderGraph.CreateTexture(destinationDesc);

        var para = new RenderGraphUtils.BlitMaterialParameters(source, destination, m_material, 0);
        renderGraph.AddBlitPass(para, passName);

        resourceData.cameraColor = destination;
      }
    }
  }
}
