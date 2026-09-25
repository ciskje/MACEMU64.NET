
/***************************************************************************/
/* CBUS.C                                                                  */
/***************************************************************************/

#include "types.h"
#include "cbus.h"

static word cbus;

void WriteCBus(word c)
{
  cbus=c;
}

word ReadCBus(void)
{
  return cbus;
}
