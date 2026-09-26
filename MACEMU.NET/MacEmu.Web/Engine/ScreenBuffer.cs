// Headless 80x50 text buffer: TextScreen.cs logic without WinForms/GDI.
// The Blazor canvas renderer (wwwroot/js/macemu.js) draws from snapshots.
namespace MacEmu.Web.Engine;

public sealed class ScreenBuffer
{
    public const int Cols = 80, Rows = 50;
    private readonly char[,] _ch = new char[Cols, Rows];
    private readonly byte[,] _at = new byte[Cols, Rows];

    // Incremented on every real change: the UI pushes a frame only then.
    public int Version { get; private set; }

    public ScreenBuffer() => Clear();

    public void Clear(byte attr = 0x17)
    {
        for (int x = 0; x < Cols; x++)
            for (int y = 0; y < Rows; y++) { _ch[x, y] = ' '; _at[x, y] = attr; }
        Version++;
    }

    public void Printc(int x, int y, int color, string format, params object[] args)
    {
        string s = args.Length > 0 ? string.Format(format, args) : format;
        int attr = (color >> 8) & 0xFF;
        for (int i = 0; i < s.Length; i++)
        {
            int cx = x + i;
            if (cx < 0 || cx >= Cols || y < 0 || y >= Rows) continue;
            if (_ch[cx, y] != s[i] || _at[cx, y] != attr)
            { _ch[cx, y] = s[i]; _at[cx, y] = (byte)attr; Version++; }
        }
    }

    public void Printh(int x, int y, int value)
    {
        uint v = unchecked((uint)(value & 0xFFFF));
        Printc(x, y, Vga.COLOR0 << 8, $"{v:X4}");
    }

    public void Printclear(int x, int y) => Printc(x, y, Vga.COLOR0 << 8, "    ");

    public void Printb(int x, int y, uint a)
    {
        char[] bits = new char[32];
        for (int i = 0; i < 32; i++) { bits[31 - i] = (char)('0' + (a & 1)); a >>= 1; }
        Printc(x, y, Vga.COLOR0 << 8, new string(bits));
    }

    public void ColWin(int x0, int y0, int x1, int y1, char c, int color)
    {
        int attr = (color >> 8) & 0xFF;
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                if (x < 0 || x >= Cols || y < 0 || y >= Rows) continue;
                if (_ch[x, y] != c || _at[x, y] != attr)
                { _ch[x, y] = c; _at[x, y] = (byte)attr; Version++; }
            }
    }

    public void Win(int x0, int y0, int x1, int y1, int color, string title)
    {
        ColWin(x0 + 1, y0 + 1, x1 + 1, y1 + 1, '░', Vga.COLOR18 << 8); // shadow
        ColWin(x0, y0, x1, y1, ' ', color << 8);
        Printc(x0, y0, color << 8, "┌");
        Printc(x1, y0, color << 8, "┐");
        Printc(x0, y1, color << 8, "└");
        Printc(x1, y1, color << 8, "┘");
        for (int i = x0 + 1; i < x1; i++) { Printc(i, y0, color << 8, "─"); Printc(i, y1, color << 8, "─"); }
        for (int i = y0 + 1; i < y1; i++) { Printc(x0, i, color << 8, "│"); Printc(x1, i, color << 8, "│"); }
        int start = x0 + 1 + ((x1 - x0) / 2) - (title.Length / 2);
        Printc(start, y0, color << 8, title);
    }

    public void Key(int x0, int y0, string text, int hilite)
    {
        Printc(x0, y0, Vga.COLOR15 << 8, text);
        if (hilite >= 0 && hilite < text.Length)
            Printc(x0 + hilite, y0, Vga.COLOR16 << 8, text[hilite].ToString());
        for (int i = 0; i < text.Length; i++) Printc(x0 + i + 1, y0 + 1, Vga.COLOR5 << 8, "▄"); // MENU.C Key: sotto = 0xDC
        Printc(x0 + text.Length, y0, Vga.COLOR5 << 8, "▀"); // MENU.C Key: lato = 0xDF
    }

    public void RefreshDirty() { /* no-op headless: ScreenBuffer.Version tracks changes; kept for 1:1 ports */ }

    public string SnapshotChars()
    {
        var c = new char[Cols * Rows];
        for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Cols; x++)
                c[y * Cols + x] = _ch[x, y];
        return new string(c);
    }

    public byte[] SnapshotAttrs()
    {
        var a = new byte[Cols * Rows];
        for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Cols; x++)
                a[y * Cols + x] = _at[x, y];
        return a;
    }
}
