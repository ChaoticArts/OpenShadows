using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using OpenShadows.Data;
using OpenShadows.Data.Graphic;
using OpenShadows.FileFormats;
using OpenShadows.FileFormats.Archive;
using OpenShadows.FileFormats.Images;
using OpenShadows.FileFormats.Text;
using Veldrid;
using Veldrid.Sdl2;

namespace OpenShadows.Workbench.Screens
{
	public class AlfBrowserScreen : IScreen
	{
		public string Title { get; } = "ALF Browser";

		private AlfArchive Alf;

		private string ReadableAlfPath = "DATA\\RIVA.ALF";

		private string SearchString = "lxt";

		private int SelectedEntry = -1;

		private ImGuiRenderer ImGuiRenderer;

		private GraphicsDevice Gd;

		public void Update(float dt)
		{
			// explicitly do nothing
		}

		public void Render(Sdl2Window window, GraphicsDevice gd, ImGuiRenderer imGuiRenderer)
		{
			Gd = gd;
			ImGuiRenderer = imGuiRenderer;

			Vector2 pos = Vector2.One;
			pos.Y = 18;
			ImGui.SetNextWindowPos(pos, ImGuiCond.Always, Vector2.Zero);
			ImGui.SetNextWindowSize(new Vector2(500, window.Height - 18), ImGuiCond.Always);

			if (ImGui.Begin("##alf_browser", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar))
			{
				ImGui.PushItemWidth(-1);
				ImGui.InputText("##alf_path", ref ReadableAlfPath, 1000, Alf != null ? ImGuiInputTextFlags.ReadOnly : 0);
				ImGui.PopItemWidth();

				if (Alf == null)
				{
					if (ImGui.Button("Open ALF") && File.Exists(ReadableAlfPath))
					{
						OpenAlf();
					}
				}
				else
				{
					if (ImGui.Button($"Close {Path.GetFileName(ReadableAlfPath)}"))
					{
						CloseAlf();
					}
				}

				if (Alf != null)
				{
					DrawContentList();

					if (SelectedEntry != -1)
					{
						DrawEntryWindow(window);
					}
				}
				ImGui.End();
			}
		}

		private void DrawContentList()
		{
			ImGui.Separator();

			ImGui.PushItemWidth(-1);
			ImGui.InputText("##search_string", ref SearchString, 1000);
			ImGui.PopItemWidth();

			ImGui.BeginChild("##file_list");

			ImGui.Columns(4);
			ImGui.Text("idx"); ImGui.NextColumn();
			ImGui.Text("module"); ImGui.NextColumn();
			ImGui.Text("name"); ImGui.NextColumn();
			ImGui.Text("size"); ImGui.NextColumn();
			ImGui.Separator();

			foreach (AlfModule module in Alf.Modules)
			{
				List<AlfEntry> filteredModules = module.Entries.Where(x => SearchString.Length == 0 || x.Name.Contains(SearchString, StringComparison.InvariantCultureIgnoreCase)).ToList();

				if (filteredModules.Count == 0)
				{
					continue;
				}

				foreach (AlfEntry entry in filteredModules)
				{
					if (ImGui.Selectable(entry.Index.ToString(), entry.Index == SelectedEntry, ImGuiSelectableFlags.SpanAllColumns))
					{
						SelectEntry(entry.Index);
					}

					ImGui.NextColumn();
					ImGui.Text(module.Name);
					ImGui.NextColumn();
					ImGui.Text(entry.Name);
					ImGui.NextColumn();
					ImGui.Text(entry.Size.ToString());
					ImGui.NextColumn();
				}

				ImGui.Separator();
			}

			ImGui.Columns(1);

			ImGui.EndChild();
		}

		private void DrawEntryWindow(Sdl2Window window)
		{
			Vector2 pos = Vector2.One;
			pos.X = 500;
			pos.Y = 18;
			ImGui.SetNextWindowPos(pos, ImGuiCond.Always, Vector2.Zero);
			ImGui.SetNextWindowSize(new Vector2(window.Width - pos.X, window.Height - 18), ImGuiCond.Always);

			if (ImGui.Begin("##entry_window", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar))
			{
				AlfEntry entry = Alf.Entries[SelectedEntry];

				ImGui.Text($"Filename: {entry.Name}");
				ImGui.SameLine();
				ImGui.Text($"   Type: {FormatHelper.GetFileType(entry.Name.End(3))}");

				if (ImGui.Button("Extract"))
				{
					const string path = @"Y:\Projekte\Reverse Engineering\NLT\OpenShadows";
					using Stream s = entry.Open();
					using FileStream fs = new FileStream(Path.Combine(path, entry.Name), FileMode.Create);
					s.CopyTo(fs);
				}

				if (string.Equals(entry.Name.End(3), "AIF", StringComparison.OrdinalIgnoreCase))
				{
					DrawImageViewer("AIF");
				}

				if (string.Equals(entry.Name.End(3), "LXT", StringComparison.Ordinal))
				{
					DrawTextViewer("lxt");
				}

				ImGui.End();
			}
		}

		private Texture CurrentImage;

		private bool HasImage;

		private uint ZoomFactor = 1;

		private void DrawImageViewer(string imageType)
		{
			ImGui.Text("I'm an viewable image");

			if (!HasImage && ImGui.Button("View me"))
			{
				using Stream s = Alf.Entries[SelectedEntry].Open();

				ImageData temp = AifExtractor.ExtractImage(s);

				Texture img = Gd.ResourceFactory.CreateTexture(new TextureDescription
				{
					Height      = (uint)temp.Height,
					Width       = (uint)temp.Width,
					Format      = PixelFormat.R8_G8_B8_A8_UNorm_SRgb,
					Type        = TextureType.Texture2D,
					Usage       = TextureUsage.Sampled,
					MipLevels   = 1,
					Depth       = 1,
					ArrayLayers = 1
				});

				GCHandle pinnedArray = GCHandle.Alloc(temp.PixelData, GCHandleType.Pinned);
				IntPtr   pointer     = pinnedArray.AddrOfPinnedObject();

				Gd.UpdateTexture(img, pointer, (uint)temp.PixelData.Length, 0, 0, 0, (uint)temp.Width, (uint)temp.Height, 1, 0, 0);

				pinnedArray.Free();

				CurrentImage = img;
				HasImage     = true;
				ZoomFactor   = 1;
			}

			if (HasImage && CurrentImage != null)
			{
				if (ImGui.Button("Close me"))
				{
					HasImage = false;
					CurrentImage.Dispose();
				}

				ImGui.SameLine();

				if (ImGui.Button("+"))
				{
					ZoomFactor++;
				}

				ImGui.SameLine();

				if (ImGui.Button("-"))
				{
					ZoomFactor = ZoomFactor == 1 ? 1 : ZoomFactor - 1;
				}

				ImGui.Text($"Image Size: {CurrentImage.Width}x{CurrentImage.Height}");

				ImGui.Image(ImGuiRenderer.GetOrCreateImGuiBinding(Gd.ResourceFactory, CurrentImage), new Vector2(CurrentImage.Width * ZoomFactor, CurrentImage.Height * ZoomFactor));
			}
		}

		private void DrawTextViewer(string textType)
		{
			using Stream s = Alf.Entries[SelectedEntry].Open();

			if (string.Equals(textType, "lxt", StringComparison.OrdinalIgnoreCase))
			{
				ImGui.BeginChild("##text_list");

				ImGui.Columns(2);
				ImGui.Text("idx"); ImGui.NextColumn();
				ImGui.Text("Text"); ImGui.NextColumn();
				ImGui.Separator();

				ImGui.SetColumnWidth(0, 50.0f);

				foreach ((int idx, string text) in LxtExtractor.ExtractTexts(s))
				{
					if (ImGui.Selectable(idx.ToString(), false, ImGuiSelectableFlags.SpanAllColumns))
					{}
					ImGui.NextColumn();
					ImGui.Text(text); ImGui.NextColumn();
				}

				ImGui.Columns(1);

				ImGui.EndChild();
			}
		}

		private void OpenAlf()
		{
			Alf = new AlfArchive(ReadableAlfPath);
			SelectedEntry = -1;
		}

		private void CloseAlf()
		{
			Alf.Dispose();
			Alf = null;
		}

		private void SelectEntry(int id)
		{
			SelectedEntry = id;

			if (HasImage || CurrentImage != null)
			{
				HasImage = false;
				CurrentImage?.Dispose();
			}
		}
	}
}
