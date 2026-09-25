// CPU.C / ALU.C / AMUX.C / SHIFTER.C / MSL.C / MMUX.C / SYSBUS.C ports
namespace MacEmu.Core;

public sealed class Machine
{
    public Registers Reg = new();
    public Latches Latch = new();
    public CBus CBus = new();
    public MarMbr Mx = new();
    public Mir Mir = new();
    public Mpc Mpc = new();
    public ControlStore CStore = new();
    public Memory Mem = new();
    public KeyboardDevice Keyb = new();
    public TimerDevice Timer = new();
    public PrinterDevice Printer = new();
    public bool Halt;
    private int _rdTime, _wrTime;

    // ---- datapath (RegistriOutput/Input, AMux, ALU+Shifter, MAR/MBR, MSL, MMux, SysBus) ----
    short AluResult;

    void Phase1() => Mir.Load(CStore.Read(Mpc.Value));
    void Phase2()
    {
        Latch.A = Reg[(int)Mir[MirField.A]];
        Latch.B = Reg[(int)Mir[MirField.B]];
    }
    void Phase3()
    {
        if (Mir[MirField.MAR] != 0) Mx.Mar = (ushort)(Latch.B & 0x0fff);
        short a = Mir[MirField.AMUX] != 0 ? Mx.Mbr : Latch.A;
        short b = Latch.B;
        AluResult = Mir[MirField.ALU] switch
        {
            0 => unchecked((short)(a + b)),
            1 => unchecked((short)(a & b)),
            2 => a,
            _ => unchecked((short)~a),
        };
        // SHIFTER.C: case 3 calls SetHalt() leaving Result UNINITIALIZED, then
        // WriteCBus(garbage). Unknowable value; we keep the previous CBus.
        // All real HALT microwords (p29) have MBR=ENC=0, so CBus is never
        // consumed afterwards: no observable difference.
        if (Mir[MirField.SH] == 3) Halt = true;
        else CBus.Value = Mir[MirField.SH] switch
        {
            0 => AluResult,
            1 => unchecked((short)(AluResult >> 1)), // arithmetic shift like C on short
            _ => unchecked((short)(AluResult << 1)),
        };
    }
    void Phase4()
    {
        if (Mir[MirField.MBR] != 0) Mx.Mbr = CBus.Value;
        if (Mir[MirField.ENC] != 0) Reg[(int)Mir[MirField.C]] = CBus.Value;
        // Mmux
        bool jump = Mir[MirField.COND] switch
        {
            0 => false,
            1 => ((ushort)AluResult & 0x8000) == 0x8000,
            2 => AluResult == 0,
            _ => true,
        };
        Mpc.Value = jump ? (byte)Mir[MirField.ADDR] : (byte)(Mpc.Value + 1);
        SysBus();
        Keyb.Poll();
        Timer.Poll();
    }

    bool EnableRD()
    {
        if (Mir[MirField.RD] != 0) _rdTime++;
        if (_rdTime == 2) { _rdTime = 0; return true; }
        return false;
    }
    bool EnableWR()
    {
        if (Mir[MirField.WR] != 0) _wrTime++;
        if (_wrTime == 2) { _wrTime = 0; return true; }
        return false;
    }

    void SysBus()
    {
        if (EnableRD())
        {
            short t = 0; bool ok = false;
            if (Mem.Read(Mx.Mar, out t)) ok = true;
            else if (Keyb.Read(Mx.Mar, out t)) ok = true;
            // printer/timer have no RD; null devices -> false
            if (ok) Mx.Mbr = t;
        }
        if (EnableWR())
        {
            short t = Mx.Mbr;
            if (!Mem.Write(Mx.Mar, t))
                if (!Keyb.Write(Mx.Mar, t))
                    if (!Printer.Write(Mx.Mar, t))
                        Timer.Write(Mx.Mar, t);
        }
        short f = Reg[MacConstants.R_F];
        short mask = 1;
        // DevIp order: mem=null, keyb, null, timer, null...
        bool[] ip = [false, Keyb.InterruptPending(), false, Timer.InterruptPending(),
            false, false, false, false];
        for (int i = 0; i < MacConstants.NumCtrl; i++, mask <<= 1)
        {
            if (ip[i]) f |= mask; else f &= (short)~mask;
        }
        Reg[MacConstants.R_F] = f;
    }

    public void StepMicro() { Phase1(); Phase2(); Phase3(); Phase4(); }
    public void StepMacro() { do StepMicro(); while (Mpc.Value != 0); }

    public void Reset()
    {
        short[] init = [0, 0, 4095, 0, 0, 0, 1, -1, 0xfff, 0xff, 0, unchecked((short)0x8000), 0, 0, 0, 0];
        for (int k = 0; k < 16; k++) Reg[k] = init[k];
        Mem.Clear(); // MENU.C ResetMac zeroes all memory; OS stub only if MACDOS.MAC missing (LoadOs)
        Mpc.Value = 0;
        Halt = false;
        Keyb.Reset(); Printer.Reset(); Timer.Reset();
        // NOTE: original SYSBUS EnableRD/WR static counters survive RESET
        // (ResetMac never touches them), so _rdTime/_wrTime are NOT cleared.
    }
}
