
/***************************************************************************/
/* REGISTRI.C                                                              */
/***************************************************************************/

#include "types.h"
#include "mir.h"
#include "latch.h"
#include "registri.h"
#include "cbus.h"

static word reg[16];   /* contiene i 16 registri dell'emulatore */

void RegistriOutput(void)   /* inizializza i latch in base alla microistruzione corrente */
{
  WriteALatch(reg[ReadMIR(A)]);
  WriteBLatch(reg[ReadMIR(B)]);
}

void RegistriInput(void)   /* salva se abilitato il contenuto del bus c nel */
{                      /* registro specificato nella microistruzione    */
  if (ReadMIR(ENC))
  {
    reg[ReadMIR(C)]=ReadCBus();
  }
}

word ReadRegistri(int r)
{
  return reg[r];
}

void WriteRegistri(int r,short value)
{
  reg[r]=value;
}
