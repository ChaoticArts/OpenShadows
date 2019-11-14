using System;
using System.Collections.Generic;
using System.Text;
using ImGuiNET;

namespace OpenShadows.Workbench
{
	public static class ImGuiHelper
	{
		public static unsafe void DeactivateIniFile()
		{
			ImGui.GetIO().NativePtr->IniFilename = null;
		}
	}
}
