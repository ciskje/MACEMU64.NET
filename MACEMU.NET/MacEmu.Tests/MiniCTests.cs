// MiniC tests: inline language checks + differential .C vs original .MAC.
using MacEmu.Core;
using MacEmu.Core.Asm;

namespace MacEmu.Tests;

public sealed class MiniCTests
{
    static string Root()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null)
        {
            if (Directory.Exists(Path.Combine(d.FullName, "MACRO"))) return d.FullName;
            d = d.Parent;
        }
        throw new DirectoryNotFoundException("MACRO asset dir not found");
    }
    static readonly string R = Root();

    static string Inc(string nm) => File.ReadAllText(Path.Combine(R, "MACRO", nm));
    sealed class Run
    {
        public Machine M = new();
        public int Bells;
        public Run(string cFile)
        {
            var c = new MiniC();
            var asm = c.Compile(File.ReadAllText(Path.Combine(R, "MACRO", cFile)), Inc);
            var (words, ilc, _) = new MacAssembler().AssembleText(asm);
            M.Reset();
            MacLoader.LoadOs(M, Path.Combine(R, "MACDOS.MAC"));
            MicLoader.LoadFile(M, Path.Combine(R, "MICRO", "CSTORE.MIC"));
            for (int i = 0; i < words.Length; i++) M.Mem.Write(ilc + i, words[i]);
            M.Reg[MacConstants.R_PC] = (short)ilc;
            M.Printer.OnBell = () => Bells++;
        }
        public Run Orig(string macFile)
        {
            M.Reset();
            MacLoader.LoadOs(M, Path.Combine(R, "MACDOS.MAC"));
            MicLoader.LoadFile(M, Path.Combine(R, "MICRO", "CSTORE.MIC"));
            MacLoader.LoadInto(M, File.ReadAllLines(Path.Combine(R, "MACRO", macFile)));
            M.Printer.OnBell = () => Bells++;
            return this;
        }
        static Run() { }
        public static Run ForC(string f) => new(f);
        public static Run ForOrig(string f) => new Run("HELLO.C").Orig(f); // ctor loads hello, Orig resets+loads mac
        public void Steps(int n) { for (int i = 0; i < n && !M.Halt; i++) M.StepMicro(); }
        public short Mem(int a) { M.Mem.Read(a, out short v); return v; }
        public void UntilHalt(int cap = 60_000_000) => Steps(cap);
        public void FeedStaged(string keys, int every = 400_000, int rounds = 0)
        {
            int ki = 0, steps = 0;
            int total = rounds > 0 ? rounds : keys.Length * every + every;
            while (!M.Halt && steps < total && steps < 60_000_000)
            {
                M.StepMicro(); steps++;
                if (steps % every == 0 && ki < keys.Length) M.Keyb.Load((short)keys[ki++]);
            }
        }
    }

    static Dictionary<string, int> Syms(string cFile)
    {
        var c = new MiniC();
        var asm = c.Compile(File.ReadAllText(Path.Combine(R, "MACRO", cFile)), Inc);
        return new MacAssembler().AssembleText(asm).symbols;
    }
    static Dictionary<string, int> SymsAsm(string aFile, int origin = 0x100)
    {
        var asm = File.ReadLines(Path.Combine(R, "MACRO", aFile));
        return new MacAssembler(origin).AssembleText(asm).symbols;
    }
    static short Peek(Machine m, int a) { m.Mem.Read(a, out short v); return v; }

    string C(string src, string keys = "", int cap = 5_000_000)
    {
        var c = new MiniC();
        var asm = c.Compile(src);
        var (words, ilc, _) = new MacAssembler().AssembleText(asm);
        var m = new Machine();
        m.Reset();
        MacLoader.LoadOs(m, Path.Combine(R, "MACDOS.MAC"));
        MicLoader.LoadFile(m, Path.Combine(R, "MICRO", "CSTORE.MIC"));
        for (int i = 0; i < words.Length; i++) m.Mem.Write(ilc + i, words[i]);
        m.Reg[MacConstants.R_PC] = (short)ilc;
        int ki = 0, steps = 0;
        while (!m.Halt && steps < cap)
        {
            m.StepMicro(); steps++;
            if (steps % 400_000 == 0 && ki < keys.Length) m.Keyb.Load((short)keys[ki++]);
        }
        return $"{m.Halt}|{m.Reg[MacConstants.R_AC]}|{m.Printer.Text}|{steps}";
    }

    static string[] Parts(string s) => s.Split('|');
    [Fact] public void Hello_Prints() { var p = Parts(C("void main(){print(72);print(69);print(76);print(76);print(79);halt;}", "", 100000)); Assert.Equal(new[] { "True", "79", "HELLO" }, p[..3]); }
    [Fact] public void Arith_Precedence() => Assert.StartsWith("True|-6|", C("int g; void main(){g=2+3*4-20;halt;}", "", 100000));
    static int MemVal(string src, string var, int cap = 5_000_000)
    {
        var c = new MiniC();
        var (words, ilc, syms) = new MacAssembler().AssembleText(c.Compile(src));
        var m = new Machine();
        m.Reset();
        MacLoader.LoadOs(m, Path.Combine(R, "MACDOS.MAC"));
        MicLoader.LoadFile(m, Path.Combine(R, "MICRO", "CSTORE.MIC"));
        for (int i = 0; i < words.Length; i++) m.Mem.Write(ilc + i, words[i]);
        m.Reg[MacConstants.R_PC] = (short)ilc;
        int steps = 0;
        while (!m.Halt && steps < cap) { m.StepMicro(); steps++; }
        Assert.True(m.Halt);
        m.Mem.Read(syms[var], out short v);
        return v;
    }
    [Fact] public void Fact_Loop() => Assert.Equal(24, MemVal("int r;void main(){int i=1;r=1;while(i<=4){r=r*i;i=i+1;}halt;}", "r"));
    [Fact] public void For_Loop() => Assert.Equal(10, MemVal("int g;void main(){int i;for(i=1;i<=4;i=i+1){g=g+i;}halt;}", "g"));
    [Fact] public void For_Decl() => Assert.Equal(10, MemVal("int g;void main(){for(int i=1;i<=4;i=i+1){g=g+i;}halt;}", "g"));
    [Fact] public void For_Empty() => Assert.Equal(3, MemVal("int g;void main(){int i=0;for(;;){i=i+1;if(i==3){g=i;halt;}}} ", "g"));
    [Fact] public void While_I_Mem() => Assert.Equal(4, MemVal("int i;void main(){i=0;while(i!=4){i=i+1;}halt;}", "i"));
    [Fact] public void Fib_Recursion() => Assert.StartsWith("True|55|", C("int fib(int n){if(n<2){return n;}return fib(n-1)+fib(n-2);}void main(){int x=fib(10);halt;}", "", 60000000));
    [Fact] public void Relops_All() => Assert.Equal(10111, MemVal("int r;void main(){r=0;if(3<4){r=r+1;}if(4<=4){r=r+10;}if(5>4){r=r+100;}if(4>=5){r=r+1000;}if(7==7){r=r+10000;}if(7!=7){r=r+100000;}halt;}", "r"));
    [Fact] public void Arrays_Global() => Assert.StartsWith("True|12|", C("int v[4];void main(){v[0]=5;v[2]=7;int x=v[0]+v[2];halt;}", "", 1000000));
    [Fact] public void Param_Order() => Assert.StartsWith("True|7|", C("int f(int a,int b){return a-b;}void main(){int x=f(10,3);halt;}", "", 1000000));
    [Fact] public void Nested_Calls() => Assert.StartsWith("True|20|", C("int dbl(int x){return x+x;}void main(){int y=dbl(dbl(3))+dbl(4);halt;}", "", 1000000));
    [Fact] public void While_All_Relops() => Assert.Equal(4, MemVal("int i;void main(){i=0;while(i!=4){i=i+1;}while(i>=4){i=i-1;}while(i<4){i=i+1;}while(i>4){i=i-1;}halt;}", "i"));
    [Fact] public void Unary_Neg_Not() => Assert.StartsWith("True|9|", C("void main(){int x=0-10;int y=0-x;int z=~0;int w=y+z;halt;}", "", 1000000));
    [Fact] public void Large_Literals() => Assert.StartsWith("True|10000|", C("void main(){int x=10000;halt;}", "", 100000));
    [Fact] public void Mult_Builtin() => Assert.StartsWith("True|42|", C("void main(){int x=mult(6,7);halt;}", "", 1000000));
    [Fact] public void Getc_Builtin() => Assert.StartsWith("True|65|", C("void main(){int c=getc();halt;}", "A", 5000000));
    [Fact] public void Readint_Builtin() => Assert.StartsWith("True|42|", C("void main(){int n=readint();halt;}", "42\r", 20000000));
    [Fact] public void Dint_Eint_Asm() => Assert.StartsWith("True|65|", C("void main(){dint();eint();asm(\"LOCO 65\");halt;}", "", 100000));
    [Fact] public void Errors()
    {
        Assert.Throws<MiniCException>(() => new MiniC().Compile("void main(){x=1;halt;}"));
        Assert.Throws<MiniCException>(() => new MiniC().Compile("int f(int a){return a;}void main(){int x=f(1,2);halt;}"));
        Assert.Throws<MiniCException>(() => new MiniC().Compile("void main(){halt;}void main(){halt;}"));
        Assert.Throws<MiniCException>(() => new MiniC().Compile("int x;int x;void main(){halt;}"));
        Assert.Throws<MiniCException>(() => new MiniC().Compile("void foo(){}"));
        Assert.Throws<MiniCException>(() => new MiniC().Compile("#include \"nope.h\"\nvoid main(){halt;}", Inc));
        Assert.Throws<MiniCException>(() => new MiniC().Compile("void main(){ontimer(1,2);halt;}"));
        Assert.Throws<MiniCException>(() => new MiniC().Compile("void main(){ontimer(1,nosuchfn);halt;}"));
        Assert.Throws<MiniCException>(() => new MiniC().Compile("void ontimer(){}void main(){halt;}"));
    }

    // ---------- differential .C vs .MAC ----------
    [Fact] public void Diff_Hello()
    {
        var c = Run.ForC("HELLO.C"); c.UntilHalt(200000);
        var o = Run.ForOrig("HELLO.MAC"); o.UntilHalt(200000);
        Assert.True(c.M.Halt && o.M.Halt);
        Assert.Equal("HELLO WORLD", c.M.Printer.Text);
        Assert.Equal(o.M.Printer.Text, c.M.Printer.Text);
    }
    [Fact] public void Diff_Fibo()
    {
        var c = Run.ForC("FIBO.C"); c.UntilHalt();
        var o = Run.ForOrig("FIBO.MAC"); o.UntilHalt();
        Assert.True(c.M.Halt && o.M.Halt);
        Assert.Equal(55, c.M.Reg[MacConstants.R_AC]);
    }
    [Fact] public void Diff_Test()
    {
        foreach (var (keys, k) in new[] { ("2\r", 2), ("3\r", 8) })
        {
            var cs = Syms("TEST.C"); var os = SymsAsm("TEST.ASM");
            var c = Run.ForC("TEST.C"); c.FeedStaged(keys); c.UntilHalt();
            var o = Run.ForOrig("TEST.MAC"); o.FeedStaged(keys); o.UntilHalt();
            Assert.True(c.M.Halt && o.M.Halt);
            Assert.Equal(k, Peek(c.M, cs["k"]));
            Assert.Equal(Peek(o.M, os["k"]), Peek(c.M, cs["k"]));
        }
    }
    [Fact] public void Diff_Instr()
    {
        var c = Run.ForC("INSTR.C"); c.FeedStaged("A"); c.UntilHalt();
        var o = Run.ForOrig("INSTR.MAC"); o.FeedStaged("A"); o.UntilHalt();
        Assert.True(c.M.Halt && o.M.Halt);
        Assert.Equal(-66, c.M.Reg[MacConstants.R_AC]);
        Assert.Equal(o.M.Reg[MacConstants.R_AC], c.M.Reg[MacConstants.R_AC]);
    }
    [Fact] public void Diff_Lento()
    {
        var c = Run.ForC("LENTO.C"); c.FeedStaged("AB");
        var o = Run.ForOrig("LENTO.MAC"); o.FeedStaged("AB");
        Assert.False(c.M.Halt || o.M.Halt);
        Assert.Equal("AB", c.M.Printer.Text);
        Assert.Equal(o.M.Printer.Text, c.M.Printer.Text);
    }
    [Fact] public void Diff_Demoint()
    {
        var c = Run.ForC("DEMOINT.C"); c.Steps(3_000_000);
        var o = Run.ForOrig("DEMOINT.MAC"); o.Steps(3_000_000);
        Assert.False(c.M.Halt || o.M.Halt);
        Assert.True(c.Bells > 0 && o.Bells > 0);
    }
    [Fact] public void Diff_Fiboint()
    {
        var c = Run.ForC("FIBOINT.C"); c.UntilHalt();
        var o = Run.ForOrig("FIBOINT.MAC"); o.UntilHalt();
        Assert.True(c.M.Halt && o.M.Halt);
        Assert.True(c.Bells > 0 && o.Bells > 0);
        Assert.Equal(55, c.M.Reg[MacConstants.R_AC]);
    }
    static short[] Video(Run r, int from = 1000, int to = 3600)
    {
        var v = new short[to - from];
        for (int i = 0; i < v.Length; i++) v[i] = r.Mem(i + from);
        return v;
    }
    [Fact] public void Diff_Circles()
    {
        var c = Run.ForC("CIRCLES.C"); c.UntilHalt();
        var o = Run.ForOrig("CIRCLES.MAC"); o.UntilHalt();
        Assert.True(c.M.Halt && o.M.Halt);
        Assert.Equal(Video(o), Video(c));
    }
    [Fact] public void Diff_Flowers()
    {
        var c = Run.ForC("FLOWERS.C"); c.UntilHalt();
        var o = Run.ForOrig("FLOWERS.MAC"); o.UntilHalt();
        Assert.True(c.M.Halt && o.M.Halt);
        Assert.Equal(Video(o), Video(c));
    }
    [Fact] public void Diff_Arcanoid()
    {
        var cs = Syms("ARCANOID.C"); var os = SymsAsm("ARCANOID.ASM");
        short[] Snap(Machine m, Dictionary<string, int> s) => new[] { Peek(m, s["x"]), Peek(m, s["y"]), Peek(m, s["xi"]), Peek(m, s["yi"]) };
        var c = Run.ForC("ARCANOID.C");
        short[]? sc = null;
        c.M.Printer.OnBell += () => { if (c.Bells == 5) sc = Snap(c.M, cs); };
        while (!c.M.Halt && c.Bells < 5) c.Steps(200_000);
        var o = Run.ForOrig("ARCANOID.MAC");
        short[]? so = null;
        o.M.Printer.OnBell += () => { if (o.Bells == 5) so = Snap(o.M, os); };
        while (!o.M.Halt && o.Bells < 5) o.Steps(200_000);
        Assert.NotNull(sc); Assert.NotNull(so);
        Assert.Equal(so, sc);
    }
    [Fact] public void Diff_Sched()
    {
        var cs = Syms("SCHED.C"); var os = SymsAsm("SCHED.ASM");
        var c = Run.ForC("SCHED.C"); var o = Run.ForOrig("SCHED.MAC");
        bool ct = false, ot = false;
        for (int i = 0; i < 10; i++)
        {
            c.Steps(200_000); o.Steps(200_000);
            if (Peek(c.M, cs["task"]) != 0) ct = true;
            if (Peek(o.M, os["task"]) != 0) ot = true;
        }
        Assert.False(c.M.Halt || o.M.Halt);
        Assert.True(ct && ot);
    }
    [Fact] public void Diff_Demotask()
    {
        var o = Run.ForOrig("DEMOTASK.MAC");
        short[]? refVid = null;
        for (int i = 0; i < 12 && !o.M.Halt; i++) { o.Steps(5_000_000); refVid = Video(o); }
        var c = Run.ForC("DEMOTASK.C");
        bool same = false;
        for (int i = 0; i < 12 && !c.M.Halt; i++) { c.Steps(5_000_000); if (Video(c).SequenceEqual(refVid!)) { same = true; break; } }
        Assert.False(c.M.Halt || o.M.Halt);
        Assert.True(same);
    }
}
