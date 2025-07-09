namespace AHR.Rendering
{
  using UnityEngine;
  using UnityEngine.Rendering;
  using UnityEngine.Rendering.RenderGraphModule;
  using UnityEngine.Rendering.RenderGraphModule.Util;
  using UnityEngine.Rendering.Universal;

  public class SimpleBlitRendererFeature : ScriptableRendererFeature
  {
    protected ScriptableRenderPass Pass;

    [SerializeField]
    private Material m_blitMaterial;
    [SerializeField]
    private RenderPassEvent m_injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
#if UNITY_EDITOR
    [SerializeField]
    private bool m_enableInSceneView;
#endif

    protected Material BlitMaterial => m_blitMaterial;

    protected RenderPassEvent InjectionPoint => m_injectionPoint;

#if UNITY_EDITOR
    protected bool EnableInSceneView => m_enableInSceneView;
#endif

    public override void Create()
    {
      if (m_blitMaterial == null)
      {
        return;
      }

      Pass = new SimpleBlitPass(m_blitMaterial) { renderPassEvent = m_injectionPoint };
    }

    public override void AddRenderPasses(
      ScriptableRenderer renderer,
      ref RenderingData renderingData)
    {
      if (Pass == null)
      {
        return;
      }

#if UNITY_EDITOR
      if (!m_enableInSceneView && (renderingData.cameraData.cameraType & CameraType.SceneView) == CameraType.SceneView)
      {
        return;
      }
#endif

      renderer.EnqueuePass(Pass);
    }

    protected class SimpleBlitPass : ScriptableRenderPass
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
