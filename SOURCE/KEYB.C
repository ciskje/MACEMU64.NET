
/***************************************************************************/
/* KEYB.C                                                                  */
/***************************************************************************/

#include "types.h"
#include "conio.h"
#include "keyb.h"

static word br=0,csr=0;
static word car=-1;

/* car==-1 non ci sono caratteri premuti rifiutati dal menu running  */
/* car!=-1 allora car contiene un carattere da far vedere al sistema */

void LoadKeyb(word c)
{
  car=c;
}

int IpKeyb(void)
{
  if ((csr&IP)!=0)   /* c'e un'interruzione pendente */
  return TRUE;
  else
  return FALSE;
}

void ResetKeyb(void)
{
  br=0;
  csr=0;
}

int WriteKeyb(address addr,word *a)
{
  if (addr==0)               /* write br of keyboard  */
  {
    br=*a;
    return(TRUE);
  }
  if (addr==1)               /* write csr of keyboard */
  {
    csr=*a;
    return(TRUE);
  }
  return(FALSE);             /* se l'address passato alla funzione non e' */
  /* il suo, risponde FALSE */
}

int ReadKeyb(address addr,word *a)
{
  if (addr==0)                 /* read br od keyboard */
  {
    *a=br;
    return(TRUE);
  }
  if (addr==1)                 /* read csr od keyboard */
  {
    *a=csr;
    return(TRUE);
  }
  return(FALSE);               /* se l'address passato alla funzione non e' */
  /* il suo, risponde FALSE */
}

void Keyb(void)
{
  if (car!=-1)        /* equivalente di fastkbhit */
  {
    csr|=IP;          /* csr=csr or IP,cioe' alza il bit meno signific. del csr (C'e' un interrupt) */
    if ((csr&RDY)==0) /* Il bit RDY e' ancora alto? */
    {
      br=car;         /* mette il carattere in br */
      csr|=RDY;       /* csr=csr or RDY cioe'alza il bit + signific. del csr */
      car=-1;         /* azzera */
    }
    else
    {
      csr|=1<<2;     /* csr=csr or 2 ovvero */
      /* alza il secondo bit meno signific. del csr (Errore: Inserito codice 1 in csr) */
      
      car=-1;        /* azzera la virtual fastkeyb */
    }
  }
}

