using SkiaSharp;
using Svg;
using System.Drawing;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Baker76.Imaging;
using System.Xml;

namespace FontBMSharp
{
    public enum DataFormat
    {
        Text,
        Xml,
        Binary
    };

    public class FontBMOptions
    {
        public int CharStart = 32; // 97
        public int CharEnd = 126;
        public int FontSize = 32;
        public int Spacing = 1;
        public Size TextureSize = new Size(256, 256);
        public bool AutoSize = true;
        public Baker76.Imaging.Color Color = Baker76.Imaging.Color.White;
        public Baker76.Imaging.Color BackgroundColor = Baker76.Imaging.Color.Transparent;
        public DataFormat DataFormat = DataFormat.Text;
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
            int charCount = options.CharEnd - options.CharStart + 1;
            Size textureSize = (options.AutoSize ? new System.Drawing.Size(64, 64) : options.TextureSize);
            var font = new TTFont(buffer, options);
            font.SvgRender += SvgRender;

            // make this a number larger than 1 to enable SDF output
            int SDF_scale = 1;

            // here is the desired height in pixels of the output
            // the real value might not be exactly this depending on which characters are part of the input text
            int height = options.FontSize * SDF_scale;
            var scale = font.ScaleInPixels(height);

            var glyphs = new Dictionary<char, FontGlyph>();
            var positions = new Dictionary<char, Point>();

            for (int i = 0; i < charCount; i++)
            {
                var ch = (char)(options.CharStart + i);
                var glyph = await font.RenderGlyph(ch, scale, options.Color, options.BackgroundColor);
                glyphs[ch] = glyph;
            }

            int ascent, descent, lineGap;
            font.GetFontVMetrics(out ascent, out descent, out lineGap);
            int baseLine = height - (int)(ascent * scale);
            float lineHeight = (ascent - descent + lineGap) * scale;

            var glyphAreas = new List<(char Glyph, int Area)>();

            // Populate the list with glyphs and their areas
            for (int i = 0; i < charCount; i++)
            {
                var ch = (char)(options.CharStart + i);
                var glyph = glyphs[ch];

                if (glyph == null)
                    continue;

                int area = glyph.Image.Width * glyph.Image.Height;
                glyphAreas.Add((ch, area));
            }

            // Sort the list by area in descending order
            glyphAreas.Sort((a, b) => b.Area.CompareTo(a.Area));

            bool allGlyphsPacked = false;

            while (!allGlyphsPacked)
            {
                // Initialize the rectangle packer
                RectanglePacker packer = new RectanglePacker(textureSize.Width, textureSize.Height);

                allGlyphsPacked = true;

                // Try to pack the glyphs in the sorted order
                for (int i = 0; i < glyphAreas.Count; i++)
                {
                    var ch = glyphAreas[i].Glyph;
                    var glyph = glyphs[ch];

                    Point position = Point.Empty;

                    if (!packer.FindPoint(new Size(glyph.Image.Width + options.Spacing, glyph.Image.Height + options.Spacing), ref position))
                    {
                        // If FindPoint fails, increase texture size to the next power of two
                        if (options.AutoSize)
                        {
                            if (textureSize.Width == textureSize.Height)
                                textureSize.Height <<= 1;
                            else
                                textureSize.Width <<= 1;

                            allGlyphsPacked = false;
                        }

                        break;
                    }
                    else
                    {
                        positions[ch] = position;
                    }
                }
            }

            var tempBmp = new GlyphBitmap(textureSize.Width, textureSize.Height, options.BackgroundColor);
            {
                for (int i = 0; i < charCount; i++)
                {
                    var ch = (char)(options.CharStart + i);
                    var glyph = glyphs[ch];

                    if (glyph == null)
                        continue;

                    var pos = positions[ch];
                    tempBmp.Draw(glyph.Image, pos.X, pos.Y, options.BackgroundColor);
                }
            }

            if (SDF_scale > 1)
            {
                tempBmp = DistanceFieldUtils.CreateDistanceField(tempBmp, SDF_scale, 32, options.BackgroundColor);
            }

            var image = new Image(tempBmp.Width, tempBmp.Height, 32, tempBmp.Pixels);

            Dictionary<(int, int), int> kernPairs = new Dictionary<(int, int), int>();

            for (int i = 0; i < charCount; i++)
            {
                var ch1 = (char)(options.CharStart + i);
                var glyph1 = glyphs[ch1];

                for (int j = 0; j < charCount; j++)
                {
                    var ch2 = (char)(options.CharStart + j);
                    var glyph2 = glyphs[ch1];

                    var kerning = font.GetKerning(ch1, ch2, scale);

                    if (kerning != 0)
                        kernPairs.Add((ch1, ch2), kerning);
                }
            }

            FontFnt fontFnt = new FontFnt(fontName, null);
            fontFnt.PageNames = new List<string>();
            fontFnt.FontInfo = new FontInfo();
            fontFnt.FontCommon = new FontCommon();
            fontFnt.CharInfo = new CharInfo[charCount];
            fontFnt.KernPairs = new KernPair[kernPairs.Count];

            fontFnt.PageNames.Add($"{fontName}.png");

            fontFnt.FontInfo.FontSize = (short)options.FontSize;
            fontFnt.FontInfo.BitField = 0;
            fontFnt.FontInfo._CharSet = 0;
            fontFnt.FontInfo.StretchH = 100;
            fontFnt.FontInfo.Aa = 1;
            fontFnt.FontInfo.PaddingUp = 0;
            fontFnt.FontInfo.PaddingRight = 0;
            fontFnt.FontInfo.PaddingBottom = 0;
            fontFnt.FontInfo.PaddingLeft = 0;
            fontFnt.FontInfo.SpacingHoriz = (byte)options.Spacing;
            fontFnt.FontInfo.SpacingVert = (byte)options.Spacing;
            fontFnt.FontInfo.Outline = 0;

            fontFnt.FontCommon.LineHeight = (ushort)lineHeight;
            fontFnt.FontCommon.Base = (ushort)(ascent * scale);
            fontFnt.FontCommon.ScaleW = (ushort)textureSize.Width;
            fontFnt.FontCommon.ScaleH = (ushort)textureSize.Height;
            fontFnt.FontCommon.Pages = 1;
            fontFnt.FontCommon.BitField = 0;
            fontFnt.FontCommon.AlphaChnl = 1;
            fontFnt.FontCommon.RedChnl = 0;
            fontFnt.FontCommon.GreenChnl = 0;
            fontFnt.FontCommon.BlueChnl = 0;

            for (int i = 0; i < charCount; i++)
            {
                var ch = (char)(options.CharStart + i);
                var glyph = glyphs[ch];

                if (glyph == null)
                    continue;

                var pos = positions[ch];

                CharInfo charInfo = new CharInfo();
                charInfo.Id = ch;
                charInfo.X = (ushort)pos.X;
                charInfo.Y = (ushort)pos.Y;
                charInfo.Width = (ushort)glyph.Image.Width;
                charInfo.Height = (ushort)glyph.Image.Height;
                charInfo.XOffset = (short)glyph.xOfs;
                charInfo.YOffset = (short)glyph.yOfs;
                charInfo.XAdvance = (short)glyph.xAdvance;
                charInfo.Page = 0;
                charInfo.Chnl = 15;

                fontFnt.CharInfo[i] = charInfo;
            }

            int index = 0;

            foreach (var kvp in kernPairs)
            {
                (var ch1, var ch2) = kvp.Key;
                var amount = kvp.Value;

                KernPair kernPair = new KernPair();
                kernPair.First = (uint)ch1;
                kernPair.Second = (uint)ch2;
                kernPair.Amount = (short)amount;

                fontFnt.KernPairs[index++] = kernPair;
            }

            return (fontFnt, image);
        }

        public static async Task<GlyphBitmap> SvgRender(TTFont font, string svgDoc, int glyph, object userData)
        {
            var options = (FontBMOptions)userData;
            var svgDocument = SvgDocument.FromSvg<SvgDocument>(svgDoc);
            var svg = new Svg.Skia.SKSvg();

            // Set the ViewBox to encompass the full glyph bounding box
            int unitsPerEm = (int)font.UnitsPerEm;
            svgDocument.ViewBox = new SvgViewBox(0, -unitsPerEm, unitsPerEm, unitsPerEm);
            svgDocument.Width = svgDocument.Height = options.FontSize;

            var scale = font.ScaleInPixels(svgDocument.Height.Value);

            font.GetGlyphHMetrics(glyph, out int advanceWidth, out int leftSideBearing);
            int xAdvance = (int)Math.Floor(advanceWidth * scale);

            font.GetFontVMetrics(out int ascent, out int descent, out int lineGap);
            int baseLine = (int)svgDocument.Height.Value - (int)(ascent * scale);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgDocument.GetXML())))
            {
                svg.Load(stream);

                var newHeight = options.FontSize;
                var newWidth = xAdvance + 1;
                var bitmap = new SKBitmap(newWidth, newHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);

                using (var canvas = new SKCanvas(bitmap))
                {
                    // Clear the canvas with the background color
                    canvas.Clear(new SKColor((uint)options.BackgroundColor.ToArgb()));

                    // Draw the SVG picture onto the canvas
                    canvas.DrawPicture(svg.Picture, 0, -baseLine);
                }

                var glyphBitmap = new GlyphBitmap(bitmap.Width, bitmap.Height, bitmap.Bytes);

                return glyphBitmap;
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
