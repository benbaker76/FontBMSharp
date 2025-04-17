using Baker76.Imaging;
using FontBMSharp;
using System.Diagnostics;
using System.Runtime.InteropServices;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            DisplayHelp();
            return;
        }

        List<string> fileList = new List<string>();
        FontBMOptions options = new FontBMOptions();
        Dictionary<string, string> argsDictionary = new Dictionary<string, string>();

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].ToLower();

            if (arg.StartsWith("-"))
            {
                string key = arg.Split('=')[0];
                string value = arg.Contains("=") ? arg.Split('=')[1] : string.Empty;

                if (!argsDictionary.ContainsKey(key))
                {
                    argsDictionary.Add(key, value);
                }
            }
            else
            {
                fileList.Add(args[i]);
            }
        }

        if (argsDictionary.ContainsKey("-include-blank-char"))
        {
            options.IncludeBlankChar = true;
        }

        if (argsDictionary.ContainsKey("-chars-file="))
        {
            string charsFile = argsDictionary["-chars-file="];
            options.ReadCharsFile(charsFile);
        }
        else if (argsDictionary.ContainsKey("-chars="))
        {
            string[] vals = argsDictionary["-chars="].Split('-');
            int charsStart = 0;
            int charsEnd = 0;

            if (vals.Length != 2 || !Int32.TryParse(vals[0], out charsStart) || !Int32.TryParse(vals[1], out charsEnd))
            {
                Console.WriteLine("ERROR: Invalid value " + argsDictionary["-chars="]);
                return;
            }

            options.CreateChars(charsStart, charsEnd);
        }
        else
            options.CreateChars();
    

        if (argsDictionary.ContainsKey("-font-size="))
        {
            if (!Int32.TryParse(argsDictionary["-font-size="], out options.FontSize))
            {
                Console.WriteLine("ERROR: Invalid value " + argsDictionary["-font-size="]);
                return;
            }

            options.OriginalFontSize = options.FontSize;
        }

        if (argsDictionary.ContainsKey("-spacing="))
        {
            if (!Int32.TryParse(argsDictionary["-spacing="], out options.Spacing))
            {
                Console.WriteLine("ERROR: Invalid value " + argsDictionary["-spacing="]);
                return;
            }
        }

        if (argsDictionary.ContainsKey("-color="))
        {
            if (!TryParseColorString(argsDictionary["-color="], out options.Color))
            {
                Console.WriteLine("ERROR: Invalid color value " + argsDictionary["-color="]);
                return;
            }
        }

        if (argsDictionary.ContainsKey("-background-color="))
        {
            if (!TryParseColorString(argsDictionary["-background-color="], out options.BackgroundColor))
            {
                Console.WriteLine("ERROR: Invalid background color value " + argsDictionary["-background-color="]);
                return;
            }
        }

        if (argsDictionary.ContainsKey("-texture-size="))
        {
            string[] vals = argsDictionary["-texture-size="].Split('x');

            if (vals.Length != 2 || !Int32.TryParse(vals[0], out int width) || !Int32.TryParse(vals[1], out int height))
            {
                Console.WriteLine("ERROR: Invalid texture size value " + argsDictionary["-texture-size="]);
                return;
            }

            options.TextureSize = new System.Drawing.Size(width, height);
        }

        if (argsDictionary.ContainsKey("-auto-size="))
        {
            string[] vals = argsDictionary["-auto-size="].Split('=');

            switch (vals[1].ToLower())
            {
                case "texture":
                    options.AutoSize = AutoSizeMode.Texture;
                    break;
                case "font":
                    options.AutoSize = AutoSizeMode.Font;
                    break;
                default:
                    Console.WriteLine("ERROR: Invalid auto-size value " + argsDictionary["-auto-size="]);
                    return;
            }
        }

        if (argsDictionary.ContainsKey("-no-packing"))
        {
            options.NoPacking = true;
        }

        if (argsDictionary.ContainsKey("-grid-size="))
        {
            string[] vals = argsDictionary["-grid-size="].Split('x');

            if (vals.Length != 2 || !Int32.TryParse(vals[0], out int rows) || !Int32.TryParse(vals[1], out int cols))
            {
                Console.WriteLine("ERROR: Invalid grid size value " + argsDictionary["-grid-size="]);
                return;
            }

            options.GridSize = new System.Drawing.Size(rows, cols);
        }

        if (argsDictionary.ContainsKey("-data-format="))
        {
            string format = argsDictionary["-data-format="].ToLower();
            switch (format)
            {
                case "txt":
                    options.DataFormat = DataFormat.Text;
                    break;
                case "xml":
                    options.DataFormat = DataFormat.Xml;
                    break;
                case "bin":
                    options.DataFormat = DataFormat.Binary;
                    break;
                default:
                    Console.WriteLine("ERROR: Invalid data format value " + argsDictionary["-data-format="]);
                    return;
            }
        }

        if (fileList.Count < 2)
        {
            Console.WriteLine("ERROR: No files specified.");
            return;
        }

        string outputPath = fileList[fileList.Count - 1];

        fileList = fileList.GetRange(0, fileList.Count - 1);

        foreach (string file in fileList)
        {
            if (file.Contains("*"))
            {
                string[] fileArray = Directory.GetFiles(@".\", file);
                foreach (string fontFile in fileArray)
                    ProcessFont(fontFile, outputPath, options);
            }
            else
            {
                ProcessFont(file, outputPath, options);
            }
        }

        Console.WriteLine("Done!");
    }

    public static bool TryParseColorString(string colorString, out Color color)
    {
        color = Color.Empty;
        string[] vals = colorString.Split(',');

        if (vals.Length != 3 && vals.Length != 4)
            return false;

        if (!Int32.TryParse(vals[0], out int r) || !Int32.TryParse(vals[1], out int g) || !Int32.TryParse(vals[2], out int b))
        {
            Console.WriteLine("ERROR: Invalid color value " + colorString);
            return false;
        }

        int a = 255; // Default alpha is 255 (fully opaque)
        if (vals.Length == 4)
        {
            if (!Int32.TryParse(vals[3], out a))
            {
                Console.WriteLine("ERROR: Invalid alpha value " + colorString);
                return false;
            }
        }

        color = Color.FromRgba(r, g, b, a);

        return true;
    }

    public static async void ProcessFont(string fontFileName, string outputPath, FontBMOptions options)
    {
        string fontName = Path.GetFileNameWithoutExtension(fontFileName);
        string fontExtention = Path.GetExtension(fontFileName);

        var buffer = File.ReadAllBytes(fontFileName);

        (FontFnt fontFnt, Image image) = await FontFnt.LoadTTF(buffer, fontName, options);

        Console.WriteLine($"Processed {fontName} ({options.Font.Name})...");

        var pngFileName = Path.Combine(outputPath, fontName + ".png");
        var fntFileName = Path.Combine(outputPath, fontName + ".fnt");

        image.Save(pngFileName);

        switch (options.DataFormat)
        {
            case DataFormat.Text:
                fontFnt.WriteText(fntFileName);
                break;
            case DataFormat.Xml:
                fontFnt.WriteXml(fntFileName);
                break;
            case DataFormat.Binary:
                await fontFnt.Save(fntFileName);
                break;
        }
    }

    static void DisplayHelp()
    {
        Console.WriteLine("Usage: FontBMSharp <filename> <output-path> [options]\n");
        Console.WriteLine("Options:");
        Console.WriteLine("-h or -?                  Display basic help");
        Console.WriteLine("<filename>                Name of the file(s) to process.");
        Console.WriteLine("                          Use the implicit name for a single file, or use");
        Console.WriteLine("                          wildcards to batch process. ie. *.ttf or *.otf");
        Console.WriteLine("<output-path>             The folder to output the processed files");
        Console.WriteLine("-chars=<n-n>              Set the starting and ending character codes. Default is 32-126.");
        Console.WriteLine("-chars-file=<filename>    Use a file to specify which characters to include.");
        Console.WriteLine("-font-size=<n>            Set the font size to be used. Default is 32.");
        Console.WriteLine("-spacing=<n>              Set the spacing between characters. Default is 1.");
        Console.WriteLine("-color=<r,g,b[,a]>        Set the font color. Default is 0,0,0,0 (transparent).");
        Console.WriteLine("-background-color=<r,g,b[,a]> Set the background color. Default is 255,255,255,255 (white).");
        Console.WriteLine("-texture-size=<nxn>       Set the texture size. Default is 256x256.");
        Console.WriteLine("-auto-size=<texture|font> Automatically adjust the texture or font size to fit all glyphs. Default is texture.");
        Console.WriteLine("-no-packing               Disable rectangle packing and draw glyphs in a grid.");
        Console.WriteLine("-grid-size=<nxn>          Set the grid size for no-packing mode. Default is 9x10.");
        Console.WriteLine("-data-format=<txt|xml|bin> Set the output format. Default is txt.");
        Console.WriteLine("-include-blank-char       Include a blank 8x8 glyph for character 0xFFFE.");
    }
}
