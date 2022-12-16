using OpenShadows.Data.Game;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using static OpenShadows.Data.Game.Level;

namespace OpenShadows.FileFormats.Levels
{
    /// <summary>
    /// This extractor is still VERY MUCH work-in-progress and will be
    /// continously updated whenever new information about the file format
    /// gets known.
    /// 
    /// TODO:
    /// - Billboards
    /// - Blocking/Non-blocking
    /// - Alpha-test (hard alpha) textures
    /// - World space texture coordinates? (Texture coordinates may actually be in world space)
    /// </summary>
    public static class Level3dmExtractor
    {
        /// <summary>
        /// Source: https://github.com/shihan42/BrightEyesWiki/wiki/3DM
        /// </summary>
        public static Level ExtractLevel(byte[] uncompressedData, string setName, float worldScaleFactor)
        {
            if (uncompressedData.Length < 4)
            {
                throw new InvalidDataException("Not a valid level file (too small)");
            }

            using var f = new BinaryReader(new MemoryStream(uncompressedData));

            // header (0x0A 0x68 0x24 0x00)
            byte[] header = f.ReadBytes(4);
            if (header[0] != 0x0A ||
                header[1] != 0x68 ||
                header[2] != 0x24 ||
                header[3] != 0x00)
            {
                throw new InvalidDataException("Not a valid level file (invalid header)");
            }

            // Length of file
            uint length = f.ReadUInt32();
            if (length != uncompressedData.Length)
            {
                throw new InvalidDataException("Not a valid level file (invalid length in header)");
            }

            // Read offsets
            uint objectCount = f.ReadUInt32();
            uint unkCount = f.ReadUInt32();
            uint materialCount = f.ReadUInt32();
            uint unk4 = f.ReadUInt32();
            uint totalFaces = f.ReadUInt32();
            uint totalVertices = f.ReadUInt32();
            uint stringTableOffset = f.ReadUInt32();
            uint objectTableOffset = f.ReadUInt32();
            uint unkTableOffset = f.ReadUInt32();
            uint materialTableOffset = f.ReadUInt32();
            uint objectDataOffset = f.ReadUInt32();
            ushort val1 = f.ReadUInt16();   // 65 00
            ushort val2 = f.ReadUInt16();   // 08 00
            uint unk12 = f.ReadUInt32();    // 00 00 00 00
            uint unk13 = f.ReadUInt32();    // 00 00 00 00

            // Read string table
            var stringTable = ReadStringTable(f, stringTableOffset, objectTableOffset);

            // Read ???
            ReadUnknownBlock(f, unkCount, unkTableOffset);

            // Read materials
            var materialLookup = ReadMaterialBlock(f, stringTable, materialCount, materialTableOffset);

            // Read object table
            var objectList = ReadObjectBlock(f, stringTable, objectCount, objectTableOffset);

            // Read object data            

            // ============================
            //  Debugging stuff
            bool runTests = true;
            List<string> objUnderTest = new List<string>();
            /*objUnderTest.Add("10N9999_B1");
            objUnderTest.Add("04N9999_MH");
            objUnderTest.Add("11N9999_TP");
            objUnderTest.Add("01N9999_21");
            objUnderTest.Add("01N9999_22");
            objUnderTest.Add("36N9999_08");
            objUnderTest.Add("01O9999_04");*/
            objUnderTest.Add("01N9999_04");
            // ============================

            for (int objIdx = 0; objIdx < objectList.Count; objIdx++)
            {
                var level3dmObject = objectList[objIdx];

                // ============================
                //  Debugging stuff
                if (runTests)
                {
                    if (objUnderTest.Contains(level3dmObject.Name) == false)
                    {
                        continue;
                    }
                }
                // ============================

                Log.Debug("Name: " + level3dmObject.Name);
                Log.Debug($"  {nameof(level3dmObject.VectorCount)}: {level3dmObject.VectorCount}");
                Log.Debug($"  {nameof(level3dmObject.GroupsCount)}: {level3dmObject.GroupsCount}");
                Log.Debug($"  {nameof(level3dmObject.SurfaceVertexCount)}: {level3dmObject.SurfaceVertexCount}");
                Log.Debug($"  {nameof(level3dmObject.QuadCount)}: {level3dmObject.QuadCount}");
                Log.Debug($"  {nameof(level3dmObject.Count3)}: {level3dmObject.Count3}");
                Log.Debug($"  {nameof(level3dmObject.AdditionalQuadCount)}: {level3dmObject.AdditionalQuadCount}");

                // Read object data
                f.BaseStream.Position = level3dmObject.DataOffset;

                // Read vertex positions
                ReadLevelObjectVectors(f, level3dmObject);

                // Read unknown properties
                // Unknown, somehow related to z-ordering?
                // Messing with these values causes the faces to have
                // different sorting in DSA3.
                // Maybe it's not needed for modern engines.
                ReadUnknownLevelObjectData(f, level3dmObject);

                // Read surface vertices
                ReadLevelObjectVertices(f, level3dmObject);

                // Create a list of surfaces/groups for this object
                // These seem to be logical "groups" or "sub objects" of vertices, but they
                // may have different materials (see next part) in the same group,
                // so its usefulness is limited.
                CreateSurfaceList(level3dmObject);

                // Parse faces (quads/triangles)
                // These index into the surfaceList above, but specify which face uses
                // which material.
                BuildLevelQuads(f, materialLookup, level3dmObject);

                // Additional materials (potentially two-sided?)
                BuildAdditionalQuads(f, materialLookup, level3dmObject);
            }

            // Create the level
            Level result = new Level();
            result.Name = setName;

            foreach (var item in materialLookup)
            {
                result.Materials.Add(new LevelMaterial()
                {
                    Name = item.Value.Name,
                    Alpha = item.Value.Alpha,
                    TextureName = item.Value.TextureName
                });
            }

            for (int objIdx = 0; objIdx < objectList.Count; objIdx++)
            {
                var level3dmObject = objectList[objIdx];

                // ============================
                //  Debugging stuff
                if (runTests)
                {
                    if (objUnderTest.Contains(level3dmObject.Name) == false)
                    {
                        continue;
                    }
                }
                // ============================

                LevelObject levelObject = new LevelObject();
                levelObject.Name = level3dmObject.Name;
                levelObject.BoundingBox = level3dmObject.CreateLevelBoundingBox(worldScaleFactor);
                levelObject.Vertices = new LevelObjectVertex[level3dmObject.SurfaceVertices.Length];
                for (int i = 0; i < level3dmObject.SurfaceVertices.Length; i++)
                {
                    levelObject.Vertices[i] = level3dmObject.SurfaceVertices[i].CreateLevelVertex(worldScaleFactor);
                }

                levelObject.FaceLists = new LevelObjectFaceList[level3dmObject.QuadList.Count];
                for (int i = 0; i < level3dmObject.QuadList.Count; i++)
                {
                    levelObject.FaceLists[i] = level3dmObject.QuadList[i].CreateLevelObjectFaceList();
                }
                result.Objects.Add(levelObject);
            }

            return result;
        }

        private static void BuildAdditionalQuads(BinaryReader f, Dictionary<uint, Level3dmMaterial> materialLookup, Level3dmObject levelObject)
        {
            f.BaseStream.Position = levelObject.DataOffset + levelObject.AdditionalMaterialsOffset;
            for (int i = 0; i < levelObject.AdditionalQuadCount; i++)
            {
                uint unk = f.ReadUInt32();
                uint materialOffset = f.ReadUInt32();
                uint quadOffset = f.ReadUInt32();

                if (levelObject.QuadOffsetLookup.ContainsKey(quadOffset) == false)
                {
                    Log.Warning("Failed to lookup quad instance");
                    continue;
                }
                var originalQuad = levelObject.QuadOffsetLookup[quadOffset];
                var copy = originalQuad.CreateCopy();
                if (materialLookup.ContainsKey(materialOffset))
                {
                    var material = materialLookup[materialOffset];
                    copy.Material = material;
                }
                copy.V1 = originalQuad.AltV1;
                copy.V2 = originalQuad.AltV2;
                copy.V3 = originalQuad.AltV3;
                copy.V4 = originalQuad.AltV4;
                Log.Verbose($"Loaded additional quad with material '{copy.Material.Name}' (copying original quad '{originalQuad}')");
                levelObject.QuadList.Add(copy);
            }
        }

        private static void BuildLevelQuads(BinaryReader f, Dictionary<uint, Level3dmMaterial> materialLookup, Level3dmObject levelObject)
        {
            Level3dmVertex[] surfaceVertices = levelObject.SurfaceVertices;
            Level3dmSurface[] surfaceList = levelObject.SurfaceList;

            f.BaseStream.Position = levelObject.DataOffset + levelObject.MaterialsOffset;

            Level3dmQuad[] quadList = new Level3dmQuad[levelObject.QuadCount];
            for (int i = 0; i < levelObject.QuadCount; i++)
            {
                long relativeOffset = f.BaseStream.Position - levelObject.DataOffset;                
                ushort _unk = f.ReadUInt16();

                ushort vertex1 = f.ReadUInt16();
                ushort vertex2 = f.ReadUInt16();
                ushort vertex3 = f.ReadUInt16();
                ushort vertex4 = f.ReadUInt16();

                uint _unk2 = f.ReadUInt32();
                uint _unk3 = f.ReadUInt32();

                // Purpose unknown. They are usually the same as the vertex indices 
                // above, but not always. The biggest issue is that these indices
                // sometimes have a higher number than available in the surfaceVertices
                // array, so they must be an index to something else.
                ushort unk_index1 = f.ReadUInt16();
                ushort unk_index2 = f.ReadUInt16();
                ushort unk_index3 = f.ReadUInt16();
                ushort unk_index4 = f.ReadUInt16();

                uint _unk4 = f.ReadUInt32();
                uint _unk5 = f.ReadUInt32();
                Log.Verbose($"  Quad {i}: _unk4 {_unk4}");
                Log.Verbose($"  Quad {i}: _unk5 {_unk5}");

                // Flags or "type", but not yet clear what it means
                ushort flags = f.ReadUInt16();
                Log.Verbose($"  Quad {i}: Flags {flags}");

                uint surfaceIndex = f.ReadUInt32();
                uint materialOffset = f.ReadUInt32();
                uint unk = f.ReadUInt32();
                Log.Verbose($"  Quad {i}: unk {unk}");

                var surface = surfaceList[surfaceIndex];

                quadList[i] = new Level3dmQuad();
                quadList[i].IsTriangle = vertex4 >= surfaceVertices.Length ||
                    surface.Vertices.Contains(surfaceVertices[vertex4]) == false;
                quadList[i].V1 = vertex1;
                quadList[i].V2 = vertex2;
                quadList[i].V3 = vertex3;
                if (quadList[i].IsTriangle == false)
                {
                    quadList[i].V4 = vertex4;
                }
                quadList[i].AltV1 = unk_index1;
                quadList[i].AltV2 = unk_index2;
                quadList[i].AltV3 = unk_index3;
                if (quadList[i].IsTriangle == false)
                {
                    quadList[i].AltV4 = unk_index4;
                }
                if (materialLookup.ContainsKey(materialOffset))
                {
                    var material = materialLookup[materialOffset];
                    quadList[i].Material = material;
                }

                levelObject.QuadOffsetLookup.Add(relativeOffset, quadList[i]);

                Log.Verbose($"  Quad {i}: {quadList[i]}");
            }

            levelObject.QuadList.AddRange(quadList);
        }

        private static void CreateSurfaceList(Level3dmObject levelObject)
        {
            var surfaceVertices = levelObject.SurfaceVertices;

            int highestIndex = 0;

            for (int i = 0; i < surfaceVertices.Length; i++)
            {
                if (surfaceVertices[i].PrimitiveIndex >= highestIndex)
                {
                    highestIndex = surfaceVertices[i].PrimitiveIndex + 1;
                }
            }

            var list = new Level3dmSurface[highestIndex];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = new Level3dmSurface();
            }

            for (int i = 0; i < surfaceVertices.Length; i++)
            {
                list[surfaceVertices[i].PrimitiveIndex].Vertices.Add(surfaceVertices[i]);
            }

            levelObject.SurfaceList = list;
        }

        private static void ReadLevelObjectVertices(BinaryReader f, Level3dmObject levelObject)
        {
            Level3dmVector[] vertexPositions = levelObject.VertexPositions;
            Level3dmVertex[] surfaceVertices = new Level3dmVertex[levelObject.SurfaceVertexCount];
            for (int j = 0; j < levelObject.SurfaceVertexCount; j++)
            {
                long pos = f.BaseStream.Position - (levelObject.DataOffset + levelObject.QuadsOffset);
                ushort vectorIndex = f.ReadUInt16();
                // World Location
                surfaceVertices[j].X = vertexPositions[vectorIndex].X;
                surfaceVertices[j].Y = vertexPositions[vectorIndex].Y;
                surfaceVertices[j].Z = vertexPositions[vectorIndex].Z;
                // Group(?) index
                surfaceVertices[j].PrimitiveIndex = f.ReadUInt16();
                // Texture coordinates
                surfaceVertices[j].U = f.ReadInt32();
                surfaceVertices[j].V = f.ReadInt32();
                Log.Debug($"{j}: {vectorIndex} {surfaceVertices[j].PrimitiveIndex} " +
                    $"{surfaceVertices[j].U} {surfaceVertices[j].V} (Offset: {pos})");
            }
            levelObject.SurfaceVertices = surfaceVertices;
        }

        private static void ReadUnknownLevelObjectData(BinaryReader f, Level3dmObject levelObject)
        {
            for (int j = 0; j < levelObject.GroupsCount; j++)
            {
                byte[] bytes = f.ReadBytes(12);
                Log.Verbose($"{j}: {BitConverter.ToString(bytes)}");

                /*ushort x = f.ReadUInt16();
                ushort y = f.ReadUInt16();
                ushort z = f.ReadUInt16();
                ushort x2 = f.ReadUInt16();
                ushort y2 = f.ReadUInt16();
                ushort z2 = f.ReadUInt16();
                Log.Verbose($"{j}: {x},{y},{z},{x2},{y2},{z2}");*/

                /*int a = f.ReadUInt16();
                int b = f.ReadUInt16();
                int y = f.ReadInt32();
                int z = f.ReadInt32();
                Log.Verbose($"{j}: {a},{b},{y},{z} ({(j + levelObject.VertexCount)})");*/

                /*int x = f.ReadInt32();
                int y = f.ReadInt32();
                int z = f.ReadInt32();
                Log.Verbose($"{j}: {x},{y},{z}");*/
            }
        }

        private static void ReadLevelObjectVectors(BinaryReader f, Level3dmObject levelObject)
        {
            Level3dmVector[] vertexPositions = new Level3dmVector[levelObject.VectorCount];
            for (int j = 0; j < levelObject.VectorCount; j++)
            {
                int x = f.ReadInt32();
                int y = f.ReadInt32();
                int z = f.ReadInt32();
                vertexPositions[j] = new Level3dmVector()
                {
                    X = x,
                    Y = y,
                    Z = z
                };
            }

            levelObject.VertexPositions = vertexPositions;
        }

        private static List<Level3dmObject> ReadObjectBlock(BinaryReader f, Dictionary<int, string> stringTable, 
            uint objectCount, uint objectTableOffset)
        {
            f.BaseStream.Position = objectTableOffset;

            var result = new List<Level3dmObject>();
            for (int i = 0; i < objectCount; i++)
            {
                // Fixed size chunks
                int stringLookup = f.ReadInt32();
                string name = "unknown";
                if (stringTable.ContainsKey(stringLookup))
                {
                    name = stringTable[stringLookup];
                }

                Level3dmObject levelObject = new Level3dmObject();
                levelObject.Name = name;

                levelObject.VectorCount = f.ReadUInt16();
                levelObject.GroupsCount = f.ReadUInt16();

                levelObject.SurfaceVertexCount = f.ReadUInt16();
                levelObject.QuadCount = f.ReadUInt16();
                levelObject.Count3 = f.ReadUInt16();
                levelObject.AdditionalQuadCount = f.ReadUInt16();

                levelObject.BoundingBox.CenterX = f.ReadInt32();
                levelObject.BoundingBox.CenterY = f.ReadInt32();
                levelObject.BoundingBox.CenterZ = f.ReadInt32();

                levelObject.BoundingBox.Unknown = f.ReadInt32();

                levelObject.BoundingBox.MinX = f.ReadInt32();
                levelObject.BoundingBox.MinY = f.ReadInt32();
                levelObject.BoundingBox.MinZ = f.ReadInt32();

                levelObject.BoundingBox.MaxX = f.ReadInt32();
                levelObject.BoundingBox.MaxY = f.ReadInt32();
                levelObject.BoundingBox.MaxZ = f.ReadInt32();

                levelObject.DataOffset = f.ReadUInt32();
                levelObject.VertexBytes = f.ReadUInt32();

                levelObject.QuadsOffset = f.ReadUInt32();
                levelObject.MaterialsOffset = f.ReadUInt32();
                levelObject.AdditionalMaterialsOffset = f.ReadUInt32();

                // Unknown
                levelObject.UnknownBlock = f.ReadBytes(48);

                result.Add(levelObject);
            }
            return result;
        }

        private static Dictionary<uint, Level3dmMaterial> ReadMaterialBlock(BinaryReader f, Dictionary<int, string> stringTable, 
            uint materialCount, uint materialTableOffset)
        {
            f.BaseStream.Position = materialTableOffset;

            var materialLookup = new Dictionary<uint, Level3dmMaterial>();
            for (int i = 0; i < materialCount; i++)
            {
                Level3dmMaterial mat = new Level3dmMaterial();

                materialLookup.Add((uint)f.BaseStream.Position, mat);

                int stringLookup = f.ReadInt32();
                string name = "unknown";
                if (stringTable.ContainsKey(stringLookup))
                {
                    name = stringTable[stringLookup];
                }
                mat.Name = name;

                f.ReadBytes(2);

                mat.Alpha = f.ReadUInt16();

                f.ReadBytes(4);
                ushort unk = f.ReadUInt16();

                f.ReadBytes(30);

                stringLookup = f.ReadInt32();
                name = "unknown";
                if (stringTable.ContainsKey(stringLookup))
                {
                    name = stringTable[stringLookup];
                }
                mat.TextureName = name;

                f.ReadInt32();

                Log.Debug($"{mat.Name} = {mat.TextureName}");
            }

            return materialLookup;
        }

        private static void ReadUnknownBlock(BinaryReader f, uint unkCount, uint unkTableOffset)
        {
            f.BaseStream.Position = unkTableOffset;

            for (int i = 0; i < unkCount; i++)
            {
                // Unknown, but each entry is 88 bytes in size
                byte[] data = f.ReadBytes(88);
            }
        }

        private static Dictionary<int, string> ReadStringTable(BinaryReader f, uint stringTableOffset, uint objectTableOffset)
        {
            f.BaseStream.Position = stringTableOffset;

            var stringTable = new Dictionary<int, string>();
            while (f.BaseStream.Position < objectTableOffset)
            {
                int lookupPosition = (int)f.BaseStream.Position - 0x40 + 1;
                string s = Utils.ExtractString(f);
                stringTable.Add(lookupPosition, s);
            }
            return stringTable;
        }

        internal class Level3dmObject
        {
            internal Level3dmBoundingBox BoundingBox;

            internal string Name;

            internal int VectorCount;
            internal int GroupsCount;
            internal int SurfaceVertexCount;
            internal int QuadCount;
            internal int Count3;
            internal int AdditionalQuadCount;

            internal uint DataOffset;
            internal uint VertexBytes;

            internal uint QuadsOffset;
            internal uint MaterialsOffset;
            internal uint AdditionalMaterialsOffset;

            internal byte[] UnknownBlock;

            internal Level3dmVector[] VertexPositions;
            internal Level3dmVertex[] SurfaceVertices;
            internal Level3dmSurface[] SurfaceList;

            internal Dictionary<long, Level3dmQuad> QuadOffsetLookup = new Dictionary<long, Level3dmQuad>();
            internal List<Level3dmQuad> QuadList = new List<Level3dmQuad>();

            internal BoundingBox CreateLevelBoundingBox(float worldScaleFactor)
            {
                return new BoundingBox()
                {
                    CenterX = (float)BoundingBox.CenterX * worldScaleFactor,
                    CenterY = (float)BoundingBox.CenterY * worldScaleFactor,
                    CenterZ = (float)BoundingBox.CenterZ * worldScaleFactor,

                    MinX = (float)BoundingBox.MinX * worldScaleFactor,
                    MinY = (float)BoundingBox.MinY * worldScaleFactor,
                    MinZ = (float)BoundingBox.MinZ * worldScaleFactor,

                    MaxX = (float)BoundingBox.MaxX * worldScaleFactor,
                    MaxY = (float)BoundingBox.MaxY * worldScaleFactor,
                    MaxZ = (float)BoundingBox.MaxZ * worldScaleFactor,

                    Unknown = (float)BoundingBox.Unknown * worldScaleFactor,
                };
            }
        }

        internal class Level3dmMaterial
        {
            public string Name;

            public ushort Alpha;

            public string TextureName;

            public override string ToString()
            {
                return $"{Name} => {TextureName} (Alpha: {Alpha})";
            }
        }

        internal struct Level3dmBoundingBox
        {
            public int CenterX;
            public int CenterY;
            public int CenterZ;

            public int Unknown;

            public int MinX;
            public int MinY;
            public int MinZ;

            public int MaxX;
            public int MaxY;
            public int MaxZ;
        }

        internal class Level3dmQuad
        {
            public int V1;
            public int V2;
            public int V3;
            public int V4;

            public int AltV1;
            public int AltV2;
            public int AltV3;
            public int AltV4;

            public Level3dmMaterial Material;

            public bool IsTriangle;

            public Level3dmQuad CreateCopy()
            {
                return new Level3dmQuad()
                {
                    V1 = V1,
                    V2 = V2,
                    V3 = V3,
                    V4 = V4,
                    Material = Material,
                    IsTriangle = IsTriangle,
                };
            }

            public override string ToString()
            {
                return $"{{ v1: {V1}; v2: {V2}; v3: {V3}; v4: {V4};Tri:{IsTriangle};Mat:{Material.Name} }}";
            }

            internal LevelObjectFaceList CreateLevelObjectFaceList()
            {
                var res = new LevelObjectFaceList();
                res.Material = new LevelMaterial()
                {
                    Name = Material.Name,
                    Alpha = Material.Alpha,
                    TextureName = Material.TextureName,
                };

                if (IsTriangle)
                {
                    res.TriangleCount = 1;
                    res.VertexIndices = new int[3]
                    {
                        V1,
                        V2,
                        V3
                    };
                }
                else
                {
                    res.TriangleCount = 2;
                    res.VertexIndices = new int[6]
                    {
                        V1,
                        V2,
                        V4,

                        V2,
                        V3,
                        V4,
                    };
                }
                return res;
            }
        }

        internal struct Level3dmVector
        {
            public int X;
            public int Y;
            public int Z;

            public override string ToString()
            {
                return $"{X},{Y},{Z}";
            }
        }

        internal struct Level3dmVertex
        {
            public int X;
            public int Y;
            public int Z;

            public ushort PrimitiveIndex;

            public int U;
            public int V;

            public override string ToString()
            {
                return $"[{X},{Y},{Z};{U},{V}]";
            }

            internal LevelObjectVertex CreateLevelVertex(float worldScaleFactor)
            {
                return new LevelObjectVertex()
                {
                    X = (float)X * worldScaleFactor,
                    Y = (float)Y * worldScaleFactor,
                    Z = (float)Z * worldScaleFactor,

                    U = (float)U / 65535.0f,
                    V = (float)V / 65535.0f
                };
            }
        }

        internal class Level3dmSurface
        {
            public List<Level3dmVertex> Vertices = new List<Level3dmVertex>();

            public Level3dmSurface()
            {
            }

            public override string ToString()
            {
                return $"{Vertices.Count}";
            }
        }
    }
}
