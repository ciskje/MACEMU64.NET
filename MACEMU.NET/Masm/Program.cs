// masm: macro assembler ASM -> MAC (port di MASM.EXE). Uso: masm PROG.ASM [OUT.MAC]
using MacEmu.Core.Asm;

if (args.Length is 0 or > 2 || args[0] is "-h" or "--help" or "/?")
{
    Console.WriteLine("uso: masm PROG.ASM [OUT.MAC]");
    return args.Length == 0 ? 1 : 0;
}
string src = args[0];
string dir = Path.GetDirectoryName(Path.GetFullPath(src)) ?? ".";
string dst = args.Length == 2 ? args[1]
    : Path.Combine(dir, Path.GetFileNameWithoutExtension(src) + ".MAC");
try
{
    var (words, ilc, _) = new MacAssembler().AssembleText(File.ReadLines(src));
    MacAssembler.WriteMacFile(dst, words, ilc);
    Console.WriteLine($"ok: {words.Length} word ilc={ilc:X4} -> {dst}");
    return 0;
}
catch (Exception e)
{
    Console.Error.WriteLine($"errore: {e.Message}");
    return 1;
}
