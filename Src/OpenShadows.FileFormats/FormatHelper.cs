using System;
using System.Collections.Generic;
using System.Text;

namespace OpenShadows.FileFormats
{
	public static class FormatHelper
	{
		public static string GetFileType(string extension)
		{
			return extension.ToUpper() switch
			{
				"ALF" => "Game archive files",
				"3DM" => "3d map",
				"DSC" => "3d map animation data",
				"NEI" => "3d map portal data (???)",
				"PAL" => "3d map texture palette",
				"PPD" => "3d map sky definition (???)",
				"TAB" => "3d map shading information (???)",
				"PIX" => "3d map texture",
				"AIF" => "image",
				"NVF" => "image set",
				"ACE" => "animated sprites",
				"BOB" => "animated screens",
				"PCX" => "image (ZSoft PC Paintbrush)",
				"AAF" => "cutscene definition",
				"SMK" => "video (Smacker)",
				"ASF" => "audio (raw PCM unsigned 8-bit, mono 22050 Hz) with ASF header",
				"RAW" => "audio (raw PCM unsigned 8-bit, mono 22050 Hz)",
				"HMI" => "audio (Human Machine Interfaces format)",
				"LXT" => "text definition",
				"XDF" => "dialogue definition",
				"DAT" => "different types of gamedata",
				"NPC" => "joinable npc data",
				"HTT" => "some form of hyper text",
				"ANN" => "map annotations for minimaps",
				"APA" => "(???)",
				"MSK" => "(???)",
				"MST" => "(???)",
				"LST" => "(???)",
				"MOF" => "(???)",
				"MOV" => "(???)",
				"OFF" => "battle screen object definitions (???)",
				_     => "<!!!NEVER SEEN BEFORE!!!>"
			};
		}
	}
}
