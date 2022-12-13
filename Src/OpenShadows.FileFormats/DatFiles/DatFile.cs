using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;

namespace OpenShadows.FileFormats.DatFiles
{
    public abstract class DatFile
    {
        internal abstract bool Extract(BinaryReader reader);
    }
}
