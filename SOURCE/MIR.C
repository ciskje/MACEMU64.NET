
/***************************************************************************/
/* MIR.C                                                                   */
/***************************************************************************/

#include "types.h"
#include "cstore.h"
#include "mir.h"


/*
mir[] e' un array che contiene tutti i campi decodificati del mir
dovrebbe essere un array di int, e' dichiarato microword per compatibilita'
con le operazioni di decodifica seguenti
*/

static microword mir[13];   /* suddivisione dei campi della microistruzione */

unsigned int ReadMIR(int campo)   /* ritorna il contenuto del campo specificato */
{
  return(unsigned int)mir[campo];
}

void LoadMIR(void)    /* carica una microistruzione dalla CSTORE */
{
  microword  temp;    /* e ne separa i campi */
  temp=ReadCSTORE();
  
  mir[0]=temp&0xff;   /*  estrae gli 8 bit meno significativi (campo addr) */
  temp>>=8;           /*  cancella da temp gli 8 bit estratti.  */
  mir[1]=temp&0xf;    /* ... */
  temp>>=4;
  mir[2]=temp&0xf;
  temp>>=4;
  mir[3]=temp&0xf;
  temp>>=4;
  mir[4]=temp&0x1;
  temp>>=1;
  mir[5]=temp&0x1;
  temp>>=1;
  mir[6]=temp&0x1;
  temp>>=1;
  mir[7]=temp&0x1;
  temp>>=1;
  mir[8]=temp&0x1;
  temp>>=1;
  mir[9]=temp&0x3;
  temp>>=2;
  mir[10]=temp&0x3;
  temp>>=2;
  mir[11]=temp&0x3;
  temp>>=2;
  mir[12]=temp&0x1;
}
