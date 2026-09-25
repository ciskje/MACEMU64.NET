
/***************************************************************************/
/* AMUX.C                                                                  */
/***************************************************************************/

#include "types.h"
#include "mir.h"
#include "latch.h"
#include "amux.h"
#include "mxr.h"

word ReadAmux(void)
{
  if (ReadMIR(AMUX))
  return ReadMBR();
  else
  return ReadALatch();
}
