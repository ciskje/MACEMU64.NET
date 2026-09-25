// MICA/PREMIC port: micro assembler PRE -> MIC (validates against CSTORE.MIC)
namespace MacEmu.Core.Asm;

public sealed class MicroAssembler
{
    static readonly Dictionary<string, int> Reg = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pc"] = 0, ["ac"] = 1, ["sp"] = 2, ["ir"] = 3, ["tir"] = 4,
        ["zero"] = 5, ["one"] = 6, ["m1"] = 7, ["menouno"] = 7,
        ["amask"] = 8, ["smask"] = 9,
        ["a"] = 10, ["b"] = 11, ["c"] = 12, ["d"] = 13, ["e"] = 14, ["f"] = 15,
    };

    sealed class Stmt { public string Text = ""; }
    sealed class MLine { public string? Label; public List<string> Stmts = new(); }

    public uint[] Assemble(IEnumerable<string> raw)
    {
        var lines = Parse(raw);
        var labAddr = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < lines.Count; i++)
            if (lines[i].Label != null) labAddr[lines[i].Label!] = i;
        var out_ = new uint[lines.Count];
        for (int i = 0; i < lines.Count; i++)
            out_[i] = Encode(lines[i], labAddr);
        return out_;
    }

    static List<MLine> Parse(IEnumerable<string> raw)
    {
        var list = new List<MLine>();
        foreach (var r in raw)
        {
            string s = r;
            int bi = s.IndexOf('{'); // strip {comment}
            if (bi >= 0) s = s[..bi];
            s = s.Trim();
            if (s.Length == 0) continue;
            var ml = new MLine();
            // label "pX:" or ":"
            int col = s.IndexOf(':');
            if (col >= 0)
            {
                string lab = s[..col].Trim();
                // fix ":=" contains colon! "alu:=f" -> col at ":=".
                // Real label pattern: starts with letter and ":=" not present before col+1
                if (lab.Length > 0 && char.IsLetter(lab[0]) && (col + 1 >= s.Length || s[col + 1] != '='))
                {
                    ml.Label = lab == "" ? null : lab;
                    s = s[(col + 1)..];
                }
            }
            else if (s.StartsWith(":"))
                s = s[1..];
            s = s.Trim();
            if (s.StartsWith(":")) s = s[1..].Trim();
            foreach (var part in s.Split(';'))
            {
                var t = part.Trim();
                if (t.Length == 0) continue;
                ml.Stmts.Add(t);
            }
            list.Add(ml);
        }
        return list;
    }

    uint Encode(MLine ml, Dictionary<string, int> lab)
    {
        int A = 0, B = 0, C = 0, ENC = 0, WR = 0, RD = 0, MAR = 0, MBR = 0;
        int SH = 0, ALU = 2, COND = 0, AMUX = 0, ADDR = 0;
        bool hasMar = false, hasMbr = false;
        int marReg = 0, mbrReg = 0; bool mbrIsMbr = false;

        foreach (var st0 in ml.Stmts)
        {
            string st = st0.Trim();
            string low = st.ToLowerInvariant();
            if (low == "halt") { SH = 3; continue; }
            if (low == "rd") { RD = 1; continue; }
            if (low == "wr") { WR = 1; continue; }
            if (low.StartsWith("goto "))
            {
                COND = 3; ADDR = ResolveLab(st[5..].Trim(), lab); continue;
            }
            if (low.StartsWith("if "))
            {
                // "if n then goto X" / "if z then goto X"
                int g = low.IndexOf("goto ");
                string cnd = low[3..g].Replace("then", "").Trim();
                COND = cnd == "n" ? 1 : cnd == "z" ? 2 : throw new AssembleException($"bad cond {st}");
                ADDR = ResolveLab(st[(g + 5)..].Trim(), lab);
                continue;
            }
            int asg = st.IndexOf(":=");
            if (asg < 0) throw new AssembleException($"bad stmt {st}");
            string dst = st[..asg].Trim().ToLowerInvariant();
            string expr = st[(asg + 2)..].Trim();
            if (dst == "mar") { MAR = 1; hasMar = true; marReg = RegOf(expr); continue; }
            if (dst == "mbr")
            {
                MBR = 1; hasMbr = true;
                if (expr.Trim().Equals("mbr", StringComparison.OrdinalIgnoreCase)) mbrIsMbr = true;
                else mbrReg = RegOf(expr);
                continue;
            }
            if (dst == "alu")
            {
                DecodeExpr(expr, ref A, ref B, ref ALU, ref SH, ref AMUX);
                continue;
            }
            // normal dest register
            if (!Reg.TryGetValue(dst, out C)) throw new AssembleException($"bad dest {dst}");
            ENC = 1;
            DecodeExpr(expr, ref A, ref B, ref ALU, ref SH, ref AMUX);
        }

        // Merge MAR/MBR bus requirements with the ALU expression.
        // MAR needs BLatch=marReg; MBR needs ALU-A-input=mbrReg.
        // ADD/AND are commutative so MICA swaps A/B on conflict
        // (e.g. "mar:=sp; sp:=sp+1" -> A=UNO,B=SP).
        bool hasExpr = ml.Stmts.Any(s =>
        {
            var t = s.Trim();
            int a2 = t.IndexOf(":=");
            if (a2 < 0) return false;
            string d = t[..a2].Trim().ToLowerInvariant();
            return d != "mar" && d != "mbr";
        });
        if (!hasExpr)
        {
            ALU = 2; // SH untouched: keeps halt (SH=3) or default 0
            if (hasMbr) { if (mbrIsMbr) AMUX = 1; else A = mbrReg; }
            if (hasMar) B = marReg;
        }
        else
        {
            if (hasMbr && !mbrIsMbr && A != mbrReg)
            {
                if ((ALU == 0 || ALU == 1) && B == mbrReg) (A, B) = (B, A);
                else throw new AssembleException($"A-bus conflict in '{string.Join("; ", ml.Stmts)}'");
            }
            if (hasMar && UsesB(ALU) && B != marReg)
            {
                if ((ALU == 0 || ALU == 1) && A == marReg) (A, B) = (B, A);
                else throw new AssembleException($"B-bus conflict in '{string.Join("; ", ml.Stmts)}'");
            }
            else if (hasMar && !UsesB(ALU)) B = marReg;
        }

        return MacEmu.Core.Mir.Encode((byte)ADDR, A, B, C, ENC, WR, RD, MAR, MBR, SH, ALU, COND, AMUX);
    }

    static bool UsesB(int alu) => alu == 0 || alu == 1; // ADD/AND use both inputs

    static int ResolveLab(string name, Dictionary<string, int> lab)
    {
        name = name.Trim().TrimEnd(';').Trim();
        if (lab.TryGetValue(name, out int a)) return a;
        throw new AssembleException($"unknown microlabel {name}");
    }

    static int RegOf(string expr)
    {
        expr = expr.Trim().Trim('(', ')').Trim();
        if (Reg.TryGetValue(expr, out int r)) return r;
        if (TryNum(expr, out int n)) return LitReg(n);
        throw new AssembleException($"bad reg {expr}");
    }

    static void DecodeExpr(string expr, ref int A, ref int B, ref int ALU, ref int SH, ref int AMUX)
    {
        expr = expr.Trim();
        // lshift/rshift wrapper
        if (expr.StartsWith("lshift", StringComparison.OrdinalIgnoreCase) ||
            expr.StartsWith("rshift", StringComparison.OrdinalIgnoreCase))
        {
            bool left = expr.StartsWith("lshift", StringComparison.OrdinalIgnoreCase);
            int p1 = expr.IndexOf('('), p2 = expr.LastIndexOf(')');
            int shTmp = 0;
            DecodeExpr(expr[(p1 + 1)..p2], ref A, ref B, ref ALU, ref shTmp, ref AMUX);
            SH = left ? 2 : 1;
            return;
        }
        int bi = expr.IndexOf("band", StringComparison.OrdinalIgnoreCase);
        if (bi >= 0)
        {
            int p1 = expr.IndexOf('('), p2 = expr.LastIndexOf(')');
            var args = expr[(p1 + 1)..p2].Split(',');
            A = Operand(args[0], ref AMUX); B = Operand(args[1], ref AMUX);
            ALU = 1; return;
        }
        if (expr.StartsWith("inv(", StringComparison.OrdinalIgnoreCase))
        {
            int p1 = expr.IndexOf('('), p2 = expr.LastIndexOf(')');
            A = Operand(expr[(p1 + 1)..p2], ref AMUX);
            ALU = 3; return;
        }
        int pl = FindPlus(expr);
        if (pl >= 0)
        {
            A = Operand(expr[..pl], ref AMUX); B = Operand(expr[(pl + 1)..], ref AMUX);
            ALU = 0; return;
        }
        A = Operand(expr, ref AMUX);
        ALU = 2;
    }

    static int Operand(string s, ref int AMUX)
    {
        s = s.Trim().Trim('(', ')').Trim();
        if (s.Equals("mbr", StringComparison.OrdinalIgnoreCase)) { AMUX = 1; return 0; }
        if (Reg.TryGetValue(s, out int r)) return r;
        if (TryNum(s, out int n)) return LitReg(n);
        throw new AssembleException($"bad operand {s}");
    }

    static int FindPlus(string s)
    {
        int depth = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '(') depth++;
            else if (s[i] == ')') depth--;
            else if (s[i] == '+' && depth == 0) return i;
        }
        return -1;
    }

    static int LitReg(int n) => n switch { 0 => 5, 1 => 6, -1 => 7, 0xffff => 7, _ => throw new AssembleException($"bad literal {n}") };

    static bool TryNum(string s, out int v)
    {
        s = s.Trim().Trim('(', ')').Trim();
        try
        {
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) { v = Convert.ToInt32(s[2..], 16); return true; }
            if (s.StartsWith("-0x", StringComparison.OrdinalIgnoreCase)) { v = -Convert.ToInt32(s[3..], 16); return true; }
            return int.TryParse(s, out v);
        }
        catch { v = 0; return false; }
    }

    public static void WriteMicFile(string path, uint[] words)
    {
        using var w = new StreamWriter(path);
        foreach (var x in words) w.WriteLine(Convert.ToString(x, 2).PadLeft(32, '0'));
    }
}
