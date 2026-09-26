// MiniC: minimal C subset -> MAC ASM text (backend: MacAssembler, byte-identical).
// Supports: int/void, globals (+arrays), locals, params, recursion, if/else,
// while, return, + - * (via MACDOS mult trap), unary - ~, relops, print(),
// getc()/mult()/readint() builtins, halt/dint/eint, asm("...") escape.
// Frame (caller-cleanup, FIBO style): DESP L on entry; local j -> LODL j;
// arg j (pushed REVERSE aK..a1) -> LODL (L+j); INSP L + RETN on exit;
// caller INSPs args. main ends with HALT. Conditions are jump-based (no bools).
namespace MacEmu.Core.Asm;

public sealed class MiniCException : Exception
{
    public MiniCException(string msg) : base(msg) { }
}

public sealed class MiniC
{
    // ---------- lexer ----------
    enum Tk { Eof, Num, Name, Str, Cmt, KwInt, KwVoid, KwIf, KwElse, KwWhile, KwReturn,
        KwPrint, KwHalt, KwDint, KwEint, KwAsm, KwFor,
        Lpar, Rpar, Lbr, Rbr, Lsq, Rsq, Semi, Comma, Assign,
        Plus, Minus, Star, Slash, Tilde, Lt, Le, Gt, Ge, Eq, Ne }
    sealed class Tok { public Tk K; public string S; public int N; }
    readonly List<Tok> _toks = new();
    int _pos;
    Tok Peek() => _toks[_pos];
    Tok Next() => _toks[_pos++];
    bool At(Tk k) => Peek().K == k;
    Tok Expect(Tk k) { var t = Next(); if (t.K != k) throw new MiniCException($"expected {k}, got '{t.S}'"); return t; }

    static readonly Dictionary<string, Tk> Kw = new()
    {
        ["int"] = Tk.KwInt, ["void"] = Tk.KwVoid, ["if"] = Tk.KwIf, ["else"] = Tk.KwElse,
        ["while"] = Tk.KwWhile, ["return"] = Tk.KwReturn, ["print"] = Tk.KwPrint,
        ["for"] = Tk.KwFor,
        ["halt"] = Tk.KwHalt, ["dint"] = Tk.KwDint, ["eint"] = Tk.KwEint, ["asm"] = Tk.KwAsm,
    };

    static string ExpandIncludes(string src, Func<string, string>? readInclude, int depth)
    {
        if (depth > 8) throw new MiniCException("include too deep");
        var sb = new System.Text.StringBuilder();
        foreach (var raw in src.Split('\n'))
        {
            string t = raw.Trim();
            if (t.StartsWith("#"))
            {
                if (readInclude == null) throw new MiniCException("#include without resolver");
                if (!t.StartsWith("#include")) throw new MiniCException($"bad directive '{t}'");
                int q1 = t.IndexOf('"'), q2 = t.LastIndexOf('"');
                if (q1 < 0 || q2 <= q1) throw new MiniCException($"bad include '{t}'");
                string inc;
                try { inc = readInclude(t[(q1 + 1)..q2]); }
                catch (Exception e) { throw new MiniCException($"include failed: {e.Message}"); }
                sb.AppendLine(ExpandIncludes(inc, readInclude, depth + 1));
            }
            else sb.AppendLine(raw);
        }
        return sb.ToString();
    }

    void Lex(string src)
    {
        int i = 0;
        while (i < src.Length)
        {
            char c = src[i];
            if (c == '/' && i + 1 < src.Length && src[i + 1] == '/') { int j = i + 2; while (j < src.Length && src[j] != '\n') j++; _toks.Add(new Tok { K = Tk.Cmt, S = src[i..j].TrimEnd('\r') }); i = j; continue; }
            if (c == '\'')
            {
                if (i + 2 >= src.Length || src[i + 2] != '\'')
                    throw new MiniCException("bad char literal (use 'x')");
                _toks.Add(new Tok { K = Tk.Num, S = src.Substring(i, 3), N = src[i + 1] });
                i += 3; continue;
            }
            if (c == '/') { _toks.Add(new Tok { K = Tk.Slash, S = "/" }); i++; continue; }
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (char.IsLetter(c) || c == '_')
            {
                int j = i; while (j < src.Length && (char.IsLetterOrDigit(src[j]) || src[j] == '_')) j++;
                string w = src[i..j]; i = j;
                _toks.Add(new Tok { K = Kw.TryGetValue(w, out var k) ? k : Tk.Name, S = w });
                continue;
            }
            if (char.IsDigit(c))
            {
                int j = i; while (j < src.Length && (char.IsLetterOrDigit(src[j]))) j++;
                string w = src[i..j]; i = j;
                int v = w.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToInt32(w[2..], 16) : int.Parse(w);
                _toks.Add(new Tok { K = Tk.Num, S = w, N = v });
                continue;
            }
            if (c == '"')
            {
                int j = src.IndexOf('"', i + 1);
                if (j < 0) throw new MiniCException("unterminated string");
                _toks.Add(new Tok { K = Tk.Str, S = src[(i + 1)..j] }); i = j + 1;
                continue;
            }
            Tk k2 = c switch
            {
                '(' => Tk.Lpar, ')' => Tk.Rpar, '{' => Tk.Lbr, '}' => Tk.Rbr,
                '[' => Tk.Lsq, ']' => Tk.Rsq, ';' => Tk.Semi, ',' => Tk.Comma,
                '+' => Tk.Plus, '-' => Tk.Minus, '*' => Tk.Star, '~' => Tk.Tilde,
                '=' => Tk.Assign, '<' => Tk.Lt, '>' => Tk.Gt, '!' => Tk.Ne,
                _ => throw new MiniCException($"bad char '{c}' (note: no division, '/' starts a comment)"),
            };
            if (c == '=' || c == '<' || c == '>' || c == '!')
            {
                if (i + 1 < src.Length && src[i + 1] == '=')
                {
                    k2 = c == '=' ? Tk.Eq : c == '<' ? Tk.Le : c == '>' ? Tk.Ge : Tk.Ne;
                    _toks.Add(new Tok { K = k2, S = src.Substring(i, 2) }); i += 2; continue;
                }
                if (c == '!') throw new MiniCException("'!' needs '=' (only != supported)");
                k2 = c == '=' ? Tk.Assign : c == '<' ? Tk.Lt : Tk.Gt;
            }
            _toks.Add(new Tok { K = k2, S = c.ToString() }); i++;
        }
        _toks.Add(new Tok { K = Tk.Eof, S = "" });
    }

    // ---------- AST ----------
    abstract class Node { }
    sealed class NNum : Node { public int V; }
    sealed class NVar : Node { public string N = ""; }
    sealed class NIdx : Node { public string N = ""; public Node? I; }
    sealed class NBin : Node { public char Op; public Node? L, R; }
    sealed class NNeg : Node { public Node? E; }
    sealed class NNot : Node { public Node? E; }
    sealed class NCall : Node { public string N = ""; public List<Node> A = new(); }
    abstract class St { public List<string> Lead = new(); public List<string> Trail = new(); }
    sealed class SExpr : St { public Node? E; }
    sealed class SDecl : St { public string N = ""; public Node? Init; }
    sealed class SSet : St { public string N = ""; public Node? Idx, E; }
    sealed class SRet : St { public Node? E; }
    sealed class SIf : St { public Cond? Cond; public List<St> T = new(), F = new(); }
    sealed class SWhile : St { public Cond? Cond; public List<St> B = new(); }
    sealed class SFor : St { public St? Init; public Cond? Cond; public St? Incr; public List<St> B = new(); }
    sealed class SPrint : St { public Node? E; }
    sealed class SHalt : St { }
    sealed class SDint : St { public bool On; }
    sealed class SAsm : St { public string Code = ""; }
    sealed class Cond { public Node? L, R; public string Op = ""; public bool IsConst; public int Const; }
    sealed class Func
    {
        public string N = "";
        public List<string> Lead = new();
        public List<string> Trail = new();
        public bool Void;
        public List<string> Params = new();
        public List<string> Locals = new();
        public List<St> Body = new();
    }
    sealed class Glob { public string N = ""; public int Size = 1; public int Init; public List<string> Lead = new(); }

    readonly List<Func> _funcs = new();
    readonly List<Glob> _globs = new();
    readonly Dictionary<string, int> _equ = new(); // NAME = constexpr (upper-case)
    readonly List<object> _order = new(); // Func/Glob/EquN in source order (comments!)
    readonly List<string> _tail = new(); // trailing comments
    sealed class EquN { public string N = ""; public List<string> Lead = new(); }

    List<string> TakeComments()
    {
        var l = new List<string>();
        while (At(Tk.Cmt)) { string t = Next().S; l.Add(t.StartsWith("//") ? t[1..] : t); }
        return l;
    }

    // ---------- parser ----------
    void Parse()
    {
        while (!At(Tk.Eof))
        {
            var lead = TakeComments();
            if (At(Tk.Eof)) { _tail.AddRange(lead); break; }
            if (At(Tk.Name))
            {
                // EQUATE: NAME = constexpr ;  (define-before-use)
                string en = Next().S;
                if (!At(Tk.Assign)) throw new MiniCException($"expected type or = after '{en}'");
                Next();
                int v = ParseConst();
                Expect(Tk.Semi);
                if (_equ.ContainsKey(en) || _funcs.Exists(f => f.N == en) || _globs.Exists(g => g.N == en))
                    throw new MiniCException($"dup symbol {en}");
                _equ[en] = v;
                _order.Add(new EquN { N = en, Lead = lead });
                continue;
            }
            bool isVoid = At(Tk.KwVoid);
            if (!At(Tk.KwInt) && !isVoid) throw new MiniCException($"expected type, got '{Peek().S}'");
            Next();
            string name = Expect(Tk.Name).S;
            if (At(Tk.Lpar)) { var f = ParseFunc(name, isVoid); f.Lead.AddRange(lead); }
            else
            {
                if (isVoid) throw new MiniCException("void global");
                if (_globs.Exists(g => g.N == name) || _funcs.Exists(f => f.N == name) || _equ.ContainsKey(name))
                    throw new MiniCException($"dup global {name}");
                int size = 1, init = 0;
                if (At(Tk.Lsq))
                {
                    Next();
                    if (At(Tk.Num)) size = Expect(Tk.Num).N;
                    else if (At(Tk.Name) && _equ.TryGetValue(Expect(Tk.Name).S, out int sz)) size = sz;
                    else throw new MiniCException("array size must be a number or = constant");
                    Expect(Tk.Rsq);
                }
                if (At(Tk.Assign))
                {
                    Next();
                    if (At(Tk.Num)) init = Expect(Tk.Num).N;
                    else if (At(Tk.Name) && _equ.TryGetValue(Expect(Tk.Name).S, out int iv)) init = iv;
                    else throw new MiniCException("global init must be a number or = constant");
                }
                _globs.Add(new Glob { N = name, Size = size, Init = init, Lead = lead });
                _order.Add(_globs[^1]);
                while (At(Tk.Comma)) throw new MiniCException("one global per declaration");
                Expect(Tk.Semi);
            }
        }
        if (!_funcs.Exists(f => f.N == "main")) throw new MiniCException("missing main");
        foreach (var r in new[] { "ontimer", "funaddr" })
            if (_funcs.Exists(f => f.N == r)) throw new MiniCException($"{r} reserved");
    }

    int ParseConst() => ParseConstAdd();
    int ParseConstAdd()
    {
        int v = ParseConstMul();
        while (At(Tk.Plus) || At(Tk.Minus)) { bool add = At(Tk.Plus); Next(); int r = ParseConstMul(); v = add ? v + r : v - r; }
        return v;
    }
    int ParseConstMul()
    {
        int v = ParseConstUna();
        while (At(Tk.Star) || At(Tk.Slash))
        {
            bool mul = At(Tk.Star); Next();
            int r = ParseConstUna();
            if (!mul && r == 0) throw new MiniCException("div by zero in constant");
            v = mul ? v * r : v / r;
        }
        return v;
    }
    int ParseConstUna()
    {
        if (At(Tk.Minus)) { Next(); return -ParseConstUna(); }
        if (At(Tk.Num)) return Next().N;
        if (At(Tk.Name))
        {
            string n = Next().S;
            if (_equ.TryGetValue(n, out int v)) return v;
            throw new MiniCException($"const expected, '{n}' undefined (define-before-use)");
        }
        if (At(Tk.Lpar)) { Next(); int v = ParseConstAdd(); Expect(Tk.Rpar); return v; }
        throw new MiniCException($"bad constant near '{Peek().S}'");
    }

    Func ParseFunc(string name, bool isVoid)
    {
        if (_funcs.Exists(f => f.N == name) || _equ.ContainsKey(name) || _globs.Exists(g => g.N == name))
            throw new MiniCException($"dup function {name}");
        var f = new Func { N = name, Void = isVoid };
        Expect(Tk.Lpar);
        if (!At(Tk.Rpar))
        {
            while (true)
            {
                Expect(Tk.KwInt);
                string p = Expect(Tk.Name).S;
                if (f.Params.Contains(p) || _equ.ContainsKey(p)) throw new MiniCException($"dup param {p}");
                f.Params.Add(p);
                if (!At(Tk.Comma)) break;
                Next();
            }
        }
        Expect(Tk.Rpar);
        Expect(Tk.Lbr);
        St? last = null;
        while (true)
        {
            var t = TakeComments();
            if (At(Tk.Rbr)) { if (last != null) last.Trail.AddRange(t); else f.Trail.AddRange(t); break; }
            last = ParseStmt(f);
            last.Lead.InsertRange(0, t);
            f.Body.Add(last);
        }
        Expect(Tk.Rbr);
        _funcs.Add(f);
        _order.Add(f);
        return f;
    }

    St ParseStmt(Func f)
    {
        var lead = TakeComments();
        var s = ParseStmtInner(f);
        s.Lead.AddRange(lead);
        return s;
    }
    St ParseStmtInner(Func f)
    {
        if (At(Tk.Lbr)) { Next(); var b = new List<St>(); St? last = null; while (true) { var t = TakeComments(); if (At(Tk.Rbr)) { if (last != null) last.Trail.AddRange(t); break; } last = ParseStmt(f); last.Lead.InsertRange(0, t); b.Add(last); } Expect(Tk.Rbr); return new SBlockList { S = b }; }
        if (At(Tk.KwInt))
        {
            Next();
            string n = Expect(Tk.Name).S;
            if (f.Params.Contains(n) || f.Locals.Contains(n) || _equ.ContainsKey(n)) throw new MiniCException($"dup local {n}");
            f.Locals.Add(n);
            Node? init = null;
            if (At(Tk.Assign)) { Next(); init = ParseExpr(); }
            Expect(Tk.Semi);
            return new SDecl { N = n, Init = init };
        }
        if (At(Tk.KwIf))
        {
            Next(); Expect(Tk.Lpar); var c = ParseCond(); Expect(Tk.Rpar);
            var t = ParseOne(f); var fl = new List<St>();
            if (At(Tk.KwElse)) { Next(); fl = ParseOne(f); }
            return new SIf { Cond = c, T = t, F = fl };
        }
        if (At(Tk.KwWhile))
        {
            Next(); Expect(Tk.Lpar); var c = ParseCond(); Expect(Tk.Rpar);
            return new SWhile { Cond = c, B = ParseOne(f) };
        }
        if (At(Tk.KwFor))
        {
            Next(); Expect(Tk.Lpar);
            St? init = null, incr = null;
            if (!At(Tk.Semi))
            {
                if (At(Tk.KwInt)) init = ParseStmt(f);
                else { init = ParseAssign(f); Expect(Tk.Semi); }
            }
            else Next();
            Cond? c = null;
            if (!At(Tk.Semi)) c = ParseCond();
            Expect(Tk.Semi);
            if (!At(Tk.Rpar)) incr = ParseAssign(f);
            Expect(Tk.Rpar);
            return new SFor { Init = init, Cond = c, Incr = incr, B = ParseOne(f) };
        }
        if (At(Tk.KwReturn)) { Next(); Node? e = At(Tk.Semi) ? null : ParseExpr(); Expect(Tk.Semi); return new SRet { E = e }; }
        if (At(Tk.KwPrint)) { Next(); Expect(Tk.Lpar); var e = ParseExpr(); Expect(Tk.Rpar); Expect(Tk.Semi); return new SPrint { E = e }; }
        if (At(Tk.KwHalt) || At(Tk.KwDint) || At(Tk.KwEint))
        {
            var k = Next().K;
            if (At(Tk.Lpar)) { Next(); Expect(Tk.Rpar); } // halt()/dint()/eint() ok
            Expect(Tk.Semi);
            if (k == Tk.KwHalt) return new SHalt();
            return new SDint { On = k == Tk.KwEint };
        }
        if (At(Tk.KwAsm)) { Next(); Expect(Tk.Lpar); string code = Expect(Tk.Str).S; Expect(Tk.Rpar); Expect(Tk.Semi); return new SAsm { Code = code }; }
        // assignment or call
        if (At(Tk.Name))
        {
            string n = Peek().S;
            if (IsAssignAhead()) return ParseAssignWithSemi(f);
            if (AtNext(Tk.Lpar, 1))
            {
                Next();
                var cl = ParseCall(n); Expect(Tk.Semi);
                return new SExpr { E = cl };
            }
        }
        throw new MiniCException($"bad statement near '{Peek().S}'");
    }

    bool AtNext(Tk k, int ahead)
    {
        int i = _pos + ahead;
        // ahead=1: token after Name. Note Name already peeked, not consumed.
        return i < _toks.Count && _toks[i].K == k;
    }
    bool IsAssignAhead()
    {
        // Name [ [expr] ] = ...  (scan without consuming: only brackets nesting)
        int i = _pos + 1;
        if (i < _toks.Count && _toks[i].K == Tk.Lsq)
        {
            int depth = 0;
            while (i < _toks.Count)
            {
                if (_toks[i].K == Tk.Lsq) depth++;
                else if (_toks[i].K == Tk.Rsq) { depth--; if (depth == 0) { i++; break; } }
                i++;
            }
        }
        return i < _toks.Count && _toks[i].K == Tk.Assign;
    }
    St ParseAssignWithSemi(Func f) { var s = ParseAssign(f); Expect(Tk.Semi); return s; }
    SSet ParseAssign(Func f)
    {
        string n = Expect(Tk.Name).S;
        Node? idx = null;
        if (At(Tk.Lsq)) { Next(); idx = ParseExpr(); Expect(Tk.Rsq); }
        Expect(Tk.Assign);
        return new SSet { N = n, Idx = idx, E = ParseExpr() };
    }

    List<St> ParseOne(Func f)
    {
        if (At(Tk.Lbr)) { var s = ParseStmt(f); return ((SBlockList)s).S; }
        return new List<St> { ParseStmt(f) };
    }
    sealed class SBlockList : St { public List<St> S = new(); }

    Cond ParseCond()
    {
        var l = ParseExpr();
        if (l is NNum num && !IsRelop()) return new Cond { IsConst = true, Const = num.V };
        string op = Peek().S;
        if (!IsRelop()) throw new MiniCException("condition needs < <= > >= == !=");
        Next();
        return new Cond { L = l, R = ParseExpr(), Op = op };
    }
    bool IsRelop() => At(Tk.Lt) || At(Tk.Le) || At(Tk.Gt) || At(Tk.Ge) || At(Tk.Eq) || At(Tk.Ne);

    Node ParseExpr()
    {
        var l = ParseTerm();
        while (At(Tk.Plus) || At(Tk.Minus)) { bool add = At(Tk.Plus); Next(); l = new NBin { Op = add ? '+' : '-', L = l, R = ParseTerm() }; }
        return l;
    }
    Node ParseTerm()
    {
        var l = ParseUnary();
        while (At(Tk.Star)) { Next(); l = new NBin { Op = '*', L = l, R = ParseUnary() }; }
        return l;
    }
    Node ParseUnary()
    {
        if (At(Tk.Minus)) { Next(); return new NNeg { E = ParseUnary() }; }
        if (At(Tk.Tilde)) { Next(); return new NNot { E = ParseUnary() }; }
        return ParseFactor();
    }
    Node ParseFactor()
    {
        if (At(Tk.Num)) return new NNum { V = Next().N };
        if (At(Tk.Lpar)) { Next(); var e = ParseExpr(); Expect(Tk.Rpar); return e; }
        if (At(Tk.Name))
        {
            string n = Next().S;
            if (At(Tk.Lsq)) { Next(); var i = ParseExpr(); Expect(Tk.Rsq); return new NIdx { N = n, I = i }; }
            if (At(Tk.Lpar)) return ParseCall(n);
            return new NVar { N = n };
        }
        throw new MiniCException($"bad expression near '{Peek().S}'");
    }
    NCall ParseCall(string n)
    {
        var c = new NCall { N = n };
        Expect(Tk.Lpar);
        if (!At(Tk.Rpar)) { while (true) { c.A.Add(ParseExpr()); if (!At(Tk.Comma)) break; Next(); } }
        Expect(Tk.Rpar);
        return c;
    }

    // ---------- codegen ----------
    readonly List<string> _out = new();
    int _lbl;
    string Lab() => $"Lc{++_lbl}";
    void Emit(string s) => _out.Add("        " + s);
    void EmitLab(string l) => _out.Add(l + ":");
    Func? _cur;
    int L => _cur!.Locals.Count;
    int _depth; // expression temp pushes (LODL/STOL indices shift by this)
    void Push() { Emit("PUSH"); _depth++; }
    void PopN(int n) { Emit($"INSP {n}"); _depth -= n; }
    void Psi() { Emit("PSHI"); _depth++; }
    void Pop1() { Emit("POP"); _depth--; }

    int LocalIdx(string n)
    {
        // after DESP L: local j (0-based) -> LODL j; ret at LODL L; param k -> LODL (L+k+1)
        // plus _depth for expression temps currently pushed
        int j = _cur!.Locals.IndexOf(n);
        if (j >= 0) return j + _depth;
        int k = _cur.Params.IndexOf(n);
        if (k >= 0) return L + k + 1 + _depth;
        throw new MiniCException($"unknown variable {n}");
    }
    Glob GlobOf(string n)
    {
        var g = _globs.Find(g => g.N == n);
        if (g == null) throw new MiniCException($"unknown variable {n}");
        return g;
    }

    void GenNum(int v)
    {
        if (v >= 0 && v <= 4095) Emit($"LOCO {v}");
        else if (v > 4095)
        {
            // LOCO holds 12 bits: sum chunks
            int rest = v; bool first = true;
            while (rest > 0)
            {
                int ck = Math.Min(rest, 4095); rest -= ck;
                if (!first) Push();
                Emit($"LOCO {ck}");
                if (!first) { Emit("ADDL 0"); PopN(1); }
                first = false;
            }
        }
        else throw new MiniCException("negative literal");
    }

    void Gen(Node e)
    {
        switch (e)
        {
            case NNum num:
                GenNum(num.V);
                break;
            case NVar v when _equ.TryGetValue(v.N, out int ev):
                GenNum(ev);
                break;
            case NVar v:
                if (_cur!.Locals.Contains(v.N) || _cur.Params.Contains(v.N)) Emit($"LODL {LocalIdx(v.N)}");
                else Emit($"LODD {GlobOf(v.N).N}");
                break;
            case NIdx idx:
                Gen(idx.I!); Push(); Emit($"LOCO {GlobOf(idx.N).N}"); Emit("ADDL 0"); PopN(1);
                Psi(); Pop1();
                break;
            case NBin b when b.Op == '+':
                Gen(b.L!); Push(); Gen(b.R!); Emit("ADDL 0"); PopN(1);
                break;
            case NBin b when b.Op == '-':
                Gen(b.R!); Push(); Gen(b.L!); Emit("SUBL 0"); PopN(1);
                break;
            case NBin b: // '*'
                Gen(b.L!); Push(); Gen(b.R!); Push(); Emit("CALL 18"); _depth -= 2;
                break;
            case NNeg n:
                Gen(n.E!); Push(); Emit("LOCO 0"); Emit("SUBL 0"); PopN(1);
                break;
            case NNot n:
                Gen(n.E!); Emit("NOT");
                break;
            case NCall c:
                GenCall(c);
                break;
            default: throw new MiniCException("bad expr");
        }
    }

    void GenCall(NCall c, bool asStmt = false)
    {
        if (c.N == "funaddr")
        {
            // funaddr(func): LOCO label (address-of-function: ASM-only)
            if (c.A.Count != 1 || c.A[0] is not NVar fn)
                throw new MiniCException("funaddr(funcname)");
            if (_funcs.Find(f => f.N == fn.N) == null) throw new MiniCException($"unknown function {fn.N}");
            Emit($"LOCO {fn.N}");
            return;
        }
        if (c.N == "ontimer")
        {
            // ontimer(periodExpr, func): STOD 4 + STOD 254 (address-of-function: ASM-only)
            if (c.A.Count != 2 || c.A[1] is not NVar fn)
                throw new MiniCException("ontimer(period, funcname)");
            if (_funcs.Find(f => f.N == fn.N) == null) throw new MiniCException($"unknown function {fn.N}");
            Gen(c.A[0]);
            Emit("STOD 4");
            Emit($"LOCO {fn.N}");
            Emit("STOD 254");
            return;
        }
        if (c.N == "mult")
        {
            if (c.A.Count != 2) throw new MiniCException("mult takes 2 args");
            Gen(c.A[0]); Push(); Gen(c.A[1]); Push(); Emit("CALL 18");
            _depth -= 2;
            return; // MACDOS callee-cleanup
        }
        if (c.N == "getc")
        {
            if (c.A.Count != 0) throw new MiniCException("getc takes no args");
            Emit("CALL 17");
            return;
        }
        if (c.N == "readint")
        {
            if (c.A.Count != 0) throw new MiniCException("readint takes no args");
            Emit("CALL 19");
            return;
        }
        var f = _funcs.Find(f => f.N == c.N) ?? throw new MiniCException($"unknown function {c.N}");
        if (f.Void && !asStmt) throw new MiniCException($"{c.N} is void");
        if (c.A.Count != f.Params.Count) throw new MiniCException($"{c.N} wants {f.Params.Count} args");
        for (int j = c.A.Count - 1; j >= 0; j--) { Gen(c.A[j]); Push(); } // reverse: param1 on top
        Emit($"CALL {f.N}");
        if (c.A.Count > 0) PopN(c.A.Count); // caller cleanup
    }

    void GenSt(St s)
    {
        foreach (var cmt in s.Lead) _out.Add("        /" + cmt);
        switch (s)
        {
            case SBlockList b: foreach (var x in b.S) GenSt(x); break;
            case SExpr e when e.E is NCall nc: GenCall(nc, true); break;
            case SExpr e: Gen(e.E!); break; // result in AC, discarded
            case SDecl d:
                if (d.Init != null) { Gen(d.Init); Emit($"STOL {_cur!.Locals.IndexOf(d.N)}"); }
                break;
            case SSet st:
                if (st.Idx == null)
                {
                    Gen(st.E!);
                    if (_cur!.Locals.Contains(st.N) || _cur.Params.Contains(st.N)) Emit($"STOL {LocalIdx(st.N)}");
                    else Emit($"STOD {GlobOf(st.N).N}");
                }
                else
                {
                    var g = GlobOf(st.N);
                    if (_cur!.Locals.Contains(st.N) || _cur.Params.Contains(st.N)) throw new MiniCException("local arrays unsupported");
                    Gen(st.E!); Push();
                    Gen(st.Idx!); Push(); Emit($"LOCO {g.N}"); Emit("ADDL 0"); PopN(1);
                    Emit("POPI"); _depth--;
                }
                break;
            case SRet r:
                if (_cur!.Void && r.E != null) throw new MiniCException("void returns value");
                if (r.E != null) Gen(r.E);
                if (L > 0) Emit($"INSP {L}");
                if (_depth != 0) throw new MiniCException("stack unbalanced");
                Emit("RETN");
                break;
            case SPrint p: Gen(p.E!); Emit("STOD 2"); break;
            case SHalt: Emit("HALT"); break;
            case SDint d: Emit(d.On ? "EINT" : "DINT"); break;
            case SAsm a: _out.Add("        " + a.Code); break;
            case SIf i: GenCond(i.Cond!, i.T, i.F); break;
            case SWhile w:
            {
                string top = Lab(), end = Lab();
                EmitLab(top);
                GenCondJump(w.Cond!, null, end);
                foreach (var x in w.B) GenSt(x);
                Emit($"JUMP {top}");
                EmitLab(end);
                break;
            }
            case SFor fr:
            {
                if (fr.Init != null) GenSt(fr.Init);
                string ftop = Lab(), fend = Lab();
                EmitLab(ftop);
                if (fr.Cond != null) GenCondJump(fr.Cond, null, fend);
                foreach (var x in fr.B) GenSt(x);
                if (fr.Incr != null) GenSt(fr.Incr);
                Emit($"JUMP {ftop}");
                EmitLab(fend);
                break;
            }
            default: throw new MiniCException("bad stmt");
        }
        foreach (var cmt in s.Trail) _out.Add("        /" + cmt);
    }

    // cond jump: Then=null means fallthrough. AC-safe (balanced stack).
    void GenDiff(Node l, Node r, bool swapped)
    {
        if (!swapped) { Gen(r); Push(); Gen(l); }
        else { Gen(l); Push(); Gen(r); }
        Emit("SUBL 0"); PopN(1); // AC = l-r (or r-l if swapped)
    }
    void GenCondJump(Cond c, string? goTrue, string? goFalse)
    {
        if (c.IsConst)
        {
            bool t = c.Const != 0;
            string? go = t ? goTrue : goFalse;
            if (go != null) Emit($"JUMP {go}");
            return;
        }
        // normalize: AC = l-r except >/>= which use r-l
        bool sw = c.Op == ">" || c.Op == ">=";
        GenDiff(c.L!, c.R!, sw);
        switch (c.Op)
        {
            case "<":
            case ">": // AC<0 -> true
                if (goTrue != null && goFalse != null) { Emit($"JNEG {goTrue}"); Emit($"JUMP {goFalse}"); }
                else if (goFalse != null) Emit($"JPOS {goFalse}");
                else if (goTrue != null) Emit($"JNEG {goTrue}");
                break;
            case "<=":
            case ">=": // AC<=0 -> true
                if (goTrue != null && goFalse != null) { Emit($"JZER {goTrue}"); Emit($"JPOS {goFalse}"); }
                else if (goFalse != null) { string lok = Lab(); Emit($"JNEG {lok}"); Emit($"JZER {lok}"); Emit($"JUMP {goFalse}"); EmitLab(lok); }
                else if (goTrue != null) { Emit($"JZER {goTrue}"); Emit($"JNEG {goTrue}"); }
                break;
            case "==":
                if (goTrue != null && goFalse != null) { Emit($"JZER {goTrue}"); Emit($"JUMP {goFalse}"); }
                else if (goFalse != null) Emit($"JNZE {goFalse}");
                else if (goTrue != null) Emit($"JZER {goTrue}");
                break;
            case "!=":
                if (goTrue != null && goFalse != null) { Emit($"JNZE {goTrue}"); Emit($"JUMP {goFalse}"); }
                else if (goFalse != null) Emit($"JZER {goFalse}");
                else if (goTrue != null) Emit($"JNZE {goTrue}");
                break;
        }
        if (goTrue != null && goFalse != null) EmitLab(goTrue);
    }
    void GenCond(Cond c, List<St> t, List<St> f)
    {
        string then = Lab(), end = Lab();
        string els = f.Count > 0 ? Lab() : end;
        GenCondJump(c, then, els);
        foreach (var x in t) GenSt(x);
        if (f.Count > 0) { Emit($"JUMP {end}"); EmitLab(els); foreach (var x in f) GenSt(x); }
        EmitLab(end);
    }

    public List<string> Compile(string src) => Compile(src, null);

    // readInclude(name) -> file text; enables #include "file" (depth <= 8)
    public List<string> Compile(string src, Func<string, string>? readInclude)
    {
        _toks.Clear(); _pos = 0;
        _funcs.Clear(); _globs.Clear(); _equ.Clear(); _order.Clear(); _tail.Clear();
        Lex(ExpandIncludes(src, readInclude, 0)); Parse();
        _out.Clear(); _lbl = 0;
        _out.Add("/ generated by MiniC");
        _out.Add("        JUMP main");
        foreach (var o in _order)
        {
            if (o is EquN eq) { foreach (var cmt in eq.Lead) _out.Add("        /" + cmt); continue; }
            if (o is Func f)
            {
                foreach (var cmt in f.Lead) _out.Add("        /" + cmt);
                if (f.N.StartsWith("Lc", StringComparison.OrdinalIgnoreCase)) throw new MiniCException("Lc prefix reserved");
                _cur = f;
                EmitLab(f.N);
                if (L > 0) Emit($"DESP {L}");
                foreach (var s in f.Body) GenSt(s);
                foreach (var cmt in f.Trail) _out.Add("        /" + cmt);
                if (f.N == "main") Emit("HALT");
                else { if (L > 0) Emit($"INSP {L}"); Emit("RETN"); }
            }
            else if (o is Glob g)
            {
                foreach (var cmt in g.Lead) _out.Add("        /" + cmt);
                _out.Add($"{g.N}: {g.Init}");
                for (int j = 1; j < g.Size; j++) _out.Add("0");
            }
        }
        foreach (var cmt in _tail) _out.Add("        /" + cmt);
        return _out;
    }
}
