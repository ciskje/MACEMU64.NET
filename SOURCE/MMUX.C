
/***************************************************************************/
/* MMUX.C                                                                  */
/***************************************************************************/

#include "types.h"
#include "mmux.h"
#include "mir.h"
#include "msl.h"
#include "mpc.h"


void Mmux(void)
{
  if (ReadMSL())            /* controlla se c'e un salto da effettuare */
  WriteMPC(ReadMIR(ADDR));  /* se c'e salta all'indirizzo specificato nel MIR */
  else
  WriteMPC(ReadMPC()+1);    /* se non c'e' carica l'istruzione seguente */
}
