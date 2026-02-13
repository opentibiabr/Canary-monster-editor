using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Canary_monster_editor
{
    public class Sprite
    {
        public Sprite()
        {
            ID = 0;
            Size = 0;
            CompressedPixels = null;
            Transparent = false;
            MemoryStream = new MemoryStream();
        }

        public const byte DefaultSize = 32;
        public const ushort RGBPixelsDataSize = 3072; // 32*32*3
        public const ushort ARGBPixelsDataSize = 4096; // 32*32*4

        public uint ID { get; set; }
        public uint Size { get; set; }
        public byte[] CompressedPixels { get; set; }
        public bool Transparent { get; set; }
        public MemoryStream MemoryStream { get; set; }

        private static byte[] BlankRGBSprite = new byte[RGBPixelsDataSize];
        private static byte[] BlankARGBSprite = new byte[ARGBPixelsDataSize];
        private static readonly Rectangle Rect = new Rectangle(0, 0, DefaultSize, DefaultSize);

        public byte[] GetPixels()
        {
            if (CompressedPixels == null || CompressedPixels.Length != Size)
            {
                return BlankARGBSprite;
            }

            int read = 0;
            int write = 0;
            int pos = 0;
            int transparentPixels = 0;
            int coloredPixels = 0;
            int length = CompressedPixels.Length;
            byte bitPerPixel = (byte)(Transparent ? 4 : 3);
            byte[] pixels = new byte[ARGBPixelsDataSize];

            for (read = 0; read < length; read += 4 + (bitPerPixel * coloredPixels))
            {
                if (pos + 4 > CompressedPixels.Length) break;

                transparentPixels = CompressedPixels[pos++] | CompressedPixels[pos++] << 8;
                coloredPixels = CompressedPixels[pos++] | CompressedPixels[pos++] << 8;

                if (write + (transparentPixels * 4) > pixels.Length) break;

                for (int i = 0; i < transparentPixels; i++)
                {
                    pixels[write++] = 0x00; // Blue
                    pixels[write++] = 0x00; // Green
                    pixels[write++] = 0x00; // Red
                    pixels[write++] = 0x00; // Alpha
                }

                if (write + (coloredPixels * 4) > pixels.Length ||
                    pos + (coloredPixels * (Transparent ? 4 : 3)) > CompressedPixels.Length) break;

                for (int i = 0; i < coloredPixels; i++)
                {
                    byte red = CompressedPixels[pos++];
                    byte green = CompressedPixels[pos++];
                    byte blue = CompressedPixels[pos++];
                    byte alpha = Transparent ? CompressedPixels[pos++] : (byte)0xFF;

                    pixels[write++] = blue;
                    pixels[write++] = green;
                    pixels[write++] = red;
                    pixels[write++] = alpha;
                }
            }

            while (write < ARGBPixelsDataSize)
            {
                pixels[write++] = 0x00; // Blue
                pixels[write++] = 0x00; // Green
                pixels[write++] = 0x00; // Red
                pixels[write++] = 0x00; // Alpha
            }

            return pixels;
        }

        public Bitmap GetBitmap()
        {
            Bitmap bitmap = new Bitmap(DefaultSize, DefaultSize, PixelFormat.Format32bppArgb);
            byte[] pixels = GetPixels();

            if (pixels != null)
            {
                BitmapData bitmapData = bitmap.LockBits(Rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
                Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        public static void CreateBlankSprite()
        {
            for (short i = 0; i < RGBPixelsDataSize; i++)
            {
                BlankRGBSprite[i] = 0x11;
            }

            for (short i = 0; i < ARGBPixelsDataSize; i++)
            {
                BlankARGBSprite[i] = 0x11;
            }
        }
    }

    public class SpriteStorage : IDisposable
    {
        public string SprPath { get; set; }
        public uint Signature;
        public bool Transparency;
        public Dictionary<uint, Sprite> Sprites { get; set; }
        public ConcurrentDictionary<uint, byte[]> SprLists { get; set; }

        public SpriteStorage(string path, bool transparency)
        {
            SprPath = path;
            Sprites = new Dictionary<uint, Sprite>();
            SprLists = new ConcurrentDictionary<uint, byte[]>();
            Transparency = transparency;
        }

        public void LoadSprites()
        {
            Sprite.CreateBlankSprite();
            if (!File.Exists(SprPath)) return;

            using (FileStream fileStream = new FileStream(SprPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader reader = new BinaryReader(fileStream))
            {
                Signature = reader.ReadUInt32();
                // We assume extended for newer Canary versions, but we should probably handle both.
                // For now, let's try to detect based on file size or just use uint32.
                uint totalPics = reader.ReadUInt32(); 

                for (uint i = 0; i < totalPics; ++i)
                {
                    Sprite sprite = new Sprite
                    {
                        ID = i + 1,
                        Transparent = Transparency
                    };
                    Sprites[sprite.ID] = sprite;
                }
            }
        }

        public MemoryStream getSpriteStream(uint id)
        {
            if (id == 0) return new MemoryStream();

            if (SprLists.TryGetValue(id, out var cachedBytes) && cachedBytes != null && cachedBytes.Length > 0)
            {
                return new MemoryStream(cachedBytes, writable: false);
            }

            if (!Sprites.ContainsKey(id)) return new MemoryStream();

            Sprite sprite = Sprites[id];
            using (FileStream fileStream = new FileStream(SprPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader reader = new BinaryReader(fileStream))
            {
                // Assuming extended (8 bytes header + uint32 for each index)
                reader.BaseStream.Seek(8 + (id - 1) * 4, SeekOrigin.Begin);
                uint index = reader.ReadUInt32();
                if (index == 0) return new MemoryStream();

                reader.BaseStream.Seek(index + 3, SeekOrigin.Begin); // Skip colorkey (3 bytes)
                sprite.Size = reader.ReadUInt16();
                sprite.CompressedPixels = reader.ReadBytes((ushort)sprite.Size);
            }

            byte[] pngBytes;
            using (Bitmap bmp = sprite.GetBitmap())
            {
                using (var tempStream = new MemoryStream())
                {
                    bmp.Save(tempStream, ImageFormat.Png);
                    pngBytes = tempStream.ToArray();
                }
            }

            sprite.CompressedPixels = null;
            SprLists[sprite.ID] = pngBytes;

            return new MemoryStream(pngBytes, writable: false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Sprites != null)
                {
                    Sprites.Clear();
                    Sprites = null;
                }

                if (SprLists != null)
                {
                    SprLists.Clear();
                    SprLists = null;
                }
            }
        }
    }
}
