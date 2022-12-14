using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace OpenShadows.Data.Game
{
    // Note: Likely wrong!!
    [Flags]
    public enum LevelObjectQuadFlags : UInt16
    {
        None            = 0b_0000_0000_0000_0000,
        Unk1            = 0b_0000_0000_0000_0001,
        Unk2            = 0b_0000_0000_0000_0010,
        Unk3            = 0b_0000_0000_0000_0100,
        Unk4            = 0b_0000_0000_0000_1000,
        Unk5            = 0b_0000_0000_0001_0000,
        Unk6            = 0b_0000_0000_0010_0000,
        Unk7            = 0b_0000_0000_0100_0000,        
    }

    public enum GeometryType
    {
        Unknown,
        Triangle,
        Quad
    }

    public struct LevelObjectQuad
    {
        public ushort[] Indices = new ushort[4];
        public ushort[] Indices2 = new ushort[4];
        public int[] Extras1 = new int[4];
        public int[] Extras2 = new int[4];

        public uint MaterialOffset = 0;
        public ObjectMaterial Material = null;
        public LevelObjectQuadFlags Flags = LevelObjectQuadFlags.None;
        public GeometryType GeometryType = GeometryType.Unknown;

        public LevelObjectQuad()
        {

        }

        public override string ToString()
        {
            if (Indices.Length != 4)
            {
                return "Invalid Quad";
            }
            return $"{GeometryType} - {Indices[0]} {Indices[1]} {Indices[2]} {Indices[3]}";
        }
    }

    public class LevelObjectPrimitive
    {
        public List<ushort> Indices = new List<ushort>();
        public List<int> Extras1 = new List<int>();
        public List<int> Extras2 = new List<int>();

        public uint MaterialOffset = 0;
        public ObjectMaterial Material = null;

        public LevelObjectPrimitive()
        {
            //
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < Indices.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(" ");
                }
                sb.Append(Indices[i].ToString());
            }
            return sb.ToString();
        }
    }

    public struct LevelObjectFace
    {
        public int V1;
        public int V2;
        public int V3;
        public ObjectMaterial Material;

        public override string ToString()
        {
            return $"{V1},{V2},{V3}";
        }
    }

    public struct LevelObjectVector
    {
        public int X;
        public int Y;
        public int Z;

        public override string ToString()
        {
            return $"{X},{Y},{Z}";
        }
    }

    public struct LevelObjectVertex
    {
        public int X;
        public int Y;
        public int Z;

        public float U;
        public float V;

        public override string ToString()
        {
            return $"{X},{Y},{Z};{U},{V}";
        }
    }

    public struct BoundingBox
    {
        public int CenterX;
        public int CenterY;
        public int CenterZ;

        public int MinX;
        public int MinY;
        public int MinZ;

        public int MaxX;
        public int MaxY;
        public int MaxZ;
    }

    public class LevelObject
    {
        public string Name;

        public int QuadCount;
        public int VertexCount;

        public BoundingBox BoundingBox;
        public LevelObjectQuad[] Quads;
        public LevelObjectVertex[] Vertices;
        public LevelObjectFace[] Faces;

        private int GetFaceCount()
        {
            int count = 0;
            for (int i = 0; i < Quads.Length; i++)
            {
                var type = Quads[i].GeometryType;

                if (type == GeometryType.Triangle)
                {
                    count++;
                }
                else
                {
                    count += 2;
                }
            }
            return count;
        }

        public void CreateTriangles()
        {
            int faceCount = GetFaceCount();

            Faces = new LevelObjectFace[faceCount];
            int index = 0;
            for (int i = 0; i < Quads.Length; i++)
            {
                var type = Quads[i].GeometryType;

                if (type == GeometryType.Triangle)
                {
                    Faces[index++] = new LevelObjectFace()
                    {
                        V1 = Quads[i].Indices[0],
                        V2 = Quads[i].Indices[1],
                        V3 = Quads[i].Indices[2],
                        Material = Quads[i].Material,
                    };
                }
                else
                {
                    Faces[index++] = new LevelObjectFace()
                    {
                        V1 = Quads[i].Indices[0],
                        V2 = Quads[i].Indices[1],
                        V3 = Quads[i].Indices[3],
                        Material = Quads[i].Material,
                    };

                    Faces[index++] = new LevelObjectFace()
                    {
                        V1 = Quads[i].Indices[1],
                        V2 = Quads[i].Indices[2],
                        V3 = Quads[i].Indices[3],
                        Material = Quads[i].Material,
                    };
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class ObjectMaterial
    {
        public string Name;

        public ushort Alpha;

        public string TextureName;

        public override string ToString()
        {
            return $"{Name} => {TextureName} (Alpha: {Alpha})";
        }
    }

    public class Level
    {
        public Dictionary<int, string> StringTable = new Dictionary<int, string>();

        public List<LevelObject> Objects = new List<LevelObject>();

        public List<ObjectMaterial> Materials = new List<ObjectMaterial>();

        public void DumpToObjIndividual(string folder, float scaleFactor)
        {
            if (Directory.Exists(folder) == false)
            {
                Directory.CreateDirectory(folder);
            }

            string fn = Path.Combine(folder, "level.mtl");
            using var sw2 = new StreamWriter(fn);

            sw2.WriteLine("newmtl checker");
            sw2.WriteLine("   Ka 1.000 1.000 1.000");
            sw2.WriteLine("   Kd 1.000 1.000 1.000");
            sw2.WriteLine("   map_Ka MH1_GRND.png");
            sw2.WriteLine("   map_Kd MH1_GRND.png");
            sw2.WriteLine();

            for (int j = 0; j < Objects.Count; j++)
            {
                var item = Objects[j];

                if (item.Name.Contains("01N9999_34"))
                {
                    int hehe = 42;
                }

                fn = Path.Combine(folder, item.Name + ".obj");
                using var sw = new StreamWriter(fn);
                sw.WriteLine("mtllib level.mtl");
                sw.WriteLine();

                for (int i = 0; i < item.Vertices.Length; i++)
                {
                    var v = item.Vertices[i];
                    float x = (float)v.X * scaleFactor;
                    float y = (float)v.Y * scaleFactor;
                    float z = (float)v.Z * scaleFactor;
                    sw.WriteLine("v " +
                        x.ToString("F5", CultureInfo.InvariantCulture) + " " +
                        y.ToString("F5", CultureInfo.InvariantCulture) + " " +
                        z.ToString("F5", CultureInfo.InvariantCulture));
                }

                sw.WriteLine("vt 0.00000 0.00000 0.00000");
                sw.WriteLine("vt 0.00000 1.00000 0.00000");
                sw.WriteLine("vt 1.00000 0.00000 0.00000");
                sw.WriteLine("vt 1.00000 1.00000 0.00000");

                sw.WriteLine("g Faces_" + item.Name);
                sw.WriteLine("usemtl checker");
                for (int i = 0; i < item.Faces.Length; i++)
                {
                    var f = item.Faces[i];
                    /*if (f.Material != null &&
                        f.Material.Alpha < 100)
                    {
                        continue;
                    }*/
                    sw.WriteLine("f " + (f.V1 + 1) + "/1 " + (f.V2 + 1) + "/2 " + (f.V3 + 1) + "/3");
                }
            }
        }

        public void DumpToObj(string folder, float scaleFactor)
        {
            if (Directory.Exists(folder) == false)
            {
                Directory.CreateDirectory(folder);
            }

            string fn = Path.Combine(folder, "level.mtl");
            using var sw2 = new StreamWriter(fn);

            sw2.WriteLine("newmtl checker");
            sw2.WriteLine("   Ka 1.000 1.000 1.000");
            sw2.WriteLine("   Kd 1.000 1.000 1.000");
            sw2.WriteLine("   map_Ka MH1_GRND.png");
            sw2.WriteLine("   map_Kd MH1_GRND.png");
            sw2.WriteLine();

            fn = Path.Combine(folder, "level.obj");
            using var sw = new StreamWriter(fn);
            sw.WriteLine("mtllib level.mtl");
            sw.WriteLine();
            //sw.WriteLine("g verts");
            int baseIndex = 0;
            List<int> objectBaseIndices = new List<int>();
            for (int j = 0; j < Objects.Count; j++)
            {
                var item = Objects[j];

                for (int i = 0; i < item.Vertices.Length; i++)
                {
                    var v = item.Vertices[i];
                    float x = (float)v.X * scaleFactor;
                    float y = (float)v.Y * scaleFactor;
                    float z = (float)v.Z * scaleFactor;
                    sw.WriteLine("v " +
                        x.ToString("F5", CultureInfo.InvariantCulture) + " " +
                        y.ToString("F5", CultureInfo.InvariantCulture) + " " +
                        z.ToString("F5", CultureInfo.InvariantCulture));
                }

                objectBaseIndices.Add(baseIndex);
                baseIndex += item.Vertices.Length;
            }

            sw.WriteLine("vt 0.00000 0.00000 0.00000");
            sw.WriteLine("vt 0.00000 1.00000 0.00000");
            sw.WriteLine("vt 1.00000 0.00000 0.00000");
            sw.WriteLine("vt 1.00000 1.00000 0.00000");

            for (int j = 0; j < Objects.Count; j++)
            {
                var item = Objects[j];

                /*if (item.Faces.Length == 1 &&
                    (item.Faces[0].Material == null ||
                     item.Faces[0].Material.Alpha < 100))
                {
                    continue;
                }*/

                baseIndex = objectBaseIndices[j];

                sw.WriteLine("g Faces_" + item.Name);
                sw.WriteLine("usemtl checker");
                for (int i = 0; i < item.Faces.Length; i++)
                {
                    var f = item.Faces[i];
                    /*if (f.Material == null ||
                        f.Material.Alpha < 100)
                    {
                        continue;
                    }*/
                    sw.WriteLine("f " + (f.V1 + 1 + baseIndex) + "/1 " + (f.V2 + 1 + baseIndex) + "/2 " + (f.V3 + 1 + baseIndex) + "/3");
                }
            }
        }
    }
}
