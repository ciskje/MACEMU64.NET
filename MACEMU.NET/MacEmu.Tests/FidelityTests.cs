using MacEmu.Core;
using MacEmu.Core.Asm;

namespace MacEmu.Tests;

public class FidelityTests
{
    // Assets (MACRO/, MICRO/, MACDOS.MAC) located by walking up from the test
    // binary dir: works from VS, dotnet test and any checkout path.
    static string FindRoot()
    {
        var d = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (d != null)
        {
            if (Directory.Exists(Path.Combine(d.FullName, "MACRO"))
                && Directory.Exists(Path.Combine(d.FullName, "MICRO")))
                return d.FullName;
            d = d.Parent;
        }
        throw new DirectoryNotFoundException("MACRO/MICRO asset dirs not found above test dir");
    }
    static readonly string Root = FindRoot();

    [Fact]
    public void Mir_Roundtrip()
    {
        uint w = Mir.Encode(12, 15, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 0);
        var mir = new Mir(); mir.Load(w);
        Assert.Equal(12u, mir[MirField.ADDR]);
        Assert.Equal(15u, mir[MirField.A]);
        Assert.Equal(2u, mir[MirField.ALU]);
        Assert.Equal(1u, mir[MirField.COND]);
    }

    [Fact]
    public void Mir_Line1_Matches_Pre()
    {
        // CSTORE.MIC line 1 <-> PRE "p1: alu:=f; if n then goto p31;"
        string mic1 = File.ReadLines(Path.Combine(Root, "MICRO", "CSTORE.MIC")).First().Trim();
        uint w = 0;
        for (int i = 0; i < 32; i++) if (mic1[i] == '1') w |= 1u << (31 - i);
        var mir = new Mir(); mir.Load(w);
        Assert.Equal(15u, mir[MirField.A]);
        Assert.Equal(2u, mir[MirField.ALU]);
        Assert.Equal(1u, mir[MirField.COND]);
        Assert.Equal(12u, mir[MirField.ADDR]); // p31 = index 12
    }

    [Theory]
    [InlineData("FIBO")][InlineData("INSTR")][InlineData("SCHED")]
    [InlineData("CIRCLES")][InlineData("FLOWERS")][InlineData("LENTO")]
    [InlineData("DEMOINT")][InlineData("DEMOTASK")][InlineData("FIBOINT")]
    public void MacroAssembler_Matches_Original_Mac(string name)
    {
        var asm = File.ReadLines(Path.Combine(Root, "MACRO", name + ".ASM"));
        var macLines = File.ReadAllLines(Path.Combine(Root, "MACRO", name + ".MAC"))
            .Where(s => s.Trim().Length > 0).ToArray();
        int ilc = Convert.ToInt32(macLines[0].Trim(), 16);
        var asmblr = new MacAssembler(ilc);
        var (words, _, _) = asmblr.AssembleText(asm);
        var expected = macLines.Skip(1).Select(s => unchecked((short)Convert.ToInt32(s.Trim(), 16))).ToArray();
        Assert.Equal(expected.Length, words.Length);
        for (int i = 0; i < expected.Length; i++)
            Assert.True(expected[i] == words[i], $"{name}[{i}]: expected {expected[i] & 0xffff:x4} got {words[i] & 0xffff:x4}");
    }

    [Fact]
    public void MacroAssembler_Test_Asm_Is_Newer_Than_Mac()
    {
        // TEST pair is stale: TEST.MAC 24/01/1996, TEST.ASM 03/02/1996 (v4.0 readint rework),
        // not reassembled in the 03/02 batch like all others. ASM must still assemble cleanly.
        var asm = File.ReadLines(Path.Combine(Root, "MACRO", "TEST.ASM"));
        var (words, ilc, _) = new MacAssembler(0x100).AssembleText(asm);
        Assert.Equal(0x100, ilc);
        Assert.Equal(unchecked((short)0xe013), words[0]); // CALL readint (trap 19)
    }

    [Fact]
    public void MacroAssembler_MacDos_Origin10()
    {
        var asm = File.ReadLines(Path.Combine(Root, "MACRO", "MACDOS.ASM"));
        var macLines = File.ReadAllLines(Path.Combine(Root, "MACDOS.MAC"))
            .Where(s => s.Trim().Length > 0).ToArray();
        int ilc = Convert.ToInt32(macLines[0].Trim(), 16);
        var asmblr = new MacAssembler(ilc);
        var (words, _, _) = asmblr.AssembleText(asm);
        var expected = macLines.Skip(1).Select(s => unchecked((short)Convert.ToInt32(s.Trim(), 16))).ToArray();
        Assert.Equal(expected.Length, words.Length);
        for (int i = 0; i < expected.Length; i++)
            Assert.True(expected[i] == words[i], $"MACDOS[{i}]: expected {expected[i] & 0xffff:x4} got {words[i] & 0xffff:x4}");
    }

    [Fact]
    public void MicroAssembler_Matches_Original_Mic()
    {
        var pre = File.ReadLines(Path.Combine(Root, "MICRO", "CSTORE.PRE"));
        var micLines = File.ReadAllLines(Path.Combine(Root, "MICRO", "CSTORE.MIC"))
            .Where(s => s.Trim().Length > 0).ToArray();
        var words = new MicroAssembler().Assemble(pre);
        var sb = new System.Text.StringBuilder();
        int bad = 0;
        Assert.Equal(micLines.Length, words.Length);
        for (int i = 0; i < micLines.Length; i++)
        {
            uint exp = 0;
            for (int b = 0; b < 32; b++) if (micLines[i][b] == '1') exp |= 1u << (31 - b);
            if (exp != words[i]) { bad++; sb.AppendLine($"line {i}: exp {micLines[i]} got {Convert.ToString(words[i], 2).PadLeft(32, '0')}"); }
        }
        Assert.True(bad == 0, sb.ToString());
    }

    [Theory]
    [InlineData("FIBO")][InlineData("CIRCLES")][InlineData("FLOWERS")]
    public void Emulator_Runs_To_Halt(string name)
    {
        var m = new Machine();
        m.Reset();
        MicLoader.LoadFile(m, Path.Combine(Root, "MICRO", "CSTORE.MIC"));
        MacLoader.LoadOs(m, Path.Combine(Root, "MACDOS.MAC"));
        MacLoader.LoadFile(m, Path.Combine(Root, "MACRO", name + ".MAC"));
        int steps = 0;
        while (!m.Halt && steps < 20_000_000) { m.StepMacro(); steps++; }
        Assert.True(m.Halt, $"{name} did not halt");
    }
}
