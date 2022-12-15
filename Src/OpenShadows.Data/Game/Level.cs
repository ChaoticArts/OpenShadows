using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace OpenShadows.Data.Game
{
    public struct LevelObjectFaceList
    {
        public int TriangleCount;
        public int[] VertexIndices;
        public LevelMaterial Material;

        public override string ToString()
        {
            return $"{VertexIndices.Length},{Material.Name}";
        }
    }

    public struct LevelObjectVertex
    {
        public float X;
        public float Y;
        public float Z;

        public float U;
        public float V;

        public override string ToString()
        {
            return $"{X},{Y},{Z};{U},{V}";
        }
    }

    public struct BoundingBox
    {
        public float CenterX;
        public float CenterY;
        public float CenterZ;

        public float Unknown;

        public float MinX;
        public float MinY;
        public float MinZ;

        public float MaxX;
        public float MaxY;
        public float MaxZ;
    }

    public class LevelMaterial
    {
        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value.Replace(' ', '_').Replace('-', '_');
            }
        }

        public ushort Alpha;

        public string TextureName;

        public override string ToString()
        {
            return $"{Name} => {TextureName} (Alpha: {Alpha})";
        }
    }

    public class LevelObject
    {
        public string Name;

        public BoundingBox BoundingBox;
        public LevelObjectVertex[] Vertices;
        public LevelObjectFaceList[] FaceLists;

        public override string ToString()
        {
            return Name;
        }
    }

    public class Level
    {
        public string Name;        

        public List<LevelObject> Objects = new List<LevelObject>();

        public List<LevelMaterial> Materials = new List<LevelMaterial>();

        public void DumpToObjIndividual(string folder, float scaleFactor)
        {
            if (Directory.Exists(folder) == false)
            {
                Directory.CreateDirectory(folder);
            }

            string fn = Path.Combine(folder, "level.mtl");
            using var sw2 = new StreamWriter(fn);

            for (int i = 0; i < Materials.Count; i++)
            {
                var mat = Materials[i];
                sw2.WriteLine("newmtl " + mat.Name);
                if (mat.TextureName != "unknown")
                {
                    sw2.WriteLine("   Ka 1.000 1.000 1.000");
                    sw2.WriteLine("   Kd 1.000 1.000 1.000");
                    sw2.WriteLine("   map_Ka " + mat.TextureName + ".png");
                    sw2.WriteLine("   map_Kd " + mat.TextureName + ".png");
                }
                else 
                {
                    sw2.WriteLine("   Ka 1.000 0.000 1.000");
                    sw2.WriteLine("   Kd 1.000 0.000 1.000");
                    sw2.WriteLine("   d 0.2");
                }
                sw2.WriteLine();
            }

            for (int objIdx = 0; objIdx < Objects.Count; objIdx++)
            {
                var item = Objects[objIdx];

                fn = Path.Combine(folder, item.Name + ".obj");
                using var sw = new StreamWriter(fn);
                sw.WriteLine("mtllib level.mtl");
                sw.WriteLine();

                sw.WriteLine("o " + item.Name);
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

                for (int i = 0; i < item.Vertices.Length; i++)
                {
                    var vert = item.Vertices[i];
                    float u = (float)vert.U;
                    float v = (float)vert.V;
                    sw.WriteLine("vt " +
                        u.ToString("F5", CultureInfo.InvariantCulture) + " " +
                        v.ToString("F5", CultureInfo.InvariantCulture));
                }

                for (int i = 0; i < item.FaceLists.Length; i++)
                {
                    var fl = item.FaceLists[i];

                    sw.WriteLine("usemtl " + fl.Material.Name);
                    for (int j = 0; j < fl.VertexIndices.Length; j+=3)
                    {
                        var v1 = fl.VertexIndices[j + 0] + 1;
                        var v2 = fl.VertexIndices[j + 1] + 1;
                        var v3 = fl.VertexIndices[j + 2] + 1;
                        sw.WriteLine($"f {v1}/{v1} {v2}/{v2} {v3}/{v3}");
                    }
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

            for (int i = 0; i < Materials.Count; i++)
            {
                var mat = Materials[i];
                sw2.WriteLine("newmtl " + mat.Name);
                if (mat.TextureName != "unknown")
                {
                    sw2.WriteLine("   Ka 1.000 1.000 1.000");
                    sw2.WriteLine("   Kd 1.000 1.000 1.000");
                    sw2.WriteLine("   map_Ka " + mat.TextureName + ".png");
                    sw2.WriteLine("   map_Kd " + mat.TextureName + ".png");
                }
                else
                {
                    sw2.WriteLine("   Ka 1.000 0.000 1.000");
                    sw2.WriteLine("   Kd 1.000 0.000 1.000");
                    sw2.WriteLine("   d 0.2");
                }
                sw2.WriteLine();
            }

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

            for (int j = 0; j < Objects.Count; j++)
            {
                var item = Objects[j];

                for (int i = 0; i < item.Vertices.Length; i++)
                {
                    var vert = item.Vertices[i];
                    float u = (float)vert.U;
                    float v = (float)vert.V;
                    sw.WriteLine("vt " +
                        u.ToString("F5", CultureInfo.InvariantCulture) + " " +
                        v.ToString("F5", CultureInfo.InvariantCulture));
                }
            }

            for (int objIdx = 0; objIdx < Objects.Count; objIdx++)
            {
                var item = Objects[objIdx];

                baseIndex = objectBaseIndices[objIdx];

                for (int i = 0; i < item.FaceLists.Length; i++)
                {
                    var fl = item.FaceLists[i];

                    sw.WriteLine("usemtl " + fl.Material.Name);
                    for (int j = 0; j < fl.VertexIndices.Length; j += 3)
                    {
                        var v1 = fl.VertexIndices[j + 0] + 1 + baseIndex;
                        var v2 = fl.VertexIndices[j + 1] + 1 + baseIndex;
                        var v3 = fl.VertexIndices[j + 2] + 1 + baseIndex;
                        sw.WriteLine($"f {v1}/{v1} {v2}/{v2} {v3}/{v3}");
                    }
                }
            }
        }
    }
}
