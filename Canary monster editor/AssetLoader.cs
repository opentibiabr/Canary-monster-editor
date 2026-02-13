using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Google.Protobuf;
using MediaColor = System.Windows.Media.Color;
using DrawingColor = System.Drawing.Color;

namespace Canary_monster_editor
{
    public class AssetLoader
    {
        public static AssetLoader Instance { get; } = new AssetLoader();

        public SpriteStorage SpriteStorage { get; private set; }
        public SpriteCatalogStorage CatalogStorage { get; private set; }
        public Dictionary<uint, ClientAppearance> Outfits { get; private set; } = new Dictionary<uint, ClientAppearance>();
        public Dictionary<uint, ClientAppearance> Objects { get; private set; } = new Dictionary<uint, ClientAppearance>();
        public bool IsLoaded { get; private set; } = false;
        public string LastAssetsPath { get; private set; }
        private static readonly object logLock = new object();

        public class MonsterPreviewSequence
        {
            public List<BitmapSource> Frames { get; } = new List<BitmapSource>();
            public List<int> DurationsMs { get; } = new List<int>();
        }

        public class CatalogEntry
        {
            public string type { get; set; }
            public string file { get; set; }
            public uint? spritelastid { get; set; }
        }

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

        public bool LoadAssets(string assetPath)
        {
            try
            {
                LastAssetsPath = assetPath;
                // Try to find catalog-content.json first (Tibia 12+)
                string jsonPath = Path.Combine(assetPath, "catalog-content.json");
                if (!File.Exists(jsonPath) && Directory.Exists(Path.Combine(assetPath, "assets")))
                {
                     jsonPath = Path.Combine(assetPath, "assets", "catalog-content.json");
                     if (File.Exists(jsonPath)) assetPath = Path.Combine(assetPath, "assets");
                }

                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var catalog = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CatalogEntry>>(jsonContent);
                    
                    string appearancesFile = null;
                    string spritesFile = null;
                    bool hasCatalogSprites = false;
                    
                    foreach (var entry in catalog)
                    {
                        if (entry.type == "appearances")
                        {
                            appearancesFile = Path.Combine(assetPath, entry.file);
                        }
                        else if (entry.type == "sprites")
                        {
                            if (spritesFile == null) spritesFile = Path.Combine(assetPath, entry.file);
                        }
                        else if (entry.type == "sprite")
                        {
                            hasCatalogSprites = true;
                        }
                    }

                    if (appearancesFile != null && File.Exists(appearancesFile) && hasCatalogSprites)
                    {
                        if (CatalogStorage != null) { CatalogStorage.Dispose(); CatalogStorage = null; }
                        if (SpriteStorage != null) { SpriteStorage.Dispose(); SpriteStorage = null; }
                        CatalogStorage = SpriteCatalogStorage.TryCreate(jsonPath, assetPath);
                        if (CatalogStorage != null)
                        {
                            SpriteStorage = null;
                            ParseAppearances(appearancesFile);
                            IsLoaded = true;
                            return true;
                        }
                    }

                    if (appearancesFile != null && File.Exists(appearancesFile) && spritesFile != null && File.Exists(spritesFile))
                    {
                        if (CatalogStorage != null) { CatalogStorage.Dispose(); CatalogStorage = null; }
                        if (SpriteStorage != null) { SpriteStorage.Dispose(); SpriteStorage = null; }
                        SpriteStorage = new SpriteStorage(spritesFile, true);
                        SpriteStorage.LoadSprites();
                        CatalogStorage = null;
                        ParseAppearances(appearancesFile);
                        IsLoaded = true;
                        return true;
                    }
                }

                string[] possibleNamesDat = { "appearances.dat", "Tibia.dat" };
                string[] possibleNamesSpr = { "appearances.spr", "Tibia.spr" };
                
                string sprPath = null;
                string datPath = null;

                foreach (var name in possibleNamesDat)
                {
                    string p = Path.Combine(assetPath, name);
                    if (File.Exists(p)) { datPath = p; break; }
                }

                foreach (var name in possibleNamesSpr)
                {
                    string p = Path.Combine(assetPath, name);
                    if (File.Exists(p)) { sprPath = p; break; }
                }

                if (sprPath == null || datPath == null)
                {
                    string subPath = Path.Combine(assetPath, "assets");
                    if (Directory.Exists(subPath))
                    {
                        foreach (var name in possibleNamesDat)
                        {
                            string p = Path.Combine(subPath, name);
                            if (File.Exists(p)) { datPath = p; break; }
                        }
                        foreach (var name in possibleNamesSpr)
                        {
                            string p = Path.Combine(subPath, name);
                            if (File.Exists(p)) { sprPath = p; break; }
                        }
                    }
                }

                // If still null, try to find matching .dat and .spr pair in limited subdirectories
                if (sprPath == null || datPath == null)
                {
                    try
                    {
                        string[] knownSubdirs = { "data", "sprites", "assets" };
                        var foundPairs = new List<(string dat, string spr)>();

                        // Search in root first
                        var rootDatFiles = Directory.EnumerateFiles(assetPath, "*.dat", SearchOption.TopDirectoryOnly).ToList();
                        foreach (var df in rootDatFiles)
                        {
                            string nameNoExt = Path.GetFileNameWithoutExtension(df);
                            string matchingSpr = Path.Combine(assetPath, nameNoExt + ".spr");
                            if (File.Exists(matchingSpr))
                            {
                                foundPairs.Add((df, matchingSpr));
                            }
                        }

                        // Search in known subdirectories (1 level deep only)
                        foreach (var subdir in knownSubdirs)
                        {
                            string subPath = Path.Combine(assetPath, subdir);
                            if (!Directory.Exists(subPath)) continue;

                            var subDatFiles = Directory.EnumerateFiles(subPath, "*.dat", SearchOption.TopDirectoryOnly).ToList();
                            foreach (var df in subDatFiles)
                            {
                                string nameNoExt = Path.GetFileNameWithoutExtension(df);
                                string matchingSpr = Path.Combine(subPath, nameNoExt + ".spr");
                                if (File.Exists(matchingSpr))
                                {
                                    foundPairs.Add((df, matchingSpr));
                                }
                            }
                        }

                        if (foundPairs.Count == 1)
                        {
                            datPath = foundPairs[0].dat;
                            sprPath = foundPairs[0].spr;
                        }
                        else if (foundPairs.Count > 1)
                        {
                            // Multiple pairs found - use first one (could prompt user in future)
                            datPath = foundPairs[0].dat;
                            sprPath = foundPairs[0].spr;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Error searching for files: " + ex.Message);
                    }
                }

                if (sprPath == null || datPath == null) return false;

                if (CatalogStorage != null) { CatalogStorage.Dispose(); CatalogStorage = null; }
                if (SpriteStorage != null) { SpriteStorage.Dispose(); SpriteStorage = null; }
                SpriteStorage = new SpriteStorage(sprPath, true);
                SpriteStorage.LoadSprites();
                CatalogStorage = null;
                LastAssetsPath = assetPath;

                ParseAppearances(datPath);

                IsLoaded = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ParseAppearances(string datPath)
        {
            try
            {
                Outfits.Clear();
                Objects.Clear();
                byte[] raw = File.ReadAllBytes(datPath);
                if (TryParseAppearances(raw)) return;

                byte[] decompressed = DecompressCatalogLzma(raw);
                if (decompressed != null && decompressed.Length > 0 && TryParseAppearances(decompressed)) return;

                throw new Exception("Failed to parse appearances data.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error parsing appearances: " + ex.Message + "\n" + ex.StackTrace);
                throw; // Rethrow so LoadAssets knows it failed
            }
        }

        private bool TryParseAppearances(byte[] data)
        {
            if (data == null || data.Length == 0) return false;

            try
            {
                using (var ms = new MemoryStream(data))
                using (var inputK = new CodedInputStream(ms))
                {
                    while (!inputK.IsAtEnd)
                    {
                        uint tag = inputK.ReadTag();
                        if (tag == 0) break;
                        int fieldNumber = (int)(tag >> 3);
                        if (fieldNumber == 1) // Objects
                        {
                            var app = ParseAppearance(inputK.ReadBytes());
                            if (app != null) Objects[app.Id] = app;
                        }
                        else if (fieldNumber == 2) // Outfits
                        {
                            var app = ParseAppearance(inputK.ReadBytes());
                            if (app != null) Outfits[app.Id] = app;
                        }
                        else
                        {
                            inputK.SkipLastField();
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private byte[] DecompressCatalogLzma(byte[] data)
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
            catch
            {
                return null;
            }
        }

        private ClientAppearance ParseAppearance(ByteString data)
        {
            var app = new ClientAppearance();
            var input = new CodedInputStream(data.ToByteArray());
            while (!input.IsAtEnd)
            {
                uint tag = input.ReadTag();
                int fieldNumber = (int)(tag >> 3);
                if (fieldNumber == 1) app.Id = input.ReadUInt32();
                else if (fieldNumber == 2) app.FrameGroups.Add(ParseFrameGroup(input.ReadBytes()));
                else if (fieldNumber == 3) // Flags
                {
                    ParseFlags(input.ReadBytes(), app);
                }
                else input.SkipLastField();
            }
            return app;
        }

        private void ParseFlags(ByteString data, ClientAppearance app)
        {
            var input = new CodedInputStream(data.ToByteArray());
            while (!input.IsAtEnd)
            {
                uint tag = input.ReadTag();
                int fieldNumber = (int)(tag >> 3);
                if (fieldNumber == 26) // Shift
                {
                    var shiftData = input.ReadBytes();
                    var shiftInput = new CodedInputStream(shiftData.ToByteArray());
                    while (!shiftInput.IsAtEnd)
                    {
                        uint stag = shiftInput.ReadTag();
                        int sfield = (int)(stag >> 3);
                        if (sfield == 1) app.ShiftX = shiftInput.ReadInt32();
                        else if (sfield == 2) app.ShiftY = shiftInput.ReadInt32();
                        else shiftInput.SkipLastField();
                    }
                }
                else input.SkipLastField();
            }
        }

        private ClientFrameGroup ParseFrameGroup(ByteString data)
        {
            var fg = new ClientFrameGroup();
            var input = new CodedInputStream(data.ToByteArray());
            while (!input.IsAtEnd)
            {
                uint tag = input.ReadTag();
                int fieldNumber = (int)(tag >> 3);
                if (fieldNumber == 3) // SpriteInfo
                {
                    ParseSpriteInfo(input.ReadBytes(), fg);
                }
                else input.SkipLastField();
            }
            return fg;
        }

        private void ParseSpriteInfo(ByteString data, ClientFrameGroup fg)
        {
            var input = new CodedInputStream(data.ToByteArray());
            while (!input.IsAtEnd)
            {
                uint tag = input.ReadTag();
                int wireType = (int)(tag & 7);
                int fieldNumber = (int)(tag >> 3);
                switch (fieldNumber)
                {
                    case 1: fg.PatternWidth = input.ReadUInt32(); break;
                    case 2: fg.PatternHeight = input.ReadUInt32(); break;
                    case 3: fg.PatternDepth = input.ReadUInt32(); break;
                    case 4: fg.Layers = input.ReadUInt32(); break;
                    case 5: // Sprite IDs (packed or unpacked)
                        if (wireType == 2)
                        {
                            byte[] packedData = input.ReadBytes().ToByteArray();
                            CodedInputStream packedInput = new CodedInputStream(packedData);
                            while (!packedInput.IsAtEnd)
                            {
                                fg.SpriteIds.Add(packedInput.ReadUInt32());
                            }
                        }
                        else if (wireType == 0)
                        {
                            fg.SpriteIds.Add(input.ReadUInt32());
                        }
                        else
                        {
                            input.SkipLastField();
                        }
                        break;
                    case 6: // Animation
                        ParseAnimation(input.ReadBytes(), fg);
                        break;
                    case 12: fg.PatternX = input.ReadUInt32(); break;
                    case 13: fg.PatternY = input.ReadUInt32(); break;
                    case 14: fg.PatternZ = input.ReadUInt32(); break;
                    case 15: fg.PatternFrames = input.ReadUInt32(); break;
                    case 11: fg.PatternLayers = input.ReadUInt32(); break;
                    default: input.SkipLastField(); break;
                }
            }
            if (fg.PatternWidth == 0) fg.PatternWidth = 1;
            if (fg.PatternHeight == 0) fg.PatternHeight = 1;
            if (fg.PatternDepth == 0) fg.PatternDepth = 1;
            if (fg.Layers == 0) fg.Layers = fg.PatternLayers > 0 ? fg.PatternLayers : 1;
            if (fg.PatternX == 0) fg.PatternX = 1;
            if (fg.PatternY == 0) fg.PatternY = 1;
            if (fg.PatternZ == 0) fg.PatternZ = 1;
            if (fg.PatternFrames == 0) fg.PatternFrames = 1;
        }

        private void ParseAnimation(ByteString data, ClientFrameGroup fg)
        {
            if (data == null || data.Length == 0) return;
            var input = new CodedInputStream(data.ToByteArray());
            while (!input.IsAtEnd)
            {
                uint tag = input.ReadTag();
                int fieldNumber = (int)(tag >> 3);
                switch (fieldNumber)
                {
                    case 6: // SpritePhase
                        var phase = ParseAnimationPhase(input.ReadBytes());
                        if (phase != null) fg.AnimationPhases.Add(phase);
                        break;
                    default:
                        input.SkipLastField();
                        break;
                }
            }
        }

        private ClientSpritePhase ParseAnimationPhase(ByteString data)
        {
            if (data == null || data.Length == 0) return null;
            var phase = new ClientSpritePhase();
            var input = new CodedInputStream(data.ToByteArray());
            while (!input.IsAtEnd)
            {
                uint tag = input.ReadTag();
                int fieldNumber = (int)(tag >> 3);
                if (fieldNumber == 1) phase.DurationMin = input.ReadUInt32();
                else if (fieldNumber == 2) phase.DurationMax = input.ReadUInt32();
                else input.SkipLastField();
            }
            return phase;
        }

        public BitmapSource GetMonsterPreview(uint lookType, uint lookTypeEx, uint head, uint body, uint legs, uint feet, uint addon)
        {
            var sequence = GetMonsterPreviewSequence(lookType, lookTypeEx, head, body, legs, feet, addon);
            if (sequence == null || sequence.Frames.Count == 0) return null;
            return sequence.Frames[0];
        }

        public MonsterPreviewSequence GetMonsterPreviewSequence(uint lookType, uint lookTypeEx, uint head, uint body, uint legs, uint feet, uint addon)
        {
            try
            {
                if (!IsLoaded) return null;

                ClientAppearance app = null;
                bool isOutfit = false;
                if (lookType > 0)
                {
                    Outfits.TryGetValue(lookType, out app);
                    isOutfit = true;
                }
                else if (lookTypeEx > 0)
                {
                    Objects.TryGetValue(lookTypeEx, out app);
                }

                if (app == null || app.FrameGroups.Count == 0) return null;

                var fg = SelectPreviewFrameGroup(app);
                if (fg == null) return null;

                var sequence = TryBuildPreviewSequence(app, fg, isOutfit, head, body, legs, feet, addon, true);
                if (sequence != null && sequence.Frames.Count > 0) return sequence;

                sequence = TryBuildPreviewSequence(app, fg, isOutfit, head, body, legs, feet, addon, false);
                return (sequence != null && sequence.Frames.Count > 0) ? sequence : null;
            }
            catch (Exception ex)
            {
                AppendLog($"Preview sequence error: {ex.GetType().Name} - {ex.Message}");
                AppendLog(ex.StackTrace ?? "no stack");
                return null;
            }
        }

        private MemoryStream GetSpriteStream(uint id)
        {
            if (CatalogStorage != null)
            {
                return CatalogStorage.GetSpriteStream(id);
            }

            if (SpriteStorage != null)
            {
                return SpriteStorage.getSpriteStream(id);
            }

            return new MemoryStream();
        }

        public string GetPreviewDebugInfo(uint lookType, uint lookTypeEx, uint head, uint body, uint legs, uint feet, uint addon)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== Preview Debug ===");
                sb.AppendLine($"AssetsPath: {LastAssetsPath}");
                sb.AppendLine($"IsLoaded: {IsLoaded}");
                sb.AppendLine($"CatalogStorage: {(CatalogStorage != null ? "Yes" : "No")}");
                sb.AppendLine($"SpriteStorage: {(SpriteStorage != null ? "Yes" : "No")}");
                sb.AppendLine($"Objects: {Objects.Count} | Outfits: {Outfits.Count}");
                sb.AppendLine($"LookType: {lookType} | LookTypeEx: {lookTypeEx} | Addon: {addon}");
                sb.AppendLine($"Colors: H{head} B{body} L{legs} F{feet}");

                ClientAppearance app = null;
                bool isOutfit = false;
                if (lookType > 0)
                {
                    Outfits.TryGetValue(lookType, out app);
                    isOutfit = true;
                }
                else if (lookTypeEx > 0)
                {
                    Objects.TryGetValue(lookTypeEx, out app);
                }

                if (app == null)
                {
                    sb.AppendLine("Appearance: NOT FOUND");
                    return sb.ToString();
                }

                sb.AppendLine($"Appearance: Found | FrameGroups: {app.FrameGroups.Count} | ShiftX: {app.ShiftX} | ShiftY: {app.ShiftY}");
                var fg = SelectPreviewFrameGroup(app);
                if (fg == null)
                {
                    sb.AppendLine("FrameGroup: NULL");
                    return sb.ToString();
                }

                sb.AppendLine($"FrameGroup: PW {fg.PatternWidth} PH {fg.PatternHeight} PD {fg.PatternDepth} L {fg.Layers} PX {fg.PatternX} PY {fg.PatternY} PZ {fg.PatternZ} PF {fg.PatternFrames}");
                sb.AppendLine($"SpriteIds: {fg.SpriteIds.Count}");

                AppendMappingDebug(sb, fg, isOutfit, addon, true);
                AppendMappingDebug(sb, fg, isOutfit, addon, false);
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Debug failed: " + ex.Message;
            }
        }

        private void AppendMappingDebug(System.Text.StringBuilder sb, ClientFrameGroup fg, bool isOutfit, uint addon, bool usePatternXYZAsSize)
        {
            int tileWidthCount = usePatternXYZAsSize ? (int)Math.Max(1, fg.PatternX) : (int)Math.Max(1, fg.PatternWidth);
            int tileHeightCount = usePatternXYZAsSize ? (int)Math.Max(1, fg.PatternY) : (int)Math.Max(1, fg.PatternHeight);
            int variationX = usePatternXYZAsSize ? (int)Math.Max(1, fg.PatternWidth) : (int)Math.Max(1, fg.PatternX);
            int variationY = usePatternXYZAsSize ? (int)Math.Max(1, fg.PatternHeight) : (int)Math.Max(1, fg.PatternY);
            int variationZ = usePatternXYZAsSize ? (int)Math.Max(1, fg.PatternDepth) : (int)Math.Max(1, fg.PatternZ);
            int frameCount = (int)Math.Max(1, fg.PatternFrames);
            int dir = (variationX >= 4) ? 2 : (variationX == 2 ? 1 : 0);
            int addonIndex = isOutfit ? ComputeAddonPatternIndex(addon, (uint)Math.Max(1, variationY)) : 0;
            int mountIndex = 0;

            int index = GetSpriteIndexResolved(
                tileWidthCount,
                tileHeightCount,
                variationX,
                variationY,
                variationZ,
                (int)fg.Layers,
                0,
                0,
                0,
                dir,
                addonIndex,
                mountIndex,
                0,
                frameCount);

            sb.AppendLine(usePatternXYZAsSize ? "--- Mapping A (tile=PatternXY) ---" : "--- Mapping B (tile=PatternWH) ---");
            sb.AppendLine($"tileWH: {tileWidthCount}x{tileHeightCount} | varXYZ: {variationX},{variationY},{variationZ} | dir: {dir} | addonIdx: {addonIndex} | mountIdx: {mountIndex} | frames: {frameCount}");
            sb.AppendLine($"Index(0,0,0): {index}");

            if (index >= 0 && index < fg.SpriteIds.Count)
            {
                uint spriteId = fg.SpriteIds[index];
                sb.AppendLine($"SpriteId: {spriteId}");
                if (CatalogStorage != null)
                {
                    string file = CatalogStorage.GetSpriteFileForId(spriteId);
                    sb.AppendLine($"SpriteFile: {file}");
                }
                using (var ms = GetSpriteStream(spriteId))
                {
                    sb.AppendLine($"SpriteStream: {(ms == null ? "null" : ms.Length.ToString())}");
                }
            }
            else
            {
                sb.AppendLine("Index out of range");
            }
        }

        private void ColorizePixel(ref int r, ref int g, ref int b, DrawingColor colorPart)
        {
            r = ClampByte((r + colorPart.R) / 2);
            g = ClampByte((g + colorPart.G) / 2);
            b = ClampByte((b + colorPart.B) / 2);
        }

        private int GetSpriteIndexResolved(
            int tileWidth,
            int tileHeight,
            int patternX,
            int patternY,
            int patternZ,
            int layers,
            int width,
            int height,
            int layer,
            int x,
            int y,
            int z,
            int frame,
            int frameCount)
        {
            int index = (int)(frame % Math.Max(1, frameCount));
            index = index * Math.Max(1, patternZ) + z;
            index = index * Math.Max(1, patternY) + y;
            index = index * Math.Max(1, patternX) + x;
            index = index * Math.Max(1, layers) + layer;
            index = index * Math.Max(1, tileHeight) + height;
            index = index * Math.Max(1, tileWidth) + width;
            return index;
        }

        private MonsterPreviewSequence TryBuildPreviewSequence(
            ClientAppearance app,
            ClientFrameGroup fg,
            bool isOutfit,
            uint head,
            uint body,
            uint legs,
            uint feet,
            uint addon,
            bool usePatternXYZAsSize)
        {
            try
            {
                int tileWidthCount = usePatternXYZAsSize ? (int)Math.Max(1, fg.PatternX) : (int)Math.Max(1, fg.PatternWidth);
                int tileHeightCount = usePatternXYZAsSize ? (int)Math.Max(1, fg.PatternY) : (int)Math.Max(1, fg.PatternHeight);
                int variationX = usePatternXYZAsSize ? (int)Math.Max(1, fg.PatternWidth) : (int)Math.Max(1, fg.PatternX);
                int variationY = usePatternXYZAsSize ? (int)Math.Max(1, fg.PatternHeight) : (int)Math.Max(1, fg.PatternY);
                int variationZ = usePatternXYZAsSize ? (int)Math.Max(1, fg.PatternDepth) : (int)Math.Max(1, fg.PatternZ);

            int tileSize = 64;
            int monsterWidth = Math.Max(1, tileWidthCount) * tileSize;
            int monsterHeight = Math.Max(1, tileHeightCount) * tileSize;
            if (monsterWidth == 0 || monsterHeight == 0) return null;

            int canvasSize = 128;

                DrawingColor colHead = GetOutfitColor((int)head);
                DrawingColor colBody = GetOutfitColor((int)body);
                DrawingColor colLegs = GetOutfitColor((int)legs);
                DrawingColor colFeet = GetOutfitColor((int)feet);

                int dir = 0;
                if (variationX >= 4) dir = 2;
                else if (variationX == 2) dir = 1;

                int offsetX = (canvasSize - monsterWidth) / 2 + app.ShiftX;
                int offsetY = (canvasSize - monsterHeight) / 2 + app.ShiftY;
                
                if (monsterHeight > 32)
                {
                    offsetY = 110 - monsterHeight + app.ShiftY;
                }

            int frameCount = GetFrameCount(fg);
            int addonIndex = isOutfit ? ComputeAddonPatternIndex(addon, (uint)Math.Max(1, variationY)) : 0;
            var addonLayersToDraw = BuildAddonLayers(addonIndex);
            int mountIndex = 0;

                var sequence = new MonsterPreviewSequence();
                bool drewAny = false;

                for (int frame = 0; frame < frameCount; frame++)
                {
                    using (Bitmap result = new Bitmap(canvasSize, canvasSize, PixelFormat.Format32bppArgb))
                    using (Graphics g = Graphics.FromImage(result))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                        foreach (int addonLayer in addonLayersToDraw)
                        {
                            for (uint l = 0; l < fg.Layers; l++)
                            {
                                using (Bitmap layerBmp = new Bitmap(monsterWidth, monsterHeight, PixelFormat.Format32bppArgb))
                                {
                                    using (Graphics lg = Graphics.FromImage(layerBmp))
                                    {
                                        for (uint w = 0; w < tileWidthCount; w++)
                                        {
                                            for (uint h = 0; h < tileHeightCount; h++)
                                            {
                                                int index = GetSpriteIndexResolved(
                                                    tileWidthCount,
                                                    tileHeightCount,
                                                    variationX,
                                                    variationY,
                                                    variationZ,
                                                    (int)fg.Layers,
                                                    (int)w,
                                                    (int)h,
                                                    (int)l,
                                                    dir,
                                                    addonLayer,
                                                    mountIndex,
                                                    frame,
                                                    frameCount);
                                                if (index >= fg.SpriteIds.Count) continue;

                                                uint spriteId = fg.SpriteIds[index];
                                                using (var mss = GetSpriteStream(spriteId))
                                                {
                                                    if (mss != null && mss.Length > 0)
                                                    {
                                                        using (Bitmap spr = new Bitmap(mss))
                                                        {
                                                        int px = (int)((tileWidthCount - w - 1) * tileSize);
                                                        int py = (int)((tileHeightCount - h - 1) * tileSize);
                                                        lg.DrawImage(spr, px, py, tileSize, tileSize);
                                                        drewAny = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    }

                                    if (isOutfit && l == 1 && fg.Layers > 1)
                                    {
                                        for (int x = 0; x < monsterWidth; x++)
                                        {
                                            for (int y = 0; y < monsterHeight; y++)
                                            {
                                                DrawingColor maskPixel = layerBmp.GetPixel(x, y);
                                                if (maskPixel.A == 0) continue;

                                                int rx = x + offsetX;
                                                int ry = y + offsetY;
                                                if (rx < 0 || rx >= canvasSize || ry < 0 || ry >= canvasSize) continue;
                                                
                                                DrawingColor basePixel = result.GetPixel(rx, ry);
                                                int pixR = basePixel.R;
                                                int pixG = basePixel.G;
                                                int pixB = basePixel.B;

                                                if (maskPixel.R > 0 && maskPixel.G > 0 && maskPixel.B == 0) ColorizePixel(ref pixR, ref pixG, ref pixB, colHead);
                                                else if (maskPixel.R > 0 && maskPixel.G == 0 && maskPixel.B == 0) ColorizePixel(ref pixR, ref pixG, ref pixB, colBody);
                                                else if (maskPixel.R == 0 && maskPixel.G > 0 && maskPixel.B == 0) ColorizePixel(ref pixR, ref pixG, ref pixB, colLegs);
                                                else if (maskPixel.R == 0 && maskPixel.G == 0 && maskPixel.B > 0) ColorizePixel(ref pixR, ref pixG, ref pixB, colFeet);
                                                else continue;

                                                result.SetPixel(rx, ry, DrawingColor.FromArgb(basePixel.A, pixR, pixG, pixB));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        g.DrawImage(layerBmp, offsetX, offsetY);
                                    }
                                }
                            }
                        }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        result.Save(ms, ImageFormat.Png);
                        sequence.Frames.Add(BitmapToBitmapImage(ms));
                        sequence.DurationsMs.Add(GetFrameDurationMs(fg, frame, frameCount));
                    }
                }
            }

                if (!drewAny) return null;
                return sequence;
            }
            catch (Exception ex)
            {
                AppendLog($"Preview render error: {ex.GetType().Name} - {ex.Message}");
                AppendLog(ex.StackTrace ?? "no stack");
                return null;
            }
        }

        private BitmapImage BitmapToBitmapImage(MemoryStream stream)
        {
            stream.Position = 0;
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private ClientFrameGroup SelectPreviewFrameGroup(ClientAppearance app)
        {
            if (app.FrameGroups == null || app.FrameGroups.Count == 0) return null;
            if (app.FrameGroups.Count > 1 && app.FrameGroups[1].PatternFrames > 1) return app.FrameGroups[1];

            foreach (var fg in app.FrameGroups)
            {
                if (GetFrameCount(fg) > 1) return fg;
            }

            return app.FrameGroups[0];
        }

        private int GetFrameCount(ClientFrameGroup fg)
        {
            if (fg == null) return 1;
            if (fg.AnimationPhases != null && fg.AnimationPhases.Count > 0) return fg.AnimationPhases.Count;
            return (int)Math.Max(1, fg.PatternFrames);
        }

        private int GetFrameDurationMs(ClientFrameGroup fg, int frameIndex, int frameCount)
        {
            int interval = ResolvePreviewInterval(frameCount);
            if (fg == null) return interval;
            if (fg.AnimationPhases != null && fg.AnimationPhases.Count > 0)
            {
                if (frameIndex >= 0 && frameIndex < fg.AnimationPhases.Count)
                {
                    uint min = fg.AnimationPhases[frameIndex].DurationMin;
                    if (min > 0) return (int)Math.Min(min, interval);
                }
                return interval;
            }
            return interval;
        }

        private int ResolvePreviewInterval(int frameCount)
        {
            int phases = Math.Max(1, frameCount);
            if (phases <= 2) return 150;
            if (phases < 4) return 100;
            if (phases <= 8) return 75;
            return 75;
        }

        private int ComputeAddonPatternIndex(uint addons, uint patternY)
        {
            if (patternY <= 1) return 0;

            if (patternY >= 4)
            {
                bool hasAddon1 = (addons & 1) != 0;
                bool hasAddon2 = (addons & 2) != 0;

                if (hasAddon1 && hasAddon2) return (int)Math.Min(patternY - 1, 3);
                if (hasAddon1) return 1;
                if (hasAddon2) return (int)Math.Min(patternY - 1, 2);
                return 0;
            }

            return (int)Math.Min(patternY - 1, Math.Max(0, (int)addons));
        }

        private List<int> BuildAddonLayers(int addonIndex)
        {
            if (addonIndex <= 0) return new List<int> { 0 };
            return new List<int> { 0, addonIndex };
        }

        private int ComputeMountPatternIndex(uint lookMount, uint patternDepth)
        {
            if (patternDepth <= 1) return 0;
            if (lookMount <= 0) return 0;
            return 1;
        }

        private int ClampByte(int value)
        {
            return Math.Max(0, Math.Min(255, value));
        }

        public static DrawingColor GetOutfitColor(int color)
        {
            const int HSI_SI_VALUES = 7;
            const int HSI_H_STEPS = 19;

            if (color >= HSI_H_STEPS * HSI_SI_VALUES)
                color = 0;

            float loc1 = 0, loc2 = 0, loc3 = 0;
            if (color % HSI_H_STEPS != 0)
            {
                loc1 = color % HSI_H_STEPS * 1.0f / 18.0f;
                loc2 = 1;
                loc3 = 1;

                switch (color / HSI_H_STEPS)
                {
                    case 0: loc2 = 0.25f; loc3 = 1.00f; break;
                    case 1: loc2 = 0.25f; loc3 = 0.75f; break;
                    case 2: loc2 = 0.50f; loc3 = 0.75f; break;
                    case 3: loc2 = 0.667f; loc3 = 0.75f; break;
                    case 4: loc2 = 1.00f; loc3 = 1.00f; break;
                    case 5: loc2 = 1.00f; loc3 = 0.75f; break;
                    case 6: loc2 = 1.00f; loc3 = 0.50f; break;
                }
            }
            else
            {
                loc1 = 0;
                loc2 = 0;
                loc3 = 1 - (float)color / HSI_H_STEPS / HSI_SI_VALUES;
            }

            if (loc3 == 0) return DrawingColor.FromArgb(0, 0, 0);

            if (loc2 == 0)
            {
                int loc7 = (int)(loc3 * 255);
                return DrawingColor.FromArgb(loc7, loc7, loc7);
            }

            float red = 0, green = 0, blue = 0;

            if (loc1 < 1.0 / 6.0) { red = loc3; blue = loc3 * (1 - loc2); green = blue + (loc3 - blue) * 6 * loc1; }
            else if (loc1 < 2.0 / 6.0) { green = loc3; blue = loc3 * (1 - loc2); red = green - (loc3 - blue) * (6 * loc1 - 1); }
            else if (loc1 < 3.0 / 6.0) { green = loc3; red = loc3 * (1 - loc2); blue = red + (loc3 - red) * (6 * loc1 - 2); }
            else if (loc1 < 4.0 / 6.0) { blue = loc3; red = loc3 * (1 - loc2); green = blue - (loc3 - red) * (6 * loc1 - 3); }
            else if (loc1 < 5.0 / 6.0) { blue = loc3; green = loc3 * (1 - loc2); red = green + (loc3 - green) * (6 * loc1 - 4); }
            else { red = loc3; green = loc3 * (1 - loc2); blue = red - (loc3 - green) * (6 * loc1 - 5); }

            return DrawingColor.FromArgb((int)(red * 255), (int)(green * 255), (int)(blue * 255));
        }
    }

    public class ClientAppearance
    {
        public uint Id { get; set; }
        public List<ClientFrameGroup> FrameGroups { get; set; } = new List<ClientFrameGroup>();
        public int ShiftX { get; set; }
        public int ShiftY { get; set; }
    }

    public class ClientFrameGroup
    {
        public uint PatternWidth { get; set; }
        public uint PatternHeight { get; set; }
        public uint PatternDepth { get; set; }
        public uint Layers { get; set; }
        public uint PatternLayers { get; set; }
        public uint PatternX { get; set; }
        public uint PatternY { get; set; }
        public uint PatternZ { get; set; }
        public uint PatternFrames { get; set; }
        public List<uint> SpriteIds { get; set; } = new List<uint>();
        public List<ClientSpritePhase> AnimationPhases { get; set; } = new List<ClientSpritePhase>();
    }

    public class ClientSpritePhase
    {
        public uint DurationMin { get; set; }
        public uint DurationMax { get; set; }
    }
}
