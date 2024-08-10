﻿using Baker76.Imaging;
using FontBMSharp;

class Program
{
    static void Main(string[] args)
    {
        //args = new string[] { @"fonts\Zebrawood.otf", "output" };
        //args = new string[] { @"fonts\*.otf", "output" };

        if (args.Length == 0)
        {
            DisplayHelp();
            return;
        }

        List<string> fileList = new List<string>();
        FontBMOptions options = new FontBMOptions { AutoSize = true, TextureSize = new System.Drawing.Size(256, 256) };

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].ToLower();

            if (arg.StartsWith("-"))
            {
                if (arg == "-?" || arg == "-h")
                {
                    DisplayHelp();
                    return;
                }

                if (arg.StartsWith("-chars="))
                {
                    string[] vals = arg.Split('=')[1].Split('-');

                    if (vals.Length != 2 || !Int32.TryParse(vals[0], out options.CharStart) || !Int32.TryParse(vals[1], out options.CharEnd))
                    {
                        Console.WriteLine("ERROR: Invalid value " + args[i]);
                        return;
                    }
                }

                if (arg.StartsWith("-font-size="))
                {
                    string[] vals = arg.Split('=');

                    if (!Int32.TryParse(vals[1], out options.FontSize))
                    {
                        Console.WriteLine("ERROR: Invalid value " + args[i]);
                        return;
                    }
                }

                if (arg.StartsWith("-spacing="))
                {
                    string[] vals = arg.Split('=');

                    if (!Int32.TryParse(vals[1], out options.Spacing))
                    {
                        Console.WriteLine("ERROR: Invalid value " + args[i]);
                        return;
                    }
                }

                if (arg.StartsWith("-color="))
                {
                    if (!TryParseColorString(arg.Split('=')[1], out options.Color))
                    {
                        Console.WriteLine("ERROR: Invalid color value " + args[i]);
                        return;
                    }
                }

                if (arg.StartsWith("-background-color="))
                {
                    if (!TryParseColorString(arg.Split('=')[1], out options.BackgroundColor))
                    {
                        Console.WriteLine("ERROR: Invalid background color value " + args[i]);
                        return;
                    }
                }

                if (arg.StartsWith("-texture-size="))
                {
                    string[] vals = arg.Split('=')[1].Split('x');

                    if (vals.Length != 2 || !Int32.TryParse(vals[0], out int width) || !Int32.TryParse(vals[1], out int height))
                    {
                        Console.WriteLine("ERROR: Invalid texture size value " + args[i]);
                        return;
                    }

                    options.TextureSize = new System.Drawing.Size(width, height);
                    options.AutoSize = false;
                }

                if (arg == "-auto-size")
                {
                    options.AutoSize = true;
                }

                if (arg.StartsWith("-data-format="))
                {
                    string format = arg.Split('=')[1].ToLower();
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
                            Console.WriteLine("ERROR: Invalid data format value " + args[i]);
                            return;
                    }
                }
            }
            else
            {
                fileList.Add(args[i]);
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

        Console.WriteLine($"Processing {fontName}...");

        var buffer = File.ReadAllBytes(fontFileName);

        (FontFnt fontFnt, Image image) = await FontFnt.LoadTTF(buffer, fontName, options);

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
        Console.WriteLine("-font-size=<n>            Set the font size to be used. Default is 32.");
        Console.WriteLine("-spacing=<n>              Set the spacing between characters. Default is 1.");
        Console.WriteLine("-color=<r,g,b[,a]>        Set the font color. Default is 0,0,0,0 (transparent).");
        Console.WriteLine("-background-color=<r,g,b[,a]> Set the background color. Default is 255,255,255,255 (white).");
        Console.WriteLine("-texture-size=<nxn>       Set the texture size. Default is 256x256.");
        Console.WriteLine("-auto-size                Automatically size the texture. Default is true.");
        Console.WriteLine("-data-format=<txt|xml|bin> Set the output format. Default is txt.");
    }
}