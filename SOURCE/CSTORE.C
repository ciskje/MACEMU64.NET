
/***************************************************************************/
/* CSTORE.C                                                                */
/***************************************************************************/

#include "types.h"
#include "mpc.h"
#include "cstore.h"

static microword cstore[256];    /* contiene il microprogramma in MIC-1 */

void WriteCSTORE (microword istr,microaddress ma)
{
  cstore[ma]=istr;
}

microword ReadCSTORE (void)
{
  return cstore[ReadMPC()];
}
