// Headless port of MacEmu.UI/EmuForm*.cs: same state machine driving
// MacEmu.Core, but rendering into a ScreenBuffer and flashing dialogs as
// data (RequestedPanel / Message / About) for the Blazor UI to display
// as HTML. Fidelity refs point at ../MacEmu.UI/ and ../SOURCE/.
using MacEmu.Core;

namespace MacEmu.Web.Engine;

public sealed class EmuEngine
{
    public enum Mode { Splash, Main, Run }

    public const int NODISPLAY = 0, TEXTDISPLAY = 1, MEMDISPLAY = 2;
    public const int STACK = 0, MEMORY = 1;

    public static readonly string[] MainItems =
    [
        " LOAD PROGRAM     ", " LOAD CTRL STORE  ", " MODIFY MEMORY    ",
        " MODIFY CTRL STORE", " RUNNING MENU     ", " RESTART          ",
        " RESET            ", " QUIT             ",
    ];

    public readonly Machine Machine = new();
    public readonly ScreenBuffer Screen = new();

    public Mode CurrentMode { get; private set; } = Mode.Splash;
    public bool SplashEnding { get; private set; }
    public string SplashBcf => SplashEnding ? "bcf/MACEND.BCF" : "bcf/MACSTART.BCF";

    public int Video { get; private set; } = TEXTDISPLAY;
    public bool IsMemStackMemory => _memstack == MEMORY;
    public int MenuPos => _menuPos;
    public int Rates { get => _rates; set { _rates = value; Screen.Printc(9, 46, Vga.COLOR6 << 8, $"{_rates,4}"); } }
    public int Mov { get => _mov; set => _mov = value; }
    public bool Running => _cont;
    public bool Hex => _hex != 0;
    public bool NeverLoad { get => _neverload; set => _neverload = value; }
    public string LastProg { get => _lastProg; set => _lastProg = value; }
    public bool RestartFlag { get => _restartPending; set => _restartPending = value; }
    public string PrinterText => Machine.Printer.Text;
    public bool HasDialog => _dlg != null;

    // File resolution (prefetched samples + browser uploads) for load dialogs.
    public Func<string, string[]?>? FileResolver;
    public string[]? ResolveFile(string rel) => FileResolver?.Invoke(rel);

    // Browser picker request for real disk files ("File not found!" stage).
    public string? UploadRequest { get; private set; }
    public string? TakeUploadRequest() { var u = UploadRequest; UploadRequest = null; return u; }

    // Bumped on mode/video/message/panel changes so the UI refreshes chrome.
    public int UiVersion { get; private set; }

    public Action? OnBell;

    private int _hex, _mov = 4094, _memstack = STACK;
    private int[] _bp = new int[MacConstants.MaxBp];
    private int _rates = 8;
    private bool _cont;
    private bool _neverload = true, _restartPending;
    private string _lastProg = "";
    private int _menuPos;
    private IDialog? _dlg;
    private int _runx = 48, _superflag;
    private bool _runRight = true;
    private string[]? _osLines, _micLines;

    public EmuEngine()
    {
        for (int i = 0; i < _bp.Length; i++) _bp[i] = MacConstants.NilAddr;
        Machine.Printer.OnBell = () => OnBell?.Invoke();
    }

    void Touch() => UiVersion++;

    // ---- boot / reset (MENU.C ResetMac + LoadOS) ----

    public void Boot(IEnumerable<string>? macdosLines, IEnumerable<string>? micLines)
    {
        _osLines = macdosLines?.ToArray();
        _micLines = micLines?.ToArray();
        FullReset();
        CurrentMode = Mode.Splash;
        SplashEnding = false;
        Touch();
    }

    // MENU.C ResetMac: machine reset + OS + microcode + breakpoints + TEXTDISPLAY.
    public void FullReset()
    {
        Machine.Reset();
        if (_osLines != null) LoadOsLines(Machine, _osLines);
        else LoadOsStub();
        if (_micLines != null) MicLoader.LoadInto(Machine, _micLines);
        for (int i = 0; i < _bp.Length; i++) _bp[i] = MacConstants.NilAddr;
        Video = TEXTDISPLAY;
        Visual();
    }

    static void LoadOsLines(Machine m, IEnumerable<string> lines)
    {
        // MENU.C LoadOS: mem[255]=ILC + program.
        using var e = lines.GetEnumerator();
        if (!e.MoveNext()) return;
        int ilc = Convert.ToInt32(e.Current.Trim(), 16);
        m.Reg[MacConstants.R_PC] = (short)ilc;
        m.Mem.Write(255, (short)ilc);
        int j = ilc;
        while (e.MoveNext())
        {
            var s = e.Current.Trim();
            if (s.Length == 0) continue;
            m.Mem.Write(j, unchecked((short)Convert.ToInt32(s, 16)));
            j++;
        }
    }

    void LoadOsStub()
    {
        // MENU.C LoadOS fallback stub (12/11/96) when MACDOS.MAC missing
        Machine.Reset();
        Machine.Mem.Write(255, 0xfc);
        Machine.Mem.Write(252, 0x7000);
        Machine.Mem.Write(253, 0x1005);
        Machine.Mem.Write(254, unchecked((short)0xfd00));
    }

    // ---- mode transitions (EmuForm.cs) ----

    public void EnterMainFromSplash()
    {
        SplashEnding = false;
        CurrentMode = Mode.Main;
        _menuPos = 0;
        DrawText();
        // Desktop EnterMain resets the machine here (fresh OS + microcode on
        // every entry to main); boot assets are cached, so FullReset is equal.
        FullReset();
        DrawMainMenu();
        Touch();
    }

    public void QuitToEnd()
    {
        SplashEnding = true;
        CurrentMode = Mode.Splash;
        _cont = false;
        Touch();
    }

    public void MenuUp()
    {
        Screen.Printc(33, 27 + _menuPos * 2, Vga.COLOR12 << 8, MainItems[_menuPos]);
        _menuPos = (_menuPos + 7) % 8;
        Screen.Printc(33, 27 + _menuPos * 2, Vga.COLOR7 << 8, MainItems[_menuPos]);
    }

    public void MenuDown()
    {
        Screen.Printc(33, 27 + _menuPos * 2, Vga.COLOR12 << 8, MainItems[_menuPos]);
        _menuPos = (_menuPos + 1) % 8;
        Screen.Printc(33, 27 + _menuPos * 2, Vga.COLOR7 << 8, MainItems[_menuPos]);
    }

    public void ActivateMain() => DoMainItem(_menuPos);

    // Mouse/touch equivalent: highlight the clicked row, then activate.
    public void ClickMainItem(int i)
    {
        if (CurrentMode != Mode.Main || i < 0 || i >= MainItems.Length) return;
        Screen.Printc(33, 27 + _menuPos * 2, Vga.COLOR12 << 8, MainItems[_menuPos]);
        _menuPos = i;
        Screen.Printc(33, 27 + _menuPos * 2, Vga.COLOR7 << 8, MainItems[_menuPos]);
        DoMainItem(_menuPos);
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
                else { DrawText(); FullReset(); RestartFlag = true; ShowDlg(new LoadProgDlg(this)); RestartFlag = false; }
                break;
            case 6:
                DrawText(); FullReset(); VisualizzaBp();
                ShowMessage(" MESSAGE ", " RESET TERMINATED!");
                break;
            case 7: QuitToEnd(); break;
        }
    }

    public void EnterRun()
    {
        CurrentMode = Mode.Run;
        _cont = false;
        DrawRunningMenu();
        Visual();
        Touch();
    }

    public void ExitRun()
    {
        _cont = false;
        CurrentMode = Mode.Main;
        DrawText();
        DrawMainMenu();
        Visual();
        VisualizzaBp();
        Touch();
    }

    // ---- RUN engine (Esecuzione, EmuForm.cs OnTick) ----

    public void SetRunning(bool on) { _cont = on; Touch(); }

    public void Tick()
    {
        if (CurrentMode != Mode.Run || !_cont || _dlg != null) return;
        if (Video == MEMDISPLAY)
        {
            for (int i = 0; i < _rates * 1500 && !Machine.Halt; i++) Machine.StepMicro();
            CheckBpOrHalt();
            return;
        }
        int budget = Video == NODISPLAY ? 300_000 : _rates * 1500;
        for (int i = 0; i < budget && !Machine.Halt && _cont; i++)
        {
            Machine.StepMicro();
            if (Machine.Mpc.Value == 0 && IsBp(Machine.Reg[MacConstants.R_PC])) { _cont = false; break; }
        }
        if (Video == NODISPLAY) Screen.Printh(8, 6, Machine.Reg[0]);
        else { SuperCar(); Visual(); }
        if (Machine.Halt)
        {
            _cont = false;
            Visual();
            Machine.Halt = false;
            ShowMessage(" MESSAGE ", "PROGRAM TERMINATED");
        }
    }

    void CheckBpOrHalt()
    {
        if (Machine.Halt)
        {
            _cont = false;
            Machine.Halt = false;
            if (Video != MEMDISPLAY) { Visual(); ShowMessage(" MESSAGE ", "PROGRAM TERMINATED"); }
        }
    }

    public void StepMicroOnce()
    {
        Machine.StepMicro();
        if (Machine.Halt) { _cont = false; Visual(); Machine.Halt = false; ShowMessage(" MESSAGE ", "PROGRAM TERMINATED"); }
        else Visual();
    }

    public void StepMacroOnce()
    {
        Machine.StepMacro();
        if (Machine.Halt) { _cont = false; Visual(); Machine.Halt = false; ShowMessage(" MESSAGE ", "PROGRAM TERMINATED"); }
        else Visual();
    }

    void SuperCar()
    {
        if (_superflag++ < 12) return;
        _superflag = 0;
        if (_runx >= 65) _runRight = false;
        if (_runx <= 46) _runRight = true;
        DrawTitleRow();
        Screen.Printc(_runx, 1, Vga.COLOR6 << 8, "████");
        _runx += _runRight ? 1 : -1;
    }

    // ---- video / view options ----

    public void SetVideo(int v)
    {
        Video = v;
        Touch();
        if (v != MEMDISPLAY)
        {
            DrawText();
            if (CurrentMode == Mode.Run) DrawRunningMenu(); else DrawMainMenu();
            VisualizzaBp();
            Visual();
        }
    }

    public void SetVideoNoRefresh()
    {
        Video = NODISPLAY;
        DrawText();
        DrawRunningMenu();
        VisualizzaBp();
        Visual();
        ShowNoRefreshMsg();
        Touch();
    }

    void ShowNoRefreshMsg()
    {
        Screen.Win(54, 38, 76, 42, Vga.COLOR13, " MESSAGE ");
        Screen.Key(56, 40, "NO REFRESH DISPLAY", 2);
    }

    public void SwapMemStack()
    {
        if (_memstack == STACK) { _memstack = MEMORY; Screen.Printc(15, 3, Vga.COLOR6 << 8, "MEMORY"); }
        else { _memstack = STACK; Screen.Printc(15, 3, Vga.COLOR6 << 8, "STACK "); }
    }

    public void ScrollMov(int delta)
    {
        if (delta < 0) { if (_mov > 15) _mov -= 15; }
        else { if (_mov < 4094) _mov += 15; }
        Visual();
    }

    public void ToggleHex() { _hex ^= 1; Visual(); }

    // ---- breakpoints (EmuFormInput.cs) ----

    public int BpCount() { int i = 0; while (i < _bp.Length && _bp[i] != MacConstants.NilAddr) i++; return i; }
    public bool IsBp(int a) { foreach (var b in _bp) if (b == a) return true; return false; }
    public int[] BpList() => _bp[..BpCount()];
    public void AddBp(int a) { int n = BpCount(); if (n < _bp.Length) { _bp[n] = a; VisualizzaBp(); Visual(); } }
    public void DelBp(int idx)
    {
        for (int i = idx; i < _bp.Length - 1; i++) _bp[i] = _bp[i + 1];
        _bp[^1] = MacConstants.NilAddr;
        VisualizzaBp(); Visual();
    }

    // ---- loaders / modifiers (LoadProgDlg / LoadCStoreDlg / Mod*Dlg) ----

    public (int ilc, int count) LoadProgram(IEnumerable<string> lines, string name)
    {
        var (ilc, n) = MacLoader.LoadInto(Machine, lines);
        _neverload = false;
        _lastProg = name;
        _restartPending = false;
        if (_memstack == MEMORY) _mov = ilc + 39;
        VisualizzaBp();
        return (ilc, n);
    }

    public int LoadCStore(IEnumerable<string> lines) => MicLoader.LoadInto(Machine, lines);

    public void RefreshViews() { Visual(); VisualizzaBp(); }

    // Assembled words straight into memory (web Masm/MiniC tabs).
    public (int ilc, int count) LoadAsmWords(short[] words, int ilc, string name)
    {
        var lines = new List<string>(words.Length + 1) { $"{ilc:x4}" };
        lines.AddRange(words.Select(w => $"{w & 0xffff:x4}"));
        return LoadProgram(lines, name);
    }

    public int LoadMicWords(uint[] words)
    {
        var lines = words.Select(MicLoader.ToMicLine);
        return LoadCStore(lines);
    }

    // ---- message / about / panels ----

    public void ShowMessage(string title, string text) => ShowDlg(new MessageDlg(this, title, text));
    public void ShowAbout() => ShowDlg(new AboutDlg(this));
    void ShowDlg(IDialog d) { _dlg = d; d.Draw(); Touch(); }
    void CloseDlg()
    {
        _dlg = null;
        RefreshAfterDialog();
        Touch();
    }

    public void RequestUpload(string kind) { UploadRequest = kind; Touch(); }
    public void SupplyUpload(string[] lines, string name)
    {
        UploadRequest = null;
        if (_dlg is LoadProgDlg lp) lp.SupplyUploaded(lines, name);
        else if (_dlg is LoadCStoreDlg lc) lc.SupplyUploaded(lines);
        Touch();
    }
    public void CancelUpload()
    {
        UploadRequest = null;
        if (_dlg is DlgBase b) b.Cancel();
        if (_dlg != null) CloseDlg(); else Touch();
    }

    void RefreshAfterDialog()
    {
        if (CurrentMode == Mode.Main)
        {
            DrawText(); DrawMainMenu(); Visual(); VisualizzaBp();
        }
        else
        {
            Screen.ColWin(30, 22, 79, 47, ' ', Vga.COLOR17 << 8);
            DrawRunningMenu(); Visual(); VisualizzaBp();
        }
    }

    // ---- keyboard (EmuFormInput.cs DispatchKey + OnKeyPress, DOM codes) ----

    // Returns true when the key is consumed (caller calls preventDefault).
    public bool HandleKey(string code, string key, bool alt)
    {
        // Browser keys that must keep their native behavior (focus nav, etc.)
        if (code is "Tab" or "F11" or "F12") return false;
        if (_dlg != null)
        {
            if (_dlg is ModRegDlg mr && (code == "ArrowUp" || code == "ArrowDown"))
            {
                mr.OnUpDown(code == "ArrowUp" ? -1 : 1);
                return true;
            }
            _dlg.OnKeyDown(ToDlgKey(code));
            if (_dlg.Finished) CloseDlg();
            return true;
        }
        if (CurrentMode == Mode.Splash)
        {
            if (IsSplashKey(code, key)) { ReenterMainRequest(); return true; }
            return false;
        }
        if (alt && HandleAlt(code)) return true;
        if (CurrentMode == Mode.Main)
        {
            switch (code)
            {
                case "ArrowUp": MenuUp(); return true;
                case "ArrowDown": MenuDown(); return true;
                case "Enter": ActivateMain(); return true;
                case "Escape": QuitToEnd(); return true;
            }
            return true;
        }
        // Run mode
        if (code == "Backspace") return false; // desktop: not consumed either
        if (code == "Enter") { Machine.Keyb.Load(13); return true; } // MACDOS readint wants CR
        if (Video == MEMDISPLAY)
        {
            switch (code)
            {
                case "Space": _cont = false; Touch(); return true;
                case "F1": _cont = true; Touch(); return true;
                case "F2": _cont = false; StepMicroOnce(); Touch(); return true;
                case "F3": _cont = false; StepMacroOnce(); Touch(); return true;
                case "F7": SetVideo(TEXTDISPLAY); return true;
                case "Escape": SetVideo(TEXTDISPLAY); return true;
            }
            return true;
        }
        switch (code)
        {
            case "Space": _cont = false; Visual(); Touch(); return true;
            case "F1": _cont = true; Touch(); return true;
            case "F2": _cont = false; StepMicroOnce(); Touch(); return true;
            case "F3": _cont = false; StepMacroOnce(); Touch(); return true;
            case "F4": ShowDlg(new ModRegDlg(this)); return true;
            case "F5": ShowDlg(new InsBpDlg(this)); return true;
            case "F6": ShowDlg(new DelBpDlg(this)); return true;
            case "F7": SetVideo(TEXTDISPLAY); return true;
            case "F8": SetVideoNoRefresh(); return true;
            case "F9": SetVideo(MEMDISPLAY); return true;
            case "F10": SwapMemStack(); Visual(); return true;
            case "PageUp": ScrollMov(-15); return true;
            case "PageDown": ScrollMov(15); return true;
            case "Escape":
                if (Video != TEXTDISPLAY && Video != NODISPLAY) SetVideo(TEXTDISPLAY);
                ExitRun(); return true;
        }
        return true;
    }

    // Desktop splash exits the process on MACEND; on the web we loop back.
    void ReenterMainRequest()
    {
        EnterMainFromSplash();
    }

    static bool IsSplashKey(string code, string key) =>
        code is "Enter" or "NumpadEnter" or "Escape" or "Space" ||
        code.StartsWith("F") || code.StartsWith("Arrow") ||
        key.Length == 1;

    public void HandleChar(char c)
    {
        if (_dlg != null)
        {
            if (c == '\x1b') return; // Esc via KeyDown
            _dlg.OnChar(c);
            if (_dlg.Finished) CloseDlg();
            return;
        }
        if (CurrentMode == Mode.Splash) { ReenterMainRequest(); return; }
        if (CurrentMode == Mode.Run && Video != MEMDISPLAY)
        {
            // CR and Space are consumed by HandleKey (CR feeds readint, Space = STOP)
            if (c is '\r' or ' ') return;
            Machine.Keyb.Load((short)c);
        }
    }

    bool HandleAlt(string code)
    {
        if (code == "KeyA" && Video != MEMDISPLAY) { ShowAbout(); return true; }
        if (code == "KeyI" && Video != MEMDISPLAY) { ToggleHex(); return true; }
        if (code == "KeyR") { ShowDlg(new RatesDlg(this)); return true; }
        if (code == "KeyJ" && Video != MEMDISPLAY) { ShowDlg(new JumpDlg(this)); return true; }
        return false;
    }

    static DlgKey ToDlgKey(string code) => code switch
    {
        "Backspace" => DlgKey.Back,
        "Enter" or "NumpadEnter" => DlgKey.Enter,
        "Escape" => DlgKey.Escape,
        _ => DlgKey.Other,
    };

    // ---- MEMDISPLAY pixel source: 4096 words -> 8192 bytes (lo,hi per word) ----

    public byte[] MemWordsBytes()
    {
        var raw = Machine.Mem.Raw;
        var b = new byte[raw.Length * 2];
        for (int i = 0; i < raw.Length; i++)
        {
            b[2 * i] = (byte)(raw[i] & 0xFF);
            b[2 * i + 1] = (byte)((raw[i] >> 8) & 0xFF);
        }
        return b;
    }

    // ---- drawing (EmuFormDraw.cs + EmuFormDrawText.generated.cs ports) ----

    void DrawText()
    {
        DrawTextBase();
        DrawTitleRow();
        Screen.Printc(15, 3, Vga.COLOR6 << 8, _memstack == MEMORY ? "MEMORY" : "STACK ");
        Screen.Printc(9, 46, Vga.COLOR6 << 8, $"{_rates,4}");
        Screen.ColWin(30, 22, 78, 46, '░', Vga.COLOR17 << 8);
    }

    void DrawTitleRow()
    {
        Screen.Printc(0, 1, Vga.COLOR1 << 8, "║ MACEMU 4.0c   (.NET)   -  Copyright(c) 1996                         ║ bout...║");
        Screen.Printc(71, 1, Vga.COLOR6 << 8, "A");
    }

    void DrawTextBase()
    {
        int c1 = Vga.COLOR1 << 8;
        Screen.Printc(0, 0, c1, "╔═════════════════════════════════════════════════════════════════════╦════════╗");
        Screen.Printc(0, 2, c1, "╠════════════╦═══════════════╦════════════════════════════════════════╩════════╣");
        Screen.Printc(0, 3, c1, "║ REGISTRI   ║ STACK         ║ MIR                                             ║");
        Screen.Printc(0, 4, c1, "╠════════════╬═══════════════╬═════════════════════════════════════════════════╣");
        Screen.Printc(0, 5, c1, "║            ║               ║                                                 ║");
        Screen.Printc(0, 6, c1, "║ PC  :      ║               ╠═|[][][]|||||[  ][  ][  ][      ]════════════════╣");
        Screen.Printc(0, 7, c1, "║ AC  :      ║               ║ │ │ │ ││││││  │   │   │     │                   ║");
        Screen.Printc(0, 8, c1, "║ SP  :      ║               ║ │ │ │ ││││││  C   B   A   ADDR                  ║");
        Screen.Printc(0, 9, c1, "║ IR  :      ║               ║ A C A S│││││                                    ║");
        Screen.Printc(0, 10, c1, "║ TIR :      ║               ║ M O L H││││└─ENC                                ║");
        Screen.Printc(0, 11, c1, "║ ZERO:      ║               ║ U N U I│││└──WR    ┌────────────────────────────║");
        Screen.Printc(0, 12, c1, "║ UNO :      ║               ║ X D   F││└───RD    │COND 00 No jump             ║");
        Screen.Printc(0, 13, c1, "║ -UNO:      ║               ║       T││          │     01 jump if n           ║");
        Screen.Printc(0, 14, c1, "║ AMSK:      ║               ║       E│└────MAR   │     10 jump if z           ║");
        Screen.Printc(0, 15, c1, "║ SMSK:      ║               ║       R└──────BR   │     11 jump always         ║");
        Screen.Printc(0, 16, c1, "║ A   :      ║               ╠════════════════╗   │                            ║");
        Screen.Printc(0, 17, c1, "║ B   :      ║               ║KEYBOARD DEVICE ║   │ALU 00 sum SHIFTER 00 id    ║");
        Screen.Printc(0, 18, c1, "║ C   :      ║               ╠════════════════╣   │    01 and         01 right ║");
        Screen.Printc(0, 19, c1, "║ D   :      ║               ║BR :            ║   │    10 id          10 left  ║");
        Screen.Printc(0, 20, c1, "║ E   :      ║               ║CSR:            ║   |    11 not         11 halt  ║");
        Screen.Printc(0, 21, c1, "║ F   :      ║               ╠════════════════╩════════════════════════════════╣");
        Screen.Printc(0, 22, c1, "║            ║               ║                                                 ║");
        Screen.Printc(0, 23, c1, "╠════════════╣               ║                                                 ║");
        Screen.Printc(0, 24, c1, "║            ║               ║                                                 ║");
        Screen.Printc(0, 25, c1, "║ MAR :      ║               ║                                                 ║");
        Screen.Printc(0, 26, c1, "║ MBR :      ║               ║                                                 ║");
        Screen.Printc(0, 27, c1, "║ MPC :      ║               ║                                                 ║");
        Screen.Printc(0, 28, c1, "║            ║               ║                                                 ║");
        Screen.Printc(0, 29, c1, "╠════════════╣               ║                                                 ║");
        Screen.Printc(0, 30, c1, "║ BREAKPOINT ║               ║                                                 ║");
        Screen.Printc(0, 31, c1, "╠════════════╣               ║                                                 ║");
        Screen.Printc(0, 32, c1, "║            ║               ║                                                 ║");
        Screen.Printc(0, 33, c1, "║ Num   Addr ║               ║                                                 ║");
        Screen.Printc(0, 34, c1, "║            ║               ║                                                 ║");
        Screen.Printc(0, 35, c1, "║ 01  :      ║               ║                                                 ║");
        Screen.Printc(0, 36, c1, "║ 02  :      ║               ║                                                 ║");
        Screen.Printc(0, 37, c1, "║ 03  :      ║               ║                                                 ║");
        Screen.Printc(0, 38, c1, "║ 04  :      ║               ║                                                 ║");
        Screen.Printc(0, 39, c1, "║ 05  :      ║               ║                                                 ║");
        Screen.Printc(0, 40, c1, "║ 06  :      ║               ║                                                 ║");
        Screen.Printc(0, 41, c1, "║ 07  :      ║               ║                                                 ║");
        Screen.Printc(0, 42, c1, "║ 08  :      ║               ║                                                 ║");
        Screen.Printc(0, 43, c1, "║ 09  :      ║               ║                                                 ║");
        Screen.Printc(0, 44, c1, "║ 10  :      ║    ║    ║     ║                                                 ║");
        Screen.Printc(0, 45, c1, "╠════════════╣╚═══╩════╩════╝║                                                 ║");
        Screen.Printc(0, 46, c1, "║REFRESH:    ║ ADD      INST ║                                                 ║");
        Screen.Printc(0, 47, c1, "╠════════════╩═══════════════╩════════════════════════╦════════════════════════╣");
        Screen.Printc(0, 48, c1, "║Alt+I=DEC/HEX  Alt+R=SetRefresh  Alt+J=Jump to       ║Printer:                ║");
        Screen.Printc(0, 49, c1, "╚═════════════════════════════════════════════════════╩════════════════════════╝");
        int[] ups = [31, 33, 35, 37, 38, 39, 40, 41, 42, 45, 49, 53, 59];
        foreach (int ux in ups) Screen.Printc(ux, 7, c1, "↑");
        Screen.Printc(71, 1, Vga.COLOR6 << 8, "A");
    }

    void DrawMainMenu()
    {
        int x0 = 31, y0 = 24, x1 = 52, y1 = 44, coordx = 33, ext = 27;
        Screen.Win(x0, y0, x1, y1, Vga.COLOR13, " MAIN MENU ");
        for (int i = 0; i < MainItems.Length; i++)
            Screen.Printc(coordx, ext + i * 2, Vga.COLOR12 << 8, MainItems[i]);
        Screen.Printc(coordx, ext + _menuPos * 2, Vga.COLOR7 << 8, MainItems[_menuPos]);
    }

    void DrawRunningMenu()
    {
        Screen.ColWin(30, 22, 78, 46, '░', Vga.COLOR17 << 8);
        int x0 = 31, y0 = 25, x1 = 51, y1 = 44, cx = 33, cy = 27;
        Screen.Win(x0, y0, x1, y1, Vga.COLOR13, " RUNNING MENU ");
        string[] items =
        [
            "SPC STOP", "F1  RUN", "F2  MICRO", "F3  MACRO", "F4  MODIFY REG",
            "F5  INS BREAKP.", "F6  DEL BREAKP.", "", "F7  TEXTDISPLAY",
            "F8  NO REFRESH", "F9  MEMDISPLAY", "F10 MEMORY/STACK", "",
            "PGUP/PGDWN SCROLL", "", "ESC PREVIOUS",
        ];
        foreach (var it in items) Screen.Printc(cx, cy++, Vga.COLOR12 << 8, it);
    }

    // VISUAL.C Visual()
    void Visual()
    {
        if (Video == NODISPLAY) { Screen.Printh(8, 6, Machine.Reg[0]); return; }
        if (Video == MEMDISPLAY) return;
        int y = 6;
        if (_hex == 0)
        {
            for (int i = 0; i < 3; i++) { Screen.Printclear(9, y + i); Screen.Printc(5, y + i, Vga.COLOR0 << 8, " "); }
            for (int i = 0; i < 16; i++) Screen.Printh(7, y++, Machine.Reg[i]);
            Screen.Printc(19, 46, Vga.COLOR1 << 8, $"{_mov & 0xFFF:X4}");
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                Screen.Printc(5, y, Vga.COLOR6 << 8, "D");
                Screen.Printc(7, y++, Vga.COLOR0 << 8, $"{Machine.Reg[i],6}");
            }
            for (int i = 3; i < 16; i++) Screen.Printh(7, y++, Machine.Reg[i]);
            Screen.Printc(19, 46, Vga.COLOR1 << 8, $"{_mov,4}");
        }
        Screen.Printh(8, 25, Machine.Mx.Mar);
        Screen.Printh(8, 26, Machine.Mx.Mbr);
        Screen.Printh(8, 27, Machine.Mpc.Value);
        Screen.Printb(30, 5, Machine.CStore.Read(Machine.Mpc.Value));
        Machine.Keyb.Read(0, out short b0); Screen.Printh(41, 19, b0);
        Machine.Keyb.Read(1, out short b1); Screen.Printh(41, 20, b1);

        int sp = Machine.Reg[2];
        if (_memstack == STACK)
        {
            for (int i = _mov; i > _mov - 40; i--)
            {
                int row = 45 - (_mov + 1 - i);
                if (sp > i) { Screen.Printc(15, row, Vga.COLOR0 << 8, "   "); Screen.Printclear(19, row); }
                else
                {
                    Machine.Mem.Read(i, out short t);
                    Screen.Printc(15, row, Vga.COLOR8 << 8, $"{i & 0xFFF:X3}");
                    Screen.Printh(19, row, t);
                }
            }
        }
        else
        {
            for (int i = _mov; i > _mov - 40; i--)
            {
                int row = 45 - (_mov + 1 - i);
                Screen.Printc(28, row, Vga.COLOR4 << 8, i == Machine.Reg[0] ? "►" : " ");
            }
            for (int i = _mov; i > _mov - 40; i--)
            {
                int row = 45 - (_mov + 1 - i);
                if (i >= MacConstants.NumCtrl * 2 && i <= 4095)
                {
                    Machine.Mem.Read(i, out short t);
                    Screen.Printc(15, row, Vga.COLOR8 << 8, $"{i & 0xFFF:X3}");
                    Screen.Printh(19, row, t);
                    bool isBp = IsBp(i);
                    Screen.Printc(24, row, (isBp ? Vga.COLOR9 : Vga.COLOR6) << 8, Disassembler.Disassemble(t));
                }
                else if (i >= 0 && i < MacConstants.NumCtrl * 2)
                {
                    Screen.Printc(15, row, Vga.COLOR8 << 8, $"{i & 0xFFF:X3}");
                    Screen.Printc(19, row, Vga.COLOR1 << 8, "----");
                    Screen.Printc(24, row, Vga.COLOR6 << 8, "I/O ");
                }
                else
                {
                    Screen.Printc(15, row, Vga.COLOR0 << 8, "   ");
                    Screen.Printc(19, row, Vga.COLOR1 << 8, "    ");
                    Screen.Printc(24, row, Vga.COLOR6 << 8, "    ");
                }
            }
        }
        Screen.Printc(63, 48, Vga.COLOR1 << 8, $"{Machine.Printer.Text,-16}");
    }

    // MENU.C Visualizza_Bp
    void VisualizzaBp()
    {
        int col = 8, row = 35, i = 0;
        while (i < _bp.Length && _bp[i] != MacConstants.NilAddr)
        {
            Screen.Printc(col, row, Vga.COLOR0 << 8, $"{_bp[i] & 0xFFF:X3}");
            i++; row++;
        }
        for (int k = i; k < _bp.Length; k++, row++) Screen.Printclear(col, row);
    }
}
