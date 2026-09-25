// GENERATED from SOURCE/VISUAL.C DrawText() - do not hand-edit.
namespace MacEmu.UI;

public partial class EmuForm
{
    void DrawTitleRow()
    {
        _scr.Printc(0, 1, Vga.COLOR1 << 8, "║ MACEMU 4.0b   (.NET)   -  Copyright(c) 1996                         ║ bout...║");
        _scr.Printc(71, 1, Vga.COLOR6 << 8, "A");
    }

    void DrawTextBase()
    {
        int c1 = Vga.COLOR1 << 8;
        _scr.Printc(0, 0, c1, "╔═════════════════════════════════════════════════════════════════════╦════════╗");
        _scr.Printc(0, 2, c1, "╠════════════╦═══════════════╦════════════════════════════════════════╩════════╣");
        _scr.Printc(0, 3, c1, "║ REGISTRI   ║ STACK         ║ MIR                                             ║");
        _scr.Printc(0, 4, c1, "╠════════════╬═══════════════╬═════════════════════════════════════════════════╣");
        _scr.Printc(0, 5, c1, "║            ║               ║                                                 ║");
        _scr.Printc(0, 6, c1, "║ PC  :      ║               ╠═|[][][]|||||[  ][  ][  ][      ]════════════════╣");
        _scr.Printc(0, 7, c1, "║ AC  :      ║               ║ │ │ │ ││││││  │   │   │     │                   ║");
        _scr.Printc(0, 8, c1, "║ SP  :      ║               ║ │ │ │ ││││││  C   B   A   ADDR                  ║");
        _scr.Printc(0, 9, c1, "║ IR  :      ║               ║ A C A S│││││                                    ║");
        _scr.Printc(0, 10, c1, "║ TIR :      ║               ║ M O L H││││└─ENC                                ║");
        _scr.Printc(0, 11, c1, "║ ZERO:      ║               ║ U N U I│││└──WR    ┌────────────────────────────║");
        _scr.Printc(0, 12, c1, "║ UNO :      ║               ║ X D   F││└───RD    │COND 00 No jump             ║");
        _scr.Printc(0, 13, c1, "║ -UNO:      ║               ║       T││          │     01 jump if n           ║");
        _scr.Printc(0, 14, c1, "║ AMSK:      ║               ║       E│└────MAR   │     10 jump if z           ║");
        _scr.Printc(0, 15, c1, "║ SMSK:      ║               ║       R└──────BR   │     11 jump always         ║");
        _scr.Printc(0, 16, c1, "║ A   :      ║               ╠════════════════╗   │                            ║");
        _scr.Printc(0, 17, c1, "║ B   :      ║               ║KEYBOARD DEVICE ║   │ALU 00 sum SHIFTER 00 id    ║");
        _scr.Printc(0, 18, c1, "║ C   :      ║               ╠════════════════╣   │    01 and         01 right ║");
        _scr.Printc(0, 19, c1, "║ D   :      ║               ║BR :            ║   │    10 id          10 left  ║");
        _scr.Printc(0, 20, c1, "║ E   :      ║               ║CSR:            ║   |    11 not         11 halt  ║");
        _scr.Printc(0, 21, c1, "║ F   :      ║               ╠════════════════╩════════════════════════════════╣");
        _scr.Printc(0, 22, c1, "║            ║               ║                                                 ║");
        _scr.Printc(0, 23, c1, "╠════════════╣               ║                                                 ║");
        _scr.Printc(0, 24, c1, "║            ║               ║                                                 ║");
        _scr.Printc(0, 25, c1, "║ MAR :      ║               ║                                                 ║");
        _scr.Printc(0, 26, c1, "║ MBR :      ║               ║                                                 ║");
        _scr.Printc(0, 27, c1, "║ MPC :      ║               ║                                                 ║");
        _scr.Printc(0, 28, c1, "║            ║               ║                                                 ║");
        _scr.Printc(0, 29, c1, "╠════════════╣               ║                                                 ║");
        _scr.Printc(0, 30, c1, "║ BREAKPOINT ║               ║                                                 ║");
        _scr.Printc(0, 31, c1, "╠════════════╣               ║                                                 ║");
        _scr.Printc(0, 32, c1, "║            ║               ║                                                 ║");
        _scr.Printc(0, 33, c1, "║ Num   Addr ║               ║                                                 ║");
        _scr.Printc(0, 34, c1, "║            ║               ║                                                 ║");
        _scr.Printc(0, 35, c1, "║ 01  :      ║               ║                                                 ║");
        _scr.Printc(0, 36, c1, "║ 02  :      ║               ║                                                 ║");
        _scr.Printc(0, 37, c1, "║ 03  :      ║               ║                                                 ║");
        _scr.Printc(0, 38, c1, "║ 04  :      ║               ║                                                 ║");
        _scr.Printc(0, 39, c1, "║ 05  :      ║               ║                                                 ║");
        _scr.Printc(0, 40, c1, "║ 06  :      ║               ║                                                 ║");
        _scr.Printc(0, 41, c1, "║ 07  :      ║               ║                                                 ║");
        _scr.Printc(0, 42, c1, "║ 08  :      ║               ║                                                 ║");
        _scr.Printc(0, 43, c1, "║ 09  :      ║               ║                                                 ║");
        _scr.Printc(0, 44, c1, "║ 10  :      ║    ║    ║     ║                                                 ║");
        _scr.Printc(0, 45, c1, "╠════════════╣╚═══╩════╩════╝║                                                 ║");
        _scr.Printc(0, 46, c1, "║REFRESH:    ║ ADD      INST ║                                                 ║");
        _scr.Printc(0, 47, c1, "╠════════════╩═══════════════╩════════════════════════╦════════════════════════╣");
        _scr.Printc(0, 48, c1, "║Alt+I=DEC/HEX  Alt+R=SetRefresh  Alt+J=Jump to       ║Printer:                ║");
        _scr.Printc(0, 49, c1, "╚═════════════════════════════════════════════════════╩════════════════════════╝");
        int[] ups = [31,33,35,37,38,39,40,41,42,45,49,53,59];
        foreach (int ux in ups) _scr.Printc(ux, 7, c1, "\u2191");
        _scr.Printc(71, 1, Vga.COLOR6 << 8, "A");
    }
}
