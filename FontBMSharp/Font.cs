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
            LineHeight = (int)Math.Floor((Ascent - Descent + LineGap) * Scale);
            font.GetFontHMetrics(out AdvanceWidthMax, out MinLeftSideBearing, out MinRightSideBearing, out xMaxExtent);
        }
    }

    public class FontBMOptions
    {
        public const char BlankChar = (char)0xFFFE;

        public TTFont Font;
        public List<char> Chars;
        public int FontSize = 32;
        public int OriginalFontSize = 32;
        public int Spacing = 1;
        public Size TextureSize = new Size(256, 256);
        public AutoSizeMode AutoSize = AutoSizeMode.Texture;
        public bool NoPacking = false;
        public bool IncludeBlankChar = false;
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

            if (IncludeBlankChar)
                Chars.Add(BlankChar);
        }

        public void CreateChars(string chars)
        {
            Chars = new List<char>();
            Chars.AddRange(chars);

            if (IncludeBlankChar)
                Chars.Add(BlankChar);
        }

        public void ReadCharsFile(string fileName)
        {
            Chars = new List<char>();
            string text = System.IO.File.ReadAllText(fileName);
            text = new string(text.Where(c => !char.IsControl(c)).ToArray());

            foreach (char ch in text)
                Chars.Add(ch);

            if (IncludeBlankChar)
                Chars.Add(BlankChar);
        }

        public char GetChar(int index)
        {
            return (NoPacking ? Chars[index] : Chars[SortedIndices[index]]);
        }

        public GlyphMetrics GetBlankCharGlyphMetrics()
        {
            return new GlyphMetrics()
            {
                Bounds = new RectangleF(0, 0, 8, 8),
                LeftSideBearing = 0,
                TopSideBearing = 0,
                AdvanceWidth = 8,
                AdvanceHeight = 8
            };
        }

        public GlyphBitmap GetBlankCharGlyphBitmap()
        {
            return new GlyphBitmap(8, 8, false, Baker76.Imaging.Color.White);
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

        public List<KernPair> GetKernPairs()
        {
            List<KernPair> kernPairs = new List<KernPair>();

            if (Font.IsSVG())
                return kernPairs;

            foreach (char ch1 in Chars)
            {
                foreach (char ch2 in Chars)
                {
                    var kerning = Font.GetKerning(ch1, ch2, FontMetrics.Scale);

                    if (kerning == 0)
                        continue;

                    KernPair kernPair = new KernPair
                    {
                        First = ch1,
                        Second = ch2,
                        Amount = (short)kerning
                    };

                    kernPairs.Add(kernPair);
                }
            }

            return kernPairs;
        }
    }

    public class FontInfo
    {
        public short FontSize;
        public byte BitField;
        public byte CharSet;
        public ushort StretchH;
        public byte Aa;
        public byte PaddingUp;
        public byte PaddingRight;
        public byte PaddingBottom;
        public byte PaddingLeft;
        public byte SpacingHoriz;
        public byte SpacingVert;
        public byte Outline;
    }

    public class FontCommon
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

    public class FontPages
    {
        public List<string> PageNames = new List<string>();
    }

    public class CharInfo
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

    public class KernPair
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
                            FontInfo fontInfo = new FontInfo
                            {
                                FontSize = BitConverter.ToInt16(buffer, offset),
                                BitField = buffer[offset + 2],
                                CharSet = buffer[offset + 3],
                                StretchH = BitConverter.ToUInt16(buffer, offset + 4),
                                Aa = buffer[offset + 6],
                                PaddingUp = buffer[offset + 7],
                                PaddingRight = buffer[offset + 8],
                                PaddingBottom = buffer[offset + 9],
                                PaddingLeft = buffer[offset + 10],
                                SpacingHoriz = buffer[offset + 11],
                                SpacingVert = buffer[offset + 12],
                                Outline = buffer[offset + 13]
                            };

                            int nameLength = 0;
                            while (buffer[offset + 14 + nameLength] != 0)
                                nameLength++;

                            font.FontName = System.Text.Encoding.Default.GetString(buffer, offset + 14, nameLength);
                            font.FontInfo = fontInfo;
                            break;
                        }
                    case 2: // common
                        {
                            FontCommon fontCommon = new FontCommon
                            {
                                LineHeight = BitConverter.ToUInt16(buffer, offset),
                                Base = BitConverter.ToUInt16(buffer, offset + 2),
                                ScaleW = BitConverter.ToUInt16(buffer, offset + 4),
                                ScaleH = BitConverter.ToUInt16(buffer, offset + 6),
                                Pages = BitConverter.ToUInt16(buffer, offset + 8),
                                BitField = buffer[offset + 10],
                                AlphaChnl = buffer[offset + 11],
                                RedChnl = buffer[offset + 12],
                                GreenChnl = buffer[offset + 13],
                                BlueChnl = buffer[offset + 14]
                            };

                            font.FontCommon = fontCommon;
                            break;
                        }
                    case 3: // pages
                        {
                            int nameStartIndex = offset;
                            FontPages fontPages = new FontPages();

                            while (nameStartIndex < offset + blockSize)
                            {
                                int nameLength = 0;
                                while (buffer[nameStartIndex + nameLength] != 0)
                                    nameLength++;

                                string pageName = System.Text.Encoding.Default.GetString(buffer, nameStartIndex, nameLength);
                                fontPages.PageNames.Add(pageName);
                                nameStartIndex += nameLength + 1;
                            }

                            font.PageNames = fontPages.PageNames;
                            font.D2FileName = Path.GetFileNameWithoutExtension(fontPages.PageNames[0]);
                            break;
                        }
                    case 4: // chars
                        {
                            int numChars = (int)(blockSize / 20);
                            font.CharInfo = new CharInfo[numChars];

                            for (int i = 0; i < numChars; i++)
                            {
                                font.CharInfo[i] = new CharInfo
                                {
                                    Id = BitConverter.ToUInt32(buffer, offset + i * 20),
                                    X = BitConverter.ToUInt16(buffer, offset + i * 20 + 4),
                                    Y = BitConverter.ToUInt16(buffer, offset + i * 20 + 6),
                                    Width = BitConverter.ToUInt16(buffer, offset + i * 20 + 8),
                                    Height = BitConverter.ToUInt16(buffer, offset + i * 20 + 10),
                                    XOffset = BitConverter.ToInt16(buffer, offset + i * 20 + 12),
                                    YOffset = BitConverter.ToInt16(buffer, offset + i * 20 + 14),
                                    XAdvance = BitConverter.ToInt16(buffer, offset + i * 20 + 16),
                                    Page = buffer[offset + i * 20 + 18],
                                    Chnl = buffer[offset + i * 20 + 19]
                                };
                            }
                            break;
                        }
                    case 5: // kerning pairs
                        {
                            int numKernPairs = (buffer.Length - offset) / 10;
                            font.KernPairs = new KernPair[numKernPairs];

                            for (int i = 0; i < numKernPairs; i++)
                            {
                                font.KernPairs[i] = new KernPair
                                {
                                    First = BitConverter.ToUInt32(buffer, offset + i * 10),
                                    Second = BitConverter.ToUInt32(buffer, offset + i * 10 + 4),
                                    Amount = BitConverter.ToInt16(buffer, offset + i * 10 + 8)
                                };
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
                    writer.Write((byte)1);

                    List<byte> infoBlock = new List<byte>();
                    infoBlock.AddRange(BitConverter.GetBytes(FontInfo.FontSize));
                    infoBlock.Add(FontInfo.BitField);
                    infoBlock.Add(FontInfo.CharSet);
                    infoBlock.AddRange(BitConverter.GetBytes(FontInfo.StretchH));
                    infoBlock.Add(FontInfo.Aa);
                    infoBlock.Add(FontInfo.PaddingUp);
                    infoBlock.Add(FontInfo.PaddingRight);
                    infoBlock.Add(FontInfo.PaddingBottom);
                    infoBlock.Add(FontInfo.PaddingLeft);
                    infoBlock.Add(FontInfo.SpacingHoriz);
                    infoBlock.Add(FontInfo.SpacingVert);
                    infoBlock.Add(FontInfo.Outline);

                    byte[] fontNameBytes = System.Text.Encoding.Default.GetBytes(FontName);
                    infoBlock.AddRange(fontNameBytes);
                    infoBlock.Add(0); // Null terminator for the string

                    writer.Write(infoBlock.Count);
                    writer.Write(infoBlock.ToArray());
                }

                // Block type 2: common
                {
                    writer.Write((byte)2);

                    List<byte> commonBlock = new List<byte>();
                    commonBlock.AddRange(BitConverter.GetBytes(FontCommon.LineHeight));
                    commonBlock.AddRange(BitConverter.GetBytes(FontCommon.Base));
                    commonBlock.AddRange(BitConverter.GetBytes(FontCommon.ScaleW));
                    commonBlock.AddRange(BitConverter.GetBytes(FontCommon.ScaleH));
                    commonBlock.AddRange(BitConverter.GetBytes(FontCommon.Pages));
                    commonBlock.Add(FontCommon.BitField);
                    commonBlock.Add(FontCommon.AlphaChnl);
                    commonBlock.Add(FontCommon.RedChnl);
                    commonBlock.Add(FontCommon.GreenChnl);
                    commonBlock.Add(FontCommon.BlueChnl);

                    writer.Write(commonBlock.Count);
                    writer.Write(commonBlock.ToArray());
                }

                // Block type 3: pages
                {
                    writer.Write((byte)3);

                    List<byte> pagesBlock = new List<byte>();
                    foreach (var pageName in PageNames)
                    {
                        byte[] pageNameBytes = System.Text.Encoding.Default.GetBytes(pageName);
                        pagesBlock.AddRange(pageNameBytes);
                        pagesBlock.Add(0); // Null terminator for each page name
                    }

                    writer.Write(pagesBlock.Count);
                    writer.Write(pagesBlock.ToArray());
                }

                // Block type 4: chars
                {
                    writer.Write((byte)4);

                    List<byte> charsBlock = new List<byte>();
                    foreach (var charInfo in CharInfo)
                    {
                        charsBlock.AddRange(BitConverter.GetBytes(charInfo.Id));
                        charsBlock.AddRange(BitConverter.GetBytes(charInfo.X));
                        charsBlock.AddRange(BitConverter.GetBytes(charInfo.Y));
                        charsBlock.AddRange(BitConverter.GetBytes(charInfo.Width));
                        charsBlock.AddRange(BitConverter.GetBytes(charInfo.Height));
                        charsBlock.AddRange(BitConverter.GetBytes(charInfo.XOffset));
                        charsBlock.AddRange(BitConverter.GetBytes(charInfo.YOffset));
                        charsBlock.AddRange(BitConverter.GetBytes(charInfo.XAdvance));
                        charsBlock.Add(charInfo.Page);
                        charsBlock.Add(charInfo.Chnl);
                    }

                    writer.Write(charsBlock.Count);
                    writer.Write(charsBlock.ToArray());
                }

                // Block type 5: kerning pairs
                {
                    if (KernPairs != null)
                    {
                        writer.Write((byte)5);

                        List<byte> kernPairsBlock = new List<byte>();
                        foreach (var kernPair in KernPairs)
                        {
                            kernPairsBlock.AddRange(BitConverter.GetBytes(kernPair.First));
                            kernPairsBlock.AddRange(BitConverter.GetBytes(kernPair.Second));
                            kernPairsBlock.AddRange(BitConverter.GetBytes(kernPair.Amount));
                        }

                        writer.Write(kernPairsBlock.Count);
                        writer.Write(kernPairsBlock.ToArray());
                    }
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

                if (codePoint == FontBMOptions.BlankChar)
                {
                    options.GlyphMetrics[codePoint] = options.GetBlankCharGlyphMetrics();
                    options.SortedIndices.Add(i);
                    continue;
                }

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
            options.TextureSize = (options.AutoSize == AutoSizeMode.Texture ? new Size(64, 64) : options.TextureSize);
            bool allGlyphsPacked = false;

            options.GlyphPositions = new Dictionary<char, Point>();

            while (!allGlyphsPacked)
            {
                RectanglePacker packer = new RectanglePacker(options.TextureSize.Width, options.TextureSize.Height);
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
                            IncreaseTextureSize(ref options.TextureSize);
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

            var glyphBitmap = new GlyphBitmap(options.TextureSize.Width, options.TextureSize.Height, false, options.BackgroundColor);
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
            fontFnt.KernPairs = options.GetKernPairs().ToArray();

            fontFnt.PageNames.Add($"{fontName}.png");

            fontFnt.FontInfo.FontSize = (short)options.FontSize;
            fontFnt.FontCommon.LineHeight = (ushort)options.FontMetrics.LineHeight;
            fontFnt.FontCommon.Base = (ushort)options.FontMetrics.BaseLine;
            fontFnt.FontCommon.ScaleW = (ushort)options.TextureSize.Width;
            fontFnt.FontCommon.ScaleH = (ushort)options.TextureSize.Height;
            fontFnt.FontCommon.Pages = 1;

            List<CharInfo> charInfoList = new List<CharInfo>();

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
                var glyphRect = options.GetGlyphRect(codePoint, 1.0f);
                var xAdvance = options.GetGlyphXAdvance(codePoint);

                CharInfo charInfo = new CharInfo();
                charInfo.Id = codePoint;
                charInfo.X = (ushort)glyphPosition.X;
                charInfo.Y = (ushort)glyphPosition.Y;
                charInfo.Width = (ushort)glyphRect.Width;
                charInfo.Height = (ushort)glyphRect.Height;
                charInfo.XOffset = (short)glyphRect.X;
                charInfo.YOffset = (short)(options.Font.IsSVG() ? 0 : options.FontMetrics.LineHeight + glyphRect.Y - options.FontMetrics.BaseLine);
                charInfo.XAdvance = (short)xAdvance;
                charInfo.Page = 0;
                charInfo.Chnl = 15;

                charInfoList.Add(charInfo);
            }

            fontFnt.CharInfo = charInfoList.ToArray();

            return fontFnt;
        }

        // Helper method to render glyphs
        private static async Task RenderGlyphs(TTFont font, FontBMOptions options)
        {
            options.GlyphBitmaps = new Dictionary<char, GlyphBitmap>();

            for (int i = 0; i < options.Chars.Count; i++)
            {
                var codePoint = options.Chars[i];

                if (codePoint == FontBMOptions.BlankChar)
                {
                    options.GlyphBitmaps[codePoint] = options.GetBlankCharGlyphBitmap();

                    continue;
                }

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