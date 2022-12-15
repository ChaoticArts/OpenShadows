﻿using System;
using System.IO;

namespace OpenShadows.Core
{
    internal static class AssetHelper
    {
        private static readonly string s_assetRoot = Path.Combine(AppContext.BaseDirectory, "Assets");

        internal static string GetPath(string assetPath)
        {
            return Path.Combine(s_assetRoot, assetPath);
        }
    }
}