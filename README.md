# FontBMSharp

FontBMSharp is a command-line tool for processing font files and generating bitmap fonts. It allows you to convert TrueType and OpenType font files into bitmap fonts with various customization options, including character range, font size, spacing, color, background color, and texture size.

The tool outputs fonts in the [AngelCode BMFont format](https://www.angelcode.com/products/bmfont/doc/file_format.html), which is widely used in game development for handling bitmap fonts.

![sample](/.github/img/sample0.png?raw=true)
![sample](/.github/img/sample1.png?raw=true)
![sample](/.github/img/sample2.png?raw=true)

## Features

- Convert TrueType (.ttf) and OpenType (.otf) fonts to bitmap fonts.
- Supports OpenType-SVG fonts.
- Specify the character range to include in the bitmap font.
- Customize the font size and spacing between characters.
- Set custom colors for the font and background.
- Automatically adjust texture size to fit all glyphs, using powers of two.
- Outputs in the [AngelCode BMFont format](https://www.angelcode.com/products/bmfont/doc/file_format.html).

## Usage

```bash
FontBMSharp <filename> <output-path> [options]
```

- `<filename>`: The name of the font file(s) to process. You can specify a single file or use wildcards (e.g., `*.ttf` or `*.otf`) for batch processing.
- `<output-path>`: The directory where the processed files will be saved.

## Command Line Arguments

| Argument              | Description                                                                 | Default Value           |
|-----------------------|-----------------------------------------------------------------------------|-------------------------|
| `-h` or `-?`          | Display help information.                                                   | -                       |
| `-chars=<n-n>`        | Set the starting and ending character codes to include in the bitmap font.  | `32-126`                |
| `-font-size=<n>`      | Set the font size to be used.                                                | `32`                    |
| `-spacing=<n>`        | Set the spacing between characters.                                          | `1`                     |
| `-color=<r,g,b[,a]>`      | Set the font color using RGB[A] values.                                         | `0,0,0,0` (transparent) |
| `-background-color=<r,g,b[,a]>` | Set the background color using RGB[A] values.                          | `255,255,255,255` (white)   |
| `-texture-size=<nxn>` | Set the initial texture size in pixels (width x height).                     | `256x256`               |
| `-auto-size`          | Automatically adjust the texture size to fit all glyphs.                     | `true`                  |
| `-data-format=<txt\|xml\|bin>` | Set the output format.                                                    | `txt`                   |

## Example

To process a font file `Arial.ttf` and save the output to the current directory with custom settings:

```bash
FontBMSharp Arial.ttf . -chars=32-126 -font-size=48 -spacing=1 -color=255,0,0 -background-color=0,0,0 -texture-size=256x256 -data-format=xml
```

## License

FontBMSharp is licensed under the MIT License. See the [LICENSE](LICENSE) file for more information.

## Contributions

Contributions are welcome! If you have suggestions or improvements, feel free to submit a pull request or open an issue.

## Acknowledgments

FontBMSharp is inspired by [fontbm](https://github.com/vladimirgamalyan/fontbm) by [Vladimir Gamalyan](https://github.com/vladimirgamalyan). Also check out [my fork of fontbm](https://github.com/benbaker76/fontbm) which adds OpenType-SVG support.

FontBMSharp uses font rendering code from [Relfos](https://github.com/Relfos)'s [LunarFonts](https://github.com/Relfos/LunarFonts) project.
