using ImGuiNET;
using OpenShadows.Data.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Sdl2;
using static GLSLang.SpirV.Instruction;

#nullable enable

namespace OpenShadows.Workbench.Screens
{
    public class DialogScreen : IScreen
    {
        private Dialog? activeDialog = null;
        private DialogEntry? activeEntry = null;

        public string Title => "Dialog" + (activeDialog != null ? (" - " + activeDialog.Title) : string.Empty);

        public void SetActiveDialog(Dialog? setActiveDialog)
        {
            activeDialog = setActiveDialog;
            if (activeDialog != null)
            {
                activeEntry = activeDialog.GetGreeting();
            }
        }

        public void Render(Sdl2Window window, GraphicsDevice gd, ImGuiRenderer imGuiRenderer)
        {
            Vector2 pos = Vector2.One;
            pos.Y = 18;
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always, Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(window.Width, window.Height - 18), ImGuiCond.Always);

            if (ImGui.Begin("##dialog_window", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar))
            {
                if (activeDialog == null)
                {
                    ImGui.PushItemWidth(-1);
                    ImGui.TextUnformatted("No active dialog. Please select an .XDF file from the AlfBrowser screen.");
                    ImGui.PopItemWidth();
                }
                else
                {
                    ImGui.PushItemWidth(-1);
                    ImGui.TextUnformatted(activeDialog.Title);
                    ImGui.PopItemWidth();

                    ImGui.BeginChild("##main_dialog");

                    ImGui.Columns(2);
                    ImGui.Text("Conversation"); ImGui.NextColumn();
                    ImGui.Text("Actions"); ImGui.NextColumn();
                    ImGui.Separator();

                    ImGui.SetColumnWidth(1, 150.0f);

                    if (activeEntry != null)
                    {                        
                        for (int pgIdx = 0; pgIdx < activeEntry.Pages.Count; pgIdx++)
                        {
                            var page = activeEntry.Pages[pgIdx];

                            if (pgIdx > 0)
                            {
                                ImGui.TextUnformatted(" --- page wrap --- ");
                            }

                            for (int strIdx = 0; strIdx < page.Strings.Count; strIdx++)
                            {
                                var str = page.Strings[strIdx];
                                ImGui.TextWrapped(str.String);
                            }
                        }
                        
                        if (activeEntry.ShouldEndDialogAfterLastPage)
                        {
                            ImGui.TextUnformatted(" --- end of dialog --- ");
                        }
                    }

                    ImGui.NextColumn();

                    if (activeDialog.HasEnded == false)
                    {
                        var ts = activeDialog.GetTopicStrings();
                        for (int i = 0; i < ts.Length; i++)
                        {
                            if (ImGui.Button(ts[i].name))
                            {
                                SelectTopic(ts[i].topicIndex);
                            }
                        }
                    }
                    else
                    {
                        ImGui.Text("Dialog has ended");
                        if (ImGui.Button("Restart"))
                        {
                            activeDialog.Restart();
                            activeEntry = activeDialog.GetGreeting();
                        }
                    }

                    ImGui.NextColumn();

                    ImGui.Columns(1);

                    ImGui.EndChild();
                }
            }
        }

        private void SelectTopic(int topicIndex)
        {
            activeEntry = activeDialog?.GetEntryForTopic(topicIndex, true);
        }

        public void Update(float dt)
        {
            if (activeDialog == null)
            {
                return;
            }
        }
    }
}
