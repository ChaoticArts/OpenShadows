﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using Veldrid;

namespace OpenShadows.Data.Rendering.ImageSharp
{
    public class ImageSharpTexture
    {
        /// <summary>
        /// An array of images, each a single element in the mipmap chain.
        /// The first element is the largest, most detailed level, and each subsequent element
        /// is half its size, down to 1x1 pixel.
        /// </summary>
        public Image<Rgba32>[] Images { get; }

        /// <summary>
        /// The width of the largest image in the chain.
        /// </summary>
        public uint Width => (uint)Images[0].Width;

        /// <summary>
        /// The height of the largest image in the chain.
        /// </summary>
        public uint Height => (uint)Images[0].Height;

        /// <summary>
        /// The pixel format of all images.
        /// </summary>
        public PixelFormat Format { get; }

        /// <summary>
        /// The size of each pixel, in bytes.
        /// </summary>
        public uint PixelSizeInBytes => sizeof(byte) * 4;

        /// <summary>
        /// The number of levels in the mipmap chain. This is equal to the length of the Images array.
        /// </summary>
        public uint MipLevels => (uint)Images.Length;

        public ImageSharpTexture(string path) : this(Image.Load<Rgba32>(path), true) { }
        public ImageSharpTexture(string path, bool mipmap) : this(Image.Load<Rgba32>(path), mipmap) { }
        public ImageSharpTexture(string path, bool mipmap, bool srgb) : this(Image.Load<Rgba32>(path), mipmap, srgb) { }
        public ImageSharpTexture(Stream stream) : this(Image.Load<Rgba32>(stream), true) { }
        public ImageSharpTexture(Stream stream, bool mipmap) : this(Image.Load<Rgba32>(stream), mipmap) { }
        public ImageSharpTexture(Stream stream, bool mipmap, bool srgb) : this(Image.Load<Rgba32>(stream), mipmap, srgb) { }
        public ImageSharpTexture(Image<Rgba32> image, bool mipmap = true) : this(image, mipmap, false) { }
        public ImageSharpTexture(Image<Rgba32> image, bool mipmap, bool srgb)
        {
            Format = srgb ? PixelFormat.R8_G8_B8_A8_UNorm_SRgb : PixelFormat.R8_G8_B8_A8_UNorm;
            if (mipmap)
            {
                Images = MipmapHelper.GenerateMipmaps(image);
            }
            else
            {
                Images = new Image<Rgba32>[] { image };
            }
        }

        public Texture CreateDeviceTexture(GraphicsDevice gd, ResourceFactory factory)
        {
            return CreateTextureViaUpdate(gd, factory);
        }

        private Texture CreateTextureViaUpdate(GraphicsDevice gd, ResourceFactory factory)
        {
            Texture tex = factory.CreateTexture(TextureDescription.Texture2D(
                Width, Height, MipLevels, 1, Format, TextureUsage.Sampled));
            for (int level = 0; level < MipLevels; level++)
            {
                Image<Rgba32> image = Images[level];
                if (!image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelMemory))
                {
                    throw new VeldridException("Unable to get image pixelspan.");
                }
                gd.UpdateTexture<Rgba32>(tex, pixelMemory.ToArray(), 0, 0, 0, (uint)image.Width, (uint)image.Height, 1, (uint)level, 0);
            }

            return tex;
        }
    }
}
