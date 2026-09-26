// KEYB.C / TIMER.C / PRINTER.C ports
namespace MacEmu.Core;

public sealed class KeyboardDevice
{
    public const int IP = 1, IE = 2, RDY = 1 << 15;
    public short Br, Csr;
    public int Pending = -1; // car in C, -1 = none
    public void Load(short c) => Pending = c;
    public bool InterruptPending() => (Csr & IP) != 0;
    public void Reset()
    {
      // NOTE: original ResetKeyb() does NOT clear car (pending char);
      // a key pressed before RESET is still delivered. Replicated on purpose.
      Br = 0; Csr = 0;
    }
    public bool Write(int addr, short v)
    {
        if (addr == 0) { Br = v; return true; }
        if (addr == 1) { Csr = v; return true; }
        return false;
    }
    public bool Read(int addr, out short v)
    {
        if (addr == 0) { v = Br; return true; }
        if (addr == 1) { v = Csr; return true; }
        v = 0; return false;
    }
    public void Poll() // Keyb()
    {
        if (Pending != -1)
        {
            Csr = (short)(Csr | IP);
            if ((Csr & RDY) == 0)
            {
                Br = (short)Pending;
                Csr = (short)(Csr | RDY);
                Pending = -1;
            }
            else
            {
                Csr = (short)(Csr | (1 << 2)); // overrun
                Pending = -1;
            }
        }
    }
}

public sealed class TimerDevice
{
    public const int IP = 1;
    public short Period = 1000, Csr;
    private int _time;
    public bool InterruptPending() => (Csr & IP) != 0;
    public void Reset()
    {
      // NOTE: original ResetTimer() does NOT clear the static tick counter;
      // a pending tick phase survives RESET. Replicated here on purpose.
      Period = 1000; Csr = 0;
    }
    public bool Write(int addr, short v)
    {
        if (addr == 4) { Period = v; return true; }
        if (addr == 5) { Csr = v; return true; }
        return false;
    }
    public bool Read(int addr, out short v)
    {
        if (addr == 4) { v = Period; return true; }
        if (addr == 5) { v = Csr; return true; }
        v = 0; return false;
    }
    public void Poll() // Timer()
    {
        _time++;
        if (_time > Period) { _time = 0; Csr = (short)(Csr | IP); }
    }
}

public sealed class PrinterDevice
{
    private readonly short[] _buf = new short[160];
    private int _pos;
    // PRINTER.C WritePrinter: *a==7 suona il bell (Gotoxy + printf bell)
    public Action? OnBell;
    public void Reset() => _pos = 0;
    public string Text
    {
        get
        {
            var ch = new char[_pos];
            for (int i = 0; i < _pos; i++) ch[i] = (char)(_buf[i] & 0xff);
            return new string(ch);
        }
    }
    public bool Write(int addr, short v)
    {
        if (addr != 2) return false;
        if (v == 7) { OnBell?.Invoke(); }
        else
        {
            if (_pos >= 15) _pos = 15;
            _buf[_pos++] = v;
            if (v == 0) _pos = 0;
        }
        return true;
    }
}
