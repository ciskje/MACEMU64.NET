// DISMAC.C port
namespace MacEmu.Core;

public static class Disassembler
{
    public static string Disassemble(short code)
    {
        ushort u = unchecked((ushort)code);
        return u switch
        {
            0xf000 => "pshi",
            0xf200 => "popi",
            0xf400 => "push",
            0xf600 => " pop",
            0xf800 => "retn",
            0xfa00 => "swap",
            0xfd20 => " not",
            0xfd00 => "reti",
            0xfd40 => "eint",
            0xfd80 => "dint",
            0xffff => "halt",
            _ => (u & 0xf000) switch
            {
                0x0000 => "lodd",
                0x1000 => "stod",
                0x2000 => "addd",
                0x3000 => "subd",
                0x4000 => "jpos",
                0x5000 => "jzer",
                0x6000 => "jump",
                0x7000 => "loco",
                0x8000 => "lodl",
                0x9000 => "stol",
                0xa000 => "addl",
                0xb000 => "subl",
                0xc000 => "jneg",
                0xd000 => "jnze",
                0xe000 => "call",
                _ => (u & 0xff00) switch
                {
                    0xfb00 => "andl",
                    0xfc00 => "insp",
                    0xfe00 => "desp",
                    _ => "????",
                },
            },
        };
    }
}
