// VISUAL.C LoadPic port: .BCF = modified uncompressed 8-bit TGA 320x200
namespace MacEmu.UI;

public static class BcfLoader
{
    public static Color[] LoadPalette(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);
        br.ReadBytes(18); // TGA header
        if (fs.Length < 18 + 768) throw new InvalidDataException($"bad BCF {path}");
        Color[] pal = new Color[256];
        for (int i = 0; i < 256; i++)
        {
            int b = br.ReadByte() >> 2, g = br.ReadByte() >> 2, r = br.ReadByte() >> 2;
            pal[i] = Color.FromArgb(r * 255 / 63, g * 255 / 63, b * 255 / 63);
        }
        return pal;
    }

    public static Bitmap Load(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);
        byte[] hdr = br.ReadBytes(18);
        int w = hdr[12] | (hdr[13] << 8), h = hdr[14] | (hdr[15] << 8);
        if (w <= 0 || h <= 0 || hdr[16] != 8) throw new InvalidDataException($"bad BCF {path}");
        Color[] pal = new Color[256];
        for (int i = 0; i < 256; i++)
        {
            int b = br.ReadByte() >> 2, g = br.ReadByte() >> 2, r = br.ReadByte() >> 2;
            pal[i] = Color.FromArgb(r * 255 / 63, g * 255 / 63, b * 255 / 63);
        }
        var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        for (int y = h - 1; y >= 0; y--)
            for (int x = 0; x < w; x++)
            {
                int idx = br.ReadByte();
                bmp.SetPixel(x, y, pal[idx]);
            }
        return bmp;
    }
}
