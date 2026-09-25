
/***************************************************************************/
/* SHIFTER.C                                                               */
/***************************************************************************/

#include "types.h"
#include "macemu.h"
#include "alu.h"
#include "mir.h"
#include "cbus.h"

void LoadShifter(void)
{
  word Result;
  word temp=ReadALU();
  
  switch(ReadMIR(SH))
  {
    case 0: Result=temp;    break;      /* non shifta      */
    case 1: Result=temp>>1; break;      /* shifta destra   */
    case 2: Result=temp<<1; break;      /* shifta sinistra */
    case 3: SetHalt();      break;   /* E' L'ISTRUZIONE HALT!!! */
    
  }
  WriteCBus(Result);
}

