
/***************************************************************************/
/* TIMER.C                                                                  */
/***************************************************************************/

#include "types.h"
#include "timer.h"

static word period=1000,csr=0;

int IpTimer(void)
{
  if ((csr&IP)!=0)   /* c'e un'interruzione pendente */
  return TRUE;
  else
  return FALSE;
}

void ResetTimer(void)
{
  period=1000;
  csr=0;
}

int WriteTimer(address addr,word *a)
{
  if (addr==4)               /* write period of timer  */
  {
    period=*a;
    return(TRUE);
  }
  if (addr==5)               /* write csr of keyboard */
  {
    csr=*a;
    return(TRUE);
  }
  return(FALSE);
  /* se l'address passato alla funzione non e' */
  /* il suo, risponde FALSE */
}

int ReadTimer(address addr,word *a)
{
  if (addr==4)                 /* read br of timer */
  {
    *a=period;
    return(TRUE);
  }
  if (addr==5)                 /* read csr of timer */
  {
    *a=csr;
    return(TRUE);
  }
  return(FALSE);
  /* se l'address passato alla funzione non e' */
  /* il suo, risponde FALSE */
}

void Timer(void)
{
  static int time=0;
  
  time++;
  if (time>period)
  {
    time=0;
    csr|=IP;          /* csr=csr or IP,cioe' alza il bit meno signific. del csr (C'e' un interrupt) */
  }
}


