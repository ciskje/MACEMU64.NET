
/***********************************************************************/
/* Printer.c                                                           */
/***********************************************************************/

#include <stdio.h>
#include "visual.h"
#include "types.h"
#include "printer.h"
#include "visual.h"

static word prn[160]; /* E' una word per compatibilita con la struttura dati dell'emulatore */
static int posx=0;

char *ReadPrinter(void)
{
  static char str[160];
  int    i;
  
  for(i=0;i<posx;i++)
  {
    str[i]=prn[i];
  }
  for(i=posx;i<18;i++)   /* pulisce la parte di array non utilizzata */
  {
    str[i]=' ';
  }
  str[posx]=0;
  return str;
}

void ResetPrinter(void)
{
  posx=0;
}

int WritePrinter(address addr,word *a)
{
  if (addr==2)      /* se e' l'indirizzo della stampante */
  {
    if (*a==7)
    {                     /*      */
      Gotoxy(0,0);        /* bell */
      printf("%c\n",7);   /*      */
    }
    else
    {
      if (posx>=15) posx=15;  /* Al 15 carattere il carrello non avanza */
      prn[posx++]=*a;        /* Il carattere viene inserito nel buffer e la posizione del carrello incrementata */
      if (*a==0) posx=0;  /* Se viene inviato 0 alla stampante viene interpretato come un reset */
    }
    return(TRUE);
  }
  else
  return(FALSE);    /* altrimenti risponde FALSE */
}

