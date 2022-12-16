using ImGuiNET;
using Microsoft.FSharp.Collections;
using OpenShadows.Core;
using OpenShadows.GUI;
using OpenShadows.Input;
using OpenShadows.Scenes;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using static System.Formats.Asn1.AsnWriter;

namespace OpenShadows
{
    public class OpenShadows
    {
        private Sdl2Window window;
        private GraphicsDevice gd;
        private ImGuiRenderable igRenderable;

        private bool wasWindowResized = false;
        private SceneBase scene;
        private CommandList frameCommands;
        private readonly SceneContext sceneContext = new SceneContext();

        private event Action<int, int> resizeHandled;

        private TextureSampleCount? newSampleCount;

        public bool Init(StartupOptions options)
        {
            WindowCreateInfo windowCI = new WindowCreateInfo
            {
                X = 50,
                Y = 50,
                WindowWidth = options.Width,
                WindowHeight = options.Height,
                WindowInitialState = options.Fullscreen ? WindowState.BorderlessFullScreen : WindowState.Normal,
                WindowTitle = "OpenShadows"
            };
            GraphicsDeviceOptions gdOptions = new GraphicsDeviceOptions(false, null, false, ResourceBindingModel.Improved, true, true, true);
#if DEBUG
            gdOptions.Debug = true;
#endif
            VeldridStartup.CreateWindowAndGraphicsDevice(
                windowCI,
                gdOptions,
                VeldridStartup.GetPlatformDefaultBackend(),
                out window,
                out gd);
            window.Resized += () =>
            {
                wasWindowResized = true;
            };

            Sdl2Native.SDL_Init(0);

            if (CompileShaders() == false)
            {
                return false;
            }

            scene = new EntryScene(gd, window);
            sceneContext.SetCurrentScene(scene);

            igRenderable = new ImGuiRenderable(window.Width, window.Height);
            resizeHandled += (w, h) => igRenderable.WindowResized(w, h);
            scene.GuiRenderable = igRenderable;

            sceneContext.Camera.Position = new Vector3(5, 5, 5);
            sceneContext.Camera.Yaw = -MathF.PI / 2;
            sceneContext.Camera.Pitch = -MathF.PI / 9;

            CreateAllObjects();
            ImGui.StyleColorsClassic();

            return true;
        }

        private bool CompileShaders()
        {
            bool success = true;

            var shaderPath = AssetHelper.GetPath("Shaders");
            // Vertex Shaders
            var vertShaders = Directory.GetFiles(shaderPath, "*.vert");
            for (int i = 0; i < vertShaders.Length; i++)
            {
                FileInfo shaderSource = new FileInfo(vertShaders[i]);
                FileInfo compiledShaderSource = new FileInfo(vertShaders[i] + ".spv");
                if (compiledShaderSource.Exists == false ||
                    shaderSource.LastWriteTimeUtc > compiledShaderSource.LastWriteTimeUtc)
                {
                    bool res = CompileShader(shaderSource, compiledShaderSource, GLSLang.ShaderStage.Vertex);
                    if (res == false)
                    {
                        // Note the failure, but still continue to get more errors (potentially)
                        success = false;
                    }
                }
            }

            // Fragment shaders
            var fragShaders = Directory.GetFiles(shaderPath, "*.frag");
            for (int i = 0; i < fragShaders.Length; i++)
            {
                FileInfo shaderSource = new FileInfo(fragShaders[i]);
                FileInfo compiledShaderSource = new FileInfo(fragShaders[i] + ".spv");
                if (compiledShaderSource.Exists == false ||
                    shaderSource.LastWriteTimeUtc > compiledShaderSource.LastWriteTimeUtc)
                {
                    bool res = CompileShader(shaderSource, compiledShaderSource, GLSLang.ShaderStage.Fragment);
                    if (res == false)
                    {
                        // Note the failure, but still continue to get more errors (potentially)
                        success = false;
                    }
                }
            }

            return success;
        }

        private bool CompileShader(FileInfo shaderSource, FileInfo compiledShaderSource, GLSLang.ShaderStage stage)
        {
            Log.Information($"Compiling shader {shaderSource.Name}");
            string source = File.ReadAllText(shaderSource.FullName);
            var result = GLSLang.GLSLang.tryCompile(stage, Path.GetFileNameWithoutExtension(shaderSource.Name),
                ListModule.Empty<string>(), source);
            var output = result.Item2;
            if (string.IsNullOrWhiteSpace(output) &&
                result.Item1 != null)
            {
                var byteCode = result.Item1.Value;
                File.WriteAllBytes(compiledShaderSource.FullName, byteCode);
                return true;
            }
            else
            {
                Log.Error("Failed to compile shader: " + output);
                return false;
            }
        }

        public void Run()
        {
            long previousFrameTicks = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (window.Exists)
            {
                long currentFrameTicks = sw.ElapsedTicks;
                double deltaSeconds = (currentFrameTicks - previousFrameTicks) / (double)Stopwatch.Frequency;

                /*while (_limitFrameRate && deltaSeconds < _desiredFrameLengthSeconds)
                {
                    currentFrameTicks = sw.ElapsedTicks;
                    deltaSeconds = (currentFrameTicks - previousFrameTicks) / (double)Stopwatch.Frequency;
                }*/

                previousFrameTicks = currentFrameTicks;

                InputSnapshot snapshot = null;
                Sdl2Events.ProcessEvents();
                snapshot = window.PumpEvents();
                InputTracker.UpdateFrameInput(snapshot, window);
                Update((float)deltaSeconds);
                if (!window.Exists)
                {
                    break;
                }

                Draw();
            }

            DestroyAllObjects();
            gd.Dispose();
        }

        private void Update(float deltaSeconds)
        {
            scene.Update(deltaSeconds);
        }

        private void Draw()
        {
            Debug.Assert(window.Exists);
            int width = window.Width;
            int height = window.Height;

            if (wasWindowResized)
            {
                wasWindowResized = false;

                gd.ResizeMainWindow((uint)width, (uint)height);
                scene.Camera.WindowResized(width, height);
                resizeHandled?.Invoke(width, height);
                CommandList cl = gd.ResourceFactory.CreateCommandList();
                cl.Begin();
                sceneContext.RecreateWindowSizedResources(gd, cl);
                cl.End();
                gd.SubmitCommands(cl);
                cl.Dispose();
            }

            if (newSampleCount != null)
            {
                sceneContext.MainSceneSampleCount = newSampleCount.Value;
                newSampleCount = null;
                DestroyAllObjects();
                CreateAllObjects();
            }

            frameCommands.Begin();

            //CommonMaterials.FlushAll(frameCommands);

            scene.Render(gd, frameCommands, sceneContext);
            gd.SwapBuffers();
        }

        private void DestroyAllObjects()
        {
            gd.WaitForIdle();
            frameCommands.Dispose();
            sceneContext.DestroyDeviceObjects();
            scene.DestroyAllDeviceObjects();
            //CommonMaterials.DestroyAllDeviceObjects();
            //StaticResourceCache.DestroyAllDeviceObjects();
            gd.WaitForIdle();
        }

        private void CreateAllObjects()
        {
            frameCommands = gd.ResourceFactory.CreateCommandList();
            frameCommands.Name = "Frame Commands List";
            CommandList initCL = gd.ResourceFactory.CreateCommandList();
            initCL.Name = "Recreation Initialization Command List";
            initCL.Begin();
            sceneContext.CreateDeviceObjects(gd, initCL);
            //CommonMaterials.CreateAllDeviceObjects(_gd, initCL, _sc);
            scene.CreateAllDeviceObjects(gd, initCL, sceneContext);
            initCL.End();
            gd.SubmitCommands(initCL);
            initCL.Dispose();
        }

        private void ChangeMsaa(int msaaOption)
        {
            TextureSampleCount sampleCount = (TextureSampleCount)msaaOption;
            newSampleCount = sampleCount;
        }

        private void ToggleFullscreenState()
        {
            bool isFullscreen = window.WindowState == WindowState.BorderlessFullScreen;
            window.WindowState = isFullscreen ? WindowState.Normal : WindowState.BorderlessFullScreen;
        }
    }
}
