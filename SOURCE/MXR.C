
/***************************************************************************/
/* MXR.C                                                                   */
/***************************************************************************/

#include "types.h"
#include "mir.h"
#include "latch.h"
#include "mxr.h"
#include "cbus.h"

static address mar=0;
static word    mbr=0;

address ReadMAR(void)
{
  return mar;
}

word ReadMBR(void)
{
  return mbr;
}

void WriteMBR(word x)
{
  mbr=x;
}

void LoadMBR(void)
{
  if(ReadMIR(MBR))
  mbr=ReadCBus();
}

void LoadMAR(void)
{
  if (ReadMIR(MAR))
  mar=ReadBLatch()&0x0fff;  /* estrazione dei 12 bit dell MAR */
}
