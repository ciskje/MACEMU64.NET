
/*************************************************************************/
/*  MENU.C                                                               */
/*************************************************************************/

#include <stdio.h>
#include <conio.h>
#include <string.h>
#include <stdlib.h>
#include <dos.h>
#include <ctype.h>

#include "macemu.h"
#include "visual.h"
#include "types.h"

#include "menu.h"
#include "mpc.h"
#include "registri.h"
#include "cstore.h"
#include "memoria.h"
#include "keyb.h"
#include "printer.h"
#include "timer.h"

#ifdef __386__
extern short *video;
extern short *text ;
#else
extern short far *video;
extern short far *text ;
#endif

address bp[MAXBP]={NIL,NIL,NIL,NIL,NIL,NIL,NIL,NIL,NIL,NIL}; /* Vett. di Breakpoint. */

/************** Funzione che disegna una finestra con ombra ****************/
void Win(int x0, int y0, int x1, int y1,int color, char* titolo)
{
  int i,start,lenght;
  Col_Win(x0+1,y0+1,x1+1,y1+1,0xb1,COLOR18);   /* Ombra della Win */
  Col_Win(x0,y0,x1,y1,0,color);
  printc(x0,y0,color,"É");
  printc(x1,y0,color,"»");
  printc(x0,y1,color,"È");
  printc(x1,y1,color,"¼");
  
  for(i = (x0+1); i < x1; i++)
  {
    printc(i,y0,color,"Í");
    printc(i,y1,color,"Í");
  }
  lenght=strlen(titolo);
  start=(x0+1+((x1-x0)/2)-(lenght/2));
  
  printc(start,y0,color,titolo);
  
  for(i = (y0+1); i < y1; i++)
  {
    printc(x0,i,color,"º");
    printc(x1,i,color,"º");
  }
}

/****** Funzione che inserisce caratteri in  una porzione di schermo. ******/
void Col_Win(int x0, int y0, int x1, int y1,int chartype, int color)
{
  int x,y;
  
  for(x=x0;x<=x1;x++)
  {
    for(y=y0;y<=y1;y++)
    {
      text[x+y*80]=chartype|color;
    }
  }
}

/**** Funzione che disegna un tasto ed evidenzia una lettera del tasto. ****/
void Key(int x0,int y0, char testo[],int lettera)
{
  int i,lenght;
  int x=x0;
  char c;
  
  lenght=strlen(testo);
  c=testo[lettera];
  for(i=0;i<lenght;i++)
  {
    printc(x,y0,COLOR15,"%c",*(testo+i));
    x++;
  }
  x=x0;
  printc(x+lettera,y0,COLOR16,"%c",c);
  for (i=0;i<lenght;i++)
  printc(x+i+1,y0+1,COLOR5,"Ü");
  printc(x+lenght,y0,COLOR5,"ß");
}

/************* Funzione per il caricamento programma in Mac1. **************/
int Ask_Prog(void)
{
  int  x0=54,y0=23,x1=77,y1=34;
  int  j,err;
  word b;
  char c,s;
  char name_file[160],tmp[160];
  static char last_file[160];
  
  FILE *fprogram;
  int  ilc;

  Win(x0,y0,x1,y1,COLOR13," LOAD PROGRAM ");
  printc(x0+2,y0+3,COLOR12,"File name:");
  
  if (ReadRestart())
  {
    strcpy(name_file,last_file);
    printc(x0+2,y0+5,COLOR4,"%s",(name_file));
  }
  else
  {
    Scanf(x0+2,y0+5,STR,20,tmp);
    strcpy(name_file,"MACRO\\");
    strcat(name_file,tmp);

  }
  if ((fprogram=fopen(name_file,"r"))==NULL)
  {
    printc(x0+2,y0+7,COLOR14,"File not found!");
    err=ERROR;
    Key(x0+2,y0+9," Cancel ",1);
    Key(x0+14,y0+9," Retry ",1);
    do
    {
      c=toupper(getch());
    }
    while ((c!='C') && (c!='R'));
    switch (c)
    {
      case 'C': {
	err=OK;
	Col_Win(x0,y0,x1+1,y1+1,0xb1,COLOR17);
	return err;
      }
      case 'R': {
	Col_Win(x0+2,y0+12,x1+1,y0+10,0xb1,COLOR17);
	return err;
      }
    }
  }
  else
  {
    err=OK;
    WriteNeverLoad(FALSE);
    strcpy(last_file,name_file);
    Win(x0,y1+3,x1,y1+11,COLOR13," LOAD ");
    printc(x0+2,y1+5,COLOR12,"Loading Mac-1...");
    
    fscanf(fprogram,"%x\n",&ilc);
    WriteRegistri(0,ilc);

    j=ilc;
    if (ReadMemStack()==MEMORY) WriteMov(j+39);
    printc(x0+2,y1+8,COLOR12,"instructions");
    while ((fscanf(fprogram,"%x\n",&b)==1)&&(j<0xfff))
    {
      WriteMemoria(j,&b);
      j++;
      printc(x0+2,y1+7,COLOR12,"Loaded %d",j-ilc);
    }
    if (j>=0xfff) printc(x0+2,y1+9,COLOR14,"Warning! Memory Full");

    fclose(fprogram);
    Key(x0+8,y0+9," Esc ",1);
    do
    {
      s=toupper(getch());
    }
    while ((s!='E') && (s!=ESC));
  }
  Col_Win(x0,y0,x1+1,y1+12,0xb1,COLOR17);
  Visual();
  return err;
}

/************* Funzione per il caricamento programma in Mac1. **************/
void LoadOS(void)
{
  int  x0=54,y0=23,x1=77,y1=34;
  int  j;
  int  ilc;
  
  word b;

  char name_file[160];
  FILE *fprogram;

  strcpy(name_file,"macdos.mac");
  
  Win(x0,y1+3,x1,y1+11,COLOR13," LOAD ");
  
  if ((fprogram=fopen(name_file,"r"))==NULL)
  {
    printc(x0+2,y1+6,COLOR14,"MACDOS.MAC not found",name_file);

    ilc=0xfc;
    WriteMemoria(255,(word *)&ilc);         // 12/11/96
    ilc=0x7000;                   /* LOCO 0 RESET */
    WriteMemoria(252,(word *)&ilc);
    ilc=0x1005;                   /* STOD 5 CSR TIMER */
    WriteMemoria(253,(word *)&ilc);
    ilc=0xfd00;                   /* RETI */
    WriteMemoria(254,(word *)&ilc);

 /* old version
    ilc=0x100;                     // retn  a causa dell' int del TIMER
    WriteMemoria(255,(word *)&ilc);
    ilc=0xf800;
    WriteMemoria(256,(word *)&ilc);
 */
    delay(500);
  }
  else
  {
    printc(x0+4,y1+6,COLOR12,"Loading MacDos...");

    delay(300);
    fscanf(fprogram,"%x\n",&ilc);
    WriteRegistri(0,ilc);

    WriteMemoria(255,(word *)&ilc);   /*   setta INT del S.O.  al valore corrente  */

    j=ilc;
    while(fscanf(fprogram,"%x\n",&b)==1)
    {
      WriteMemoria(j,&b);
      j++;
    }
    fclose(fprogram);
  }
  Col_Win(x0,y0,x1+1,y1+12,0xb1,COLOR17);
}

/************ Funzione per il caricamento della Control-Store. *************/
int Ask_Cstore(void)
{
  int        x0=54,y0=23,x1=77,y1=34;
  FILE       *fcstore;
  char       buf[40];
  char       name_cstore[40];
  char       tmp[40];
  char       c,s;
  int        i,j,err;
  microword  a;

  Win(x0,y0,x1,y1,COLOR13," LOAD CSTORE ");
  printc(x0+2,y0+3,COLOR12,"File name:");
  printc(x0+2,y0+5,COLOR4,"CSTORE.MIC          ");
  strcpy(tmp,"cstore.mic");
  OnCursor();
  Gotoxy(x0+2,y0+5);
  c=getch();
  if (c!=ENTER)
  {
    Scanf(x0+2,y0+5,STR,20,tmp);
  }
  OffCursor();
  strcpy(name_cstore,"MICRO\\");
  strcat(name_cstore,tmp);

  if ((fcstore=fopen(name_cstore,"r"))==NULL)
  {
    printc(x0+2,y0+7,COLOR6,"File not found!");
    err=ERROR;
    Key(x0+2,y0+9," Cancel ",1);
    Key(x0+14,y0+9," Retry ",1);
    do
    {
      c=toupper(getch());
    }
    while ((c!='C') && (c!='R'));
    switch (c)
    {
      case 'C': {
	err=OK;
	Col_Win(x0,y0,x1+1,y1+1,0xb1,COLOR17);
	return err;
      }
      case 'R': {
	Col_Win(x0+2,y0+7,x1+1,y0+10,0xb1,COLOR17);
	return err;
      }
    }
  }
  else
  {
    err=OK;
    Win(x0,y1+3,x1,y1+11,COLOR13," LOAD ");
    printc(x0+2,y1+5,COLOR12,"Loading Ctrl Store...");

    j=0;
    
    while((fscanf(fcstore,"%s\n",buf)==1)&&(j<0xff))
    {
      a=0;
      for(i=0;i<32;i++)
      a|=( (long)(buf[i]-'0') << (31-i));
      WriteCSTORE(a,j);
      j++;
    }
    if (j>=0xff) printc(x0+1,y1+10,COLOR14,"Warning! CSTORE Full!");

    printc(x0+2,y1+7,COLOR12,"Loaded %d",j);
    printc(x0+2,y1+8,COLOR12,"microwords");
    fclose(fcstore);
    Key(x0+8,y0+8," Esc ",1);
    do
    {
      s=toupper(getch());
    }
    while ((s!='E') && (s!=ESC));
  }
  Col_Win(x0,y0,x1+1,y1+12,0xb1,COLOR17);
  return err;
  
}


/************ Funzione per la modifica dello stato dei registri ************/
void Mod_Registri(void)
{
  static char registro[NUM_REGISTRI][LUNG_REGISTRI] = {
    {"PC     : \0"},
    {"AC     : \0"},
    {"SP     : \0"},
    {"IR     : \0"},
    {"TIR    : \0"},
    {"ZERO   : \0"},
    {"UNO    : \0"},
    {"MENOUNO: \0"},
    {"AMASK  : \0"},
    {"SMASK  : \0"},
    {"A      : \0"},
    {"B      : \0"},
    {"C      : \0"},
    {"D      : \0"},
    {"E      : \0"},
    {"F      : \0"}
  };

  int inc=1;            /* Distanza tra ogni elemento della Win */

  int x0=54;                            /* Coordinate Win.*/
  int y0=25;                             /* Coordinate Win.*/
  int x1=x0+9+LUNG_REGISTRI;             /* Coordinate Win.*/
  int y1=(y0+2+NUM_REGISTRI+1);           /* Coordinate Win.*/

  int ext_sup=y0+2;                       /* Coord. ext. sup. elementi Win. */
  int ext_inf=(ext_sup+NUM_REGISTRI*inc-1);   /* Coord. ext. inf. elementi Win. */

  int i;                            /* Cursore per il vettore dei registri. */

  int coordx=x0+2;                /* Coordinate parziali per la stampa    */
  int coordy=ext_sup;            /* degli elementi della Win registri.   */

  int  res=0;                /* Gestione Switch per la scelta del registro.  */
  word value_reg;         /* Memorizza il nuovo contenuto di un registro. */

  Win(x0,y0,x1,y1,COLOR13," MODIFY REGISTER ");
  for(i=0; i<=(NUM_REGISTRI-1); i++)
  {
    printc(coordx,coordy,COLOR12,registro[i]);
    value_reg=ReadRegistri(i);
    printh(coordx+11,coordy,value_reg);
    coordy++;
  }
  coordy=ext_sup;
  printc(coordx,coordy,COLOR7,registro[0]);
  do
  {
    res=Gest_Key(ext_inf,ext_sup,coordy,coordx,res,*registro,NUM_REGISTRI-1,LUNG_REGISTRI,inc);

    if (res!=-1)
    {
      coordy=(ext_sup+res*inc);
      Scanf(coordx+11,coordy,HEX,4,&value_reg);
      WriteRegistri(res,value_reg);
      printh(coordx+11,coordy,value_reg);
      Visual();
    }
  }
  while (res!=-1);
  Col_Win(x0,y0,x1+1,y1+1,0xb1,COLOR17);
}


/* Funzione per la modifica del contenuto di una cella di memoria centrale.*/
void Mod_Memoria(void)
{
  int x0=54,y0=25,x1=77,y1=36;
  word value_mem;
  address ind;

  Win(x0,y0,x1,y1,COLOR13," MODIFY MEMORY ");
  printc(x0+2,y0+2,COLOR12,"Address:");
  do
  {
    Scanf(x0+2,y0+4,HEX,3,&ind);
  }
  while (ind==0xfff);
  printc(x0+2,y0+6,COLOR12,"New value:");
  printc(x0+2,y0+8,COLOR4,"    ");
  Gotoxy(x0+2,y0+8);
  Scanf(x0+2,y0+8,HEX,4,&value_mem);
  WriteMemoria(ind,&value_mem);
  Col_Win(x0,y0,x1+1,y1+1,0xb1,COLOR17); /* v 3.01 */
}


/* Funzione per la modifica del contenuto di una cella della Ctrl_strore.  */
void Mod_Cstore(void)
{
  int x0=54,y0=25,x1=77,y1=36;
  microword value_cstore;
  microaddress ind;

  Win(x0,y0,x1,y1,COLOR13," MODIFY CTRL STORE ");
  printc(x0+2,y0+2,COLOR12,"Address:");
  Scanf(x0+2,y0+4,HEX,2,&ind);
  printc(x0+2,y0+6,COLOR12,"New Value:");
  printc(x0+2,y0+8,COLOR4,"        ");
  Gotoxy(x0+2,y0+8);
  Scanf(x0+2,y0+8,HEX,8,&value_cstore);
  WriteCSTORE(value_cstore,ind);
  Col_Win(x0,y0,x1+1,y1+1,0xb1,COLOR17); /* v 3.01 */
}


/******** Funzione che controlla se un breakpoint e' gia' presente *********/
int Ricerca_Breakpoint(address add)
{
  int i=0;
  int ctrl;

  while ((i<MAXBP-1) && (bp[i]!=add))
  i++;
  if (bp[i]!=add)
  ctrl=OK;
  else ctrl=ERROR;
  return ctrl;
}

/************* Funzione per l'inserimento dei breakpoints. *****************/
void  InsBreakpoints(void)
{
  int x0=54,y0=25,x1=69,y1=34;
  int i;                /* Cursore per il vettore di Breakpoints.          */
  address add;          /* Variabile per la memorizzazione tmp del Breakp. */
  int ctrl;        /* Variabile di controllo sui breakp. inseriti     */
  char c;
  
  i=Visualizza_Bp();
  Win(x0,y0,x1,y1,COLOR13," INSERT ");
  if (i<MAXBP)
  {
    printc(x0+2,y0+2,COLOR12,"Address:");
    do
    {
      Scanf(x0+10,y0+2,HEX,3,&add);
      ctrl=Ricerca_Breakpoint(add);
      switch (ctrl)
      {
	case ERROR: {
	  printc(x0+2,y0+4,COLOR6,"Already Ins");
	  break;
	}
	case OK: {
	  bp[i]=add;
	  i=Visualizza_Bp();
	  Visual();                            /* modificato 27/5/95  */
	}
      }
      if (i<MAXBP)
      {
	Key(x0+2,y0+6,"Ins",0);
	Key(x0+8,y0+6,"Esc",0);
	c=0x57;
	do
	c=toupper(getch());
	while (((c!='E')&&(c!=ESC)) && (c!='I'));
	if (c=='I') Col_Win(x0+1,y0+4,x0+13,y0+7,0,COLOR13);
      }
      else
      {
	Key(x0+5,y0+7," Esc ",1);
	c=0x57;
	do
	c=toupper(getch());
	while ((c!='E') && (c!=ESC));
      }
    }
    while ((c!='E') && (c!=ESC));
    Col_Win(x0,y0,x0+16,y0+12,0xb1,COLOR17);
  }
  else
  {
    Col_Win(x0+1,y0+4,x0+13,y0+7,0,COLOR13);
    printc(x0+2,y0+4,COLOR6,"Memory full");
    printc(x0+2,y0+5,COLOR6,"for new ins");
    Key(x0+5,y0+7," Esc ",1);
    do
    {
      c=toupper(getch());
    }
    while ((c!='E') && (c!=ESC));
    Col_Win(x0,y0,x0+16,y0+12,0xb1,COLOR17);
  }
}

void DelBreakpoints(void)
{
  int x0=54,y0=25,x1=69,y1=34;
  int num_bp=0;
  int i=0;
  char c;
  
  do
  {
    Win(x0,y0,x1,y1,COLOR13," DELETE ");
    if (bp[0]!=NIL)    /* se ci sono Breakpoints allora ... */
    {
      i=Visualizza_Bp();
      printc(x0+2,y0+2,COLOR12,"Number :");
      Scanf(x0+10,y0+2,INT,2,&num_bp);
      if ( (*(bp+num_bp-1)==NIL) || (num_bp>MAXBP) )
      {
	Col_Win(x0+1,y0+4,x0+13,y0+7,0,COLOR13);
	printc(x0+2,y0+4,COLOR6,"Breakpoints");
	printc(x0+2,y0+5,COLOR6,"inexistent!");
	Key(x0+4,y0+7," Esc ",1);
	do
	{
	  c=toupper(getch());
	}
	while ((c!='E') && (c!=ESC));
	Col_Win(x0,y0,x0+16,y0+12,0xb1,COLOR17);
      }
      else
      {
	i=num_bp-1;
	if (i<MAXBP-1)
	{
	  while ( (bp[i+1]!=NIL) && (i<MAXBP-1) )
	  {
	    bp[i]=bp[i+1];
	    i++;
	    if (i==MAXBP-1) break;
	  }
	}
	bp[i]=NIL;

	
	i=Visualizza_Bp();
	if (i>0)
	{
	  Key(x0+2,y0+6,"Del",0);
	  Key(x0+8,y0+6,"Esc",0);
	  c=0x57;
	  Visual();                            /* modificato 27/5/95  */
	  do
	  c=toupper(getch());
	  while (((c!='E')&&(c!=ESC)) && (c!='D'));
	  if (c=='D') Col_Win(x0+1,y0+4,x0+13,y0+7,0,COLOR13);
	}
	else
	{
	  Key(x0+5,y0+7," Esc ",1);
	  Visual();                            /* modificato 27/5/95  */
	  c=0x57;
	  do
	  c=toupper(getch());
	  while ((c!='E') && (c!=ESC));
	}
      }
    }
    else
    {
      Col_Win(x0+1,y0+4,x0+13,y0+7,0,COLOR13);
      printc(x0+2,y0+4,COLOR6,"Memory empty");
      printc(x0+2,y0+5,COLOR6,"for new del!");
      Key(x0+4,y0+7," Esc ",1);
      do
      c=toupper(getch());
      while ((c!='E') && (c!=ESC));
    }
  }
  while ((c!='E')&&(c!=ESC));
  Col_Win(x0,y0,x0+16,y0+12,0xb1,COLOR17);
}

/********* Funzione per la visualizzazione dei breakpoints attivi. *********/
int Visualizza_Bp(void)
{
  int col=8;
  int rig=35;
  int i=0;
  int k=0;
  while ((bp[i]!=NIL) && (i<MAXBP))
  {
    printc(col,rig,COLOR0,"%03X",bp[i]);
    i++;
    rig++;
  }
  for(k=i;k<MAXBP;k++,rig++) printclear(col,rig);
  return i;
}


/************** Funzione per la gestione dei tasti di ogni menu. ***********/
int Gest_Key(
int extinf, int extsup,              /* Estremi di movimento. */
int rig, int col,                    /* Coord. cursore        */
int pos,                             /* Posiz. cursore.       */
char *elemento,                      /* Elementi Win.         */
int num_elementi,                    /* # elementi Win.       */
int ext_elementi,                    /* Lunghezza elementi.   */
int inc
)
{
  char c = 0x57;
  do
  {
    while (!(fastkbhit()));
    c=getch();
    if (c==0) c=getch();

    switch (c)
    {
      case ENTER: break;

      case ESC:
      pos=-1;
      break;

      case ALT_A:
      if (col!=56)
      About();
      break;

      case ALT_I:
      SetMode1();
      break;

      case ALT_R:
      if (col!=56)
      RequestRates();
      break;

      case ALT_J:
      if (col!=56)
      RequestLocationJump();
      break;

      case UP:
      if (rig==extsup)
      {
	printc(col,rig,COLOR12,(elemento+ext_elementi*pos));
	pos=num_elementi;
	rig=extinf;
	printc(col,rig,COLOR7,(elemento+ext_elementi*pos));
      }
      else
      {
	printc(col,rig,COLOR12,(elemento+ext_elementi*pos));
	pos--;
	rig-=inc;
	printc(col,rig,COLOR7,(elemento+ext_elementi*pos));
      }
      break;

      case DOWN:
      if (rig==extinf)
      {
	printc(col,rig,COLOR12,(elemento+ext_elementi*pos));
	pos=0;
	rig=extsup;
	printc(col,rig,COLOR7,(elemento+ext_elementi*pos));
      }
      else
      {
	printc(col,rig,COLOR12,(elemento+ext_elementi*pos));
	pos++;
	rig+=inc;
	printc(col,rig,COLOR7,(elemento+ext_elementi*pos));
      }
      break;
    }
  }
  while ((c!=ESC) && (c!=ENTER));
  return pos;
}

/*************** Funzione che stampa il menu esecuzione ********************/
void Menu_Esecuzione()
{
  int x0=31,y0=25,x1=51,y1=44;
  int coordx=x0+2,coordy=y0+2;

  Col_Win(30,22,78,46,0xb1,COLOR17);
  Win(x0,y0,x1,y1,COLOR13," RUNNING MENU ");
  printc(coordx,coordy++,COLOR12,"SPC STOP");
  printc(coordx,coordy++,COLOR12,"F1  RUN");
  printc(coordx,coordy++,COLOR12,"F2  MICRO");
  printc(coordx,coordy++,COLOR12,"F3  MACRO");
  printc(coordx,coordy++,COLOR12,"F4  MODIFY REG");
  printc(coordx,coordy++,COLOR12,"F5  INS BREAKP.");
  printc(coordx,coordy++,COLOR12,"F6  DEL BREAKP.");
  coordy++;
  printc(coordx,coordy++,COLOR12,"F7  TEXTDISPLAY");
  printc(coordx,coordy++,COLOR12,"F8  NO REFRESH");
  printc(coordx,coordy++,COLOR12,"F9  MEMDISPLAY");
  printc(coordx,coordy++,COLOR12,"F10 MEMORY/STACK");
  coordy++;
  printc(coordx,coordy++,COLOR12,"PGUP/PGDWN SCROLL");
  coordy++;
  printc(coordx,coordy++,COLOR12,"ESC PREVIOUS");
}
/******************************  About... **********************************/
void About(void)
{
	char c=0x57;
	int  x0=34;
	int  x1=73;
	int  y0=23;
	int  y1=45;
	short *memscreen;

	int i=0;
	int x,y;


	if (ReadVideoMode()!=MEMDISPLAY)
	{
		memscreen=(short *)calloc((2+x1-x0)*(2+y1-y0),sizeof(short));

		/*  SaveScreen  */
		for (x=x0;x<=x1+1;x++)
		for (y=y0;y<=y1+1;y++)
		{
			memscreen[i]=text[x+y*80];
			i++;
		}

		Win(x0,y0,x1,y1,COLOR13," ABOUT... ");
		printc(x0+8,y0+2,COLOR12," MACEMU "VER" -  ("COMP")");
		printc(x0+12,y0+4,COLOR12,"Copyright(c) 1996");
		printc(x0+18,y0+6,COLOR12,"by");
		printc(x0+3,y0+8,COLOR12,"F.Ferrara  G.Baragiotta  A.Carrera");
		printc(x0+14,y0+9,COLOR12,"group ARCHA8");
		printc(x0+10,y0+10,COLOR12,"University of Turin");
		printc(x0+6,y0+12,COLOR12,"Computer Science Department");
		printc(x0+4,y0+14,COLOR12,"E-MAIL:");
		printc(x0+3,y0+16,COLOR12,"ferrara.francesco@educ.di.unito.it");
		printc(x0+3,y0+17,COLOR12,"baragiogiotta.luca@educ.di.unito.it");
		printc(x0+3,y0+18,COLOR12,"carreraa.alberto@educ.di.unito.it");

		Key(x0+17,y0+20," Esc ",1);
		do
		c=toupper(getch());
		while ((c!=ESC) && (c!='E'));

		/*  WriteScreen  */
		i=0;
		for (x=x0;x<=x1+1;x++)
		for (y=y0;y<=y1+1;y++)
		{
			text[x+y*80]=memscreen[i];
			i++;
		}
		free(memscreen);
	}
}
/**************************************************************************/
void ResetMac(void)
{
  int k=0;
  word temp=0;
  word regtmp[16]={0,0,4095,0,0,0,1,-1,0xfff,0xff,0,0x8000,0,0,0,0};

  for(k=0;k<16;k++) WriteRegistri(k,regtmp[k]);
  for(k=0;k<4096;k++) WriteMemoria(k,&temp);
  LoadOS();
  WriteMPC(0);

  for(k=0;k<MAXBP;k++) bp[k]=NIL;
  ResetHalt();
  ResetKeyb();
  ResetPrinter();
  ResetTimer();
  Visual();
}
/**************************************************************************/
void Message_No_Display(void)
{
  if (ReadVideoMode()==NODISPLAY)
  {
    Win(54,38,76,42,COLOR13," MESSAGE ");
    Key(56,40,"NO REFRESH DISPLAY",2);
  }
}
/**************************************************************************/
void Message_End_Prog(void)
{
  char c=0x57;
  if (ReadVideoMode()!=MEMDISPLAY)
  {
    Win(54,38,76,44,COLOR13," MESSAGE ");
    Key(56,40,"PROGRAM TERMINATED",7);
    Key(63,42," Esc ",1);
    do
      c=toupper(getch());
    while ((c!='E') && (c!=ESC));
    Col_Win(54,38,77,45,0xb1,COLOR17);
  }
  else
  {
    Blinking();
    do
    c=toupper(getch());
    while ((c!='E') && (c!=ESC));
    Blinking();
  }
}
/**************************************************************************/
void Message_Reset_Ok(void)
{
  char c=0x57;
  Win(55,38,76,44,COLOR13," MESSAGE ");
  Key(56,40," RESET TERMINATED!",0);
  Key(64,42," Esc ",1);
  do
  c=toupper(getch());
  while ((c!='E') && (c!=ESC));
  Col_Win(55,38,77,45,0xb1,COLOR17);
}
/**************************************************************************/
void Message_Never_Load()
{
  char c=0x57;
  Win(55,38,76,44,COLOR13," MESSAGE ");
  Key(56,40," NEVER FILE LOADED!",0);
  Key(64,42," Esc ",1);
  do
  c=toupper(getch());
  while ((c!='E') && (c!=ESC));
  Col_Win(55,38,77,45,0xb1,COLOR17);
}
/**************************************************************************/
void RequestRates(void)
{
  int x0=54,y0=25,x1=69,y1=34;
  int rates;
  Win(x0,y0,x1,y1,COLOR13," SET RATES ");
  printc(x0+2,y0+2,COLOR13,"New Rates:");
  do
  {
   Scanf(x0+2,y0+4,INT,4,&rates);
  }
  while (rates==0);
  SetRates(rates);
  Col_Win(x0,y0,x1+1,y1+1,0xb1,COLOR17); /* v 3.01 */
  printc(9,46,COLOR6,"%4d",rates);
}
/**************************************************************************/
void RequestLocationJump(void)
{
  int x0=54,y0=25,x1=69,y1=34;
  int address=-1;
  Win(x0,y0,x1,y1,COLOR13," SET ADDRESS ");
  printc(x0+2,y0+2,COLOR13,"Jump to:");
  do
  {
    Scanf(x0+2,y0+4,HEX,3,&address);
  }
  while (address==0xFFF);
  if (!(address<0 || address>4095))
  WriteMov(address);
  Visual();
  Col_Win(54,25,70,35,0xb1,COLOR17);
}





