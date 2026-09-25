
/**********************************************************************/
/* SYSBUS.H                                                           */
/**********************************************************************/

#define NUMCTRL  8

int  null(address a,word *t);
int  nullip(void);
void SysBus(void);
int  EnableRD(void);
int  EnableWR(void);
