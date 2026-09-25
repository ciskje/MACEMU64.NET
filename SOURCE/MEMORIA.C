
/***************************************************************************/
/* MEMORIA.C                                                               */
/***************************************************************************/

#include "types.h"
#include "mir.h"
#include "mxr.h"
#include "memoria.h"
#include "sysbus.h"

static word mem[4096]; /* memoria dell'emulatore */

word *ReadPmem(void)   /* legge il puntatore alla memoria: e' utilizzato */
{         /* esclusivamente per velocizzare la grafica      */
  return mem;
}

int WriteMemoria(address addr,word *a)
{
  if (addr>=NUMCTRL*2)           /* controlla che si scriva nelle    */
  {                              /* locazioni riservate alla memoria */
    mem[addr]=*a;
    return(TRUE);
  }
  else
  return(FALSE);
}

int ReadMemoria(address addr,word *a)
{
  if (addr>=NUMCTRL*2)            /* controlla che si legga nelle     */
  {                               /* locazioni riservate alla memoria */
    *a=mem[addr];
    return(TRUE);
  }
  else
  return(FALSE);
}

