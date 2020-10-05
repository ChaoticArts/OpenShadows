using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace OpenShadows.Workbench.Screens
{
	public class MainScreen : IDisposable
	{
		private GraphicsDevice Gd;

		private Sdl2Window Window;

		private CommandList Cl;

		private ImGuiRenderer ImGuiRenderer;

		private int CurrentScreen = 1;

		private List<IScreen> Screens = new List<IScreen>();

		public MainScreen()
		{
			var windowCreateInfo = new WindowCreateInfo
			{
				X                  = 100,
				Y                  = 100,
				WindowWidth        = 800,
				WindowHeight       = 600,
				WindowInitialState = WindowState.Maximized,
				WindowTitle        = "OpenShadows - Workbench"
			};

			var graphicsDeviceOptions = new GraphicsDeviceOptions(true, null, false, ResourceBindingModel.Improved, true, true, true);

			VeldridStartup.CreateWindowAndGraphicsDevice(windowCreateInfo, graphicsDeviceOptions, GraphicsBackend.Direct3D11, out Window, out Gd);

			Cl = Gd.ResourceFactory.CreateCommandList();

			ImGuiRenderer = new ImGuiRenderer(Gd, Gd.MainSwapchain.Framebuffer.OutputDescription, 800, 600, ColorSpaceHandling.Linear);
			ImGuiHelper.DeactivateIniFile();
			ImGui.StyleColorsLight();

			Window.Resized += () =>
			{
				Gd.MainSwapchain.Resize((uint)Window.Width, (uint)Window.Height);
				ImGuiRenderer.WindowResized(Window.Width, Window.Height);
			};

			Screens.Add(new AlfBrowserScreen());
			Screens.Add(new LevelBrowserScreen());
		}

		public void Dispose()
		{
			Cl?.Dispose();
			Gd?.Dispose();
		}

		public void Run()
		{
			Stopwatch s = new Stopwatch();
			s.Start();
			double secs = 0;

			while (Window.Exists)
			{
				InputSnapshot inputSnapshot = Window.PumpEvents();

				if (!Window.Exists)
				{
					break;
				}

				// ToDo: GameTimer implementieren
				double news = s.Elapsed.TotalSeconds;
				ImGuiRenderer.Update((float)(news - secs), inputSnapshot);
				secs = news;
				UpdateAndDrawGui();

				Cl.Begin();
				Cl.SetFramebuffer(Gd.SwapchainFramebuffer);
				Cl.ClearColorTarget(0, new RgbaFloat(0.45f, 0.55f, 0.6f, 1.0f));

				ImGuiRenderer.Render(Gd, Cl);

				Cl.End();
				Gd.SubmitCommands(Cl);
				Gd.SwapBuffers();
			}
		}

		private void UpdateAndDrawGui()
		{
			ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always, Vector2.Zero);
			ImGui.SetNextWindowSize(new Vector2(Window.Width, Window.Height), ImGuiCond.Always);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);

			if (ImGui.BeginMainMenuBar())
			{
				if (ImGui.BeginMenu("File"))
				{
					if (ImGui.MenuItem("Exit"))
					{
						Window.Close();
					}

					ImGui.EndMenu();
				}

				if (ImGui.BeginMenu($"Change Screen ({Screens[CurrentScreen].Title})"))
				{
					if (ImGui.MenuItem("ALF-Browser") && CurrentScreen != 0)
					{
						CurrentScreen = 0;
					}

					if (ImGui.MenuItem("Level Browser") && CurrentScreen != 1)
					{
						CurrentScreen = 1;
					}

					ImGui.EndMenu();
				}

				ImGui.EndMainMenuBar();
			}

			Screens[CurrentScreen]?.Update(1.0f / 60.0f);
			Screens[CurrentScreen]?.Render(Window, Gd, ImGuiRenderer);

			ImGui.PopStyleVar();
		}

		private readonly byte[] BufferAlfFilepath = new byte[1024];
	}
}
