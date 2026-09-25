
/*************************************************************************/
/* SYSBUS.C                                                              */
/*************************************************************************/

#include "types.h"
#include "mir.h"
#include "memoria.h"
#include "keyb.h"
#include "printer.h"
#include "mxr.h"
#include "sysbus.h"
#include "registri.h"
#include "timer.h"

/* array di puntatori alle funzioni dei dispositivi presenti (lettura) */
int (* DevRead[NUMCTRL])(address,word *) =
{
  ReadMemoria, ReadKeyb,   null,          null,       null,null,null,null
};

/* array di puntatori alle funzioni dei dispositivi presenti (scrittura) */
int (* DevWrite[NUMCTRL])(address,word *) =
{
  WriteMemoria,WriteKeyb,  WritePrinter,  WriteTimer, null,null,null,null
};
/* array di puntatori alle funzioni dei dispositivi presenti (interrupt) */
int (* DevIp[NUMCTRL])(void) =
{
  nullip,      IpKeyb,     nullip,        IpTimer,    nullip,nullip,nullip,nullip
};

int EnableRD()
{
  static int time=0;
  
  if (ReadMIR(RD)) time++;
  if (time==2)
  {
    time=0;
    return(TRUE);
  }
  else
  {
    return(FALSE);
  }
}

int EnableWR()
{
  static int time=0;
  
  if (ReadMIR(WR)) time++;
  if (time==2)
  {
    time=0;
    return(TRUE);
  }
  else
  {
    return(FALSE);
  }
}

void SysBus(void)
{
  int i=0,j;
  word temp=0;
  short newf;
  
  if (EnableRD())      /* legge */
  {
    for(i=0;(i<NUMCTRL)&&((DevRead[i](ReadMAR(),&temp)==FALSE));i++);
    /* chiama tutti i dispositivi e il primo che risponde scrive in temp */
    if (i<NUMCTRL) WriteMBR(temp);  /* se qualcuno ha risposto, il valore ritornato */
  }
  if (EnableWR())
  {
    temp=ReadMBR();
    /* vedi sopra... */
    for(i=0;(i<NUMCTRL)&&((DevWrite[i](ReadMAR(),&temp)==FALSE));i++);
  }
  
  /* controllo interrupt pendenti: */
  j=1;
  newf=ReadRegistri(15); /* memorizza il contenuto del registro  F */
  for(i=0;i<NUMCTRL;i++)
  {
    if (DevIp[i]())   /* controlla se c'e' un interrupt pendente in ogni dispositivo */
    {
      newf|= j;         /* setta a uno il bit corrispondente di ogni dispositivo */
    }
    else
    {
      newf&= ~j;        /* setta a zero il bit corrispondente di ogni dispositivo */
    }
    
    j<<=1;            /* shifta a sinistra di uno per poter settare eventualmente il bit */
  }                   /* di interrupt del dispositivo successivo */
  WriteRegistri(15,newf);   /* memorizza le modifiche in F */
}

int null(address a,word *t)
{
  a=a;t=t; /* per non avere il warning di parametro non utilizzato */
  return (FALSE);
}

int nullip(void)
{
  return(FALSE);
}













