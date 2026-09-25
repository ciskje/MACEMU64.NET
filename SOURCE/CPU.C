
/***************************************************************************/
/* CPU.C                                                                   */
/***************************************************************************/

#include "types.h"
#include "cpu.h"
#include "mir.h"
#include "alu.h"
#include "mxr.h"
#include "shifter.h"
#include "registri.h"
#include "memoria.h"
#include "amux.h"
#include "mmux.h"
#include "mpc.h"
#include "cstore.h"
#include "sysbus.h"
#include "keyb.h"
#include "timer.h"

void CPU(void)
{
  Phase1();
  Phase2();
  Phase3();
  Phase4();
}

void MacroCPU(void)
{
  do
  {
    CPU();
  } while(ReadMPC()!=0);
}

void Phase1(void)
{
  LoadMIR();
}

void Phase2(void)
{
  RegistriOutput();
}

void Phase3(void)
{
  LoadMAR();
  LoadALU();
  LoadShifter();
}

void Phase4(void)
{
  LoadMBR();
  RegistriInput();
  Mmux();
  SysBus();
  Keyb();
  Timer();
}



