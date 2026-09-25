// VISUAL.C DrawText()/Visual() + MENU.C Menu_Esecuzione()/Visualizza_Bp() ports
using MacEmu.Core;

namespace MacEmu.UI;

public partial class EmuForm
{
    void DrawText()
    {
        DrawTextBase(); // exact rows from SOURCE/VISUAL.C (generated file)
        DrawTitleRow(); // row 1 lives here (supercar also redraws it live)
        _scr.Printc(15, 3, Vga.COLOR6 << 8, _memstack == MEMORY ? "MEMORY" : "STACK ");
        _scr.Printc(9, 46, Vga.COLOR6 << 8, $"{_rates,4}");
        _scr.ColWin(30, 22, 78, 46, '\u2591', Vga.COLOR17 << 8);
    }


    void DrawMainMenu()
    {
        int x0 = 31, y0 = 24, x1 = 52, y1 = 44, coordx = 33, ext = 27;
        _scr.Win(x0, y0, x1, y1, Vga.COLOR13, " MAIN MENU ");
        for (int i = 0; i < MainItems.Length; i++)
            _scr.Printc(coordx, ext + i * 2, Vga.COLOR12 << 8, MainItems[i]);
        _scr.Printc(coordx, ext + _menuPos * 2, Vga.COLOR7 << 8, MainItems[_menuPos]);
    }

    void DrawRunningMenu()
    {
        _scr.ColWin(30, 22, 78, 46, '░', Vga.COLOR17 << 8);
        int x0 = 31, y0 = 25, x1 = 51, y1 = 44, cx = 33, cy = 27;
        _scr.Win(x0, y0, x1, y1, Vga.COLOR13, " RUNNING MENU ");
        string[] items =
        [
            "SPC STOP", "F1  RUN", "F2  MICRO", "F3  MACRO", "F4  MODIFY REG",
            "F5  INS BREAKP.", "F6  DEL BREAKP.", "", "F7  TEXTDISPLAY",
            "F8  NO REFRESH", "F9  MEMDISPLAY", "F10 MEMORY/STACK", "",
            "PGUP/PGDWN SCROLL", "", "ESC PREVIOUS",
        ];
        foreach (var it in items) _scr.Printc(cx, cy++, Vga.COLOR12 << 8, it);
    }

    // VISUAL.C Visual()
    void Visual()
    {
        if (_video == NODISPLAY) { _scr.Printh(8, 6, _m.Reg[0]); return; }
        if (_video == MEMDISPLAY) return; // picture box path
        int y = 6;
        if (_hex == 0)
        {
            for (int i = 0; i < 3; i++) { _scr.Printclear(9, y + i); _scr.Printc(5, y + i, Vga.COLOR0 << 8, " "); }
            for (int i = 0; i < 16; i++) _scr.Printh(7, y++, _m.Reg[i]);
            _scr.Printc(19, 46, Vga.COLOR1 << 8, $"{_mov & 0xFFF:X4}");
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                _scr.Printc(5, y, Vga.COLOR6 << 8, "D");
                _scr.Printc(7, y++, Vga.COLOR0 << 8, $"{_m.Reg[i],6}");
            }
            for (int i = 3; i < 16; i++) _scr.Printh(7, y++, _m.Reg[i]);
            _scr.Printc(19, 46, Vga.COLOR1 << 8, $"{_mov,4}");
        }
        _scr.Printh(8, 25, _m.Mx.Mar);
        _scr.Printh(8, 26, _m.Mx.Mbr);
        _scr.Printh(8, 27, _m.Mpc.Value);
        _scr.Printb(30, 5, _m.CStore.Read(_m.Mpc.Value));
        _m.Keyb.Read(0, out short b0); _scr.Printh(41, 19, b0);
        _m.Keyb.Read(1, out short b1); _scr.Printh(41, 20, b1);

        int sp = _m.Reg[2];
        if (_memstack == STACK)
        {
            for (int i = _mov; i > _mov - 40; i--)
            {
                int row = 45 - (_mov + 1 - i);
                if (sp > i) { _scr.Printc(15, row, Vga.COLOR0 << 8, "   "); _scr.Printclear(19, row); }
                else
                {
                    _m.Mem.Read(i, out short t);
                    _scr.Printc(15, row, Vga.COLOR8 << 8, $"{i & 0xFFF:X3}");
                    _scr.Printh(19, row, t);
                }
            }
        }
        else
        {
            for (int i = _mov; i > _mov - 40; i--)
            {
                int row = 45 - (_mov + 1 - i);
                _scr.Printc(28, row, Vga.COLOR4 << 8, i == _m.Reg[0] ? "►" : " ");
            }
            for (int i = _mov; i > _mov - 40; i--)
            {
                int row = 45 - (_mov + 1 - i);
                if (i >= MacConstants.NumCtrl * 2 && i <= 4095)
                {
                    _m.Mem.Read(i, out short t);
                    _scr.Printc(15, row, Vga.COLOR8 << 8, $"{i & 0xFFF:X3}");
                    _scr.Printh(19, row, t);
                    bool isBp = IsBp(i);
                    _scr.Printc(24, row, (isBp ? Vga.COLOR9 : Vga.COLOR6) << 8, Disassembler.Disassemble(t));
                }
                else if (i >= 0 && i < MacConstants.NumCtrl * 2)
                {
                    _scr.Printc(15, row, Vga.COLOR8 << 8, $"{i & 0xFFF:X3}");
                    _scr.Printc(19, row, Vga.COLOR1 << 8, "----");
                    _scr.Printc(24, row, Vga.COLOR6 << 8, "I/O ");
                }
                else
                {
                    _scr.Printc(15, row, Vga.COLOR0 << 8, "   ");
                    _scr.Printc(19, row, Vga.COLOR1 << 8, "    ");
                    _scr.Printc(24, row, Vga.COLOR6 << 8, "    ");
                }
            }
        }
        _scr.Printc(63, 48, Vga.COLOR1 << 8, $"\"{_m.Printer.Text,-16}\"");
    }

    // MENU.C Visualizza_Bp
    void VisualizzaBp()
    {
        int col = 8, row = 35, i = 0;
        while (i < _bp.Length && _bp[i] != MacConstants.NilAddr)
        {
            _scr.Printc(col, row, Vga.COLOR0 << 8, $"{_bp[i] & 0xFFF:X3}");
            i++; row++;
        }
        for (int k = i; k < _bp.Length; k++, row++) _scr.Printclear(col, row);
    }

    Color[]? _memPal;

    void DrawMemDisplay()
    {
        // VGA mode 13h replica: 320x200 canvas (MEMVIEW.BCF + 64x64 memory
        // pixels 1:1 at 48,68, low byte = index into MEMVIEW's own 256-color
        // palette, like the real DAC), then integer x2 NearestNeighbor =>
        // pixel-perfect 640x400 centered by the PictureBox.
        try
        {
            _memview ??= BcfLoader.Load(Asset("MEMVIEW.BCF"));
            _memPal ??= BcfLoader.LoadPalette(Asset("MEMVIEW.BCF"));
            using var src = new Bitmap(320, 200);
            using (var g = Graphics.FromImage(src))
            {
                g.DrawImage(_memview, 0, 0, 320, 200);
                // VISUAL.C: video is short* (16-bit words) at word offset
                // 48+68*160, one mem word per video word => each mem word
                // paints TWO adjacent pixels (low byte left). Real viewport:
                // 128x64 byte-pixels at (96,68).
                for (int i = 0; i < 4096; i++)
                {
                    short w = _m.Mem.Raw[i];
                    int px = 96 + 2 * (i % 64), py = 68 + (i / 64);
                    using var b0 = new SolidBrush(_memPal[w & 0xFF]);
                    using var b1 = new SolidBrush(_memPal[(w >> 8) & 0xFF]);
                    g.FillRectangle(b0, px, py, 1, 1);
                    g.FillRectangle(b1, px + 1, py, 1, 1);
                }
            }
            var bmp = new Bitmap(1280, 800);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.DrawImage(src, 0, 0, 1280, 800);
            }
            _gfx.Image?.Dispose();
            _gfx.Image = bmp;
        }
        catch (Exception ex)
        {
            // Never leave a black screen: show the error so it can be reported.
            var bmp = new Bitmap(1280, 800);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.DrawString("MEMDISPLAY error: " + ex.Message, SystemFonts.DefaultFont,
                    Brushes.Red, 10, 10);
            }
            _gfx.Image?.Dispose();
            _gfx.Image = bmp;
        }
    }

    void SetVideo(int v)
    {
        _video = v;
        if (v == MEMDISPLAY)
        {
            _scr.Visible = false;
            _gfx.Visible = true;
            DrawMemDisplay();
        }
        else
        {
            _gfx.Visible = false;
            _scr.Visible = true;
            DrawText();
            if (_mode == Mode.Run) DrawRunningMenu(); else DrawMainMenu();
            VisualizzaBp();
            Visual();
            _scr.RefreshDirty();
        }
    }
}
