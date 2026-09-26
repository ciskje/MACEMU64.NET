// MACEMU.C Principale()/Esecuzione() port: WinForms state machine driving the core
using MacEmu.Core;

namespace MacEmu.UI;

public partial class EmuForm : Form
{
    enum Mode { Splash, Main, Run }
    enum Overlay { None, Dialog }

    const int NODISPLAY = 0, TEXTDISPLAY = 1, MEMDISPLAY = 2;
    const int STACK = 0, MEMORY = 1;

    readonly Machine _m = new();
    readonly TextScreen _scr = new();
    readonly PictureBox _gfx = new();
    readonly System.Windows.Forms.Timer _tick = new();
    readonly string _baseDir;

    Mode _mode = Mode.Splash;
    bool _ending; // showing MACEND splash
    int _video = TEXTDISPLAY, _hex = 0, _mov = 4094, _memstack = STACK;
    readonly int[] _bp = new int[MacConstants.MaxBp];
    int _rates = 8;
    bool _cont; // RUN on/off
    bool _neverload = true, _restart;
    string _lastProg = "";
    int _menuPos;
    int _runx = 48, _superflag;
    bool _runRight = true;
    IDialog? _dlg;
    Bitmap? _memview;

    static readonly string[] MainItems =
    [
        " LOAD PROGRAM     ", " LOAD CTRL STORE  ", " MODIFY MEMORY    ",
        " MODIFY CTRL STORE", " RUNNING MENU     ", " RESTART          ",
        " RESET            ", " QUIT             ",
    ];

    public EmuForm()
    {
        _baseDir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < _bp.Length; i++) _bp[i] = MacConstants.NilAddr;
        KeyPreview = true;
        string exe = Environment.ProcessPath ?? Application.ExecutablePath;
        string stamp;
        try { stamp = File.GetLastWriteTime(exe).ToString("yyyyMMdd-HHmm"); }
        catch { stamp = "dev"; }
        Text = $"MACEMU 4.0b (.NET) [{stamp}]";
        ClientSize = new Size(1280, 800);
        StartPosition = FormStartPosition.CenterScreen;
        _scr.Dock = DockStyle.Fill;
        Controls.Add(_scr);
        _gfx.Dock = DockStyle.Fill;
        _gfx.SizeMode = PictureBoxSizeMode.CenterImage;
        _gfx.BackColor = Color.Black;
        _gfx.Visible = false;
        Controls.Add(_gfx);
        _tick.Interval = 30;
        _tick.Tick += OnTick;
        KeyDown += OnKeyDown;
        KeyPress += OnKeyPress;
        _m.Printer.OnBell = Bell;
        Shown += (_, _) => ShowSplash("MACSTART.BCF", false);
    }

    int _lastBell;
    // Bell stampante (PRINTER.C): suono di sistema, strozzato per non mitragliare durante RUN veloce
    void Bell()
    {
        int now = Environment.TickCount;
        if (now - _lastBell < 250) return;
        _lastBell = now;
        System.Media.SystemSounds.Beep.Play();
    }

    string Asset(string rel) => Path.Combine(_baseDir, rel);

    void ShowSplash(string bcf, bool ending)
    {
        _ending = ending;
        try
        {
            using var bmp = BcfLoader.Load(Asset(bcf));
            // Same x4 NearestNeighbor as MEMDISPLAY: 1280x800 centered.
            var big = new Bitmap(1280, 800);
            using (var g = Graphics.FromImage(big))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.DrawImage(bmp, 0, 0, 1280, 800);
            }
            _gfx.Image?.Dispose();
            _gfx.Image = big;
        }
        catch { _gfx.Image = null; }
        _gfx.Visible = true;
        _scr.Visible = false;
        _tick.Stop();
        _mode = Mode.Splash;
    }

    void EnterMain()
    {
        _gfx.Visible = false;
        _scr.Visible = true;
        _mode = Mode.Main;
        _menuPos = 0;
        DrawText();
        ResetMac();
        DrawMainMenu();
        _scr.RefreshDirty();
        _tick.Stop();
    }

    // MENU.C ResetMac
    void ResetMac()
    {
        _m.Reset();
        try { MacLoader.LoadOs(_m, Asset("MACDOS.MAC")); } catch { MacLoader.LoadOs(_m, "missing"); }
        try { MicLoader.LoadFile(_m, Asset(Path.Combine("MICRO", "CSTORE.MIC"))); } catch { }
        for (int i = 0; i < _bp.Length; i++) _bp[i] = MacConstants.NilAddr;
        _video = TEXTDISPLAY;
        Visual();
    }

    // ---- RUN engine (Esecuzione) ----
    void EnterRun()
    {
        _mode = Mode.Run;
        _cont = false;
        _scr.Visible = true;
        _gfx.Visible = false;
        DrawRunningMenu();
        Visual();
        _scr.RefreshDirty();
        _tick.Start();
    }

    void ExitRun()
    {
        _tick.Stop();
        _cont = false;
        _mode = Mode.Main;
        _scr.Visible = true;
        _gfx.Visible = false;
        DrawText();
        DrawMainMenu();
        Visual();
        VisualizzaBp();
        _scr.RefreshDirty();
    }

    void OnTick(object? s, EventArgs e)
    {
        if (_mode != Mode.Run || !_cont || _dlg != null) return;
        if (_video == MEMDISPLAY)
        {
            for (int i = 0; i < _rates * 1500 && !_m.Halt; i++) _m.StepMicro();
            CheckBpOrHalt();
            DrawMemDisplay();
            return;
        }
        int budget = _video == NODISPLAY ? 300_000 : _rates * 1500;
        for (int i = 0; i < budget && !_m.Halt && _cont; i++)
        {
            _m.StepMicro();
            if (_m.Mpc.Value == 0 && IsBp(_m.Reg[MacConstants.R_PC])) { _cont = false; break; }
        }
        if (_video == NODISPLAY) _scr.Printh(8, 6, _m.Reg[MacConstants.R_PC]);
        else { SuperCar(); Visual(); }
        if (_m.Halt)
        {
            _cont = false;
            Visual();
            _m.Halt = false;
            ShowMessage(" MESSAGE ", "PROGRAM TERMINATED");
        }
        _scr.RefreshDirty();
    }

    void CheckBpOrHalt()
    {
        if (_m.Halt)
        {
            _cont = false;
            _m.Halt = false;
            if (_video != MEMDISPLAY) { Visual(); ShowMessage(" MESSAGE ", "PROGRAM TERMINATED"); }
            else { _cont = false; }
        }
    }

    void StepMicroOnce() { _m.StepMicro(); if (_m.Halt) { _cont = false; Visual(); _m.Halt = false; ShowMessage(" MESSAGE ", "PROGRAM TERMINATED"); } else Visual(); }
    void StepMacroOnce() { _m.StepMacro(); if (_m.Halt) { _cont = false; Visual(); _m.Halt = false; ShowMessage(" MESSAGE ", "PROGRAM TERMINATED"); } else Visual(); }

    void SuperCar()
    {
        if (_superflag++ < 12) return;
        _superflag = 0;
        if (_runx >= 65) _runRight = false;
        if (_runx <= 46) _runRight = true;
        DrawTitleRow();
        _scr.Printc(_runx, 1, Vga.COLOR6 << 8, _runRight ? "████" : "████");
        _runx += _runRight ? 1 : -1;
    }
}
