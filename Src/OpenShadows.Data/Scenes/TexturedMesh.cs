using System.Numerics;
using Veldrid.Utilities;
using Veldrid;
using OpenShadows.Data.Rendering.ImageSharp;
using OpenShadows.Data.Rendering;

namespace OpenShadows.Data.Scenes
{
    public class TexturedMesh : CullRenderable
    {
        // Useful for testing uniform bindings with an offset.
        private static readonly bool s_useUniformOffset = false;
        private uint _uniformOffset = 0;

        private readonly string _name;
        private readonly MeshData _meshData;
        private readonly ImageSharpTexture _textureData;
        private readonly ImageSharpTexture _alphaTextureData;
        private readonly Transform _transform = new Transform();

        private BoundingBox _centeredBounds;
        private DeviceBuffer _vb;
        private DeviceBuffer _ib;
        private int _indexCount;
        private Texture _texture;
        private Texture _alphamapTexture;
        private TextureView _alphaMapView;

        private Pipeline _pipeline;
        private ResourceSet _mainProjViewRS;
        private ResourceSet _mainSharedRS;
        private ResourceSet _mainPerObjectRS;
        private DeviceBuffer _worldAndInverseBuffer;

        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        private readonly Vector3 _objectCenter;

        public Transform Transform => _transform;

        public TexturedMesh(string name, MeshData meshData, ImageSharpTexture textureData, ImageSharpTexture alphaTexture)
        {
            _name = name;
            _meshData = meshData;
            _centeredBounds = meshData.GetBoundingBox();
            _objectCenter = _centeredBounds.GetCenter();
            _textureData = textureData;
            _alphaTextureData = alphaTexture;
        }

        public override BoundingBox BoundingBox => BoundingBox.Transform(_centeredBounds, _transform.GetTransformMatrix());

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            if (s_useUniformOffset)
            {
                _uniformOffset = gd.UniformBufferMinOffsetAlignment;
            }
            ResourceFactory disposeFactory = new DisposeCollectorResourceFactory(gd.ResourceFactory, _disposeCollector);
            _vb = _meshData.CreateVertexBuffer(disposeFactory, cl);
            _vb.Name = _name + "_VB";
            _ib = _meshData.CreateIndexBuffer(disposeFactory, cl, out _indexCount);
            _ib.Name = _name + "_IB";

            uint bufferSize = 128;
            if (s_useUniformOffset) { bufferSize += _uniformOffset * 2; }

            _worldAndInverseBuffer = disposeFactory.CreateBuffer(new BufferDescription(bufferSize, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            if (_textureData != null)
            {
                _texture = StaticResourceCache.GetTexture2D(gd, gd.ResourceFactory, _textureData);
            }
            else
            {
                _texture = disposeFactory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
                RgbaByte[] color = new RgbaByte[] { RgbaByte.Pink };
                gd.UpdateTexture(_texture, color, 0, 0, 0, 1, 1, 1, 0, 0);
            }

            if (_alphaTextureData != null)
            {
                _alphamapTexture = _alphaTextureData.CreateDeviceTexture(gd, disposeFactory);
            }
            else
            {
                _alphamapTexture = StaticResourceCache.GetPinkTexture(gd, gd.ResourceFactory);
            }
            _alphaMapView = StaticResourceCache.GetTextureView(gd.ResourceFactory, _alphamapTexture);

            ResourceLayout projViewCombinedLayout = StaticResourceCache.GetResourceLayout(
                gd.ResourceFactory,
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            ResourceLayout worldLayout = StaticResourceCache.GetResourceLayout(gd.ResourceFactory, new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("WorldAndInverse", ResourceKind.UniformBuffer, ShaderStages.Vertex, ResourceLayoutElementOptions.DynamicBinding)));

            VertexLayoutDescription[] mainVertexLayouts = new VertexLayoutDescription[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
            };

            (Shader mainVS, Shader mainFS) = StaticResourceCache.GetShaders(gd, gd.ResourceFactory, "Main");

            ResourceLayout projViewLayout = StaticResourceCache.GetResourceLayout(
                gd.ResourceFactory,
                StaticResourceCache.ProjViewLayoutDescription);

            ResourceLayout mainSharedLayout = StaticResourceCache.GetResourceLayout(gd.ResourceFactory, new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("LightInfo", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
                new ResourceLayoutElementDescription("CameraInfo", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)));

            ResourceLayout mainPerObjectLayout = StaticResourceCache.GetResourceLayout(gd.ResourceFactory, new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("WorldAndInverse", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment, ResourceLayoutElementOptions.DynamicBinding),
                new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("RegularSampler", ResourceKind.Sampler, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("AlphaMap", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("AlphaMapSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            BlendStateDescription alphaBlendDesc = BlendStateDescription.SingleAlphaBlend;
            alphaBlendDesc.AlphaToCoverageEnabled = true;

            GraphicsPipelineDescription mainPD = new GraphicsPipelineDescription(
                _alphamapTexture != null ? alphaBlendDesc : BlendStateDescription.SingleOverrideBlend,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Wireframe, FrontFace.Clockwise,
                        RasterizerStateDescription.Default.DepthClipEnabled, RasterizerStateDescription.Default.ScissorTestEnabled),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(mainVertexLayouts, new[] { mainVS, mainFS }, new[] { new SpecializationConstant(100, gd.IsClipSpaceYInverted) }),
                new ResourceLayout[] { projViewLayout, mainSharedLayout, mainPerObjectLayout },
                sc.MainSceneFramebuffer.OutputDescription);
            _pipeline = StaticResourceCache.GetPipeline(gd.ResourceFactory, ref mainPD);
            _pipeline.Name = "TexturedMesh Main Pipeline";
            //mainPD.RasterizerState.CullMode = FaceCullMode.None;
            mainPD.Outputs = sc.MainSceneFramebuffer.OutputDescription;

            _mainProjViewRS = StaticResourceCache.GetResourceSet(gd.ResourceFactory, new ResourceSetDescription(projViewLayout,
                sc.ProjectionMatrixBuffer,
                sc.ViewMatrixBuffer));

            _mainSharedRS = StaticResourceCache.GetResourceSet(gd.ResourceFactory, new ResourceSetDescription(mainSharedLayout,
                sc.LightInfoBuffer,
                sc.CameraInfoBuffer));

            _mainPerObjectRS = disposeFactory.CreateResourceSet(new ResourceSetDescription(mainPerObjectLayout,
                new DeviceBufferRange(_worldAndInverseBuffer, _uniformOffset, 128),
                _texture,
                gd.Aniso4xSampler,
                _alphaMapView,
                gd.LinearSampler));
        }

        public override void DestroyDeviceObjects()
        {
            _disposeCollector.DisposeAll();
        }

        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return RenderOrderKey.Create(
                _pipeline.GetHashCode(),
                Vector3.Distance(_objectCenter * _transform.Scale + _transform.Position, cameraPosition));
        }

        public override void Render(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            cl.SetVertexBuffer(0, _vb);
            cl.SetIndexBuffer(_ib, IndexFormat.UInt16);
            cl.SetPipeline(_pipeline);
            cl.SetGraphicsResourceSet(0, _mainProjViewRS);
            cl.SetGraphicsResourceSet(1, _mainSharedRS);
            uint offset = _uniformOffset;
            cl.SetGraphicsResourceSet(2, _mainPerObjectRS, 1, ref offset);
            cl.DrawIndexed((uint)_indexCount, 1, 0, 0, 0);
        }

        public override void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            WorldAndInverse wai;
            wai.World = _transform.GetTransformMatrix();
            wai.InverseWorld = VdUtilities.CalculateInverseTranspose(ref wai.World);
            gd.UpdateBuffer(_worldAndInverseBuffer, _uniformOffset * 2, ref wai);
        }
    }

    public struct WorldAndInverse
    {
        public Matrix4x4 World;
        public Matrix4x4 InverseWorld;
    }
}
