using ImGuiNET;
using OpenShadows.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Sdl2;
using Veldrid.Utilities;
using Vulkan.Xlib;

namespace OpenShadows.Scenes
{
    internal class EntryScene : SceneBase
    {
        private Skybox skybox;
        private List<TexturedMesh> meshes = new List<TexturedMesh>();

        public EntryScene(GraphicsDevice setGD, Sdl2Window setWindow) 
            : base(setGD, setWindow)
        {
        }

        public override void Init()
        {
            base.Init();

            skybox = Skybox.LoadDefaultSkybox();
            //LoadMesh();
        }

        public override void Update(float deltaSeconds)
        {
            base.Update(deltaSeconds);

            ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always, Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(200, window.Height), ImGuiCond.Always);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);

            if (ImGui.Begin("window", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove))
            {
                ImGui.PushItemWidth(-1);
                ImGui.Text("Test");
                //ImGui.InputText("##alf_path", ref ReadableAlfPath, 1000, Alf != null ? ImGuiInputTextFlags.ReadOnly : 0);
                ImGui.PopItemWidth();
                ImGui.End();
            }

            ImGui.PopStyleVar();
        }

        internal override void CreateAllDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            base.CreateAllDeviceObjects(gd, cl, sc);

            skybox.CreateDeviceObjects(gd, cl, sc);
            for (int i = 0; i < meshes.Count; i++)
            {
                meshes[i].CreateDeviceObjects(gd, cl, sc);
            }
        }

        internal override void DestroyAllDeviceObjects()
        {
            base.DestroyAllDeviceObjects();

            for (int i = 0; i < meshes.Count; i++)
            {
                meshes[i].Dispose();
            }
            meshes.Clear();
            skybox.DestroyDeviceObjects();
        }

        private void LoadMesh()
        {
            string fn = "01O9999_03.obj";

            ObjParser parser = new ObjParser();
            using (FileStream objStream = File.OpenRead(AssetHelper.GetPath("Models/" + fn)))
            {
                ObjFile atriumFile = parser.Parse(objStream);
                MtlFile atriumMtls;
                using (FileStream mtlStream = File.OpenRead(AssetHelper.GetPath("Models/level.mtl")))
                {
                    atriumMtls = new MtlParser().Parse(mtlStream);
                }

                foreach (ObjFile.MeshGroup group in atriumFile.MeshGroups)
                {
                    Vector3 scale = new Vector3(0.1f);
                    ConstructedMeshInfo mesh = atriumFile.GetMesh(group);
                    MaterialDefinition materialDef = atriumMtls.Definitions[mesh.MaterialName];
                    ImageSharpTexture overrideTextureData = null;
                    ImageSharpTexture alphaTexture = null;
                    //if (materialDef.DiffuseTexture != null)
                    {
                        string texturePath = AssetHelper.GetPath("Models/" + materialDef.DiffuseTexture);
                        overrideTextureData = LoadTexture(texturePath, true);
                    }
                    /*if (materialDef.AlphaMap != null)
                    {
                        string texturePath = AssetHelper.GetPath("Models/SponzaAtrium/" + materialDef.AlphaMap);
                        alphaTexture = LoadTexture(texturePath, false);
                    }*/

                    var new_mesh = CreateTexturedMesh(
                        mesh,
                        overrideTextureData,
                        alphaTexture,
                        Vector3.Zero,
                        Quaternion.Identity,
                        scale,
                        group.Name);
                    meshes.Add(new_mesh);
                }
            }
        }

        protected override void RenderMainPass(GraphicsDevice gd, CommandList cl, SceneContext sceneContext, BoundingFrustum cameraFrustum)
        {
            base.RenderMainPass(gd, cl, sceneContext, cameraFrustum);

            skybox.Render(gd, cl, sceneContext);
            for (int i = 0; i < meshes.Count; i++)
            {
                var m = meshes[i];
                m.Render(gd, cl, sceneContext);
            }
        }

        protected override void RenderMainSwapChainPass(GraphicsDevice gd, CommandList cl, SceneContext sceneContext)
        {
            base.RenderMainSwapChainPass(gd, cl, sceneContext);
        }
    }
}
