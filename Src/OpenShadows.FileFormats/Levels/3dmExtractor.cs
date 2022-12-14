using OpenShadows.Data.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.Serialization;
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
            Dictionary<uint, ObjectMaterial> materialLookup = new Dictionary<uint, ObjectMaterial>();
            f.BaseStream.Position = materialTableOffset;
            for (int i = 0; i < materialCount; i++)
            {
                ObjectMaterial mat = new ObjectMaterial();
                result.Materials.Add(mat);

                materialLookup.Add((uint)f.BaseStream.Position, mat);

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

                levelObject.VertexCount = f.ReadUInt16();
                ushort primitiveCount1 = f.ReadUInt16();

                ushort surfaceVertexCount = f.ReadUInt16();
                levelObject.QuadCount = f.ReadUInt16();
                ushort vertexCount3 = f.ReadUInt16();
                ushort quadCount3 = f.ReadUInt16();

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

                uint dataOffset = f.ReadUInt32();
                uint vertexBytes = f.ReadUInt32();

                uint quadsOffset = f.ReadUInt32();
                uint materialsOffset = f.ReadUInt32();
                uint quadMaterialsOffset = f.ReadUInt32();

                // Unknown
                f.ReadBytes(48);
                long curPos = f.BaseStream.Position;

                // ============================
                //  Debugging stuff
                bool runTests = false;
                List<string> objUnderTest = new List<string>();
                objUnderTest.Add("10N9999_B1");
                objUnderTest.Add("04N9999_MH");
                objUnderTest.Add("11N9999_TP");
                objUnderTest.Add("01N9999_21");
                objUnderTest.Add("01N9999_22");
                objUnderTest.Add("36N9999_08");                

                if (runTests)
                {
                    if (objUnderTest.Contains(levelObject.Name) == false)
                    {
                        continue;
                    }
                }
                // ============================

                result.Objects.Add(levelObject);

                // Read faces
                try
                {
                    Console.WriteLine("Name: " + levelObject.Name);
                    Console.WriteLine($"{nameof(levelObject.VertexCount)}: {levelObject.VertexCount}");
                    Console.WriteLine($"{nameof(primitiveCount1)}: {primitiveCount1}");
                    Console.WriteLine($"{nameof(surfaceVertexCount)}: {surfaceVertexCount}");
                    Console.WriteLine($"{nameof(levelObject.QuadCount)}: {levelObject.QuadCount}");
                    Console.WriteLine($"{nameof(vertexCount3)}: {vertexCount3}");
                    Console.WriteLine($"{nameof(quadCount3)}: {quadCount3}");

                    // Read object data
                    f.BaseStream.Position = dataOffset;

                    // Read vertex positions
                    LevelObjectVector[] vertexPositions = new LevelObjectVector[levelObject.VertexCount];
                    for (int j = 0; j < levelObject.VertexCount; j++)
                    {
                        int x = f.ReadInt32();
                        int y = f.ReadInt32();
                        int z = f.ReadInt32();
                        vertexPositions[j] = new LevelObjectVector()
                        {
                            X = x,
                            Y = y,
                            Z = z
                        };
                    }
                    for (int j = 0; j < primitiveCount1; j++)
                    {
                        /*ushort x = f.ReadUInt16();
                        ushort y = f.ReadUInt16();
                        ushort z = f.ReadUInt16();
                        ushort x2 = f.ReadUInt16();
                        ushort y2 = f.ReadUInt16();
                        ushort z2 = f.ReadUInt16();
                        Console.WriteLine($"{j}: {x},{y},{z},{x2},{y2},{z2}");*/

                        /*int a = f.ReadUInt16();
                        int b = f.ReadUInt16();
                        int y = f.ReadInt32();
                        int z = f.ReadInt32();
                        Console.WriteLine($"{j}: {a},{b},{y},{z} ({(j + levelObject.VertexCount)})");*/

                        int x = f.ReadInt32();
                        int y = f.ReadInt32();
                        int z = f.ReadInt32();
                        Console.WriteLine($"{j}: {x},{y},{z} ({(j + levelObject.VertexCount)})");
                    }

                    LevelObjectPrimitive[] primitives = new LevelObjectPrimitive[primitiveCount1];
                    for (int j = 0; j < primitives.Length; j++)
                    {
                        primitives[j] = new LevelObjectPrimitive();
                    }

                    //f.BaseStream.Position = dataOffset + quadsOffset;
                    // Read surface vertices
                    LevelObjectVertex[] surfaceVertices = new LevelObjectVertex[surfaceVertexCount];
                    for (int j = 0; j < surfaceVertexCount; j++)
                    {
                        /*quads[j] = new LevelObjectQuad();
                        for (int k = 0; k < 4; k++)
                        {*/
                        long pos = f.BaseStream.Position - (dataOffset + quadsOffset);
                        ushort positionIndex = f.ReadUInt16();
                        surfaceVertices[j].X = vertexPositions[positionIndex].X;
                        surfaceVertices[j].Y = vertexPositions[positionIndex].Y;
                        surfaceVertices[j].Z = vertexPositions[positionIndex].Z;
                        var primitiveIndex = f.ReadUInt16();
                        primitives[primitiveIndex].Indices.Add((ushort)j);
                        var extras1 = f.ReadInt32();
                        var extras2 = f.ReadInt32();
                        primitives[primitiveIndex].Extras1.Add(extras1);
                        primitives[primitiveIndex].Extras2.Add(extras2);
                        Console.WriteLine($"{j}: {positionIndex} {primitiveIndex} {extras1} {extras2} (Offset: {pos})");
                        //}
                    }
                    levelObject.Vertices = surfaceVertices;

                    // Read materials
                    Dictionary<long, LevelObjectQuad> quadOffsetLookup = new Dictionary<long, LevelObjectQuad>();
                    LevelObjectQuad[] quads = new LevelObjectQuad[levelObject.QuadCount + quadCount3];
                    levelObject.Quads = quads;
                    //f.BaseStream.Position = dataOffset + materialsOffset;
                    for (int j = 0; j < levelObject.QuadCount; j++)
                    {
                        //quads[j] = new LevelObjectQuad();

                        //long pos = f.BaseStream.Position - (dataOffset + materialsOffset);
                        long relativeOffset = f.BaseStream.Position - dataOffset;

                        Console.WriteLine($"Quad {j} (Offset: {relativeOffset})");

                        ushort _unk = f.ReadUInt16();
                        //Console.WriteLine($"Quad {j}: {_unk} @ {pos} / {pos2}");

                        ushort vertex1 = f.ReadUInt16();
                        ushort vertex2 = f.ReadUInt16();
                        ushort vertex3 = f.ReadUInt16();
                        ushort vertex4 = f.ReadUInt16();

                        Console.WriteLine($"  Quad {j}: {vertex1},{vertex2},{vertex3},{vertex4}");

                        uint _unk2 = f.ReadUInt32();
                        uint _unk3 = f.ReadUInt32();

                        ushort vertex1a = f.ReadUInt16();
                        ushort vertex2a = f.ReadUInt16();
                        ushort vertex3a = f.ReadUInt16();
                        ushort vertex4a = f.ReadUInt16();

                        bool anyDifference = 
                            vertex1 != vertex1a || 
                            vertex2 != vertex2a || 
                            vertex3 != vertex3a || 
                            vertex4 != vertex4a;

                        if (anyDifference)
                        {
                            Console.WriteLine($"  Quad {j}: {vertex1a},{vertex2a},{vertex3a},{vertex4a}");

                            Console.WriteLine($"  Quad {j}: anyDifference: {anyDifference}");
                        }

                        uint _unk4 = f.ReadUInt32();
                        uint _unk5 = f.ReadUInt32();

                        LevelObjectQuadFlags flags = (LevelObjectQuadFlags)f.ReadUInt16();

                        if (_unk4 != 0 || _unk5 != 0)
                        {
                            Console.WriteLine($"  Quad {j}: _unk4: {_unk4} -- _unk5: {_unk5}");
                        }

                        Console.WriteLine($"  Quad {j}: flags: {flags}");

                        uint primitiveIndex = f.ReadUInt32();
                        uint materialOffset = f.ReadUInt32();
                        uint unk = f.ReadUInt32();

                        Console.WriteLine($"  Quad {j}: primitiveIndex: {primitiveIndex}");
                        Console.WriteLine($"  Quad {j}: unk: {unk}");

                        var primitive = primitives[primitiveIndex];

                        quads[j] = new LevelObjectQuad();
                        quadOffsetLookup.Add(relativeOffset, quads[j]);
                        quads[j].Flags = flags;
                        if (primitive.Indices.Count == 3)
                        {
                            quads[j].Indices[0] = primitive.Indices[0];
                            quads[j].Indices[1] = primitive.Indices[1];
                            quads[j].Indices[2] = primitive.Indices[2];
                            quads[j].GeometryType = GeometryType.Triangle;
                        }
                        else if (primitive.Indices.Count == 4)
                        {
                            quads[j].Indices[0] = primitive.Indices[0];
                            quads[j].Indices[1] = primitive.Indices[1];
                            quads[j].Indices[2] = primitive.Indices[2];
                            quads[j].Indices[3] = primitive.Indices[3];
                            quads[j].GeometryType = GeometryType.Quad;
                        }
                        else
                        {
                            Console.WriteLine("Primitive count mismatch on " + levelObject.Name + " - quad: " + j);
                            quads[j].Indices[0] = vertex1;
                            quads[j].Indices[1] = vertex2;
                            quads[j].Indices[2] = vertex3;
                            quads[j].Indices[3] = vertex4;
                            quads[j].GeometryType = GeometryType.Quad;
                        }

                        if (materialLookup.ContainsKey(materialOffset))
                        {
                            var material = materialLookup[materialOffset];
                            quads[j].MaterialOffset = materialOffset;
                            quads[j].Material = material;

                            Console.WriteLine($"  Quad {j}: Material: {(material != null ? material.Name : "null")}");
                        }
                    }                    

                    // additional material properties (duplicated quads with (potentially) a different material)
                    for (int j = 0; j < quadCount3; j++)
                    {
                        int index = j + levelObject.QuadCount;
                        uint unk = f.ReadUInt32();
                        uint materialOffset = f.ReadUInt32();
                        uint quadOffset = f.ReadUInt32();

                        Console.WriteLine($"Quad instance {index}: quadOffset: {quadOffset} -- materialOffset: {materialOffset} -- unk: {unk}");

                        if (quadOffsetLookup.ContainsKey(quadOffset) == false)
                        {
                            Console.WriteLine("Failed to lookup quad instance");
                            continue;
                        }
                        LevelObjectQuad originalQuad = quadOffsetLookup[quadOffset];

                        quads[index] = new LevelObjectQuad();
                        quads[index].Indices = originalQuad.Indices;
                        quads[index].Indices2 = originalQuad.Indices2;
                        quads[index].Extras1 = originalQuad.Extras1;
                        quads[index].Extras2 = originalQuad.Extras2;
                        quads[index].Flags = originalQuad.Flags;

                        if (materialLookup.ContainsKey(materialOffset)) 
                        { 
                            var material = materialLookup[materialOffset];
                            quads[index].MaterialOffset = materialOffset;
                            quads[index].Material = material;

                            Console.WriteLine($"  Quad instance {index}: Material: {(material != null ? material.Name : "null")}");
                        }
                    }

                    levelObject.CreateTriangles();
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
