// MASM.EXE port: macro assembler ASM -> MAC (two-pass, case-insensitive)
namespace MacEmu.Core.Asm;

public sealed class AssembleException : Exception
{
    public AssembleException(string msg) : base(msg) { }
}

public sealed class MacAssembler
{
    static readonly Dictionary<string, ushort> Base12 = new(StringComparer.OrdinalIgnoreCase)
    {
        ["lodd"] = 0x0000, ["stod"] = 0x1000, ["addd"] = 0x2000, ["subd"] = 0x3000,
        ["jpos"] = 0x4000, ["jzer"] = 0x5000, ["jump"] = 0x6000, ["loco"] = 0x7000,
        ["lodl"] = 0x8000, ["stol"] = 0x9000, ["addl"] = 0xa000, ["subl"] = 0xb000,
        ["jneg"] = 0xc000, ["jnze"] = 0xd000, ["call"] = 0xe000,
    };
    static readonly Dictionary<string, ushort> Base8 = new(StringComparer.OrdinalIgnoreCase)
    {
        ["andl"] = 0xfb00, ["insp"] = 0xfc00, ["desp"] = 0xfe00,
    };
    static readonly Dictionary<string, ushort> Fixed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pshi"] = 0xf000, ["popi"] = 0xf200, ["push"] = 0xf400, ["pop"] = 0xf600,
        ["retn"] = 0xf800, ["swap"] = 0xfa00, ["reti"] = 0xfd00, ["eint"] = 0xfd40,
        ["dint"] = 0xfd80, ["not"] = 0xfd20, ["halt"] = 0xffff,
    };

    sealed class Line
    {
        public string? EquName, EquRaw;
        public string? Label, Mnemonic, OperandRaw;
        public bool IsData; public string? DataRaw;
        public int Address;
    }

    public int Origin { get; }
    public MacAssembler(int origin = 0x100) => Origin = origin;

    public short[] Assemble(IEnumerable<string> rawLines) => AssembleText(rawLines).words;

    public (short[] words, int ilc, Dictionary<string, int> symbols) AssembleText(IEnumerable<string> rawLines)
    {
        var lines = Parse(rawLines);
        var sym = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var equRaw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int addr = Origin;
        foreach (var l in lines)
        {
            if (l.EquName != null) { equRaw[l.EquName] = l.EquRaw!; continue; }
            if (l.Label != null)
            {
                if (sym.ContainsKey(l.Label)) throw new AssembleException($"duplicate label {l.Label}");
                sym[l.Label] = addr;
            }
            if (l.Mnemonic != null || l.IsData) { l.Address = addr; addr++; }
        }
        // resolve equates (numbers or symbols, iterative)
        var equVal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int pass = 0; pass < 10; pass++)
        {
            bool progress = false;
            foreach (var kv in equRaw)
            {
                if (equVal.ContainsKey(kv.Key)) continue;
                if (TryNumber(kv.Value, out int n)) { equVal[kv.Key] = n; progress = true; }
                else if (sym.TryGetValue(kv.Value.Trim(), out n) || equVal.TryGetValue(kv.Value.Trim(), out n))
                { equVal[kv.Key] = n; progress = true; }
            }
            if (!progress) break;
        }
        foreach (var kv in equRaw)
            if (!equVal.ContainsKey(kv.Key))
                throw new AssembleException($"cannot resolve equate {kv.Key}={kv.Value}");
        foreach (var kv in equVal) sym[kv.Key] = kv.Value;

        var out_ = new List<short>();
        foreach (var l in lines)
        {
            if (l.EquName != null) continue;
            if (l.Mnemonic == null && !l.IsData) continue; // label-only
            out_.Add(Encode(l, sym));
        }
        return (out_.ToArray(), Origin, sym);
    }

    short Encode(Line l, Dictionary<string, int> sym)
    {
        if (l.IsData) return unchecked((short)Resolve(l.DataRaw!, sym));
        string m = l.Mnemonic!;
        if (Fixed.TryGetValue(m, out ushort f))
        {
            if (l.OperandRaw != null) throw new AssembleException($"{m} takes no operand");
            return unchecked((short)f);
        }
        if (Base12.TryGetValue(m, out ushort b12))
        {
            if (l.OperandRaw == null) throw new AssembleException($"{m} needs operand");
            return unchecked((short)(b12 | (Resolve(l.OperandRaw, sym) & 0xfff)));
        }
        if (Base8.TryGetValue(m, out ushort b8))
        {
            if (l.OperandRaw == null) throw new AssembleException($"{m} needs operand");
            return unchecked((short)(b8 | (Resolve(l.OperandRaw, sym) & 0xff)));
        }
        throw new AssembleException($"unknown mnemonic {m}");
    }

    int Resolve(string raw, Dictionary<string, int> sym)
    {
        raw = raw.Trim();
        if (TryNumber(raw, out int n)) return n;
        if (sym.TryGetValue(raw, out n)) return n;
        throw new AssembleException($"unknown symbol {raw}");
    }

    static bool TryNumber(string s, out int v)
    {
        s = s.Trim();
        try
        {
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            { v = Convert.ToInt32(s[2..], 16); return true; }
            if (s.StartsWith("-0x", StringComparison.OrdinalIgnoreCase))
            { v = -Convert.ToInt32(s[3..], 16); return true; }
            if (int.TryParse(s, out v)) return true;
        }
        catch { }
        v = 0; return false;
    }

    static List<Line> Parse(IEnumerable<string> rawLines)
    {
        var list = new List<Line>();
        foreach (var raw in rawLines)
        {
            string s = raw;
            int ci = s.IndexOf('/'); // comment char (no division in this asm)
            if (ci >= 0) s = s[..ci];
            s = s.Trim();
            if (s.Length == 0) continue;
            var l = new Line();
            int eq = s.IndexOf('=');
            if (eq > 0 && !s.Contains(':'))
            {
                l.EquName = s[..eq].Trim();
                l.EquRaw = s[(eq + 1)..].Trim();
                list.Add(l); continue;
            }
            string rest = s;
            int col = s.IndexOf(':');
            if (col >= 0)
            {
                l.Label = s[..col].Trim();
                rest = s[(col + 1)..].Trim();
                if (rest.Length == 0) { list.Add(l); continue; }
            }
            var parts = rest.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) { list.Add(l); continue; }
            if (parts.Length == 1)
            {
                if (IsMnemonic(parts[0])) { l.Mnemonic = parts[0]; }
                else { l.IsData = true; l.DataRaw = parts[0]; }
            }
            else
            {
                l.Mnemonic = parts[0];
                l.OperandRaw = parts[1];
            }
            list.Add(l);
        }
        return list;
    }

    static bool IsMnemonic(string t) =>
        Base12.ContainsKey(t) || Base8.ContainsKey(t) || Fixed.ContainsKey(t);

    public static void WriteMacFile(string path, short[] words, int ilc)
    {
        using var w = new StreamWriter(path);
        w.WriteLine($"{ilc:x4}");
        foreach (var x in words) w.WriteLine($"{x & 0xffff:x4}");
    }
}
