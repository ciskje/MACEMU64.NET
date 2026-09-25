
/***************************************************************************/
/* LATCH.C                                                                 */
/***************************************************************************/

#include "types.h"
#include "latch.h"

static word alatch;
static word blatch;

void WriteALatch(word a)
{
  alatch=a;
}
void WriteBLatch(word b)
{
  blatch=b;
}

word ReadALatch(void)
{
  return alatch;
}
word ReadBLatch(void)
{
  return blatch;
}
