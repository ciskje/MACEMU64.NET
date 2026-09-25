
/***************************************************************************/
/* MSL.C                                                                   */
/***************************************************************************/

#include "types.h"
#include "msl.h"
#include "alu.h"
#include "mir.h"

int ReadMSL(void)
{
  int temp;
  
  switch(ReadMIR(COND))     /* carica campo COND della microistruzione */
  {
    case 0:                 /* non fa niente */
    temp= FALSE;
    break;
    case 1:       /* salta se negativo */
    temp= ((ReadALU()&0x8000)==0x8000);
    break;
    case 2:                 /* salta se zero */
    temp= (ReadALU()==0);
    break;
    case 3:                 /* salta sempre */
    temp= TRUE;
    break;
  }
  return(temp);
}


