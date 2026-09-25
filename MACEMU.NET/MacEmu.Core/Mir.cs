// MIR.H/C port - decode order MUST match MIR.C exactly
namespace MacEmu.Core;

public enum MirField { ADDR, A, B, C, ENC, WR, RD, MAR, MBR, SH, ALU, COND, AMUX }

public sealed class Mir
{
    private readonly uint[] _f = new uint[13];
    public uint this[MirField f] => _f[(int)f];

    public void Load(uint micro)
    {
        uint t = micro;
        _f[0] = t & 0xff; t >>= 8;
        _f[1] = t & 0xf; t >>= 4;
        _f[2] = t & 0xf; t >>= 4;
        _f[3] = t & 0xf; t >>= 4;
        _f[4] = t & 0x1; t >>= 1;
        _f[5] = t & 0x1; t >>= 1;
        _f[6] = t & 0x1; t >>= 1;
        _f[7] = t & 0x1; t >>= 1;
        _f[8] = t & 0x1; t >>= 1;
        _f[9] = t & 0x3; t >>= 2;
        _f[10] = t & 0x3; t >>= 2;
        _f[11] = t & 0x3; t >>= 2;
        _f[12] = t & 0x1;
    }

    public static uint Encode(byte addr, int a, int b, int c, int enc, int wr, int rd,
        int mar, int mbr, int sh, int alu, int cond, int amux)
    {
        uint w = addr;
        w |= (uint)(a & 0xf) << 8;
        w |= (uint)(b & 0xf) << 12;
        w |= (uint)(c & 0xf) << 16;
        w |= (uint)(enc & 1) << 20;
        w |= (uint)(wr & 1) << 21;
        w |= (uint)(rd & 1) << 22;
        w |= (uint)(mar & 1) << 23;
        w |= (uint)(mbr & 1) << 24;
        w |= (uint)(sh & 3) << 25;
        w |= (uint)(alu & 3) << 27;
        w |= (uint)(cond & 3) << 29;
        w |= (uint)(amux & 1) << 31;
        return w;
    }
}
