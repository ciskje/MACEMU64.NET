
/**************************************************************************/
/* DISMAC.C                                                               */
/**************************************************************************/

#include <stdio.h>
#include <stdlib.h>

#include "types.h"
#include "dismac.h"

char * disassemble(word hexcode)
{
  static char tmpstr[160];
  int  instruction;
  
  switch(hexcode)
  {
    case 0xf000 : sprintf(tmpstr,"pshi"); break;
    case 0xf200 : sprintf(tmpstr,"popi"); break;
    case 0xf400 : sprintf(tmpstr,"push"); break;
    case 0xf600 : sprintf(tmpstr," pop"); break;
    case 0xf800 : sprintf(tmpstr,"retn"); break;
    case 0xfa00 : sprintf(tmpstr,"swap"); break;
    case 0xfd20 : sprintf(tmpstr," not"); break;
    case 0xfd00 : sprintf(tmpstr,"reti"); break;
    case 0xfd40 : sprintf(tmpstr,"eint"); break;
    case 0xfd80 : sprintf(tmpstr,"dint"); break;
    case 0xffff : sprintf(tmpstr,"halt"); break;
    default :
    {
      instruction = hexcode & TWELVE_BIT_INSTR_MASK;/* mask away 12 low order bits*/
      switch(instruction)
      {
        case 0x0 :
        sprintf(tmpstr,"lodd");
        break;
        case 0x1000 :
        sprintf(tmpstr,"stod");
        break;
        case 0x2000 :
        sprintf(tmpstr,"addd");
        break;
        case 0x3000 :
        sprintf(tmpstr,"subd");
        break;
        case 0x4000 :
        sprintf(tmpstr,"jpos");
        break;
        case 0x5000 :
        sprintf(tmpstr,"jzer");
        break;
        case 0x6000 :
        sprintf(tmpstr,"jump");
        break;
        case 0x7000 :
        sprintf(tmpstr,"loco");
        break;
        case 0x8000 :
        sprintf(tmpstr,"lodl");
        break;
        case 0x9000 :
        sprintf(tmpstr,"stol");
        break;
        case 0xa000 :
        sprintf(tmpstr,"addl");
        break;
        case 0xb000 :
        sprintf(tmpstr,"subl");
        break;
        case 0xc000 :
        sprintf(tmpstr,"jneg");
        break;
        case 0xd000 :
        sprintf(tmpstr,"jnze");
        break;
        case 0xe000 :
        sprintf(tmpstr,"call");
        break;
        default :
        {
          instruction = hexcode & EIGHT_BIT_INSTR_MASK;
          switch(instruction)
          {
            /* mio */  case 0xfb00 :
            sprintf(tmpstr,"andl");
            break;
            case 0xfc00 :
            sprintf(tmpstr,"insp");
            break;
            case 0xfe00 :
            sprintf(tmpstr,"desp");
            break;
            /* revised - 4/91 - SL, SW */
            default :
            {
              sprintf(tmpstr,"????");
              break;
            }
            
          }
        }
        break; /* the first default */
      }
    }
  }
  return(tmpstr);
}

long sign_extend(long temphex,int number_length)
{
  switch (number_length)
  {
    case 4:
    if (temphex >= MIN_4_BIT_NEG_VALUE)
    temphex = temphex | FOUR_BIT_SIGN_EXTEND_MASK;
    break;
    case 16:
    if (temphex >= MIN_16_BIT_NEG_VALUE)
    temphex = temphex | SIXTEEN_BIT_SIGN_EXTEND_MASK;
    break;
  }
  return(temphex);
}
