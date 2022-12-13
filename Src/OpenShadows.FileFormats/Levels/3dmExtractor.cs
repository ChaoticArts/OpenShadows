using OpenShadows.Data.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static OpenShadows.Data.Game.Level;

namespace OpenShadows.FileFormats.Levels
{
    public static class Level3dmExtractor
    {
        public static byte[] UncompressLevel(byte[] data)
        {
            using var f = new BinaryReader(new MemoryStream(data));

            // skip
            f.ReadBytes(0x0a);

            // unpack BoPa
            uint uncompressedSize = Utils.SwapEndianess(f.ReadUInt32());
            uint compressedSize = Utils.SwapEndianess(f.ReadUInt32());
            var uncompressedData = new byte[uncompressedSize];
            var compressedData = f.ReadBytes((int)compressedSize);

            Utils.UnpackBoPa(compressedData, compressedSize, uncompressedData, uncompressedSize);

            return uncompressedData;
        }

        /// <summary>
        /// Source: https://github.com/shihan42/BrightEyesWiki/wiki/3DM
        /// </summary>
        /// <param name="uncompressedData"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static Level ExtractLevel(byte[] uncompressedData)
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

            Level result = new Level();

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
            f.BaseStream.Position = stringTableOffset;
            while (f.BaseStream.Position < objectTableOffset)
            {
                int lookupPosition = (int)f.BaseStream.Position - 0x40 + 1;
                string s = Utils.ExtractString(f);
                if (string.IsNullOrEmpty(s)) 
                {
                    break;
                }
                result.StringTable.Add(lookupPosition, s);
            }

            // Read ???
            f.BaseStream.Position = unkTableOffset;
            for (int i = 0; i < unkCount; i++)
            {
                // Unknown, but each entry is 88 bytes in size
                byte[] data = f.ReadBytes(88);
            }

            // Read materials
            f.BaseStream.Position = materialTableOffset;
            for (int i = 0; i < materialCount; i++)
            {
                ObjectMaterial mat = new ObjectMaterial();
                result.Materials.Add(mat);

                int stringLookup = f.ReadInt32();
                string name = "unknown";
                if (result.StringTable.ContainsKey(stringLookup))
                {
                    name = result.StringTable[stringLookup];
                }
                mat.Name = name;

                f.ReadBytes(2);

                mat.Alpha = f.ReadUInt16();

                f.ReadBytes(4);
                ushort unk = f.ReadUInt16();

                f.ReadBytes(30);

                stringLookup = f.ReadInt32();
                name = "unknown";
                if (result.StringTable.ContainsKey(stringLookup))
                {
                    name = result.StringTable[stringLookup];
                }
                mat.TextureName = name;

                f.ReadInt32();
            }

            // Read object data
            f.BaseStream.Position = objectDataOffset;

            // Read object table
            f.BaseStream.Position = objectTableOffset;
            for (int i = 0; i < objectCount; i++)
            {
                // Fixed size chunks
                int stringLookup = f.ReadInt32();
                string name = "unknown";
                if (result.StringTable.ContainsKey(stringLookup))
                {
                    name = result.StringTable[stringLookup];
                }

                LevelObject levelObject = new LevelObject();
                levelObject.Name = name;
                result.Objects.Add(levelObject);

                levelObject.VertexCount = f.ReadUInt16();
                levelObject.QuadCount = f.ReadUInt16();

                f.ReadInt16();
                f.ReadInt16();
                f.ReadInt16();
                f.ReadInt16();

                levelObject.BoundingBox.CenterX = f.ReadInt32();
                levelObject.BoundingBox.CenterY = f.ReadInt32();
                levelObject.BoundingBox.CenterZ = f.ReadInt32();

                f.ReadInt32();

                levelObject.BoundingBox.MinX = f.ReadInt32();
                levelObject.BoundingBox.MinY = f.ReadInt32();
                levelObject.BoundingBox.MinZ = f.ReadInt32();

                levelObject.BoundingBox.MaxX = f.ReadInt32();
                levelObject.BoundingBox.MaxY = f.ReadInt32();
                levelObject.BoundingBox.MaxZ = f.ReadInt32();

                uint offset = f.ReadUInt32();
                uint vertexBytes = f.ReadUInt32();

                uint quadsOffset = f.ReadUInt32();
                uint materialsOffset = f.ReadUInt32();
                uint materials2Offset = f.ReadUInt32();

                // Unknown
                f.ReadBytes(48);
                long curPos = f.BaseStream.Position;

                // Read faces
                try
                {
                    // Read object data
                    f.BaseStream.Position = offset;

                    // Read vertices
                    LevelObjectVertex[] vertices = new LevelObjectVertex[levelObject.VertexCount];
                    for (int j = 0; j < levelObject.VertexCount; j++)
                    {
                        vertices[j] = new LevelObjectVertex()
                        {
                            X = f.ReadInt32(),
                            Y = f.ReadInt32(),
                            Z = f.ReadInt32()
                        };
                    }
                    levelObject.Vertices = vertices;

                    f.BaseStream.Position = offset + quadsOffset;
                    // "Faces" are actually quads
                    LevelObjectQuad[] quads = new LevelObjectQuad[levelObject.QuadCount];
                    for (int j = 0; j < levelObject.QuadCount; j++)
                    {
                        quads[j] = new LevelObjectQuad();
                        for (int k = 0; k < 4; k++)
                        {
                            quads[j].Indices[k] = f.ReadUInt16();
                            quads[j].Indices2[k] = f.ReadUInt16();
                            quads[j].Extra1[k] = f.ReadInt32();
                            quads[j].Extra2[k] = f.ReadInt32();
                        }
                    }
                    levelObject.Quads = quads;
                    levelObject.CreateTriangles();

                    // Read materials
                    f.BaseStream.Position = offset + materialsOffset;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                finally
                {
                    f.BaseStream.Position = curPos;
                }
            }

            return result;
        }
    }
}
