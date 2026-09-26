// MENU.C/VISUAL.C dialog ports: Scanf + Win-based overlays as non-blocking state machines
using MacEmu.Core;

namespace MacEmu.UI;

interface IDialog
{
    bool Finished { get; }
    void Draw();
    void OnKeyDown(KeyEventArgs e);
    void OnChar(char c);
}

public enum ScanType { Hex, Str, Int }

// VISUAL.C Scanf()
sealed class ScanInput
{
    readonly EmuForm _f;
    public int X, Y, MaxLen;
    public ScanType Type;
    public string Buf = "";
    public bool Done;
    public Action? OnDone;

    public ScanInput(EmuForm f, int x, int y, ScanType t, int maxLen, Action? onDone = null)
    { _f = f; X = x; Y = y; Type = t; MaxLen = maxLen; OnDone = onDone; }

    public void Draw()
    {
        for (int k = 0; k < MaxLen; k++) _f.Screen.Printc(X + k, Y, Vga.COLOR4 << 8, " ");
        for (int k = 0; k < Buf.Length && k < MaxLen; k++)
            _f.Screen.Printc(X + k, Y, Vga.COLOR4 << 8, Buf[k].ToString());
        _f.Screen.RefreshDirty();
    }

    public void OnChar(char raw)
    {
        char c = char.ToUpperInvariant(raw);
        bool ok = Type switch
        {
            ScanType.Hex => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'),
            ScanType.Str => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || c == ':' || c == '\\' || c == '.',
            _ => c >= '0' && c <= '9',
        };
        if (ok && Buf.Length < MaxLen) { Buf += c; Draw(); }
    }

    public void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Back && Buf.Length > 0)
        { Buf = Buf[..^1]; Draw(); e.Handled = true; }
        else if (e.KeyCode == Keys.Enter)
        { Done = true; OnDone?.Invoke(); e.Handled = true; }
    }

    public uint HexVal() => Buf.Length == 0 ? 0 : Convert.ToUInt32(Buf, 16);
    public int IntVal() => Buf.Length == 0 ? 0 : int.Parse(Buf);
}

abstract class DlgBase : IDialog
{
    protected readonly EmuForm F;
    public bool Finished { get; protected set; }
    protected DlgBase(EmuForm f) => F = f;
    public abstract void Draw();
    public virtual void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { Finished = true; e.Handled = true; }
    }
    public virtual void OnChar(char c) { }
    protected void Key(int x, int y, string t, int h) => F.Screen.Key(x, y, t, h);
}

sealed class MessageDlg : DlgBase
{
    readonly string _title, _text;
    public MessageDlg(EmuForm f, string title, string text) : base(f) { _title = title; _text = text; }
    public override void Draw()
    {
        F.Screen.Win(55, 38, 76, 44, Vga.COLOR13, _title);
        Key(56, 40, _text, 0);
        Key(64, 42, " Esc ", 1);
        F.Screen.RefreshDirty();
    }
    public override void OnChar(char c)
    {
        if (char.ToUpperInvariant(c) == 'E') Finished = true;
    }
    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { Finished = true; e.Handled = true; }
    }
}

sealed class AboutDlg : DlgBase
{
    public AboutDlg(EmuForm f) : base(f) { }
    public override void Draw()
    {
        var s = F.Screen;
        s.Win(34, 23, 73, 45, Vga.COLOR13, " ABOUT... ");
        s.Printc(42, 25, Vga.COLOR12 << 8, " MACEMU 4.0c (.NET)");
        s.Printc(46, 27, Vga.COLOR12 << 8, "Copyright(c) 1996");
        s.Printc(52, 29, Vga.COLOR12 << 8, "by");
        s.Printc(37, 31, Vga.COLOR12 << 8, "F.Ferrara  G.Baragiotta  A.Carrera");
        s.Printc(48, 32, Vga.COLOR12 << 8, "group ARCHA8");
        s.Printc(44, 33, Vga.COLOR12 << 8, "University of Turin");
        s.Printc(40, 35, Vga.COLOR12 << 8, "Computer Science Department");
        s.Printc(38, 37, Vga.COLOR12 << 8, "E-MAIL:");
        s.Printc(37, 39, Vga.COLOR12 << 8, "ferrara.francesco@educ.di.unito.it");
        s.Printc(37, 40, Vga.COLOR12 << 8, "baragiogiotta.luca@educ.di.unito.it");
        s.Printc(37, 41, Vga.COLOR12 << 8, "carreraa.alberto@educ.di.unito.it");
        Key(51, 43, " Esc ", 1);
        s.RefreshDirty();
    }
    public override void OnChar(char c)
    {
        if (char.ToUpperInvariant(c) == 'E') Finished = true;
    }
}

sealed class RatesDlg : DlgBase
{
    ScanInput? _in;
    public RatesDlg(EmuForm f) : base(f) { }
    public override void Draw()
    {
        F.Screen.Win(54, 25, 69, 34, Vga.COLOR13, " SET RATES ");
        F.Screen.Printc(56, 27, Vga.COLOR13 << 8, "New Rates:");
        _in = new ScanInput(F, 56, 29, ScanType.Int, 4, () =>
        {
            int r = _in!.IntVal();
            if (r != 0) { F.Rates = r; Finished = true; }
            else { _in.Buf = ""; _in.Draw(); }
        });
        _in.Draw();
    }
    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { Finished = true; e.Handled = true; return; }
        _in?.OnKeyDown(e);
    }
    public override void OnChar(char c) => _in?.OnChar(c);
}

sealed class JumpDlg : DlgBase
{
    ScanInput? _in;
    public JumpDlg(EmuForm f) : base(f) { }
    public override void Draw()
    {
        F.Screen.Win(54, 25, 69, 34, Vga.COLOR13, " SET ADDRESS ");
        F.Screen.Printc(56, 27, Vga.COLOR13 << 8, "Jump to:");
        _in = new ScanInput(F, 56, 29, ScanType.Hex, 3, () =>
        {
            int a = (int)_in!.HexVal();
            if (a == 0xFFF) { _in.Buf = ""; _in.Draw(); return; }
            if (a >= 0 && a <= 4095) F.Mov = a;
            Finished = true;
        });
        _in.Draw();
    }
    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { Finished = true; e.Handled = true; return; }
        _in?.OnKeyDown(e);
    }
    public override void OnChar(char c) => _in?.OnChar(c);
}

sealed class ModMemDlg : DlgBase
{
    ScanInput? _in;
    int _addr;
    public ModMemDlg(EmuForm f) : base(f) { }
    public override void Draw()
    {
        F.Screen.Win(54, 25, 77, 36, Vga.COLOR13, " MODIFY MEMORY ");
        F.Screen.Printc(56, 27, Vga.COLOR12 << 8, "Address:");
        AskAddr();
    }
    void AskAddr()
    {
        _in = new ScanInput(F, 56, 29, ScanType.Hex, 3, () =>
        {
            _addr = (int)_in!.HexVal();
            if (_addr == 0xFFF) { _in.Buf = ""; _in.Draw(); return; }
            F.Screen.Printc(56, 31, Vga.COLOR12 << 8, "New value:");
            _in = new ScanInput(F, 56, 33, ScanType.Hex, 4,
                () => { F.Machine.Mem.Write(_addr, unchecked((short)_in!.HexVal())); Finished = true; });
            _in.Draw();
        });
        _in.Draw();
    }
    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { Finished = true; e.Handled = true; return; }
        _in?.OnKeyDown(e);
    }
    public override void OnChar(char c) => _in?.OnChar(c);
}

sealed class ModCStoreDlg : DlgBase
{
    ScanInput? _in;
    int _addr;
    public ModCStoreDlg(EmuForm f) : base(f) { }
    public override void Draw()
    {
        F.Screen.Win(54, 25, 77, 36, Vga.COLOR13, " MODIFY CTRL STORE ");
        F.Screen.Printc(56, 27, Vga.COLOR12 << 8, "Address:");
        _in = new ScanInput(F, 56, 29, ScanType.Hex, 2, () =>
        {
            _addr = (int)_in!.HexVal();
            F.Screen.Printc(56, 31, Vga.COLOR12 << 8, "New Value:");
            _in = new ScanInput(F, 56, 33, ScanType.Hex, 8,
                () => { F.Machine.CStore.Write(_in!.HexVal(), (byte)_addr); Finished = true; });
            _in.Draw();
        });
        _in.Draw();
    }
    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { Finished = true; e.Handled = true; return; }
        _in?.OnKeyDown(e);
    }
    public override void OnChar(char c) => _in?.OnChar(c);
}

sealed class ModRegDlg : DlgBase
{
    static readonly string[] Names =
        ["PC     : ", "AC     : ", "SP     : ", "IR     : ", "TIR    : ", "ZERO   : ",
         "UNO    : ", "MENOUNO: ", "AMASK  : ", "SMASK  : ", "A      : ", "B      : ",
         "C      : ", "D      : ", "E      : ", "F      : "];
    int _pos;
    ScanInput? _in;
    const int X0 = 54, Y0 = 25, CX = 56;
    public ModRegDlg(EmuForm f) : base(f) { }
    public override void Draw()
    {
        F.Screen.Win(X0, Y0, X0 + 20, Y0 + 19, Vga.COLOR13, " MODIFY REGISTER ");
        for (int i = 0; i < 16; i++)
        {
            F.Screen.Printc(CX, Y0 + 2 + i, Vga.COLOR12 << 8, Names[i]);
            F.Screen.Printh(CX + 11, Y0 + 2 + i, F.Machine.Reg[i]);
        }
        Hi();
        F.Screen.RefreshDirty();
    }
    void Hi() => F.Screen.Printc(CX, Y0 + 2 + _pos, Vga.COLOR7 << 8, Names[_pos]);
    void Lo() => F.Screen.Printc(CX, Y0 + 2 + _pos, Vga.COLOR12 << 8, Names[_pos]);
    public override void OnKeyDown(KeyEventArgs e)
    {
        if (_in != null)
        {
            if (e.KeyCode == Keys.Escape)
            {
                // cancel current field edit, redraw list
                _in = null;
                Draw();
                e.Handled = true;
                return;
            }
            _in.OnKeyDown(e);
            return;
        }
        if (e.KeyCode == Keys.Escape) { Finished = true; e.Handled = true; }
        else if (e.KeyCode == Keys.Up) { Lo(); _pos = (_pos + 15) % 16; Hi(); F.Screen.RefreshDirty(); e.Handled = true; }
        else if (e.KeyCode == Keys.Down) { Lo(); _pos = (_pos + 1) % 16; Hi(); F.Screen.RefreshDirty(); e.Handled = true; }
        else if (e.KeyCode == Keys.Enter)
        {
            int row = Y0 + 2 + _pos, p = _pos;
            _in = new ScanInput(F, CX + 11, row, ScanType.Hex, 4, () =>
            {
                F.Machine.Reg[p] = unchecked((short)_in!.HexVal());
                F.Screen.Printh(CX + 11, row, F.Machine.Reg[p]);
                _in = null;
                F.Screen.RefreshDirty();
            });
            _in.Draw();
            e.Handled = true;
        }
    }
    public override void OnChar(char c) => _in?.OnChar(c);
}

sealed class InsBpDlg : DlgBase
{
    ScanInput? _in;
    int _stage; // 0 input addr, 1 Ins/Esc choice
    public InsBpDlg(EmuForm f) : base(f) { }
    public override void Draw()
    {
        F.Screen.Win(54, 25, 69, 34, Vga.COLOR13, " INSERT ");
        if (F.BpCount() >= MacConstants.MaxBp)
        {
            F.Screen.Printc(56, 29, Vga.COLOR6 << 8, "Memory full");
            F.Screen.Printc(56, 30, Vga.COLOR6 << 8, "for new ins");
            Key(59, 32, " Esc ", 1);
            _stage = 2;
            F.Screen.RefreshDirty();
            return;
        }
        F.Screen.Printc(56, 27, Vga.COLOR12 << 8, "Address:");
        _in = new ScanInput(F, 64, 27, ScanType.Hex, 3, () =>
        {
            int a = (int)_in!.HexVal();
            if (F.IsBp(a))
            {
                F.Screen.Printc(56, 29, Vga.COLOR6 << 8, "Already Ins");
                _in.Buf = ""; _in.Draw();
                return;
            }
            F.AddBp(a);
            _stage = 1;
            Key(56, 31, "Ins", 0);
            Key(62, 31, "Esc", 0);
            F.Screen.RefreshDirty();
        });
        _in.Draw();
    }
    public override void OnKeyDown(KeyEventArgs e)
    {
        if (_stage == 0 && _in != null)
        {
            if (e.KeyCode == Keys.Escape) { Finished = true; e.Handled = true; return; }
            _in.OnKeyDown(e);
            return;
        }
        if ((_stage == 1 || _stage == 2) && e.KeyCode == Keys.Escape) { Finished = true; e.Handled = true; }
        else if (_stage == 2 && e.KeyCode == Keys.Enter) { Finished = true; e.Handled = true; }
    }
    public override void OnChar(char c)
    {
        if (_stage == 0) { _in?.OnChar(c); return; }
        c = char.ToUpperInvariant(c);
        if (_stage == 1 && c == 'I') { _stage = 0; Draw(); }
        else if (c == 'E') Finished = true;
    }
}

sealed class DelBpDlg : DlgBase
{
    ScanInput? _in;
    int _stage; // 0 input num, 1 Del/Esc, 2 empty/err Esc
    public DelBpDlg(EmuForm f) : base(f) { }
    public override void Draw()
    {
        F.Screen.Win(54, 25, 69, 34, Vga.COLOR13, " DELETE ");
        if (F.BpCount() == 0)
        {
            F.Screen.Printc(56, 29, Vga.COLOR6 << 8, "Memory empty");
            F.Screen.Printc(56, 30, Vga.COLOR6 << 8, "for new del!");
            Key(58, 32, " Esc ", 1);
            _stage = 2;
            F.Screen.RefreshDirty();
            return;
        }
        F.Screen.Printc(56, 27, Vga.COLOR12 << 8, "Number :");
        _in = new ScanInput(F, 64, 27, ScanType.Int, 2, () =>
        {
            int n = _in!.IntVal();
            if (n < 1 || n > F.BpCount()) { Err("inexistent!"); return; }
            F.DelBp(n - 1);
            if (F.BpCount() > 0)
            {
                _stage = 1;
                Key(56, 31, "Del", 0);
                Key(62, 31, "Esc", 0);
            }
            else { Key(59, 32, " Esc ", 1); _stage = 2; }
            F.Screen.RefreshDirty();
        });
        _in.Draw();
    }
    void Err(string what)
    {
        F.Screen.Printc(56, 29, Vga.COLOR6 << 8, "Breakpoints");
        F.Screen.Printc(56, 30, Vga.COLOR6 << 8, what);
        Key(58, 32, " Esc ", 1);
        _stage = 2;
        F.Screen.RefreshDirty();
    }
    public override void OnKeyDown(KeyEventArgs e)
    {
        if (_stage == 0 && _in != null)
        {
            if (e.KeyCode == Keys.Escape) { Finished = true; e.Handled = true; return; }
            _in.OnKeyDown(e);
            return;
        }
        if (_stage == 2 && e.KeyCode is Keys.Escape or Keys.Enter) { Finished = true; e.Handled = true; }
    }
    public override void OnChar(char c)
    {
        if (_stage == 0) { _in?.OnChar(c); return; }
        c = char.ToUpperInvariant(c);
        if (_stage == 1 && c == 'D') { _stage = 0; Draw(); }
        else if (c == 'E') Finished = true;
    }
}

sealed class LoadProgDlg : DlgBase
{
    ScanInput? _in;
    int _stage; // 0 name, 1 Cancel/Retry, 2 loaded-Esc
    bool _ok;
    public bool LoadedOk => _ok;
    public LoadProgDlg(EmuForm f) : base(f) { }
    public override void Draw()
    {
        F.Screen.Win(54, 23, 77, 34, Vga.COLOR13, " LOAD PROGRAM ");
        F.Screen.Printc(56, 26, Vga.COLOR12 << 8, "File name:");
        if (F.RestartFlag && F.LastProg.Length > 0)
        {
            F.Screen.Printc(56, 28, Vga.COLOR4 << 8, F.LastProg);
            TryLoad(F.LastProg);
            return;
        }
        _in = new ScanInput(F, 56, 28, ScanType.Str, 20,
            () => TryLoad(Path.Combine("MACRO", _in!.Buf)));
        _in.Draw();
    }
    void TryLoad(string rel)
    {
        string path = Path.Combine(F.BaseDir, rel);
        if (!File.Exists(path))
        {
            F.Screen.Printc(56, 30, Vga.COLOR14 << 8, "File not found!");
            Key(56, 32, " Cancel ", 1);
            Key(68, 32, " Retry ", 1);
            _stage = 1;
            F.Screen.RefreshDirty();
            return;
        }
        _ok = true;
        F.NeverLoad = false;
        F.LastProg = rel;
        var lines = File.ReadAllLines(path);
        var (ilc, n) = MacLoader.LoadInto(F.Machine, lines);
        if (F.MemStackIsMemory) F.Mov = ilc + 39;
        F.Screen.Win(54, 37, 77, 45, Vga.COLOR13, " LOAD ");
        F.Screen.Printc(56, 39, Vga.COLOR12 << 8, "Loading Mac-1...");
        F.Screen.Printc(56, 41, Vga.COLOR12 << 8, $"Loaded {n}");
        F.Screen.Printc(56, 42, Vga.COLOR12 << 8, "instructions");
        Key(62, 32, " Esc ", 1);
        _stage = 2;
        F.Screen.RefreshDirty();
    }
    public override void OnKeyDown(KeyEventArgs e)
    {
        if (_stage == 0 && _in != null)
        {
            if (e.KeyCode == Keys.Escape) { Finished = true; e.Handled = true; return; }
            _in.OnKeyDown(e);
            return;
        }
        if (e.KeyCode is Keys.Escape or Keys.Enter) { Finished = true; e.Handled = true; }
    }
    public override void OnChar(char c)
    {
        if (_stage == 0) { _in?.OnChar(c); return; }
        c = char.ToUpperInvariant(c);
        if (_stage == 1 && c == 'C') Finished = true;
        else if (_stage == 1 && c == 'R') { _stage = 0; Draw(); }
        else if (_stage == 2 && c == 'E') Finished = true;
    }
}

sealed class LoadCStoreDlg : DlgBase
{
    ScanInput? _in;
    int _stage; // 0 name, 1 Cancel/Retry, 2 loaded
    bool _first = true;
    public LoadCStoreDlg(EmuForm f) : base(f) { }
    public override void Draw()
    {
        F.Screen.Win(54, 23, 77, 34, Vga.COLOR13, " LOAD CSTORE ");
        F.Screen.Printc(56, 26, Vga.COLOR12 << 8, "File name:");
        F.Screen.Printc(56, 28, Vga.COLOR4 << 8, "CSTORE.MIC          ");
        _first = true;
        F.Screen.RefreshDirty();
    }
    public override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape && _stage != 1 && _stage != 2) { Finished = true; e.Handled = true; return; }
        if (_first && e.KeyCode == Keys.Enter) { _first = false; TryLoad(Path.Combine("MICRO", "CSTORE.MIC")); e.Handled = true; }
        else if ((_stage == 1 || _stage == 2) && e.KeyCode == Keys.Escape) { Finished = true; e.Handled = true; }
        else if (_stage == 2 && e.KeyCode == Keys.Enter) { Finished = true; e.Handled = true; }
        else if (_in != null) _in.OnKeyDown(e);
    }
    public override void OnChar(char c)
    {
        if (_first)
        {
            _first = false;
            _in = new ScanInput(F, 56, 28, ScanType.Str, 20,
                () => TryLoad(Path.Combine("MICRO", _in!.Buf)));
            _in.OnChar(c);
            return;
        }
        if (_stage == 0) { _in?.OnChar(c); return; }
        c = char.ToUpperInvariant(c);
        if (_stage == 1 && c == 'C') Finished = true;
        else if (_stage == 1 && c == 'R') { _stage = 0; Draw(); }
        else if (_stage == 2 && c == 'E') Finished = true;
    }
    void TryLoad(string rel)
    {
        string path = Path.Combine(F.BaseDir, rel);
        if (!File.Exists(path))
        {
            F.Screen.Printc(56, 30, Vga.COLOR6 << 8, "File not found!");
            Key(56, 32, " Cancel ", 1);
            Key(68, 32, " Retry ", 1);
            _stage = 1;
            F.Screen.RefreshDirty();
            return;
        }
        int n = MicLoader.LoadFile(F.Machine, path);
        F.Screen.Win(54, 37, 77, 45, Vga.COLOR13, " LOAD ");
        F.Screen.Printc(56, 39, Vga.COLOR12 << 8, "Loading Ctrl Store...");
        F.Screen.Printc(56, 41, Vga.COLOR12 << 8, $"Loaded {n}");
        F.Screen.Printc(56, 42, Vga.COLOR12 << 8, "microwords");
        Key(62, 31, " Esc ", 1);
        _stage = 2;
        F.Screen.RefreshDirty();
    }
}
