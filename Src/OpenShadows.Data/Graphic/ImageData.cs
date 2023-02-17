using OpenShadows.Data.Rendering;
using OpenShadows.Data.Rendering.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using Veldrid;

#nullable enable

namespace OpenShadows.Data.Graphic
{
	public class ImageData
	{
		public int Width { get; set; } = 0;

		public int Height { get; set; } = 0;

		public byte[] PixelData { get; set; } = Array.Empty<byte>();

		/// <summary>
		/// Creates a new ImageSharpTexture from this ImageData instance.
		/// 
		/// Uncached.
		/// </summary>
		public ImageSharpTexture CreateImageSharpTexture(bool createMipmaps = true)
		{
			var memory = new Memory<byte>(PixelData);
			var image = Image.WrapMemory<Rgba32>(memory, Width, Height);
			return new ImageSharpTexture(image, createMipmaps);
		}

		/// <summary>
		/// Create a device Texture-object from this ImageData.
		/// Returns a cached version if available.
		/// </summary>
		public Texture GetTexture(GraphicsDevice gd, ResourceFactory factory, 
			ImageSharpTexture? imageSharpTexture = null, bool createMipMaps = true)
		{
			if (imageSharpTexture == null)
			{
				imageSharpTexture = CreateImageSharpTexture(createMipMaps);
            }

			return StaticResourceCache.GetTexture2D(gd, factory, imageSharpTexture);
		}
	}
}
