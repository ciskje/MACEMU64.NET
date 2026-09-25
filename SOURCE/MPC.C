
/***************************************************************************/
/* MPC.C                                                                   */
/***************************************************************************/

#include "types.h"
#include "mpc.h"

static microaddress MPC=0;   /* inizializzazione  MPC */

void WriteMPC(microaddress ma)
{
  MPC=ma;
}

microaddress ReadMPC(void)
{
  return MPC;
}
