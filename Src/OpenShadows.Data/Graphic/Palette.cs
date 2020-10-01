namespace OpenShadows.Data.Graphic
{
	public class Palette
	{
		private readonly byte[] PaletteData;

		private readonly int NumberOfColors;

		public Palette(byte[] rawData, int numberOfColors)
		{
			PaletteData = rawData;
			NumberOfColors = numberOfColors;

			// palette must be cleaned
			for (int i = 0; i < PaletteData.Length; i++)
			{
				int cleaned = PaletteData[i] & 0x3f;
				PaletteData[i] = (byte)(cleaned * 4);
			}
		}

		public byte[] GetColor(int idx)
		{
			var color = new byte[4];

			color[0] = PaletteData[idx * 3 + 0];
			color[1] = PaletteData[idx * 3 + 1];
			color[2] = PaletteData[idx * 3 + 2];
			color[3] = 0xff;

			return color;
		}
	}
}
