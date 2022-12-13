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
using OpenShadows.FileFormats.DatFiles;
using OpenShadows.FileFormats.Images;
using OpenShadows.FileFormats.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.Sdl2;
using static System.Net.Mime.MediaTypeNames;

namespace OpenShadows.Workbench.Screens
{
    public class AlfBrowserScreen : IScreen
    {
        public string Title { get; } = "ALF Browser";

        private AlfArchive Alf;

        private string ReadableAlfPath = "DATA\\RIVA.ALF";

        private string SearchString = "ACE";

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
                ushort crc16 = Utils.Crc16(entry.GetContents());
                ImGui.Text($"   CRC16: {BitConverter.ToString(new byte[] { (byte)(crc16 >> 8), (byte)crc16 })} ({crc16})");

                if (ImGui.Button("Extract"))
                {
                    string path = Path.Combine(Path.GetDirectoryName(ReadableAlfPath), "extract");
                    if (Directory.Exists(path) == false)
                    {
                        Directory.CreateDirectory(path);
                    }
                    byte[] entryContents = entry.GetContents();
                    File.WriteAllBytes(Path.Combine(path, entry.Name), entryContents);
                }

                if (string.Equals(entry.Name.End(3), "AIF", StringComparison.OrdinalIgnoreCase))
                {
                    DrawImageViewer("AIF");
                }
                if (string.Equals(entry.Name.End(3), "NVF", StringComparison.OrdinalIgnoreCase))
                {
                    DrawImageViewer("NVF");
                }
                if (string.Equals(entry.Name.End(3), "ACE", StringComparison.OrdinalIgnoreCase))
                {
                    DrawImageViewer("ACE");
                }

                if (string.Equals(entry.Name.End(3), "LXT", StringComparison.Ordinal))
                {
                    DrawTextViewer("lxt");
                }
                if (string.Equals(entry.Name.End(3), "XDF", StringComparison.Ordinal))
                {
                    DrawTextViewer("xdf");
                }

                if (string.Equals(entry.Name.End(3), "DAT", StringComparison.Ordinal))
                {
                    DrawDatViewer(entry.Name);
                }

                if (string.Equals(entry.Name.End(3), "OFF", StringComparison.Ordinal))
                {
                    DrawOffsetsViewer();
                }

                ImGui.End();
            }
        }

        private Texture CurrentImage;

        private List<ImageData> CurrentImages;

        private int CurrentImageIndex = 0;

        private bool HasImage;

        private uint ZoomFactor = 1;

        private void DrawImageViewer(string imageType)
        {
            ImGui.Text("I'm a viewable image");

            if (!HasImage && ImGui.Button("View me"))
            {
                byte[] data = Alf.Entries[SelectedEntry].GetContents();

                if (imageType == "AIF")
                {
                    CurrentImages = new List<ImageData>
                    {
                        AifExtractor.ExtractImage(data)
                    };
                    CurrentImageIndex = 0;
                }
                else if (imageType == "NVF")
                {
                    // special handling of DUALPICS.NVF
                    if (Alf.Entries[SelectedEntry].Name == "DUALPICS.NVF")
                    {
                        byte[] dualPals = Alf.Entries.First(p => p.Name == "DUALPALS.DAT").GetContents();
                        var extraPalettes = new List<Palette>();
                        for (int i = 0; i < 13; i++)
                        {
                            var span = new ReadOnlySpan<byte>(dualPals, i * 96, 96);
                            var p = new Palette(span.ToArray(), 32);
                            extraPalettes.Add(p);
                        }

                        CurrentImages = NvfExtractor.ExtractImage(data, extraPalettes);
                    }
                    else
                    {
                        CurrentImages = NvfExtractor.ExtractImage(data);
                    }
                    CurrentImageIndex = 0;
                }
                else if (imageType == "ACE")
                {
                    try
                    {
                        CurrentImages = AceExtractor.ExtractImage(data).Animations[0].Images;
                        CurrentImageIndex = 0;
                    }
                    catch (NotImplementedException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                else
                {
                    throw new NotSupportedException($"format {imageType} is not yet supported");
                }

                if (CurrentImages != null)
                {
                    LoadImage();
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

                ImGui.Text($"Image {CurrentImageIndex + 1}/{CurrentImages.Count}");

                ImGui.SameLine();
                if (ImGui.Button("Next") && CurrentImageIndex < CurrentImages.Count - 1)
                {
                    CurrentImageIndex++;
                    LoadImage(true);
                }

                ImGui.SameLine();
                if (ImGui.Button("Last") && CurrentImageIndex > 0)
                {
                    CurrentImageIndex--;
                    LoadImage(true);
                }

                ImGui.Image(ImGuiRenderer.GetOrCreateImGuiBinding(Gd.ResourceFactory, CurrentImage), new Vector2(CurrentImage.Width * ZoomFactor, CurrentImage.Height * ZoomFactor));
            }

            if (HasImage && ImGui.Button("Extract as PNG"))
            {
                for (int i = 0; i < CurrentImages.Count; i++)
                {
                    var curImg = CurrentImages[i];
                    string fn =
                        Path.Combine(
                            Path.GetDirectoryName(ReadableAlfPath),
                            "extract");
                    if (CurrentImages.Count == 1)
                    {
                        fn = Path.Combine(fn, Path.GetFileNameWithoutExtension(Alf.Entries[SelectedEntry].Name) + ".png");
                    }
                    else
                    {
                        fn = Path.Combine(fn, Path.GetFileNameWithoutExtension(Alf.Entries[SelectedEntry].Name) + "_" + i.ToString("D3") + ".png");
                    }
                    var img = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(curImg.PixelData, curImg.Width, curImg.Height);
                    img.Save(fn);
                }
            }
        }

        private void DrawDatViewer(string filename)
        {
            byte[] data = Alf.Entries[SelectedEntry].GetContents();
            switch (filename)
            {
                case "AMAPIPOS.DAT":
                    {
                        DatMapIconPosition file = DatExtractor.ExtractDatFile<DatMapIconPosition>(data);
                    }
                    break;

                default:
                    {

                    }
                    break;
            }
        }

        private void DrawTextViewer(string textType)
        {
            byte[] data = Alf.Entries[SelectedEntry].GetContents();

            List<Tuple<int, string>> textTuples = null;

            if (string.Equals(textType, "lxt", StringComparison.OrdinalIgnoreCase))
            {
                if (Alf.Entries[SelectedEntry].Name == "ITEMNAME.LXT" ||
                    Alf.Entries[SelectedEntry].Name == "PRINTER.LXT" ||
                    Alf.Entries[SelectedEntry].Name == "MONNAMES.LXT")
                {
                    textTuples = LxtExtractor.ExtractRawTexts(data);
                }
                else
                {
                    textTuples = LxtExtractor.ExtractTexts(data);
                }
            }
            if (string.Equals(textType, "xdf", StringComparison.OrdinalIgnoreCase))
            {
                textTuples = XdfExtractor.ExtractTexts(data);
            }

            if (textTuples != null)
            {
                ImGui.BeginChild("##text_list");

                ImGui.Columns(2);
                ImGui.Text("idx"); ImGui.NextColumn();
                ImGui.Text("Text"); ImGui.NextColumn();
                ImGui.Separator();

                ImGui.SetColumnWidth(0, 50.0f);

                foreach ((int idx, string text) in textTuples)
                {
                    if (ImGui.Selectable(idx.ToString(), false, ImGuiSelectableFlags.SpanAllColumns))
                    { }
                    ImGui.NextColumn();
                    ImGui.TextUnformatted(text); ImGui.NextColumn();
                }

                ImGui.Columns(1);

                ImGui.EndChild();
            }
        }

        private void DrawOffsetsViewer()
        {
            byte[] data = Alf.Entries[SelectedEntry].GetContents();
            var offsets = OffExtractor.ExtractOffsets(data);

            ImGui.BeginChild("##text_list");

            ImGui.Columns(13);
            for (int i = 0; i < 13; i++)
            {
                ImGui.Text(i.ToString()); ImGui.NextColumn();
            }
            ImGui.Separator();

            ImGui.SetColumnWidth(0, 50.0f);

            foreach (var off in offsets)
            {
                for (int i = 0; i < 13; i++)
                {
                    ImGui.Text(off.Data[i].ToString());
                    ImGui.NextColumn();
                }
            }

            ImGui.Columns(1);

            ImGui.EndChild();
        }

        private void LoadImage(bool keepZoom = false)
        {
            CurrentImage?.Dispose();

            ImageData temp = CurrentImages[CurrentImageIndex];

            if (temp?.Height > 0)
            {
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
                ZoomFactor = keepZoom ? ZoomFactor : 1;
            }
        }

        private void OpenAlf()
        {
            Alf = new AlfArchive(ReadableAlfPath);
            SelectedEntry = -1;

            /*
			foreach (AlfEntry entry in Alf.Entries.Where(e => e.Name.ToUpper().EndsWith("ACE")))
			{
				byte[] data = entry.GetContents();
				string path = Path.Combine(@"Y:\Projekte\Reverse Engineering\NLT\OpenShadows", entry.Name);
				File.WriteAllBytes(path, data);
			}
			*/
        }

        private void CloseAlf()
        {
            Alf = null;
            GC.Collect();
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
