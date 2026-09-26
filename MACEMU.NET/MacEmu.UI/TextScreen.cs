// VISUAL.C text[] VRAM port: 80x50 cell buffer rendered with dirty tracking
namespace MacEmu.UI;

public sealed class TextScreen : Control
{
    public const int Cols = 80, Rows = 50;
    private readonly char[,] _ch = new char[Cols, Rows];
    private readonly byte[,] _at = new byte[Cols, Rows];
    private readonly bool[,] _dirty = new bool[Cols, Rows];
    private Bitmap? _buf;
    private Font _font;
    private readonly System.Drawing.Text.PrivateFontCollection _pfc = new();
    private readonly StringFormat _fmt = new(StringFormat.GenericTypographic);
    private float _cwF = 16, _chF = 16;
    private bool _vgaFont;
    // Glyph atlas: each char pre-rendered once into its cell bitmap, then
    // blitted at exact multiples => zero advance drift by construction.
    private readonly Dictionary<(char c, byte fg), Bitmap> _glyphs = new();

    public TextScreen()
    {
        DoubleBuffered = true;
        TabStop = false;
        _font = LoadVgaFont();
        Clear();
        try { Measure(); } catch { /* handle not ready: measured on first paint */ }
    }

    private Font LoadVgaFont()
    {
        try
        {
            string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Fonts", "Px437_IBM_EGA_8x8.ttf");
            if (File.Exists(p))
            {
                _pfc.AddFontFile(p);
                _vgaFont = true;
                // 8x8 outlines: 16px => exact 16x16 cells, pixel-perfect
                return new Font(_pfc.Families[0], 16f, FontStyle.Regular, GraphicsUnit.Pixel);
            }
        }
        catch { }
        _vgaFont = false;
        return new Font("Consolas", 11f, FontStyle.Regular, GraphicsUnit.Pixel);
    }

    private void Measure()
    {
        if (_vgaFont)
        {
            _cwF = 16; _chF = 16;
        }
        else
        {
            using var g = CreateGraphics();
            _chF = _font.Height;
            _cwF = g.MeasureString(new string('W', 32), _font, PointF.Empty, _fmt).Width / 32;
            if (_cwF < 1) _cwF = 1;
        }
        _buf = new Bitmap((int)Math.Ceiling(Cols * _cwF), (int)Math.Ceiling(Rows * _chF));
        for (int x = 0; x < Cols; x++)
            for (int y = 0; y < Rows; y++) _dirty[x, y] = true;
    }

    public void Clear(byte attr = 0x17)
    {
        for (int x = 0; x < Cols; x++)
            for (int y = 0; y < Rows; y++) { _ch[x, y] = ' '; _at[x, y] = attr; _dirty[x, y] = true; }
        Invalidate();
    }

    private Bitmap GetGlyph(char c, byte fgIdx)
    {
        var key = (c, fgIdx);
        if (_glyphs.TryGetValue(key, out var b)) return b;
        int w = Math.Max(1, (int)Math.Round(_cwF)), h = Math.Max(1, (int)Math.Round(_chF));
        b = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(b))
        {
            g.Clear(Color.Transparent);
            g.TextRenderingHint = _vgaFont
                ? System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit
                : System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using var fg = new SolidBrush(Vga.Fg(fgIdx));
            g.DrawString(c.ToString(), _font, fg, 0, 0, _fmt);
        }
        _glyphs[key] = b;
        return b;
    }

    private void PaintDirty()
    {
        // Background runs + per-cell glyph blits at exact multiples:
        // cell grid can no longer drift, whatever the font advances are.
        if (_buf == null) return;
        using var g = Graphics.FromImage(_buf);
        for (int y = 0; y < Rows; y++)
        {
            bool any = false;
            for (int x = 0; x < Cols; x++)
                if (_dirty[x, y]) { any = true; break; }
            if (!any) continue;
            int cx = 0;
            while (cx < Cols)
            {
                byte a = _at[cx, y];
                int x1 = cx + 1;
                while (x1 < Cols && _at[x1, y] == a) x1++;
                using var bg = new SolidBrush(Vga.Bg(a));
                g.FillRectangle(bg, cx * _cwF, y * _chF, (x1 - cx) * _cwF, _chF);
                for (int i = cx; i < x1; i++)
                {
                    char c = _ch[i, y];
                    if (c == '\0' || c == ' ') continue;
                    g.DrawImageUnscaled(GetGlyph(c, a), (int)(i * _cwF), (int)(y * _chF));
                }
                cx = x1;
            }
            for (int i = 0; i < Cols; i++) _dirty[i, y] = false;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_buf == null) { try { Measure(); } catch { return; } }
        PaintDirty();
        if (_buf == null) return;
        // Pixel-perfect: never stretch; center 1:1 on blue background.
        e.Graphics.Clear(Vga.Bg(Vga.COLOR1));
        int x = (ClientSize.Width - _buf.Width) / 2;
        int y = (ClientSize.Height - _buf.Height) / 2;
        e.Graphics.DrawImageUnscaled(_buf, x, y);
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); Invalidate(); }

    public void RefreshDirty() { PaintDirty(); Invalidate(); }

    // --- printc/printh/printclear/Col_Win ports (VISUAL.C, MENU.C) ---
    public void Printc(int x, int y, int color, string format, params object[] args)
    {
        string s = args.Length > 0 ? string.Format(format, args) : format;
        int attr = (color >> 8) & 0xFF;
        for (int i = 0; i < s.Length; i++)
        {
            int cx = x + i;
            if (cx < 0 || cx >= Cols || y < 0 || y >= Rows) continue;
            if (_ch[cx, y] != s[i] || _at[cx, y] != attr)
            { _ch[cx, y] = s[i]; _at[cx, y] = (byte)attr; _dirty[cx, y] = true; }
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
                { _ch[x, y] = c; _at[x, y] = (byte)attr; _dirty[x, y] = true; }
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
}
