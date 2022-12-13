using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace OpenShadows.Data.Game
{
    public struct LevelObjectQuad
    {
        public ushort[] Indices = new ushort[4];
        public ushort[] Indices2 = new ushort[4];
        public int[] Extras1 = new int[4];
        public int[] Extras2 = new int[4];

        public uint MaterialOffset = 0;
        public ObjectMaterial Material = null;

        public LevelObjectQuad()
        {

        }

        public override string ToString()
        {
            if (Indices.Length != 4)
            {
                return "Invalid Quad";
            }
            return $"{Indices[0]} {Indices[1]} {Indices[2]} {Indices[3]}";
        }
    }

    public struct LevelObjectFace
    {
        public int V1;
        public int V2;
        public int V3;

        public override string ToString()
        {
            return $"{V1},{V2},{V3}";
        }
    }

    public struct LevelObjectVertex
    {
        public int X;
        public int Y;
        public int Z;

        public override string ToString()
        {
            return $"{X},{Y},{Z}";
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

        public void CreateTriangles()
        {
            Faces = new LevelObjectFace[Quads.Length * 2];
            for (int i = 0; i < Quads.Length; i++)
            {
                Faces[i * 2 + 0] = new LevelObjectFace()
                {
                    V1 = Quads[i].Indices[0],
                    V2 = Quads[i].Indices[1],
                    V3 = Quads[i].Indices[3],
                };

                Faces[i * 2 + 1] = new LevelObjectFace()
                {
                    V1 = Quads[i].Indices[1],
                    V2 = Quads[i].Indices[2],
                    V3 = Quads[i].Indices[3],
                };
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

        public void DumpToObj(string folder, float scaleFactor)
        {
            if (Directory.Exists(folder) == false)
            {
                Directory.CreateDirectory(folder);
            }

            string fn = Path.Combine(folder, "level.obj");
            using var sw = new StreamWriter(fn);
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

            for (int j = 0; j < Objects.Count; j++)
            {
                var item = Objects[j];
                baseIndex = objectBaseIndices[j];

                sw.WriteLine("g Faces_" + item.Name);
                for (int i = 0; i < item.Faces.Length; i++)
                {
                    var f = item.Faces[i];
                    sw.WriteLine("f " + (f.V1 + 1 + baseIndex) + " " + (f.V2 + 1 + baseIndex) + " " + (f.V3 + 1 + baseIndex));
                }
            }
        }
    }
}
