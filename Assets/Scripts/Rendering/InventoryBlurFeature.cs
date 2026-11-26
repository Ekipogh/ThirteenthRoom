using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public sealed class InventoryBlurFeature : ScriptableRendererFeature
{
    [SerializeField] Shader blurShader;
    Material _material;
    BlurPass _pass;

    public override void Create()
    {
        CoreUtils.Destroy(_material);
        _material = blurShader != null ? CoreUtils.CreateEngineMaterial(blurShader) : null;
        _pass = new BlurPass(_material);
    }

    public static bool TryGetRadius(Camera camera, out float radius)
    {
        radius = 0f;
        if (camera == null || camera.cameraType != CameraType.Game ||
            !camera.TryGetComponent<InventoryBackgroundBlur>(out var blur) || !blur.IsActive) return false;
        radius = blur.Radius;
        return true;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null || renderingData.cameraData.renderType != CameraRenderType.Base ||
            !TryGetRadius(renderingData.cameraData.camera, out float radius)) return;
        _pass.SetRadius(radius);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_material);
        _material = null;
        _pass = null;
    }

    sealed class BlurPass : ScriptableRenderPass
    {
        readonly Material _material;
        readonly MaterialPropertyBlock _parameters = new();
        static readonly int SampleRadius = Shader.PropertyToID("_SampleRadius");
        float _radius;

        public BlurPass(Material material)
        {
            _material = material;
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            // Requested only by this enqueued pass, so closed inventory has no intermediate-copy cost.
            requiresIntermediateTexture = true;
        }
        public void SetRadius(float radius) => _radius = radius;

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer || _material == null) return;
            var source = resources.activeColorTexture;
            var full = renderGraph.GetTextureDesc(source);
            full.clearBuffer = false;
            full.depthBufferBits = DepthBits.None;
            full.msaaSamples = MSAASamples.None;
            var half = full;
            half.width = Mathf.Max(1, full.width / 2);
            half.height = Mathf.Max(1, full.height / 2);
            half.name = "InventoryBlur.Downsample";
            var downsample = renderGraph.CreateTexture(half);
            half.name = "InventoryBlur.Horizontal";
            var horizontal = renderGraph.CreateTexture(half);
            full.name = "InventoryBlur.Output";
            var output = renderGraph.CreateTexture(full);

            // Five bilinear Gaussian samples reach the configured radius, measured at 1080p.
            _parameters.SetFloat(SampleRadius, _radius * full.height / 1080f * .5f / 3.230769f);
            renderGraph.AddBlitPass(source, downsample, Vector2.one, Vector2.zero, passName: "Inventory Blur / Downsample");
            var horizontalParameters = new RenderGraphUtils.BlitMaterialParameters(downsample, horizontal, _material, 0)
            { propertyBlock = _parameters };
            var verticalParameters = new RenderGraphUtils.BlitMaterialParameters(horizontal, output, _material, 1)
            { propertyBlock = _parameters };
            renderGraph.AddBlitPass(horizontalParameters, passName: "Inventory Blur / Horizontal");
            renderGraph.AddBlitPass(verticalParameters, passName: "Inventory Blur / Vertical");
            resources.cameraColor = output;
        }
    }
}
