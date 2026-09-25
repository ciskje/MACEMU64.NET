// MACEMU.C Esecuzione()/Principale() input routing + menu actions
namespace MacEmu.UI;

public partial class EmuForm
{
    public TextScreen Screen => _scr;
    public Core.Machine Machine => _m;
    public string BaseDir => _baseDir;
    public int Rates { get => _rates; set { _rates = value; _scr.Printc(9, 46, Vga.COLOR6 << 8, $"{_rates,4}"); } }
    public int Mov { get => _mov; set => _mov = value; }
    public bool RestartFlag => _restart;
    public string LastProg { get => _lastProg; set => _lastProg = value; }
    public bool NeverLoad { get => _neverload; set => _neverload = value; }
    public bool MemStackIsMemory => _memstack == MEMORY;
    public int BpCount() { int i = 0; while (i < _bp.Length && _bp[i] != Core.MacConstants.NilAddr) i++; return i; }
    public bool IsBp(int a) { foreach (var b in _bp) if (b == a) return true; return false; }
    public void AddBp(int a) { int n = BpCount(); if (n < _bp.Length) { _bp[n] = a; VisualizzaBp(); Visual(); _scr.RefreshDirty(); } }
    public void DelBp(int idx)
    {
        for (int i = idx; i < _bp.Length - 1; i++) _bp[i] = _bp[i + 1];
        _bp[^1] = Core.MacConstants.NilAddr;
        VisualizzaBp(); Visual(); _scr.RefreshDirty();
    }

    void ShowMessage(string title, string text) => ShowDlg(new MessageDlg(this, title, text));
    void ShowDlg(IDialog d) { _dlg = d; d.Draw(); _scr.RefreshDirty(); }
    void CloseDlg()
    {
        _dlg = null;
        if (_mode == Mode.Main)
        {
            DrawText(); DrawMainMenu(); Visual(); VisualizzaBp();
        }
        else
        {
            _scr.ColWin(30, 22, 79, 47, ' ', Vga.COLOR17 << 8);
            DrawRunningMenu(); Visual(); VisualizzaBp();
        }
        _scr.RefreshDirty();
    }

    void SwapMemStack()
    {
        if (_memstack == STACK) { _memstack = MEMORY; _scr.Printc(15, 3, Vga.COLOR6 << 8, "MEMORY"); }
        else { _memstack = STACK; _scr.Printc(15, 3, Vga.COLOR6 << 8, "STACK "); }
    }

    // ---- keyboard: single dispatch from BOTH ProcessCmdKey and KeyDown ----
    // (arrows/F-keys/Alt combos don't reliably reach KeyDown; chars don't reach
    // ProcessCmdKey). A 100ms dedup guard makes double delivery harmless.
    Keys _lastKey;
    int _lastKeyStamp;

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (DispatchKey(keyData)) return true;
        return base.ProcessCmdKey(ref msg, keyData);
    }

    void OnKeyDown(object? s, KeyEventArgs e)
    {
        if (DispatchKey(e.KeyCode | e.Modifiers)) e.Handled = true;
    }

    static bool IsCommandKey(Keys kd, out Keys code, out bool alt)
    {
        code = kd & Keys.KeyCode;
        alt = (kd & Keys.Alt) == Keys.Alt;
        if (alt) return true;
        return code is Keys.Up or Keys.Down or Keys.Left or Keys.Right
            or Keys.PageUp or Keys.PageDown
            or Keys.F1 or Keys.F2 or Keys.F3 or Keys.F4 or Keys.F5
            or Keys.F6 or Keys.F7 or Keys.F8 or Keys.F9 or Keys.F10
            or Keys.F11 or Keys.F12
            or Keys.Enter or Keys.Escape or Keys.Space or Keys.Back;
    }

    bool DispatchKey(Keys kd)
    {
        if (!IsCommandKey(kd, out Keys code, out bool alt)) return false;
        int now = Environment.TickCount;
        if (kd == _lastKey && now - _lastKeyStamp < 100) return true;
        _lastKey = kd;
        _lastKeyStamp = now;

        if (_dlg != null)
        {
            var se = new KeyEventArgs(kd);
            _dlg.OnKeyDown(se);
            if (_dlg.Finished) CloseDlg(); else _scr.RefreshDirty();
            return true;
        }
        if (_mode == Mode.Splash)
        {
            if (_ending) Application.Exit();
            else EnterMain();
            return true;
        }
        if (_mode == Mode.Main)
        {
            if (alt && code == Keys.A) { ShowDlg(new AboutDlg(this)); return true; }
            if (alt && code == Keys.I) { _hex ^= 1; Visual(); _scr.RefreshDirty(); return true; }
            if (alt && code == Keys.R) { ShowDlg(new RatesDlg(this)); return true; }
            if (alt && code == Keys.J) { ShowDlg(new JumpDlg(this)); return true; }
            switch (code)
            {
                case Keys.Up:
                    _scr.Printc(33, 27 + _menuPos * 2, Vga.COLOR12 << 8, MainItems[_menuPos]);
                    _menuPos = (_menuPos + 7) % 8;
                    _scr.Printc(33, 27 + _menuPos * 2, Vga.COLOR7 << 8, MainItems[_menuPos]);
                    _scr.RefreshDirty(); return true;
                case Keys.Down:
                    _scr.Printc(33, 27 + _menuPos * 2, Vga.COLOR12 << 8, MainItems[_menuPos]);
                    _menuPos = (_menuPos + 1) % 8;
                    _scr.Printc(33, 27 + _menuPos * 2, Vga.COLOR7 << 8, MainItems[_menuPos]);
                    _scr.RefreshDirty(); return true;
                case Keys.Enter: DoMainItem(_menuPos); return true;
                case Keys.Escape: QuitToEnd(); return true;
            }
            return true;
        }
        // Run mode
        if (alt && code == Keys.A) { ShowDlg(new AboutDlg(this)); return true; }
        if (alt && code == Keys.I && _video != MEMDISPLAY) { _hex ^= 1; Visual(); _scr.RefreshDirty(); return true; }
        if (alt && code == Keys.R) { ShowDlg(new RatesDlg(this)); return true; }
        if (alt && code == Keys.J && _video != MEMDISPLAY) { ShowDlg(new JumpDlg(this)); return true; }
        if (code == Keys.Back) return false; // no dialog: let KeyPress deliver to emulated keyboard
        if (code == Keys.Enter) { _m.Keyb.Load(13); return true; } // MACDOS readint wants CR
        if (_video == MEMDISPLAY)
        {
            switch (code)
            {
                case Keys.Space: _cont = false; return true;
                case Keys.F1: _cont = true; return true;
                case Keys.F2: _cont = false; StepMicroOnce(); DrawMemDisplay(); _scr.RefreshDirty(); return true;
                case Keys.F3: _cont = false; StepMacroOnce(); DrawMemDisplay(); _scr.RefreshDirty(); return true;
                case Keys.F7: SetVideo(TEXTDISPLAY); DrawRunningMenu(); Visual(); _scr.RefreshDirty(); return true;
                case Keys.Escape: SetVideo(TEXTDISPLAY); DrawRunningMenu(); Visual(); _scr.RefreshDirty(); return true;
            }
            return true;
        }
        switch (code)
        {
            case Keys.Space: _cont = false; Visual(); _scr.RefreshDirty(); return true;
            case Keys.F1: _cont = true; return true;
            case Keys.F2: _cont = false; StepMicroOnce(); _scr.RefreshDirty(); return true;
            case Keys.F3: _cont = false; StepMacroOnce(); _scr.RefreshDirty(); return true;
            case Keys.F4: Visual(); ShowDlg(new ModRegDlg(this)); return true;
            case Keys.F5: ShowDlg(new InsBpDlg(this)); return true;
            case Keys.F6: ShowDlg(new DelBpDlg(this)); return true;
            case Keys.F7: SetVideo(TEXTDISPLAY); DrawRunningMenu(); Visual(); _scr.RefreshDirty(); return true;
            case Keys.F8:
                SetVideoNoRefresh();
                return true;
            case Keys.F9: SetVideo(MEMDISPLAY); return true;
            case Keys.F10: SwapMemStack(); Visual(); _scr.RefreshDirty(); return true;
            case Keys.PageUp: if (_mov > 15) _mov -= 15; Visual(); _scr.RefreshDirty(); return true;
            case Keys.PageDown: if (_mov < 4094) _mov += 15; Visual(); _scr.RefreshDirty(); return true;
            case Keys.Escape:
                if (_video != TEXTDISPLAY && _video != NODISPLAY) SetVideo(TEXTDISPLAY);
                ExitRun(); return true;
        }
        return true;
    }

    void SetVideoNoRefresh()
    {
        _video = NODISPLAY;
        DrawText();
        DrawRunningMenu();
        VisualizzaBp();
        Visual();
        ShowNoRefreshMsg();
        _scr.RefreshDirty();
    }

    void ShowNoRefreshMsg()
    {
        _scr.Win(54, 38, 76, 42, Vga.COLOR13, " MESSAGE ");
        _scr.Key(56, 40, "NO REFRESH DISPLAY", 2);
    }

    void OnKeyPress(object? s, KeyPressEventArgs e)
    {
        if (_mode == Mode.Splash)
        {
            if (_ending) Application.Exit();
            else EnterMain();
            return;
        }
        if (_dlg != null)
        {
            if (e.KeyChar == '\x1b') return; // Esc via KeyDown
            _dlg.OnChar(e.KeyChar);
            if (_dlg.Finished) CloseDlg(); else _scr.RefreshDirty();
            return;
        }
        if (_mode == Mode.Run)
        {
            // CR and Space are consumed by DispatchKey (CR feeds readint, Space = STOP)
            if (e.KeyChar is '\r' or ' ') return;
            _m.Keyb.Load((short)e.KeyChar);
        }
    }

    void DoMainItem(int i)
    {
        switch (i)
        {
            case 0: ShowDlg(new LoadProgDlg(this)); break;
            case 1: ShowDlg(new LoadCStoreDlg(this)); break;
            case 2: ShowDlg(new ModMemDlg(this)); break;
            case 3: ShowDlg(new ModCStoreDlg(this)); break;
            case 4: EnterRun(); break;
            case 5:
                if (_neverload) ShowMessage(" MESSAGE ", " NEVER FILE LOADED!");
                else { ResetMac(); _restart = true; ShowDlg(new LoadProgDlg(this)); _restart = false; }
                break;
            case 6: ResetMac(); VisualizzaBp(); ShowMessage(" MESSAGE ", " RESET TERMINATED!"); break;
            case 7: QuitToEnd(); break;
        }
    }

    void QuitToEnd() => ShowSplash("MACEND.BCF", true);
}
