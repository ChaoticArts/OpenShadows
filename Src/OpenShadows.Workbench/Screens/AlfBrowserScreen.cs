using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;
using OpenShadows.FileFormats;
using OpenShadows.FileFormats.ALF;
using Veldrid.Sdl2;

namespace OpenShadows.Workbench.Screens
{
	public class AlfBrowserScreen : IScreen
	{
		public string Title { get; } = "ALF Browser";

		private AlfArchive Alf;

		private string ReadableAlfPath = "DATA\\RIVA.ALF";

		private int SelectedEntry = -1;

		public void Update(float dt)
		{
			// explicitly do nothing
		}

		public void Render(Sdl2Window window)
		{
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

			ImGui.BeginChild("##file_list");

			ImGui.Columns(4);
			ImGui.Text("idx"); ImGui.NextColumn();
			ImGui.Text("module"); ImGui.NextColumn();
			ImGui.Text("name"); ImGui.NextColumn();
			ImGui.Text("size"); ImGui.NextColumn();
			ImGui.Separator();

			foreach (AlfModule module in Alf.Modules)
			{
				if (!module.Entries.Any())
				{
					continue;
				}

				foreach (AlfEntry entry in module.Entries)
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

				ImGui.End();
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
		}
	}
}
