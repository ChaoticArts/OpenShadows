using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using OpenShadows.Data.Graphic;
using OpenShadows.FileFormats;
using OpenShadows.FileFormats.Archive;
using OpenShadows.FileFormats.Images;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Veldrid;
using Veldrid.Sdl2;
using BinaryReader = System.IO.BinaryReader;
using OpenShadows.FileFormats.Levels;
using OpenShadows.FileFormats.Text;

namespace OpenShadows.Workbench.Screens
{
    public class LevelBrowserScreen : IScreen
    {
        public string Title { get; } = "Level Browser";

        private AlfArchive Alf;

        private string ReadableAlfPath = "DATA\\DUNGEON.ALF";

        private int SelectedEntry = -1;

        private int SelectedTexture = -1;

        private int SelectedLevelEntry = -1;

        private ImGuiRenderer ImGuiRenderer;

        private GraphicsDevice Gd;

        private bool alreadyTriedOpeningDefaultAlf = false;

        public void Update(float dt)
        {
            // explicitly do nothing

            if (alreadyTriedOpeningDefaultAlf == false &&
                Alf == null && 
                File.Exists(ReadableAlfPath))
            {
                alreadyTriedOpeningDefaultAlf = true;
                OpenAlf();
            }
        }

        public void Render(Sdl2Window window, GraphicsDevice gd, ImGuiRenderer imGuiRenderer)
        {
            Gd = gd;
            ImGuiRenderer = imGuiRenderer;

            Vector2 pos = Vector2.One;
            pos.Y = 18;
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always, Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(500, window.Height - 18), ImGuiCond.Always);

            if (ImGui.Begin("##level_browser", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar))
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
                else if (ImGui.Button($"Close {Path.GetFileName(ReadableAlfPath)}"))
                {
                    CloseAlf();
                }

                if (Alf != null)
                {
                    DrawContentList();

                    if (SelectedEntry != -1)
                    {
                        DrawEntryWindow(window);
                    }
                }
            }
        }

        private void DrawContentList()
        {
            ImGui.Separator();

            ImGui.BeginChild("##file_list");

            ImGui.Columns(3);
            ImGui.Text("idx"); ImGui.NextColumn();
            ImGui.Text("level"); ImGui.NextColumn();
            ImGui.Text("textures"); ImGui.NextColumn();
            ImGui.Separator();

            ImGui.SetColumnWidth(0, 50.0f);

            for (int i = 0; i < Alf.Modules.Count - 1; i += 2)
            {
                AlfModule levelMod = Alf.Modules[i];

                if (levelMod.Name == "FINAL03")
                {
                    continue;
                }

                AlfModule textureMod = Alf.Modules[i + 1];

                if (ImGui.Selectable(i.ToString(), i == SelectedEntry, ImGuiSelectableFlags.SpanAllColumns))
                {
                    SelectEntry(i);
                }
                ImGui.NextColumn();
                ImGui.Text(levelMod.Name);
                ImGui.NextColumn();
                ImGui.Text(textureMod.Name);
                ImGui.NextColumn();
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
            ImGui.SetNextWindowSize(new Vector2(350.0f, window.Height - 18), ImGuiCond.Always);

            if (ImGui.Begin("##entry_window", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar))
            {
                AlfModule levelModule = Alf.Modules[SelectedEntry];
                AlfModule textureModule = Alf.Modules[SelectedEntry + 1];

                ImGui.Text($"Level: {levelModule.Name}");
                ImGui.Text($"3D-Definition: {levelModule.Entries.First(e => e.Name.EndsWith("3DM")).Name}");
                ImGui.SameLine();
                if (ImGui.Button("Extract Level"))
                {
                    var entry = levelModule.Entries.First(e => e.Name.EndsWith("3DM"));
                    string fn =
                        Path.Combine(
                            Path.GetDirectoryName(ReadableAlfPath),
                            "extract");
                    fn = Path.Combine(fn, entry.Name);
                    var compressedData = entry.GetContents();
                    var uncompressedData = Utils.UnpackBoPaCompressedData(compressedData);
                    File.WriteAllBytes(fn, uncompressedData);

                    var level = Level3dmExtractor.ExtractLevel(uncompressedData, entry.Name, 0.00001f);
                    level.DumpToObj(
                        Path.Combine(
                            Path.GetDirectoryName(ReadableAlfPath),
                            "extract",
                            Path.GetFileNameWithoutExtension(entry.Name)),
                        1.0f);
                    level.DumpToObjIndividual(
                        Path.Combine(
                            Path.GetDirectoryName(ReadableAlfPath),
                            "extract",
                            Path.GetFileNameWithoutExtension(entry.Name)),
                        1.0f);                    
                }
                ImGui.Text($"Palette: {levelModule.Entries.First(e => e.Name.EndsWith("PAL")).Name}");
                ImGui.SameLine();
                if (ImGui.Button("Extract Palette"))
                {
                    var entry = levelModule.Entries.First(e => e.Name.EndsWith("PAL"));
                    string fn =
                        Path.Combine(
                            Path.GetDirectoryName(ReadableAlfPath),
                            "extract");
                    fn = Path.Combine(fn, entry.Name);
                    File.WriteAllBytes(fn, entry.GetContents());
                }

                ImGui.BeginChild("##text_list");

                ImGui.Columns(2);
                ImGui.Text("idx"); ImGui.NextColumn();
                ImGui.Text("Name"); ImGui.NextColumn();
                ImGui.Separator();

                ImGui.SetColumnWidth(0, 50.0f);

                for (int i = 0; i < textureModule.Entries.Count; i++)
                {
                    if (ImGui.Selectable(i.ToString(), i == SelectedTexture, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        SelectTexture(i);
                    }
                    ImGui.NextColumn();
                    ImGui.Text(textureModule.Entries[i].Name); ImGui.NextColumn();
                }

                for (int i = 0; i < levelModule.Entries.Count; i++)
                {
                    if (ImGui.Selectable((textureModule.Entries.Count + i).ToString(),
                        i == SelectedLevelEntry, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        SelectLevelEntry(i);
                    }
                    ImGui.NextColumn();
                    ImGui.Text(levelModule.Entries[i].Name); ImGui.NextColumn();
                }

                ImGui.Columns(1);

                ImGui.EndChild();

                ImGui.End();
            }

            if (SelectedTexture != -1)
            {
                DrawTextureWindow(window);
            }
            if (SelectedLevelEntry != -1)
            {
                AlfModule levelModule = Alf.Modules[SelectedEntry];
                AlfEntry entry = levelModule.Entries[SelectedLevelEntry];

                if (string.Equals(entry.Name.End(3), "PIX", StringComparison.Ordinal))
                {
                    DrawTextureWindowLevel(window);
                }
                else if (string.Equals(entry.Name.End(3), "DSC", StringComparison.Ordinal))
                {
                    DrawDscLevelEntry(window);
                }
                else if (string.Equals(entry.Name.End(3), "PPD", StringComparison.Ordinal))
                {
                    DrawPpdLevelEntry(window);
                }
                else
                {
                    DrawGenericLevelEntry(window);
                }
            }
        }

        private Texture CurrentImage;

        private bool HasImage;

        private uint ZoomFactor = 1;

        private void DrawGenericLevelEntry(Sdl2Window window)
        {
            Vector2 pos = Vector2.One;
            pos.X = 500 + 350 - 1;
            pos.Y = 18;
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always, Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(window.Width - pos.X, window.Height - 18), ImGuiCond.Always);

            if (ImGui.Begin("##levelentry_window", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar))
            {
                AlfModule levelModule = Alf.Modules[SelectedEntry];
                AlfEntry selectedEntry = levelModule.Entries[SelectedLevelEntry];

                ImGui.Text($"Entry: {selectedEntry.Name}");
                if (ImGui.Button("Extract"))
                {
                    string fn =
                        Path.Combine(
                            Path.GetDirectoryName(ReadableAlfPath),
                            "extract");
                    fn = Path.Combine(fn, selectedEntry.Name);
                    File.WriteAllBytes(fn, selectedEntry.GetContents());
                }

                ImGui.End();
            }
        }

        private void DrawPpdLevelEntry(Sdl2Window window)
        {
            Vector2 pos = Vector2.One;
            pos.X = 500 + 350 - 1;
            pos.Y = 18;
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always, Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(window.Width - pos.X, window.Height - 18), ImGuiCond.Always);

            if (ImGui.Begin("##ppd_window", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar))
            {
                AlfModule levelModule = Alf.Modules[SelectedEntry];
                AlfEntry selectedEntry = levelModule.Entries[SelectedLevelEntry];

                ImGui.Text($"PPD: {selectedEntry.Name}");
                if (ImGui.Button("Extract"))
                {
                    string fn =
                        Path.Combine(
                            Path.GetDirectoryName(ReadableAlfPath),
                            "extract");
                    fn = Path.Combine(fn, selectedEntry.Name);
                    var uncompData = PpdExtractor.ExtractPpd(selectedEntry.GetContents());
                    File.WriteAllBytes(fn, uncompData);
                }

                ImGui.End();
            }
        }

        private void DrawDscLevelEntry(Sdl2Window window)
        {
            Vector2 pos = Vector2.One;
            pos.X = 500 + 350 - 1;
            pos.Y = 18;
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always, Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(window.Width - pos.X, window.Height - 18), ImGuiCond.Always);

            if (ImGui.Begin("##dsc_window", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar))
            {
                AlfModule levelModule = Alf.Modules[SelectedEntry];
                AlfEntry selectedEntry = levelModule.Entries[SelectedLevelEntry];

                ImGui.Text($"DSC: {selectedEntry.Name}");

                var extracted = DscExtractor.ExtractDsc(selectedEntry.GetContents());
                ImGui.TextUnformatted(extracted);

                ImGui.End();
            }
        }

        private void DrawTextureWindow(Sdl2Window window)
        {
            Vector2 pos = Vector2.One;
            pos.X = 500 + 350 - 1;
            pos.Y = 18;
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always, Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(window.Width - pos.X, window.Height - 18), ImGuiCond.Always);

            if (ImGui.Begin("##texture_window", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar))
            {
                AlfModule levelModule = Alf.Modules[SelectedEntry];
                AlfModule textureModule = Alf.Modules[SelectedEntry + 1];
                AlfEntry selectedTexture = textureModule.Entries[SelectedTexture];
                DrawTextureEntry(levelModule, selectedTexture);

                ImGui.End();
            }
        }

        private void DrawTextureWindowLevel(Sdl2Window window)
        {
            Vector2 pos = Vector2.One;
            pos.X = 500 + 350 - 1;
            pos.Y = 18;
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always, Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(window.Width - pos.X, window.Height - 18), ImGuiCond.Always);

            if (ImGui.Begin("##texture_window", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar))
            {
                AlfModule levelModule = Alf.Modules[SelectedEntry];
                AlfModule textureModule = Alf.Modules[SelectedEntry + 1];
                AlfEntry selectedTexture = levelModule.Entries[SelectedLevelEntry];
                DrawTextureEntry(levelModule, selectedTexture);

                ImGui.End();
            }
        }

        private void DrawTextureEntry(AlfModule levelModule, AlfEntry selectedTextureEntry)
        {
            ImGui.Text($"Texture: {selectedTextureEntry.Name}");

            if (!HasImage && ImGui.Button("View me"))
            {
                byte[] paletteData = levelModule.Entries.Find(e => e.Name.EndsWith("PAL")).GetContents();
                var br = new BinaryReader(new MemoryStream(paletteData));
                Palette p = Palette.LoadFromPal(br);

                byte[] imageData = selectedTextureEntry.GetContents();
                ImageData temp = PixExtractor.ExtractImage(imageData, p);

                Texture img = Gd.ResourceFactory.CreateTexture(new TextureDescription
                {
                    Height = (uint)temp.Height,
                    Width = (uint)temp.Width,
                    Format = PixelFormat.R8_G8_B8_A8_UNorm_SRgb,
                    Type = TextureType.Texture2D,
                    Usage = TextureUsage.Sampled,
                    MipLevels = 1,
                    Depth = 1,
                    ArrayLayers = 1
                });

                GCHandle pinnedArray = GCHandle.Alloc(temp.PixelData, GCHandleType.Pinned);
                IntPtr pointer = pinnedArray.AddrOfPinnedObject();

                Gd.UpdateTexture(img, pointer, (uint)temp.PixelData.Length, 0, 0, 0, (uint)temp.Width, (uint)temp.Height, 1, 0, 0);

                pinnedArray.Free();

                CurrentImage = img;
                HasImage = true;
                ZoomFactor = 1;
            }

            if (HasImage)
            {
                if (ImGui.Button("Extract as PNG"))
                {
                    byte[] paletteData = levelModule.Entries.Find(e => e.Name.EndsWith("PAL")).GetContents();
                    var br = new BinaryReader(new MemoryStream(paletteData));
                    Palette p = Palette.LoadFromPal(br);

                    byte[] imageData = selectedTextureEntry.GetContents();
                    ImageData temp = PixExtractor.ExtractImage(imageData, p);

                    string fn =
                        Path.Combine(
                            Path.GetDirectoryName(ReadableAlfPath),
                            "extract");
                    fn = Path.Combine(fn, Path.GetFileNameWithoutExtension(selectedTextureEntry.Name) + ".png");
                    var img = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(temp.PixelData, temp.Width, temp.Height);
                    img.Save(fn);
                }
                ImGui.SameLine();
                if (ImGui.Button("Extract uncompressed"))
                {
                    byte[] fileData = Utils.UnpackBoPaCompressedData(selectedTextureEntry.GetContents());
                    string fn =
                        Path.Combine(
                            Path.GetDirectoryName(ReadableAlfPath),
                            "extract");
                    fn = Path.Combine(fn, selectedTextureEntry.Name);
                    File.WriteAllBytes(fn, fileData);
                }
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

        private void OpenAlf()
        {
            Alf = new AlfArchive(ReadableAlfPath);
            SelectedEntry = -1;
            SelectedLevelEntry = -1;
        }

        private void CloseAlf()
        {
            Alf = null;
        }

        private void SelectEntry(int id)
        {
            SelectedEntry = id;
            SelectedTexture = -1;
            SelectedLevelEntry = -1;
        }

        private void SelectTexture(int id)
        {
            SelectedLevelEntry = -1;
            SelectedTexture = id;

            if (HasImage || CurrentImage != null)
            {
                HasImage = false;
                CurrentImage?.Dispose();
            }
        }

        private void SelectLevelEntry(int id)
        {
            SelectedTexture = -1;
            SelectedLevelEntry = id;

            if (HasImage || CurrentImage != null)
            {
                HasImage = false;
                CurrentImage?.Dispose();
            }
        }
    }
}
