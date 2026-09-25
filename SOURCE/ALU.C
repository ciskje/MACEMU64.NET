
/***************************************************************************/
/* ALU.C                                                                   */
/***************************************************************************/

#include "types.h"
#include "mir.h"
#include "latch.h"
#include "amux.h"
#include "alu.h"

static word Result;   /* contiene il l'output dell'ALU */

void LoadALU(void)
{
  word a=ReadAmux();
  word b=ReadBLatch();
  
  switch(ReadMIR(ALU))    /* legge dal MIR l'operazione da eseguire */
  {
    case 0: Result = a+b;   break;
    case 1: Result = a & b; break;
    case 2: Result = a;     break;
    case 3: Result = ~a;    break;
  }
}

word ReadALU(void)
{
  return Result;
}

