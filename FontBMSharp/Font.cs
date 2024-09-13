using SkiaSharp;
using Svg;
using System.Drawing;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Baker76.Imaging;
using System.Xml;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Svg.Skia;
using Svg.Pathing;
using System.ComponentModel.Design;
using HarfBuzzSharp;
using Svg.Transforms;
using Svg.Model.Drawables;
namespace FontBMSharp
{
    public enum DataFormat
    {
        Text,
        Xml,
        Binary
    };

    public enum AutoSizeMode
    {
        None,
        Texture,
        Font
    }

    public class FontMetrics
    {
        public int SDFScale;
        public int Height;
        public float Scale;
        public int Ascent;
        public int Descent;
        public int LineGap;
        public int BaseLine;
        public int LineHeight;
        public int AdvanceWidthMax;
        public int MinLeftSideBearing;
        public int MinRightSideBearing;
        public int xMaxExtent;

        public FontMetrics(TTFont font, FontBMOptions options, int sdfScale)
        {
            SDFScale = sdfScale;
            Height = options.FontSize * SDFScale;
            Scale = font.IsSVG() ? (float)options.FontSize / font.UnitsPerEm : font.ScaleInPixels(Height);
            font.GetFontVMetrics(out Ascent, out Descent, out LineGap);
            BaseLine = Height - (int)(Ascent * Scale);
            LineHeight = (int)Math.Ceiling((Ascent - Descent + LineGap) * Scale);
            font.GetFontHMetrics(out AdvanceWidthMax, out MinLeftSideBearing, out MinRightSideBearing, out xMaxExtent);
        }
    }

    public class FontBMOptions
    {
        public TTFont Font;
        public List<char> Chars;
        public int FontSize = 32;
        public int OriginalFontSize = 32;
        public int Spacing = 1;
        public Size TextureSize = new Size(256, 256);
        public AutoSizeMode AutoSize = AutoSizeMode.None;
        public bool NoPacking = false;
        public Size GridSize = new Size(9, 10); // Default grid size
        public Baker76.Imaging.Color Color = Baker76.Imaging.Color.White;
        public Baker76.Imaging.Color BackgroundColor = Baker76.Imaging.Color.Transparent;
        public DataFormat DataFormat = DataFormat.Text;
        public FontMetrics FontMetrics;
        public Dictionary<char, Point> GlyphPositions;
        public Dictionary<char, GlyphMetrics> GlyphMetrics;
        public Dictionary<char, GlyphBitmap> GlyphBitmaps;
        public List<int> SortedIndices;
        public RectangleF Bounds;

        public FontBMOptions()
        {
            CreateChars();
        }

        public void CreateChars()
        {
            // Createchars(97, 126);
            CreateChars(32, 126);
        }

        public void CreateChars(int charStart, int charEnd)
        {
            Chars = new List<char>();
            int charCount = charEnd - charStart + 1;

            for (int i = 0; i < charCount; i++)
            {
                var codePoint = (char)(charStart + i);
                Chars.Add(codePoint);
            }
        }

        public void ReadCharsFile(string fileName)
        {
            Chars = new List<char>();
            string text = System.IO.File.ReadAllText(fileName);
            text = new string(text.Where(c => !char.IsControl(c)).ToArray());

            foreach (char ch in text)
                Chars.Add(ch);
        }

        public char GetChar(int index)
        {
            return (NoPacking ? Chars[index] : Chars[SortedIndices[index]]);
        }

        public RectangleF GetGlyphRect(char codePoint, float scale)
        {
            if (!GlyphMetrics.ContainsKey(codePoint))
                return RectangleF.Empty;

            var glyphMetrics = GlyphMetrics[codePoint];

            scale = Font.IsSVG() ? (float)FontSize / Font.UnitsPerEm : scale;

            return RectangleF.FromLTRB(
                      glyphMetrics.Bounds.Left * scale,  // Adjust relative to overall bounds
                      glyphMetrics.Bounds.Top * scale,   // Adjust relative to overall bounds
                      glyphMetrics.Bounds.Right * scale, // Scale width
                      glyphMetrics.Bounds.Bottom * scale // Scale height
                  );
        }

        public int GetGlyphXAdvance(char codePoint)
        {
            if (!GlyphMetrics.ContainsKey(codePoint))
                return 0;

            var glyphMetrics = GlyphMetrics[codePoint];

            if (Font.IsSVG())
            {
                float svgScale = FontSize / glyphMetrics.Bounds.Height;

                return (int)(glyphMetrics.Bounds.Width * svgScale);
            }

            return (int)GlyphMetrics[codePoint].AdvanceWidth;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct FontInfo
    {
        public short FontSize;
        public byte BitField;
        public byte _CharSet;
        public ushort StretchH;
        public byte Aa;
        public byte PaddingUp;
        public byte PaddingRight;
        public byte PaddingBottom;
        public byte PaddingLeft;
        public byte SpacingHoriz;
        public byte SpacingVert;
        public byte Outline;
        //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
        //public string FontName;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FontCommon
    {
        public ushort LineHeight;
        public ushort Base;
        public ushort ScaleW;
        public ushort ScaleH;
        public ushort Pages;
        public byte BitField;
        public byte AlphaChnl;
        public byte RedChnl;
        public byte GreenChnl;
        public byte BlueChnl;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct FontPages
    {
        //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
        //public string PageNames;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CharInfo
    {
        public uint Id;
        public ushort X;
        public ushort Y;
        public ushort Width;
        public ushort Height;
        public short XOffset;
        public short YOffset;
        public short XAdvance;
        public byte Page;
        public byte Chnl;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct KernPair
    {
        public uint First;
        public uint Second;
        public short Amount;
    }

    public class FontFnt
    {
        public string Name;
        public string D2FileName;
        public List<string> PageNames;
        public FontInfo FontInfo;
        public string FontName;
        public FontCommon FontCommon;
        public CharInfo[] CharInfo;
        public KernPair[] KernPairs;

        private static Random _random = new Random();

        public FontFnt()
        {
        }

        public FontFnt(string name, string d2FileName)
        {
            Name = name;
            D2FileName = d2FileName;
        }

        public static async Task<FontFnt> Load(HttpClient httpClient, string fileName)
        {
            byte[] buffer = await httpClient.GetByteArrayAsync(fileName);

            return await Load(buffer, fileName);
        }

        public static async Task<FontFnt> Load(byte[] buffer, string fileName)
        {
            if (buffer[0] != 'B' || buffer[1] != 'M' || buffer[2] != 'F' || buffer[3] != 3)
            {
                Console.WriteLine("Invalid Font Header!");
                return null;
            }

            FontFnt font = new FontFnt(Path.GetFileNameWithoutExtension(fileName), null);
            int offset = 4;
            ushort lineHeight = 0;

            while (offset < buffer.Length)
            {
                byte blockType = buffer[offset];
                offset += sizeof(byte);
                uint blockSize = BitConverter.ToUInt32(buffer, offset);
                offset += sizeof(uint);

                switch (blockType)
                {
                    case 1: // info
                        {
                            font.FontInfo = Utility.ToObject<FontInfo>(buffer, offset);

                            int nameLength = 0;

                            while (buffer[offset + Marshal.SizeOf(typeof(FontInfo)) + nameLength] != 0)
                                nameLength++;

                            font.FontName = Encoding.Default.GetString(buffer, offset + Marshal.SizeOf(typeof(FontInfo)), nameLength);

                            // Console.WriteLine("Font Name: " + font.FontName);
                            // Console.WriteLine("Font Size: " +  font.FontInfo.FontSize);
                            break;
                        }
                    case 2: // common
                        {
                            font.FontCommon = Utility.ToObject<FontCommon>(buffer, offset);
                            // Console.WriteLine("Line Height: " + lineHeight);
                            break;
                        }
                    case 3: // pages
                        {
                            int nameStartIndex = offset;
                            List<string> pageNames = new List<string>();

                            while (nameStartIndex < offset + blockSize)
                            {
                                int nameLength = 0;

                                while (buffer[nameStartIndex + nameLength] != 0)
                                    nameLength++;

                                string pageName = Encoding.Default.GetString(buffer, nameStartIndex, nameLength);
                                pageNames.Add(pageName);

                                nameStartIndex += nameLength + 1;
                            }

                            //foreach (string pageName in pageNames)
                            //    Console.WriteLine("Page Name: " + pageName);

                            font.D2FileName = Path.GetFileNameWithoutExtension(pageNames[0]);
                            font.PageNames = pageNames;

                            //FontPages pages = new FontPages();

                            //Array.Copy(buffer, offset, pages.PageNames, 0, pages.PageNames.Length);
                            break;
                        }
                    case 4: // chars
                        {
                            int numChars = (int)(blockSize / Marshal.SizeOf(typeof(CharInfo)));
                            font.CharInfo = new CharInfo[numChars];

                            // Console.WriteLine("Number of Characters: " + numChars);
                            for (int i = 0; i < numChars; i++)
                            {
                                font.CharInfo[i] = Utility.ToObject<CharInfo>(buffer, offset + i * Marshal.SizeOf(typeof(CharInfo)));

                                // Console.WriteLine("Character ID: " + font.CharInfo[i].Id + ", X: " + font.CharInfo[i].X + ", Y: " + font.CharInfo[i].Y + ", Width: " + font.CharInfo[i].Width + ", Height: " + font.CharInfo[i].Height);
                            }
                            break;
                        }
                    case 5: // kerning pairs
                        {
                            int numKernPairs = (buffer.Length - offset) / Marshal.SizeOf(typeof(KernPair));
                            font.KernPairs = new KernPair[numKernPairs];

                            // Console.WriteLine("Number of Kerning Pairs: " + numKernPairs);
                            for (int i = 0; i < numKernPairs; i++)
                            {
                                font.KernPairs[i] = Utility.ToObject<KernPair>(buffer, offset + i * Marshal.SizeOf(typeof(KernPair)));

                                // Console.WriteLine("First: " + font.KernPairs[i].First + ", Second: " + font.KernPairs[i].Second + ", Amount: " + font.KernPairs[i].Amount);
                            }
                            break;
                        }
                    default:
                        Console.WriteLine("Invalid Font Block!");
                        return null;
                }
                offset += (int)blockSize;
            }

            return font;
        }

        public async Task Save(string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                await Save(stream, fileName);
            }
        }

        public async Task Save(Stream stream, string fileName)
        {
            using (var writer = new BinaryWriter(stream))
            {
                // Write the BMF header
                writer.Write(new byte[] { (byte)'B', (byte)'M', (byte)'F', 3 });

                // Block type 1: info
                {
                    List<byte> infoBlock = new List<byte>();

                    // Create FontInfo struct
                    byte[] infoBytes = Utility.ToBytes(FontInfo);
                    infoBlock.AddRange(infoBytes);

                    // Write font name
                    byte[] fontNameBytes = Encoding.Default.GetBytes(Name);
                    infoBlock.AddRange(fontNameBytes);
                    infoBlock.Add(0); // Null terminator for the string

                    // Write block type and size
                    writer.Write((byte)1);
                    writer.Write(infoBlock.Count);
                    writer.Write(infoBlock.ToArray());
                }

                // Block type 2: common
                {
                    byte[] commonBytes = Utility.ToBytes(FontCommon);

                    writer.Write((byte)2);
                    writer.Write(commonBytes.Length);
                    writer.Write(commonBytes);
                }

                // Block type 3: pages
                {
                    List<byte> pagesBlock = new List<byte>();

                    foreach (var pageName in PageNames)
                    {
                        byte[] pageNameBytes = Encoding.Default.GetBytes(pageName);
                        pagesBlock.AddRange(pageNameBytes);
                        pagesBlock.Add(0); // Null terminator for each page name
                    }

                    writer.Write((byte)3);
                    writer.Write(pagesBlock.Count);
                    writer.Write(pagesBlock.ToArray());
                }

                // Block type 4: chars
                {
                    List<byte> charsBlock = new List<byte>();

                    foreach (var charInfo in CharInfo)
                    {
                        byte[] charInfoBytes = Utility.ToBytes(charInfo);
                        charsBlock.AddRange(charInfoBytes);
                    }

                    writer.Write((byte)4);
                    writer.Write(charsBlock.Count);
                    writer.Write(charsBlock.ToArray());
                }

                // Block type 5: kerning pairs
                if (KernPairs != null)
                {
                    List<byte> kernPairsBlock = new List<byte>();

                    foreach (var kernPair in KernPairs)
                    {
                        byte[] kernPairBytes = Utility.ToBytes(kernPair);
                        kernPairsBlock.AddRange(kernPairBytes);
                    }

                    writer.Write((byte)5);
                    writer.Write(kernPairsBlock.Count);
                    writer.Write(kernPairsBlock.ToArray());
                }
            }
        }

        public static async Task<(FontFnt, Image)> LoadTTF(byte[] buffer, string fontName, FontBMOptions options)
        {
            options.FontSize = options.OriginalFontSize;

            var font = new TTFont(buffer, options);
            font.SvgRender += SvgRender;

            options.Font = font;
            options.GlyphMetrics = new Dictionary<char, GlyphMetrics>();
            options.SortedIndices = new List<int>(options.Chars.Count);
            options.FontSize = options.AutoSize == AutoSizeMode.Font ? 1000 : options.FontSize;
            options.FontMetrics = new FontMetrics(font, options, 1);

            await GetGlyphBounds(font, options);
            GetFontBounds(font, options);

            return options.NoPacking ? await GenerateFontWithGrid(fontName, font, options) : await GenerateFontWithPacking(fontName, font, options);
        }

        private static async Task GetGlyphBounds(TTFont font, FontBMOptions options)
        {
            options.SortedIndices.Clear();

            for (int i = 0; i < options.Chars.Count; i++)
            {
                var codePoint = options.Chars[i];

                options.GlyphMetrics[codePoint] = await font.GetGlyphMetrics(codePoint, options.FontMetrics.Scale, options.FontMetrics.Scale, 0, 0);
                options.SortedIndices.Add(i);
            }

            options.SortedIndices = options.SortedIndices
            .OrderByDescending(a => {
                var glyphMetrics = options.GlyphMetrics[options.Chars[a]];
                return glyphMetrics.Bounds.Width * glyphMetrics.Bounds.Height;
            })
            .ToList();
        }

        private static void GetFontBounds(TTFont font, FontBMOptions options)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            for (int i = 0; i < options.Chars.Count; i++)
            {
                var codePoint = options.GetChar(i);
                var glyphMetrics = options.GlyphMetrics[codePoint];

                minX = Math.Min(minX, glyphMetrics.Bounds.Left);
                minY = Math.Min(minY, glyphMetrics.Bounds.Top);
                maxX = Math.Max(maxX, glyphMetrics.Bounds.Right);
                maxY = Math.Max(maxY, glyphMetrics.Bounds.Bottom);
            }

            options.Bounds = new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        private static SizeF GetGlyphMaxSize(TTFont font, FontBMOptions options)
        {
            if (font.IsSVG())
            {
                float scale = (float)options.FontSize / font.UnitsPerEm;

                return new SizeF(options.Bounds.Width * scale, options.Bounds.Height * scale);
            }

            return new SizeF(options.FontSize, options.FontSize);
        }

        // Generates font with NoPacking mode
        private static async Task<(FontFnt, Image)> GenerateFontWithGrid(string fontName, TTFont font, FontBMOptions options)
        {
            var charCount = options.GridSize.Width * options.GridSize.Height;
            var fontSize = options.FontSize;
            SizeF maxSize = GetGlyphMaxSize(font, options);
            SizeF cellSize = new SizeF((float)options.TextureSize.Width / options.GridSize.Width, (float)options.TextureSize.Height / options.GridSize.Height);

            options.GlyphPositions = new Dictionary<char, Point>();

            if (options.AutoSize == AutoSizeMode.Texture)
            {
                while (maxSize.Width > cellSize.Width || maxSize.Height > cellSize.Height)
                {
                    IncreaseTextureSize(ref options.TextureSize);
                    cellSize = new SizeF((float)options.TextureSize.Width / options.GridSize.Width, (float)options.TextureSize.Height / options.GridSize.Height);
                }
            }
            else if (options.AutoSize == AutoSizeMode.Font)
            {
                while (maxSize.Width > cellSize.Width || maxSize.Height > cellSize.Height)
                {
                    options.FontSize--;
                    maxSize = GetGlyphMaxSize(font, options);
                }
            }

            options.FontMetrics = new FontMetrics(font, options, 1);

            await GetGlyphBounds(font, options);
            GetFontBounds(font, options);

            for (int i = 0; i < options.Chars.Count; i++)
            {
                if (i >= charCount)
                    break;

                var codePoint = options.GetChar(i);
                var glyphMetrics = options.GlyphMetrics[codePoint];
                var glyphRect = options.GetGlyphRect(codePoint, 1.0f);

                int col = i % options.GridSize.Width;
                int row = i / options.GridSize.Width;
                float cellX = col * cellSize.Width;
                float cellY = row * cellSize.Height;
                float positionX = cellX;
                float positionY = cellY;

                if (font.IsSVG())
                {
                    var scale = (float)options.FontSize / font.UnitsPerEm;

                    positionX = MathF.Max(cellX + (cellSize.Width / 2.0f - glyphRect.Width / 2.0f), 0);
                    positionY = cellY - options.FontMetrics.BaseLine * scale;
                }
                else
                {
                    positionX = MathF.Max(cellX + (cellSize.Width / 2.0f - glyphRect.Width / 2.0f), 0);
                    positionY = cellY + options.FontMetrics.Height - options.FontMetrics.BaseLine + (int)glyphMetrics.Bounds.Y;
                }

                options.GlyphPositions[codePoint] = new Point((int)positionX, (int)positionY);
            }

            await RenderGlyphs(font, options);

            var glyphBitmap = new GlyphBitmap(options.TextureSize.Width, options.TextureSize.Height, false, options.BackgroundColor);

            DrawGlyphs(glyphBitmap, options);

            var fontFnt = CreateFontFnt(fontName, options);
            var image = new Image(glyphBitmap.Width, glyphBitmap.Height, 32, glyphBitmap.Pixels);

            return (fontFnt, image);
        }

        // Generates font with RectanglePacker (default packing mode)
        private static async Task<(FontFnt, Image)> GenerateFontWithPacking(string fontName, TTFont font, FontBMOptions options)
        {
            var fontSize = options.FontSize;
            var scale = 1.0f;
            Size textureSize = (options.AutoSize == AutoSizeMode.Texture ? new Size(64, 64) : options.TextureSize);
            bool allGlyphsPacked = false;

            options.GlyphPositions = new Dictionary<char, Point>();

            while (!allGlyphsPacked)
            {
                RectanglePacker packer = new RectanglePacker(textureSize.Width, textureSize.Height);
                allGlyphsPacked = true;

                for (int i = 0; i < options.Chars.Count; i++)
                {
                    var codePoint = options.GetChar(i);
                    var glyphRect = options.GetGlyphRect(codePoint, scale);
                    Size canvasSize = new Size((int)(glyphRect.Width + options.Spacing), (int)(glyphRect.Height + options.Spacing));
                    Point position = Point.Empty;

                    if (!packer.FindPoint(canvasSize, ref position))
                    {
                        if (options.AutoSize == AutoSizeMode.Texture)
                        {
                            IncreaseTextureSize(ref textureSize);
                            allGlyphsPacked = false;
                        }
                        else if (options.AutoSize == AutoSizeMode.Font)
                        {
                            options.FontSize--;
                            scale = (float)options.FontSize / fontSize;
                            allGlyphsPacked = false;
                        }
                        break;
                    }
                    else
                    {
                        options.GlyphPositions[codePoint] = position;
                    }
                }
            }

            options.FontMetrics = new FontMetrics(font, options, 1);

            await GetGlyphBounds(font, options);
            GetFontBounds(font, options);

            await RenderGlyphs(font, options);

            var glyphBitmap = new GlyphBitmap(textureSize.Width, textureSize.Height, false, options.BackgroundColor);
            DrawGlyphs(glyphBitmap, options);

            var fontFnt = CreateFontFnt(fontName, options);
            var image = new Image(glyphBitmap.Width, glyphBitmap.Height, 32, glyphBitmap.Pixels);

            return (fontFnt, image);
        }

        // Helper method to increase the texture size
        private static void IncreaseTextureSize(ref Size textureSize)
        {
            if (textureSize.Width == textureSize.Height)
                textureSize.Height <<= 1;
            else
                textureSize.Width <<= 1;
        }

        // Helper method to create a FontFnt object
        private static FontFnt CreateFontFnt(string fontName, FontBMOptions options)
        {
            var fontFnt = new FontFnt(fontName, null);

            fontFnt.PageNames = new List<string>();
            fontFnt.FontInfo = new FontInfo();
            fontFnt.FontCommon = new FontCommon();
            fontFnt.CharInfo = new CharInfo[options.Chars.Count];
            fontFnt.KernPairs = new KernPair[0]; // Placeholder for kerning pairs

            fontFnt.PageNames.Add($"{fontName}.png");

            fontFnt.FontInfo.FontSize = (short)options.FontSize;
            fontFnt.FontCommon.ScaleW = (ushort)options.TextureSize.Width;
            fontFnt.FontCommon.ScaleH = (ushort)options.TextureSize.Height;
            fontFnt.FontCommon.Pages = 1;

            // Populate CharInfo
            for (int i = 0; i < options.Chars.Count; i++)
            {
                var codePoint = options.GetChar(i);
                var glyphMetrics = options.GlyphMetrics[codePoint];

                if (!options.GlyphPositions.ContainsKey(codePoint))
                    continue;

                var glyphPosition = options.GlyphPositions[codePoint];

                if (!options.GlyphBitmaps.ContainsKey(codePoint))
                    continue;

                var glyphBitmap = options.GlyphBitmaps[codePoint];
                var glpyhRect = options.GetGlyphRect(codePoint, 1.0f);
                var xAdvance = options.GetGlyphXAdvance(codePoint);

                CharInfo charInfo = new CharInfo();
                charInfo.Id = codePoint;
                charInfo.X = (ushort)glyphPosition.X;
                charInfo.Y = (ushort)glyphPosition.Y;
                charInfo.Width = (ushort)glpyhRect.Width;
                charInfo.Height = (ushort)glpyhRect.Height;
                charInfo.XOffset = (short)glpyhRect.X;
                charInfo.YOffset = (short)glpyhRect.Y;
                charInfo.XAdvance = (short)xAdvance;
                charInfo.Page = 0;
                charInfo.Chnl = 15;

                fontFnt.CharInfo[i] = charInfo;
            }

            return fontFnt;
        }

        // Helper method to render glyphs
        private static async Task RenderGlyphs(TTFont font, FontBMOptions options)
        {
            options.GlyphBitmaps = new Dictionary<char, GlyphBitmap>();

            for (int i = 0; i < options.Chars.Count; i++)
            {
                var codePoint = options.Chars[i];
                var glyphBitmap = await font.RenderGlyph(codePoint, options.FontMetrics.Scale, options.Color, Baker76.Imaging.Color.Empty);

                if (glyphBitmap == null)
                    continue;

                options.GlyphBitmaps[codePoint] = glyphBitmap;
            }
        }

        // Helper method to draw glyphs using RectanglePacker
        private static void DrawGlyphs(GlyphBitmap bitmap, FontBMOptions options)
        {
            for (int i = 0; i < options.Chars.Count; i++)
            {
                var codePoint = options.Chars[i];
                var glyphMetrics = options.GlyphMetrics[codePoint];

                if (!options.GlyphBitmaps.ContainsKey(codePoint))
                    continue;

                var glyphBitmap = options.GlyphBitmaps[codePoint];

                if (!options.GlyphPositions.ContainsKey(codePoint))
                    continue;

                var glyphPosition = options.GlyphPositions[codePoint];

                bitmap.Draw(glyphBitmap, glyphPosition.X, glyphPosition.Y, options.BackgroundColor);
            }
        }

        public static async Task<GlyphBitmap> SvgRender(TTFont font, string svgDoc, char codePoint, int glyph, object userData)
        {
            try
            {
                var options = (FontBMOptions)userData;
                var svgDocument = SvgDocument.FromSvg<SvgDocument>(svgDoc);

                var glyphMetrics = options.GlyphMetrics;

                if (!glyphMetrics.ContainsKey(codePoint))
                    return new GlyphBitmap(0, 0, true);

                var glyphMetric = glyphMetrics[codePoint];
                int unitsPerEm = (int)font.UnitsPerEm;
                var fontScale = (float)options.FontSize / unitsPerEm;
                RectangleF svgRect = options.GetGlyphRect(codePoint, 1.0f);
                Size glyphSize = new Size((int)Math.Max(svgRect.Width, options.FontSize), (int)Math.Max(svgRect.Height, options.FontSize));
                Size canvasSize = new Size((int)svgRect.Left + glyphSize.Width, (int)svgRect.Top + glyphSize.Height);
                svgDocument.ViewBox = new SvgViewBox(0, -unitsPerEm, Math.Max(glyphMetric.Bounds.Width, unitsPerEm), Math.Max(glyphMetric.Bounds.Height, unitsPerEm));
                svgDocument.Overflow = SvgOverflow.Visible;
                svgDocument.Width = glyphSize.Width;
                svgDocument.Height = glyphSize.Height;

                var svg = SKSvg.CreateFromSvgDocument(svgDocument);
                var bitmap = new SKBitmap(canvasSize.Width, canvasSize.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);

                using (var canvas = new SKCanvas(bitmap))
                {
                    // Create random color
                    //SKColor color = new SKColor((byte)_random.Next(256), (byte)_random.Next(256), (byte)_random.Next(256));
                    //canvas.DrawRect(svgRect.Left, svgRect.Top, svgRect.Width, svgRect.Height, new SKPaint { Color = color });
                    
                    canvas.DrawPicture(svg.Picture, 0, -options.FontMetrics.BaseLine);
                }

                var glyphBitmap = new GlyphBitmap(bitmap.Width, bitmap.Height, bitmap.Bytes, true);

                return glyphBitmap;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    
        public void WriteXml(string fileName)
        {
            if (CharInfo == null)
                return;

            XmlTextWriter xmlTextWriter = null;

            try
            {
                xmlTextWriter = new XmlTextWriter(fileName, Encoding.UTF8);

                xmlTextWriter.Formatting = Formatting.Indented;

                xmlTextWriter.WriteStartDocument();
                xmlTextWriter.WriteStartElement("font");

                //<info face="MarkerFelt-Thin" size="43" bold="0" italic="0" chasrset="" unicode="0" stretchH="100" smooth="1" aa="1" padding="0,0,0,0" spacing="2,2"/>

                xmlTextWriter.WriteStartElement("info");

                xmlTextWriter.WriteAttributeString("face", Name);
                xmlTextWriter.WriteAttributeString("size", FontInfo.FontSize.ToString());
                xmlTextWriter.WriteAttributeString("bold", "0");
                xmlTextWriter.WriteAttributeString("italic", "0");
                xmlTextWriter.WriteAttributeString("charset", "");
                xmlTextWriter.WriteAttributeString("unicode", "0");
                xmlTextWriter.WriteAttributeString("stretchH", "100");
                xmlTextWriter.WriteAttributeString("smooth", "1");
                xmlTextWriter.WriteAttributeString("aa", "1");
                xmlTextWriter.WriteAttributeString("padding", "0,0,0,0");
                xmlTextWriter.WriteAttributeString("spacing", String.Format("{0},{1}", FontInfo.SpacingHoriz, FontInfo.SpacingVert));

                xmlTextWriter.WriteEndElement();

                //<common lineHeight="47" base="37" scaleW="512" scaleH="256" pages="1" packed="0"/>

                xmlTextWriter.WriteStartElement("common");

                xmlTextWriter.WriteAttributeString("lineHeight", FontCommon.LineHeight.ToString());
                xmlTextWriter.WriteAttributeString("base", FontCommon.Base.ToString());
                xmlTextWriter.WriteAttributeString("scaleW", FontCommon.ScaleW.ToString());
                xmlTextWriter.WriteAttributeString("scaleH", FontCommon.ScaleH.ToString());
                xmlTextWriter.WriteAttributeString("pages", "1");
                xmlTextWriter.WriteAttributeString("packed", "0");

                xmlTextWriter.WriteEndElement();

                //<page id="0" file="MarkerFelt.png"/>

                xmlTextWriter.WriteStartElement("page");

                xmlTextWriter.WriteAttributeString("id", "0");
                xmlTextWriter.WriteAttributeString("file", PageNames[0]);

                xmlTextWriter.WriteEndElement();

                //<chars count="94">
                //<char id="32" x="114" y="224" width="0" height="0" xoffset="0" yoffset="38" xadvance="13" page="0" chnl="0" letter="space"/>

                xmlTextWriter.WriteStartElement("chars");

                xmlTextWriter.WriteAttributeString("count", CharInfo.Length.ToString());

                foreach (CharInfo charInfo in CharInfo)
                {
                    xmlTextWriter.WriteStartElement("char");

                    xmlTextWriter.WriteAttributeString("id", charInfo.Id.ToString());
                    xmlTextWriter.WriteAttributeString("x", charInfo.X.ToString());
                    xmlTextWriter.WriteAttributeString("y", charInfo.Y.ToString());
                    xmlTextWriter.WriteAttributeString("width", charInfo.Width.ToString());
                    xmlTextWriter.WriteAttributeString("height", charInfo.Height.ToString());
                    xmlTextWriter.WriteAttributeString("xoffset", charInfo.XOffset.ToString());
                    xmlTextWriter.WriteAttributeString("yoffset", charInfo.YOffset.ToString());
                    xmlTextWriter.WriteAttributeString("xadvance", charInfo.XAdvance.ToString());
                    xmlTextWriter.WriteAttributeString("page", "0");
                    xmlTextWriter.WriteAttributeString("chnl", "0");
                    xmlTextWriter.WriteAttributeString("letter", ((char)charInfo.Id).ToString());

                    xmlTextWriter.WriteEndElement();
                }

                xmlTextWriter.WriteEndElement();

                //<kernings count="3680">
                //<kerning first="32" second="53" amount="-2"/>

                if (KernPairs.Length > 0)
                {
                    xmlTextWriter.WriteStartElement("kernings");

                    xmlTextWriter.WriteAttributeString("count", KernPairs.Length.ToString());

                    foreach (KernPair kerning in KernPairs)
                    {
                        xmlTextWriter.WriteStartElement("kerning");

                        xmlTextWriter.WriteAttributeString("first", ((int)kerning.First).ToString());
                        xmlTextWriter.WriteAttributeString("second", ((int)kerning.Second).ToString());
                        xmlTextWriter.WriteAttributeString("amount", kerning.Amount.ToString());

                        xmlTextWriter.WriteEndElement();
                    }
                }

                xmlTextWriter.WriteEndDocument();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (xmlTextWriter != null)
                {
                    xmlTextWriter.Flush();
                    xmlTextWriter.Close();
                }
            }
        }

        public void WriteText(string fileName)
        {
            StringBuilder sb = new StringBuilder();

            // Info Section
            sb.AppendLine($"info face=\"{Name}\" size={FontInfo.FontSize} bold={FontInfo.BitField & 0x01} italic={FontInfo.BitField & 0x02} charset=\"\" unicode=1 stretchH={FontInfo.StretchH} smooth=1 aa={FontInfo.Aa} padding={FontInfo.PaddingUp},{FontInfo.PaddingRight},{FontInfo.PaddingBottom},{FontInfo.PaddingLeft} spacing={FontInfo.SpacingHoriz},{FontInfo.SpacingVert} outline={FontInfo.Outline}");

            // Common Section
            sb.AppendLine($"common lineHeight={FontCommon.LineHeight} base={FontCommon.Base} scaleW={FontCommon.ScaleW} scaleH={FontCommon.ScaleH} pages={FontCommon.Pages} packed={FontCommon.BitField} alphaChnl={FontCommon.AlphaChnl} redChnl={FontCommon.RedChnl} greenChnl={FontCommon.GreenChnl} blueChnl={FontCommon.BlueChnl}");

            // Page Section
            for (int i = 0; i < PageNames.Count; i++)
            {
                sb.AppendLine($"page id={i} file=\"{PageNames[i]}\"");
            }

            // Chars Section
            sb.AppendLine($"chars count={CharInfo.Length}");
            foreach (var ch in CharInfo)
            {
                if (ch.Id == 0)
                    continue;

                sb.AppendLine($"char id={ch.Id} x={ch.X} y={ch.Y} width={ch.Width} height={ch.Height} xoffset={ch.XOffset} yoffset={ch.YOffset} xadvance={ch.XAdvance} page={ch.Page} chnl={ch.Chnl}");
            }

            // Kerning Section
            sb.AppendLine($"kernings count={KernPairs.Length}");
            foreach (var kern in KernPairs)
            {
                sb.AppendLine($"kerning first={kern.First} second={kern.Second} amount={kern.Amount}");
            }

            File.WriteAllText(fileName, sb.ToString());
        }
    }
}