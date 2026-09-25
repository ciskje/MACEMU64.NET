// Loader .MAC (MENU.C Ask_Prog) and .MIC (Ask_Cstore) ports
namespace MacEmu.Core;

public static class MacLoader
{
    public static (int ilc, int count) LoadInto(Machine m, IEnumerable<string> lines)
    {
        using var e = lines.GetEnumerator();
        if (!e.MoveNext()) throw new InvalidDataException("empty .mac");
        int ilc = Convert.ToInt32(e.Current.Trim(), 16);
        m.Reg[MacConstants.R_PC] = (short)ilc;
        int j = ilc, n = 0;
        while (e.MoveNext() && j < 0xfff)
        {
            var s = e.Current.Trim();
            if (s.Length == 0) continue;
            short b = unchecked((short)Convert.ToInt32(s, 16));
            m.Mem.Write(j, b);
            j++; n++;
        }
        return (ilc, n);
    }

    public static (int ilc, int count) LoadFile(Machine m, string path)
        => LoadInto(m, File.ReadLines(path));

    public static (int ilc, int count) LoadOs(Machine m, string path)
    {
        // MENU.C LoadOS: mem[255]=ILC + program; fallback stub if missing
        if (!File.Exists(path))
        {
            // MENU.C LoadOS fallback stub (12/11/96) when MACDOS.MAC missing
            m.Reset();
            m.Mem.Write(255, 0xfc);
            m.Mem.Write(252, 0x7000);
            m.Mem.Write(253, 0x1005);
            m.Mem.Write(254, unchecked((short)0xfd00));
            return (0xfc, 0);
        }
        var lines = File.ReadLines(path).GetEnumerator();
        lines.MoveNext();
        int ilc = Convert.ToInt32(lines.Current.Trim(), 16);
        m.Reg[MacConstants.R_PC] = (short)ilc;
        m.Mem.Write(255, (short)ilc);
        int j = ilc;
        while (lines.MoveNext())
        {
            var s = lines.Current.Trim();
            if (s.Length == 0) continue;
            m.Mem.Write(j, unchecked((short)Convert.ToInt32(s, 16)));
            j++;
        }
        return (ilc, j - ilc);
    }
}

public static class MicLoader
{
    public static int LoadInto(Machine m, IEnumerable<string> lines)
    {
        int j = 0;
        foreach (var raw in lines)
        {
            if (j >= 0xff) break;
            var buf = raw.Trim();
            if (buf.Length < 32) continue;
            uint a = 0;
            for (int i = 0; i < 32; i++)
                if (buf[i] == '1') a |= 1u << (31 - i);
            m.CStore.Write(a, (byte)j);
            j++;
        }
        return j;
    }
    public static int LoadFile(Machine m, string path) => LoadInto(m, File.ReadLines(path));
    public static string ToMicLine(uint w) => Convert.ToString(w, 2).PadLeft(32, '0');
}
