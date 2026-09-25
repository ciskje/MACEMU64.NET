
/***************************************************************************/
/* MACEMU.C                                                                */
/***************************************************************************/

#include <stdio.h>
#include <stdlib.h>
#include <conio.h>
#include <time.h>

#include "types.h"
#include "menu.h"
#include "macemu.h"
#include "visual.h"
#include "cpu.h"
#include "cstore.h"
#include "memoria.h"
#include "registri.h"
#include "keyb.h"
#include "mpc.h"

extern address bp[MAXBP];      /* vettore breakpoint definito in MENU.C */
static int halt;               /* flag di fine programma */
static int rates=8;            /* parametro di refresh video */
static int restart=FALSE;      /* flag per la funzione di RESTART */
static int neverload=TRUE;     /* flag per la funzione di RESTART */

/***************************************************************************/
void main()
{
  SetGraph();
  LoadPic("MACSTART.BCF");
  getch();
  BlankScreen();

  SetText50();             /* modalita' 80x50 */

  DrawText();              /* disegna la grafica di background */
  ResetMac();              /* inizializza l'emulatore */
  Principale();            /* nucleo dell'emulatore */

  SetGraph();
  LoadPic("MACEND.BCF");
  getch();

  SetText50();
  OnCursor();
}



/***************************************************************************/
int ReadRestart(void)
{
  return restart;
}
/***************************************************************************/
void WriteRestart(int a)
{
  restart=a;
}
/***************************************************************************/
void WriteNeverLoad(int a)
{
  neverload=a;
}
/***************************************************************************/
int ReadNeverLoad(void)
{
  return neverload;
}
/***************************************************************************/
void SetRates(int rts)
{
  rates=rts;
}
/***************************************************************************/
int ReadRates(void)
{
  return (rates);
}
/***************************************************************************/
void SetHalt()
{
  halt=TRUE;
}
/***************************************************************************/
void ResetHalt()
{
  halt=FALSE;
}
/***************************************************************************/
void Esecuzione()
{
  int  cont=FALSE;
  int  show=0;
  char c=0;
  int  i=0;

  static int runx=48;       /*  variabili per la gestione */
  int startx=46,endx=65;    /*  dell'effetto 'moving'     */
  int right=TRUE;           /*  durante l'esecuzione del  */
  int superflag=0;          /*  programma MAC             */
  
  Visual();
  Menu_Esecuzione();
  ResetHalt();
  while(c!=ESC)
  {
    if (fastkbhit())   /* se un tasto e' stato premuto */
    {
      c=getch();       /*  leggilo */
      if (c==0) c=getch();  /* se e' speciale (F1,ALT,SHIFT,ecc...), rileggilo */
      if (ReadVideoMode()==MEMDISPLAY)   /* se siamo in grafica  accetta solo i seguenti tasti: */
      {
        switch (c)
        {
          case SPACE:     /* STOP */
          {
            cont=FALSE;
            Visual();
            break;
          }
          case F1:        /* RUN  */
          {
            cont=TRUE;
            break;
          }
          case F2:        /* MICRO BY MICRO */
          {
            cont=FALSE;
            CPU();
            Visual();
            break;
          }
          case F3:        /* MACRO BY MACRO */
          {
            cont=FALSE;
            MacroCPU();
            Visual();
            break;
          }
          case F7:        /* BACK TO TEXT MODE */
          {
            WriteVisual(TEXTDISPLAY);
            Visual();
            Menu_Esecuzione();
            break;
          }
          case ALT_R:
          {
            RequestRates();
            break;
          }
          case ESC:
          {
            if ((ReadVideoMode()!=TEXTDISPLAY)&&(ReadVideoMode()!=NODISPLAY))
            WriteVisual(TEXTDISPLAY);
            break;
          }
          default:
          {
            LoadKeyb(c);
            break;
          }
        }
      }
      else
      {
        switch (c)
        {
          case SPACE:     /* STOP */
          {
            cont=FALSE;
            Visual();
            break;
          }
          case F1:        /* RUN  */
          {
            cont=TRUE;
            break;
          }
          case F2:        /* MICRO */
          {
            cont=FALSE;
            CPU();
            Visual();
            break;
          }
          case F3:        /* MACRO */
          {
            cont=FALSE;
            MacroCPU();
            Visual();
            break;
          }
          case F7:        /* TEXT MODE */
          {
            WriteVisual(TEXTDISPLAY);
            Visual();
            Menu_Esecuzione();
            break;
          }
          case F9:        /* MEM MODE  */
          {
            WriteVisual(MEMDISPLAY);
            Visual();
            break;
          }
          case F10:       /* MAC MODE */
          {
            SwapMemStack();
            Visual();
            break;
          }
          case F8:        /* NO REFRESH */
          {
            WriteVisual(NODISPLAY);
            Visual();
            Menu_Esecuzione();
            Message_No_Display();
            break;
          }
          case F4:        /* MOD REGISTRI */
          {
            Visual();
            Mod_Registri();
            Visual();
            Message_No_Display();
            break;
          }
          case F5:       /*  INS BREAK    */
          {
            InsBreakpoints();
            Message_No_Display();
            Visual();
            break;
          }
          case F6:       /*  DEL BREAK    */
          {
            DelBreakpoints();
            Message_No_Display();
            Visual();
            break;
          }
          case PGUP:
          {
            UpMov();
            Visual();
            break;
          }
          case PGDOWN:
          {
            DownMov();
            Visual();
            break;
          }
          case ALT_A:
          {
            About();
            break;
          }
          case ALT_I:
          {
            SetMode1();
            break;
          }
          case ALT_R:
          {
            RequestRates();
            break;
          }
          case ALT_J:
          {
            RequestLocationJump();
            Visual();
            break;
          }
          case ESC:
          {
            if ((ReadVideoMode()!=TEXTDISPLAY)&&(ReadVideoMode()!=NODISPLAY))
            WriteVisual(TEXTDISPLAY);
            break;
          }
          default:
          {
            LoadKeyb(c);
            break;
          }
        }
      }
    }
    if (cont)  /* se si e' in modo run */
    {

      /* start supercar */
      if (superflag>=350)
      {
        if (runx>=endx)
        {
          right=FALSE;
        }
        if (runx<=startx)
        {
          right=TRUE;
        }

        if (right)
        {
          printc(runx,1,COLOR6," °±²Û");  /* 219 */
          runx++;
        }
        else
        {
          printc(runx,1,COLOR6,"Û²±° ");  /* 219 */
          runx--;
        }

        superflag=0;
      }
      else
      {
        superflag++;
      }
      /* end supercar */
      
      
      CPU();
      
      /* Controllo breakpoint  */
      for(i=0;((i<MAXBP)&&(bp[i]!=NIL));i++)
      {
        if ((ReadRegistri(0)==bp[i])&&ReadMPC()==0)
        {
          cont=FALSE;       /* stop se breakpoint  */
          Visual();         /* aggiorna dati schermo */
        }
      }
      
      /* Le Informazioni sono visualizzate ogni rates microistruzioni */
      if (show++>rates)
      {
        show=0;
        Visual();
      }
    }
    if (halt) /* se il programma e' terminato */
    {
      cont=FALSE;
      Visual();
      Message_End_Prog();
      ResetHalt();
    }
  }
}
/***************************************************************************/
void Principale()
{
  /* vettore degli elementi del menu' */
  char elemento[NUM_ELEMENTI][LUNG_ELEMENTI]={
    {" LOAD PROGRAM     \0"},
    {" LOAD CTRL STORE  \0"},
    {" MODIFY MEMORY    \0"},
    {" MODIFY CTRL STORE\0"},
    {" RUNNING MENU     \0"},
    {" RESTART          \0"},
    {" RESET            \0"},
    {" QUIT             \0"}
  };
  int inc=2;        /* Distanza tra ogni elemento della Win */
  int x0=31;                        /* Coordinate Win.*/
  int y0=24;                        /* Coordinate Win.*/
  int x1=x0+LUNG_ELEMENTI-1;        /* Coordinate Win.*/
  int y1=y0+(NUM_ELEMENTI*inc)+4;   /* Coordinate Win.*/
  
  int ext_sup=y0+3;           /* Coord. estremo sup. elementi Win. princ. */
  int ext_inf=y1-3;           /* Coord. estremo inf. elementi Win. princ. */
  
  int i;                      /* Cursore. */
  
  int coordx=x0+2;            /* Coordinate parziali per la stampa        */
  int coordy=ext_sup;         /* degli elementi Win. princ.               */
  
  int res=0;                  /* Gestione switch per le opzioni possibili */
  int err=ERROR;              /* Gestione errori sui file.                */
  
  
  Win(x0,y0,x1,y1,COLOR13," MAIN MENU ");
  for(i=0; i<=(NUM_ELEMENTI-1); i++)                     /******************/
  { printc(coordx,coordy,COLOR12,*(elemento+i));         /*  Stampa il     */
    coordy+=2;                                           /*  menu'         */
  }                                                      /*                */
  coordy=ext_sup;                                        /*  principale.   */
  printc(coordx,coordy,COLOR7,*(elemento));              /******************/
  
  do
  {
    Visual();
    Visualizza_Bp();
    res=Gest_Key(ext_inf,ext_sup,coordy,coordx,res,*elemento,NUM_ELEMENTI-1,LUNG_ELEMENTI,2);     /* controllo tasti premuti */
    switch (res)   /* esegue funzione relativa */
    {
      case CARICA_PROGRAMMA:
      {
        while (err==ERROR)  err=Ask_Prog();
        Visual();
        coordy=ext_sup+res*inc;
        err=ERROR;
        break;
      }
      case CARICA_CSTORE:
      {
        while (err==ERROR)  err=Ask_Cstore();
        err=ERROR;
        coordy=ext_sup+res*inc;
        break;
      }
      case MODIFICA_MEMORIA:
      {
        Mod_Memoria();
        Visual();
        coordy=ext_sup+res*inc;
        break;
      }
      case MODIFICA_CSTORE:
      {
        Mod_Cstore();
        coordy=ext_sup+res*inc;
        break;
      }
      case MENU_ESECUZIONE:
      {
        Esecuzione();
        Col_Win(30,22,78,46,0xb1,COLOR17);
        coordy=ext_sup+res*inc;
        
        Win(x0,y0,x1,y1,COLOR13," MAIN MENU ");
        for(i=0; i<=(NUM_ELEMENTI-1); i++)
        printc(coordx,ext_sup+i*2,COLOR12,*(elemento+i));
        printc(coordx,coordy,COLOR7,*(elemento+4));
        
        break;
      }
      case RESET:
      {
        ResetMac();
        Message_Reset_Ok();
        Visual();
        coordy=ext_sup+res*inc;
        break;
      }
      case RESTART:
      {
        if (ReadNeverLoad())
        {
          Message_Never_Load();
        }
        else
        {
          ResetMac();
          WriteRestart(TRUE);
          Ask_Prog();
          WriteRestart(FALSE);
        }
        Visual();
        coordy=ext_sup+res*inc;
        break;
      }
      case USCITA:
      {
        coordy=ext_sup+res*inc;
        res=-1;
        break;
      }
    }
  }
  while (res!=-1);
}

