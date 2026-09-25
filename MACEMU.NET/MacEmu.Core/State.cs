namespace MacEmu.Core;

// REGISTRI.C, LATCH.C, CBUS.C, MPC.C, CSTORE.C, MXR.C ports
public sealed class Registers
{
    private readonly short[] _r = new short[MacConstants.NumRegs];
    public short this[int i] { get => _r[i]; set => _r[i] = value; }
    public short[] Snapshot() => (short[])_r.Clone();
}

public sealed class Latches { public short A; public short B; }
public sealed class CBus { public short Value; }
public sealed class Mpc { public byte Value; }
public sealed class MarMbr { public ushort Mar; public short Mbr; }

public sealed class ControlStore
{
    private readonly uint[] _cs = new uint[MacConstants.CStoreSize];
    public void Write(uint micro, byte addr) => _cs[addr] = micro;
    public uint Read(byte mpc) => _cs[mpc];
    public uint[] Snapshot() => (uint[])_cs.Clone();
}

public sealed class Memory
{
    private readonly short[] _m = new short[MacConstants.MemSize];
    public short[] Raw => _m; // for MEMDISPLAY fast path (like ReadPmem)
    public bool Write(int addr, short v)
    {
        if (addr < MacConstants.NumCtrl * 2) return false;
        _m[addr] = v; return true;
    }
    public bool Read(int addr, out short v)
    {
        if (addr < MacConstants.NumCtrl * 2) { v = 0; return false; }
        v = _m[addr]; return true;
    }
    public short ReadRaw(int addr) => _m[addr];
    public void Clear() => Array.Clear(_m, 0, _m.Length);
}
