﻿using System;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace OpenShadows.Data.Rendering
{
    public abstract class Renderable : IDisposable
    {
        public abstract void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, SceneContext sc);
        public abstract void Render(GraphicsDevice gd, CommandList cl, SceneContext sc);
        public abstract void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc);
        public abstract void DestroyDeviceObjects();
        public abstract RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition);

        public void Dispose()
        {
            DestroyDeviceObjects();
        }
    }

    public abstract class CullRenderable : Renderable
    {
        public bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return visibleFrustum.Contains(BoundingBox) == ContainmentType.Disjoint;
        }

        public abstract BoundingBox BoundingBox { get; }
    }
}
