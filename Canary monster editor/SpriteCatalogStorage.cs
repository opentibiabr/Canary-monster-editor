using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Newtonsoft.Json;

namespace Canary_monster_editor
{

    public class SpriteCatalogStorage : IDisposable
    {
        public class SpriteCatalogEntry
        {
            [JsonProperty("type")]
            public string EntryType { get; set; }

            [JsonProperty("file")]
            public string File { get; set; }

            [JsonProperty("spritetype")]
            public uint? SpriteType { get; set; }

            [JsonProperty("firstspriteid")]
            public uint? FirstSpriteId { get; set; }

            [JsonProperty("lastspriteid")]
            public uint? LastSpriteId { get; set; }

            [JsonProperty("area")]
            public uint? Area { get; set; }
        }

        private class SpriteSheetCacheEntry
        {
            public Bitmap Sheet { get; set; }
            public int TileWidth { get; set; }
            public int TileHeight { get; set; }
            public int SpritesPerRow { get; set; }
            public int TotalSprites { get; set; }
            public uint FirstSpriteId { get; set; }
        }

        private readonly string assetsDir;
        private readonly List<SpriteCatalogEntry> entries;
        private readonly Dictionary<uint, int> spriteMap;
        private readonly Dictionary<string, SpriteSheetCacheEntry> sheetCache = new Dictionary<string, SpriteSheetCacheEntry>();
        private readonly ConcurrentDictionary<uint, byte[]> spritePngCache = new ConcurrentDictionary<uint, byte[]>();
        private readonly object sheetLock = new object();
        private static readonly object logLock = new object();
        private bool disposed = false;

        private static void AppendLog(string message)
        {
            try
            {
                lock (logLock)
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preview-debug.log");
                    File.AppendAllText(path, message + Environment.NewLine);
                }
            }
            catch { }
        }

        public static SpriteCatalogStorage TryCreate(string catalogPath, string assetsDir)
        {
            if (!File.Exists(catalogPath)) return null;

            try
            {
                string json = File.ReadAllText(catalogPath);
                var allEntries = JsonConvert.DeserializeObject<List<SpriteCatalogEntry>>(json);
                if (allEntries == null) return null;

                var spriteEntries = allEntries.FindAll(e => string.Equals(e.EntryType, "sprite", StringComparison.OrdinalIgnoreCase));
                if (spriteEntries.Count == 0) return null;

                return new SpriteCatalogStorage(spriteEntries, assetsDir);
            }
            catch
            {
                return null;
            }
        }

        private SpriteCatalogStorage(List<SpriteCatalogEntry> spriteEntries, string assetsDir)
        {
            this.assetsDir = assetsDir;
            entries = spriteEntries;
            spriteMap = new Dictionary<uint, int>();

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.FirstSpriteId.HasValue || !entry.LastSpriteId.HasValue) continue;
                if (entry.FirstSpriteId.Value > entry.LastSpriteId.Value) continue;

                uint id = entry.FirstSpriteId.Value;
                do
                {
                    spriteMap[id] = i;
                    if (id == entry.LastSpriteId.Value) break;
                    id++;
                } while (true);
            }
        }

        public MemoryStream GetSpriteStream(uint spriteId)
        {
            CheckDisposed();
            if (spriteId == 0) return new MemoryStream();

            if (spritePngCache.TryGetValue(spriteId, out var cachedBytes) && cachedBytes != null && cachedBytes.Length > 0)
            {
                return new MemoryStream(cachedBytes, writable: false);
            }

            if (!spriteMap.TryGetValue(spriteId, out var entryIndex)) return new MemoryStream();
            if (entryIndex < 0 || entryIndex >= entries.Count) return new MemoryStream();

            var entry = entries[entryIndex];
            var sheet = GetOrLoadSheet(entry);
            if (sheet == null) return new MemoryStream();

            if (sheet.TileWidth <= 0 || sheet.TileHeight <= 0 || sheet.SpritesPerRow <= 0)
            {
                AppendLog($"SpriteCatalogStorage: Invalid sheet metadata for {spriteId} (TW={sheet.TileWidth}, TH={sheet.TileHeight}, SPR={sheet.SpritesPerRow})");
                return new MemoryStream();
            }

            int index = (int)(spriteId - sheet.FirstSpriteId);
            if (index < 0 || index >= sheet.TotalSprites) return new MemoryStream();

            int row = index / sheet.SpritesPerRow;
            int col = index % sheet.SpritesPerRow;

            var src = new Rectangle(col * sheet.TileWidth, row * sheet.TileHeight, sheet.TileWidth, sheet.TileHeight);

            Bitmap sheetClone;
            lock (sheetLock)
            {
                if (src.X < 0 || src.Y < 0 || src.Right > sheet.Sheet.Width || src.Bottom > sheet.Sheet.Height)
                {
                    AppendLog($"SpriteCatalogStorage: Source rect out of bounds for {spriteId} (src={src}, sheet={sheet.Sheet.Width}x{sheet.Sheet.Height}, first={sheet.FirstSpriteId})");
                    return new MemoryStream();
                }

                sheetClone = new Bitmap(sheet.Sheet);
            }

            using (sheetClone)
            using (var tile = new Bitmap(sheet.TileWidth, sheet.TileHeight, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(tile))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.DrawImage(sheetClone, new Rectangle(0, 0, sheet.TileWidth, sheet.TileHeight), src, GraphicsUnit.Pixel);

                using (var ms = new MemoryStream())
                {
                    tile.Save(ms, ImageFormat.Png);
                    var bytes = ms.ToArray();
                    if (bytes.Length > 0)
                    {
                        spritePngCache[spriteId] = bytes;
                    }
                    return new MemoryStream(bytes, writable: false);
                }
            }
        }

        public string GetSpriteFileForId(uint spriteId)
        {
            CheckDisposed();
            if (!spriteMap.TryGetValue(spriteId, out var entryIndex)) return null;
            if (entryIndex < 0 || entryIndex >= entries.Count) return null;
            return entries[entryIndex].File;
        }

        private SpriteSheetCacheEntry GetOrLoadSheet(SpriteCatalogEntry entry)
        {
            CheckDisposed();
            if (entry == null || string.IsNullOrEmpty(entry.File)) return null;

            lock (sheetLock)
            {
                if (sheetCache.TryGetValue(entry.File, out var cached))
                {
                    return cached;
                }

                try
                {
                    string path = Path.Combine(assetsDir, entry.File);
                    if (!File.Exists(path))
                    {
                        AppendLog($"SpriteCatalogStorage: File not found: {path}");
                        return null;
                    }

                    byte[] compressed = File.ReadAllBytes(path);
                    byte[] bmpData = DecompressCatalogLzma(compressed);
                    if (bmpData == null || bmpData.Length == 0)
                    {
                        AppendLog($"SpriteCatalogStorage: Decompress failed or empty for {entry.File} (len={compressed.Length})");
                        return null;
                    }

                    using (var bmpStream = new MemoryStream(bmpData))
                    {
                        using (var bmp = new Bitmap(bmpStream))
                        {
                            int firstId = entry.FirstSpriteId.HasValue ? (int)entry.FirstSpriteId.Value : 0;
                            int lastId = entry.LastSpriteId.HasValue ? (int)entry.LastSpriteId.Value : firstId;
                            int totalCount = Math.Max(1, lastId - firstId + 1);

                            GetTileSize(bmp.Width, bmp.Height, totalCount, entry.SpriteType, out int tileW, out int tileH);
                            int spritesPerRow = tileW > 0 ? bmp.Width / tileW : 0;
                            int spriteRows = tileH > 0 ? bmp.Height / tileH : 0;
                            int totalSprites = Math.Max(0, spritesPerRow * spriteRows);

                            var sheet = new SpriteSheetCacheEntry
                            {
                                Sheet = new Bitmap(bmp),
                                TileWidth = tileW,
                                TileHeight = tileH,
                                SpritesPerRow = spritesPerRow,
                                TotalSprites = totalSprites,
                                FirstSpriteId = (uint)firstId
                            };

                            sheetCache[entry.File] = sheet;
                            return sheet;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"SpriteCatalogStorage: Exception loading sheet {entry.File}: {ex.Message}");
                    return null;
                }
            }
        }

        private static void GetTileSize(int width, int height, int totalCount, uint? spriteType, out int tileWidth, out int tileHeight)
        {
            if (spriteType.HasValue)
            {
                switch (spriteType.Value)
                {
                    case 0: tileWidth = 32; tileHeight = 32; return;
                    case 1: tileWidth = 32; tileHeight = 64; return;
                    case 2: tileWidth = 64; tileHeight = 32; return;
                    default: tileWidth = 64; tileHeight = 64; return;
                }
            }

            int tileArea = totalCount > 0 ? (width * height) / totalCount : 0;
            switch (tileArea)
            {
                case 1024:
                    tileWidth = 32; tileHeight = 32; return;
                case 2048:
                    if (width % 32 == 0 && height % 64 == 0)
                    {
                        tileWidth = 32; tileHeight = 64; return;
                    }
                    tileWidth = 64; tileHeight = 32; return;
                case 4096:
                    tileWidth = 64; tileHeight = 64; return;
                default:
                    tileWidth = (width % 64 == 0) ? 64 : 32;
                    tileHeight = (height % 64 == 0) ? 64 : 32;
                    return;
            }
        }

        private static byte[] DecompressCatalogLzma(byte[] data)
        {
            if (data == null || data.Length == 0) return null;

            try
            {
                var stripped = LzmaUtils.StripCipHeader(data);
                var result = LzmaUtils.DecompressLzma(stripped);
                if (result != null) return result;

                // Fallback: try without stripping
                return LzmaUtils.DecompressLzma(data);
            }
            catch (Exception ex)
            {
                AppendLog($"SpriteCatalogStorage: DecompressCatalogLzma failed: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                lock (sheetLock)
                {
                    foreach (var entry in sheetCache.Values)
                    {
                        entry.Sheet?.Dispose();
                        entry.Sheet = null;
                    }
                    sheetCache.Clear();
                    spritePngCache.Clear();
                }
            }

            disposed = true;
        }

        private void CheckDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(SpriteCatalogStorage));
        }
    }
}
