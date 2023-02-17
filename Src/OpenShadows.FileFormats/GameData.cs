using ImGuiNET;
using OpenShadows.Data.Game;
using OpenShadows.Data.Graphic;
using OpenShadows.Data.Rendering;
using OpenShadows.FileFormats.Archive;
using OpenShadows.FileFormats.Images;
using OpenShadows.FileFormats.Text;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using static GLSLang.SpirV.Instruction;
using static System.Runtime.InteropServices.JavaScript.JSType;

#nullable enable

namespace OpenShadows.FileFormats
{
    public enum GameDataFolderValidation
    {
        Ok,
        NotFound,
        NotSet,
        Invalid
    }

    /// <summary>
    /// Accessor class for DSA3 game data.
    /// This works on abstracted resource paths and caches game data.
    /// 
    /// Use for actual (original) game data, not for derived data (e.g. 3D meshes, textures, etc.)
    /// </summary>
    public class GameData
    {
        public static string PrimaryFolder { get; set; } = string.Empty;

        public static string PrimaryDataFolder => Path.Combine(PrimaryFolder, "DATA");

        private static Dictionary<string, object> resourceCache = new Dictionary<string, object>();
        private static Dictionary<string, AlfArchive> archiveCache = new Dictionary<string, AlfArchive>();

        public static GameDataFolderValidation ValidatePrimaryFolder()
        {
            if (string.IsNullOrWhiteSpace(PrimaryFolder))
            {
                return GameDataFolderValidation.NotSet;
            }

            DirectoryInfo di = new DirectoryInfo(PrimaryFolder);
            if (di.Exists == false)
            {
                return GameDataFolderValidation.NotFound;
            }

            FileInfo rivaExeInfo = new FileInfo(Path.Combine(di.FullName, "RIVA.EXE"));
            if (rivaExeInfo.Exists == false)
            {
                return GameDataFolderValidation.Invalid;
            }

            return GameDataFolderValidation.Ok;
        }

        /// <summary>
        /// Returns the full path to the file in the primary data folder as specified by the 
        /// resource path (e.g. dungeon.alf/market01.3dm/mh1_wal2.pix would return the full
        /// path to the file dungeon.alf).
        /// </summary>
        public static string GetFullFilePathToResource(string resource)
        {
            resource = resource.ToUpperInvariant();
            string[] resourceElements = resource.Split('/');

            if (resourceElements.Length == 0)
            {
                throw new InvalidDataException("Invalid resource path: " + resource);
            }

            string firstElement = resourceElements[0];
            // The first element must always be a file name in the data folder
            string fullFilename = Path.Combine(PrimaryDataFolder, firstElement);
            if (File.Exists(fullFilename) == false)
            {
                throw new InvalidDataException($"Resource '{resource}' does not point to a valid file in the primary data folder at '{PrimaryDataFolder}'");
            }

            return fullFilename;
        }

        /// <summary>
        /// Returns true if the specified resource is in a container file (i.e. in an .alf file)
        /// riva.alf/jette.bob would return true
        /// bosper.bob would return false
        /// </summary>
        public static bool IsResourceInContainer(string resource)
        {
            resource = resource.ToUpperInvariant();
            string[] resourceElements = resource.Split('/');

            if (resourceElements.Length == 0)
            {
                throw new InvalidDataException("Invalid resource path: " + resource);
            }

            string firstElement = resourceElements[0];
            // The first element must always be a file name in the data folder
            string fullFilename = Path.Combine(PrimaryDataFolder, firstElement);
            if (File.Exists(fullFilename) == false)
            {
                throw new InvalidDataException($"Resource '{resource}' does not point to a valid file in the primary data folder at '{PrimaryDataFolder}'");
            }

            return Path.GetExtension(fullFilename).ToLowerInvariant() == ".alf";
        }

        /// <summary>
        /// Removes the resource from the cache.
        /// 
        /// Faster than <see cref="UnloadResource{T}(T)"/>.
        /// </summary>
        public static void UnloadResource(string resource)
        {
            resource = resource.ToUpperInvariant();

            if (resourceCache.ContainsKey(resource))
            {
                resourceCache.Remove(resource);
            }
        }

        /// <summary>
        /// Removes the resource from the cache.
        /// 
        /// Slow. Use <see cref="UnloadResource(string)"/> for a faster variant.
        /// </summary>
        public static void UnloadResource<T>(T resource) where T : class
        {
            foreach (var pair in resourceCache)
            {
                if ((pair.Value as T) == resource)
                {
                    resourceCache.Remove(pair.Key);
                }
            }
        }

        /// <summary>
        /// Loads and returns the specified resource.
        /// 
        /// Safe to call multiple times: If the resource was already previously loaded, a cached 
        /// version is returned.
        /// </summary>
        public static T? LoadResource<T>(string resource) where T : class
        {
            resource = resource.ToUpperInvariant();
            if (resourceCache.ContainsKey(resource))
            {
                return (T)resourceCache[resource];
            }

            // Must be refactored to be more robust using strongly typed load functions in the
            // respective classes. The current way of using a switch on the type and the 
            // [xyz]Extractor classes isn't good.

            Type t = typeof(T);
            string typeName = t.Name.ToLowerInvariant();

            switch (typeName)
            {
                case "byte[]":
                    {
                        var result = GetResourceContentRaw(resource);
                        if (result != null)
                        {
                            resourceCache[resource] = result;
                        }
                    }
                    break;

                case "imagedata":
                    {
                        var result = GetResourceContentRaw(resource);
                        if (result != null)
                        {
                            var palResource = FindPaletteResourceForImage(resource);
                            if (palResource != null)
                            {
                                var palette = LoadResource<Palette>(palResource);
                                if (palette != null)
                                {
                                    var imageData = PixExtractor.ExtractImage(result, palette);
                                    if (imageData != null)
                                    {
                                        resourceCache[resource] = imageData;
                                    }
                                }
                            }
                        }
                    }
                    break;

                case "palette":
                    {
                        var result = GetResourceContentRaw(resource);
                        if (result != null)
                        {
                            var br = new BinaryReader(new MemoryStream(result));
                            var palette = Palette.LoadFromPal(br);
                            if (palette != null)
                            {
                                resourceCache[resource] = palette;
                            }
                        }
                    }
                    break;

                case "stringtable":
                    {
                        var resBytes = GetResourceContentRaw(resource);
                        if (resBytes != null)
                        {
                            StringTable stringTable;
                            var resName = GetPrimaryResourceName(resource).ToUpperInvariant();
                            if (resName == "ITEMNAME.LXT" ||
                                resName == "PRINTER.LXT" ||
                                resName == "MONNAMES.LXT")
                            {
                                stringTable = LxtExtractor.ExtractRawStringTable(resBytes);
                            }
                            else
                            {
                                stringTable = LxtExtractor.ExtractStringTable(resBytes);
                            }
                            resourceCache[resource] = stringTable;
                        }
                    }
                    break;

                default:
                    throw new InvalidOperationException($"The type '{t.Name}' is not a valid resource type. " +
                        $"Use byte[] at <T> to access raw data of a resource.");
            }

            if (resourceCache.ContainsKey(resource))
            {
                return (T)resourceCache[resource];
            }

            return default(T);
        }

        private static byte[]? GetResourceContentRaw(string resource)
        {
            string fullFilepath = GetFullFilePathToResource(resource);
            bool isContainer = IsResourceInContainer(resource);

            if (isContainer == false)
            {
                return File.ReadAllBytes(fullFilepath);
            }
            else
            {
                var subresource = GetSubResourceNameInContainer(resource).ToUpperInvariant();
                var archive = GetArchive(resource);
                for (int i = 0; i < archive.Entries.Count; i++)
                {
                    if (archive.Entries[i].Name.ToUpperInvariant() == subresource)
                    {
                        return archive.Entries[i].GetContents();
                    }
                }
            }

            return null;
        }

        private static AlfArchive GetArchive(string resource)
        {
            var archiveName = GetPrimaryResourceName(resource);
            if (archiveCache.ContainsKey(archiveName))
            {
                return archiveCache[archiveName];
            }

            string fullFilepath = GetFullFilePathToResource(resource);
            var arch = new AlfArchive(fullFilepath);
            archiveCache[archiveName] = arch;
            return arch;
        }

        private static string GetPrimaryResourceName(string resource)
        {
            string[] resourceElements = resource.Split('/');

            if (resourceElements.Length == 0)
            {
                throw new InvalidDataException("Invalid resource path: " + resource);
            }

            return resourceElements[0];
        }

        private static string GetSubResourceNameInContainer(string resource)
        {
            string[] resourceElements = resource.Split('/');

            if (resourceElements.Length != 2)
            {
                throw new InvalidDataException("Invalid resource path: " + resource);
            }

            return resourceElements[1];
        }

        /// <summary>
        /// Searches for the matching palette resource when given 
        /// a path to a valid .PIX resource.
        /// 
        /// Relatively slow operation, because every module has to be searched.
        /// </summary>
        public static string? FindPaletteResourceForImage(string resource)
        {
            resource = resource.ToUpperInvariant();
            bool isContainer = IsResourceInContainer(resource);
            if (isContainer == false)
            {
                throw new InvalidOperationException("Can only search for palette entries of .PIX files in .alf containers.");
            }

            string pixFile = GetSubResourceNameInContainer(resource).ToLowerInvariant();
            if (Path.GetExtension(pixFile) != ".pix")
            {
                throw new InvalidOperationException("Can only search for palette entries of .PIX files");
            }

            AlfArchive archive = GetArchive(resource);
            if (archive == null)
            {
                throw new InvalidOperationException($"No ALF file found with name '{resource}'");
            }

            string archiveResourceName = GetPrimaryResourceName(resource);

            for (int modIdx = 0; modIdx < archive.Modules.Count - 1; modIdx += 2)
            {
                AlfModule levelMod = archive.Modules[modIdx];
                AlfModule textureMod = archive.Modules[modIdx + 1];

                for (int i = 0; i < textureMod.Entries.Count; i++)
                {
                    var entry = textureMod.Entries[i];
                    if (entry.Name.ToLowerInvariant() == pixFile)
                    {
                        var palEntry = levelMod.Entries.Find(e => e.Name.EndsWith("PAL"));
                        if (palEntry != null)
                        {
                            return archiveResourceName + "/" + palEntry.Name;
                        }
                    }
                }
            }

            return null;
        }
    }
}
