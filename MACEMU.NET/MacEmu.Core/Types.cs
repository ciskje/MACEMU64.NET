// TYPES.H port
namespace MacEmu.Core;

public static class MacConstants
{
    public const int MemSize = 4096;
    public const int CStoreSize = 256;
    public const int NumRegs = 16;
    public const int NumCtrl = 8;
    public const int MaxBp = 10;
    public const int NilAddr = 4097;
    public const string Version = "4.0b";

    // Register indices (from MENU.C ResetMac regtmp order)
    public const int R_PC = 0, R_AC = 1, R_SP = 2, R_IR = 3, R_TIR = 4;
    public const int R_ZERO = 5, R_UNO = 6, R_MENOUNO = 7;
    public const int R_AMASK = 8, R_SMASK = 9;
    public const int R_A = 10, R_B = 11, R_C = 12, R_D = 13, R_E = 14, R_F = 15;
}
