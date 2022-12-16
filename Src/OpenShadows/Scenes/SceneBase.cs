using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid.Sdl2;
using Veldrid;
using OpenShadows.Core;
using System.Numerics;
using Veldrid.Utilities;
using Veldrid.ImageSharp;
using OpenShadows.GUI;

namespace OpenShadows.Scenes
{
    internal abstract class SceneBase
    {
        protected GraphicsDevice gd;
        protected Sdl2Window window;

        private readonly Camera camera;
        private CommandList resourceUpdateCL;
        private readonly Dictionary<string, ImageSharpTexture> _textures = new Dictionary<string, ImageSharpTexture>();

        public Camera Camera => camera;

        public ImGuiRenderable GuiRenderable { get; set; }

        public SceneBase(GraphicsDevice setGD, Sdl2Window setWindow) 
        {
            gd = setGD;
            window = setWindow;
            camera = new Camera(gd, window);
        }

        public virtual void Init()
        {
            //
        }

        public virtual void Update(float deltaSeconds)
        {
            camera.Update(deltaSeconds);
            GuiRenderable.Update(deltaSeconds);
        }

        public virtual void Render(GraphicsDevice gd, CommandList cl, SceneContext sceneContext)
        {
            float depthClear = gd.IsDepthRangeZeroToOne ? 0f : 1f;
            Matrix4x4 cameraProj = Camera.ProjectionMatrix;

            cl.UpdateBuffer(sceneContext.LightInfoBuffer, 0, sceneContext.DirectionalLight.GetInfo());
            Vector3 lightPos = sceneContext.DirectionalLight.Transform.Position - sceneContext.DirectionalLight.Direction * 1000f;

            cl.PushDebugGroup("Main Scene Pass");

            cl.SetFramebuffer(sceneContext.MainSceneFramebuffer);
            uint fbWidth = sceneContext.MainSceneFramebuffer.Width;
            uint fbHeight = sceneContext.MainSceneFramebuffer.Height;
            cl.SetViewport(0, new Viewport(0, 0, fbWidth, fbHeight, 0, 1));
            cl.SetFullViewports();
            cl.SetFullScissorRects();

            cl.ClearColorTarget(0, RgbaFloat.CornflowerBlue);
            cl.ClearDepthStencil(depthClear);

            sceneContext.UpdateCameraBuffers(cl);
            BoundingFrustum cameraFrustum = new BoundingFrustum(Camera.ViewMatrix * Camera.ProjectionMatrix);

            RenderMainPass(gd, cl, sceneContext, cameraFrustum);

            GuiRenderable.Render(gd, cl, sceneContext);

            cl.PopDebugGroup();

            if (sceneContext.MainSceneColorTexture.SampleCount != TextureSampleCount.Count1)
            {
                cl.ResolveTexture(sceneContext.MainSceneColorTexture, sceneContext.MainSceneResolvedColorTexture);
            }

            cl.PushDebugGroup("Swapchain Pass");

            cl.SetFramebuffer(gd.SwapchainFramebuffer);
            fbWidth = gd.SwapchainFramebuffer.Width;
            fbHeight = gd.SwapchainFramebuffer.Height;
            cl.SetFullViewports();
            RenderMainSwapChainPass(gd, cl, sceneContext);            

            cl.PopDebugGroup();

            cl.End();

            resourceUpdateCL.Begin();
            UpdatePerFrameResources(gd, resourceUpdateCL, sceneContext);
            resourceUpdateCL.End();

            gd.SubmitCommands(cl);
        }

        protected virtual void RenderMainPass(GraphicsDevice gd, CommandList cl, SceneContext sceneContext, BoundingFrustum cameraFrustum)
        {
            //
        }

        protected virtual void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, SceneContext sceneContext)
        {
            //
        }

        protected virtual void RenderMainSwapChainPass(GraphicsDevice gd, CommandList cl, SceneContext sceneContext)
        {
            sceneContext.FullScreenQuad.Render(gd, cl, sceneContext);
        }

        internal virtual void CreateAllDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            GuiRenderable.CreateDeviceObjects(gd, cl, sc);

            resourceUpdateCL = gd.ResourceFactory.CreateCommandList();
            resourceUpdateCL.Name = "Scene Resource Update Command List";
        }

        internal virtual void DestroyAllDeviceObjects()
        {
            resourceUpdateCL.Dispose();

            GuiRenderable.DestroyDeviceObjects();
        }

        protected ImageSharpTexture LoadTexture(string texturePath, bool mipmap)
        {
            if (!_textures.TryGetValue(texturePath, out ImageSharpTexture tex))
            {
                tex = new ImageSharpTexture(texturePath, mipmap, true);
                _textures.Add(texturePath, tex);
            }

            return tex;
        }

        protected TexturedMesh CreateTexturedMesh(
            MeshData meshData,
            ImageSharpTexture texData,
            ImageSharpTexture alphaTexData,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            string name)
        {
            TexturedMesh mesh = new TexturedMesh(name, meshData, texData, alphaTexData);
            mesh.Transform.Position = position;
            mesh.Transform.Rotation = rotation;
            mesh.Transform.Scale = scale;
            return mesh;
        }
    }
}
