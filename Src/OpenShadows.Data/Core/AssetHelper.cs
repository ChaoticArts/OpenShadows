using System;
using System.IO;

namespace OpenShadows.Data.Core
{
    public static class AssetHelper
    {
        private static readonly string s_assetRoot = Path.Combine(AppContext.BaseDirectory, "Assets");

        public static string GetPath(string assetPath)
        {
            return Path.Combine(s_assetRoot, assetPath);
        }
    }
}