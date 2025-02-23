﻿using OpenShadows.Data.Input;
using OpenShadows.Data.Rendering;
using System.Numerics;
using Veldrid;

namespace OpenShadows.Data.GUI
{
    public class ImGuiRenderable : Renderable, IUpdateable
    {
        private ImGuiRenderer _imguiRenderer;
        private int _width;
        private int _height;

        public ImGuiRenderable(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public void WindowResized(int width, int height) => _imguiRenderer.WindowResized(width, height);

        public override void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            if (_imguiRenderer == null)
            {
                _imguiRenderer = new ImGuiRenderer(gd, sc.MainSceneFramebuffer.OutputDescription, _width, _height, ColorSpaceHandling.Linear);
            }
            else
            {
                _imguiRenderer.CreateDeviceResources(gd, sc.MainSceneFramebuffer.OutputDescription, ColorSpaceHandling.Linear);
            }
        }

        public override void DestroyDeviceObjects()
        {
            _imguiRenderer.Dispose();
        }

        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return new RenderOrderKey(ulong.MaxValue);
        }

        public override void Render(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            _imguiRenderer.Render(gd, cl);
        }

        public override void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
        }

        public void Update(float deltaSeconds)
        {
            _imguiRenderer.Update(deltaSeconds, InputTracker.FrameSnapshot);
        }
    }
}
