using System;
using System.Collections.Generic;
using System.Text;

namespace OpenShadows.Data.Game
{
    public struct LevelObjectFace
    {
        public int v1;
        public int v2;
        public int v3;
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

    public class Level
    {
        public class LevelObject
        {
            public string Name;

            public ushort FaceCount;
            public ushort VertexCount;

            public BoundingBox BoundingBox;
            public LevelObjectFace[] Faces;

            public override string ToString()
            {
                return Name;
            }
        }

        public Dictionary<int, string> StringTable = new Dictionary<int, string>();

        public List<LevelObject> Objects = new List<LevelObject>();
    }
}
