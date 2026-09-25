
/***************************************************************************/
/* VISUAL.C                                                                */
/***************************************************************************/

#include <stdio.h>
#include <conio.h>
#include <dos.h>
#include <stdarg.h>
#include <string.h>
#include <stdlib.h>
#include <ctype.h>

#ifdef __386__
#include <i86.h>
#else
#include <alloc.h>
#endif

#include "menu.h"
#include "types.h"
#include "macemu.h"
#include "registri.h"
#include "mxr.h"
#include "memoria.h"
#include "mpc.h"
#include "cstore.h"
#include "dismac.h"
#include "printer.h"
#include "sysbus.h"
#include "keyb.h"

#include "visual.h"

#ifdef __386__
short *video=(short *)0xa0000;
short *text =(short *)0xb8000;
#else
short far *video=(short far *)0xa0000000L;
short far *text =(short far *)0xb8000000L;
#endif

static int mode=TEXTDISPLAY;    /* serve a settare il modo video */
static int mode1=HEX;           /* visualizzazione in HEX/DEC dei registri */
static int mov=4094;            /* indice di partenza della parte di memoria visualizzata */
static int memstack=STACK;      /* flag di settaggio per la visualizzazione di STACK/MEMORY */

/***************************************************************************/
void SetMode1()
{
  if (mode1==HEX)
  {
    mode1=DEC;
  }
  else
  {
    mode1=HEX;
  }
  Visual();
}
/***************************************************************************/
int ReadMemStack()
{
  return memstack;
}
/***************************************************************************/
void SwapMemStack()
{
  static int posmem=295;
  static int posstack=4094;
  int i=0;
  
  if (memstack==STACK)
  {
    posstack=mov;
    memstack=MEMORY;
    mov=posmem;
    printc(15,3,COLOR6,"MEMORY");
  }
  else
  {
    posmem=mov;
    memstack=STACK;
    mov=posstack;
    printc(15,3,COLOR6,"STACK ");
    for(i=5;i<45;i++)
    printc(24,i,COLOR0,"     ");
  }
}
/***************************************************************************/
int ReadVideoMode(void)
{
  return (mode);
}
/***************************************************************************/
void DownMov(void)
{
  if (mov<4094)
  mov+=15;
}
/***************************************************************************/
void UpMov(void)
{
  if (mov>15)
  mov-=15;
}
/***************************************************************************/
void WriteMov(int addr)
{
  mov=addr;
}
/***************************************************************************/
/*  Si occupa dell'aggiornamento dei dati sullo schermo in ai parametri settati */
void Visual(void)
{
  int  y=6,i;
  int  sp=4095;
  word  *pmem,temp;
  word tmp;
  /* controlla e visualizza modo video */
  switch (mode)
  {
    case NODISPLAY:  /* visualizza  solo PC */
    printh(8,6,ReadRegistri(0));
    break;
    case TEXTDISPLAY:
    /* controlla e visualizza HEX/DEC */
    switch (mode1)
    {
      case HEX:
      for(i=0;i<3;i++)
      {
        printclear(9,y+i);     /* Cancella eventuale decimale */
        printc(5,y+i,COLOR0," ");
      }
      for(i=0;i<16;i++)
      {
        printh(7,y++,ReadRegistri(i));
      }
      printc(19,46,COLOR1,"%4X",mov);
      break;
      case DEC:
      for(i=0;i<3;i++)
      {
        printc(5,y,COLOR6,"D");
        printc(7,y++,COLOR0,"%6d",ReadRegistri(i));
      }
      for(i=3;i<16;i++)
      printh(7,y++,ReadRegistri(i));
      printc(19,46,COLOR1,"%4d",mov);
      break;
    }
    printh(8,25,ReadMAR());
    printh(8,26,ReadMBR());
    printh(8,27,ReadMPC());
    printb(30,5,ReadCSTORE());
    ReadKeyb(0,&tmp);
    printh(41,19,tmp);
    ReadKeyb(1,&tmp);
    printh(41,20,tmp);

    sp=ReadRegistri(2);    /* SP = 2 */
    /* controlla e visualizza STACK/MEMORY  */
    switch (memstack)
    {
      case STACK:
      {
        for(i=mov;i>mov-40;i--)
        {
          if (sp>i)
          {
            printc(15,45-(mov+1-i),COLOR0,"   ");
            printclear(19,45-(mov+1-i));
          }
          else
          {
            ReadMemoria(i,&temp);
            printc(15,45-(mov+1-i),COLOR8,"%03X",i);
            printh(19,45-(mov+1-i),temp);
          }
        }
        break;
      }
      case MEMORY:
      {
        for(i=mov;i>mov-40;i--)
        {
          if (i==ReadRegistri(0))
          printc(28,45-(mov+1-i),COLOR4,"ฎ");
          else
          printc(28,45-(mov+1-i),COLOR4," ");
        }
        for(i=mov;i>mov-40;i--)
        {
          if (i>=NUMCTRL*2 && i<=4095)
          {
            ReadMemoria(i,&temp);
            printc(15,45-(mov+1-i),COLOR8,"%03X",i);
            printh(19,45-(mov+1-i),temp);

            if (Ricerca_Breakpoint(i))
            printc(24,45-(mov+1-i),COLOR9,"%s",disassemble(temp));
            else
            printc(24,45-(mov+1-i),COLOR6,"%s",disassemble(temp));
          }
          else if (i>=0 && i<NUMCTRL*2)
          {
            printc(15,45-(mov+1-i),COLOR8,"%03X",i);
            printc(19,45-(mov+1-i),COLOR1,"----");
            printc(24,45-(mov+1-i),COLOR6,"I/O ");
          }
          else
          {
            printc(15,45-(mov+1-i),COLOR0,"   ");
            printc(19,45-(mov+1-i),COLOR1,"    ");
            printc(24,45-(mov+1-i),COLOR6,"    ");
          }
        }
        break;
      }
    }
    printc(63,48,COLOR1,"%-16s",ReadPrinter());
    break;
    
    case MEMDISPLAY:
    {
      int x,y,sum;
      
      sum=48+68*160;
      pmem=ReadPmem();
      for(y=0;y<64;y++)
      for(x=0;x<64;x++)
      video[sum+x+y*160]=*(pmem++);
    }
    break;
  }
}

void WriteVisual(int m)
{
  mode=m;
  switch(mode)
  {
    case TEXTDISPLAY:
    SetText50();
    DrawText();
    Visualizza_Bp();
    break;
    case NODISPLAY:
    SetText50();
    DrawText();
    Visualizza_Bp();
    break;
    case MEMDISPLAY:
    SetGraph();
    LoadPic("MEMVIEW.BCF");
    break;
  }
}
/***************************************************************************/
/* CARICA immagini TGA 8 bit uncompressed  con header modificato */
void LoadPic(char *fname)
{
  FILE     *fi;
  int      c,y,x;
  unsigned int  r=0,g=0,b=0;
  short   k[160];
  
  fi=fopen(fname,"rb");
  
  if ((fi=fopen(fname,"rb"))==NULL)
  {
    SetText50();
    printc(0,0,COLOR14,"File not found! (%s)",fname);
    Gotoxy(0,1); OnCursor();
    exit(1);
  }
  fseek(fi,18,SEEK_SET);
  outp(0x3c8,0);
  for(c=0;c<256;c++)
  {
    fread(&b,1,1,fi); b>>=2;
    fread(&g,1,1,fi); g>>=2;
    fread(&r,1,1,fi); r>>=2;

    outp(0x3c9,r);
    outp(0x3c9,g);
    outp(0x3c9,b);
  }
  for(y=199;y>=0;y--)
  {
    fread(k,160,2,fi);
    for(x=0;x<160;x++)
      video[x+y*160]=k[x];
  }
  fclose(fi);
}
/***************************************************************************/
/* SFUMA la schermata corrente in nero */
void BlankScreen(void)
{
  int  pal[768];
  int   i,j,k=0;
  unsigned l;
  
  outp(0x3c7,0);
  for(i=0;i<256;i++)
  {
    pal[k++]=inp(0x3c9);
    pal[k++]=inp(0x3c9);
    pal[k++]=inp(0x3c9);
  }
  
  for(j=0;j<100;j++)
  {
    k=0;
    while((inp(0x3da)&8)==0);
    outp(0x3c8,0);
    
    for(i=0;i<256;i++)
    {
      outp(0x3c9,pal[k]);
      if (--pal[k]<0) pal[k]=0; k++;
      outp(0x3c9,pal[k]);
      if (--pal[k]<0) pal[k]=0; k++;
      outp(0x3c9,pal[k]);
      if (--pal[k]<0) pal[k]=0; k++;
    }
  }
  for(l=0;l<0x7fff;l++) video[l]=0;
  
}
/***************************************************************************/
/* ALTERA il colore della palette */
void Blinking(void)
{
  int r,g,b;
  int i;
  
  for(i=0;i<256;i++)
  {
    outp(0x3c7,i);
    r=inp(0x3c9);
    g=inp(0x3c9);
    b=inp(0x3c9);
    
    outp(0x3c8,i);
    outp(0x3c9,63-r);
    outp(0x3c9,63-g);
    outp(0x3c9,63-b);
  }
}
/**************************************************************************/
/* printc = gotoxy+color+printf  */
void printc(int x,int y,unsigned int color,char *format,...)
{
  va_list  marker;
  unsigned char str[81];
  int      i,sum;
  
  va_start(marker,format);
  vsprintf(str,format,marker);
  va_end(marker);
  sum=x+y*80;
  for(i=0;i<strlen(str);i++)
  {
    text[i+sum]=color+str[i];
  }
}
/**************************************************************************/
/* printh = gotoxy+printf("%04X")  */
void printh(int x,int y,int value)
{
  int sum=x+y*80;
  int temp;
  
  temp=(value>>12)&0xf;
  text[sum++]=(temp>9 ? (temp+'A')-10 : (temp+'0'))+COLOR0;
  temp=(value>>8)&0xf;
  text[sum++]=(temp>9 ? (temp+'A')-10 : (temp+'0'))+COLOR0;
  temp=(value>>4)&0xf;
  text[sum++]=(temp>9 ? (temp+'A')-10 : (temp+'0'))+COLOR0;
  temp=value&0xf;
  text[sum++]=(temp>9 ? (temp+'A')-10 : (temp+'0'))+COLOR0;
}
/**************************************************************************/
/* printb = gotoxy+printf("%32b")  b=binario */
void printb(int x,int y,microword a)
{
  int sum=x+y*80+32;
  int i;
  
  for(i=0;i<32;i++)
  {
    text[sum--]='0'+(a&1)+COLOR0;
    a>>=1;
  }
}
/**************************************************************************/
/* printclear = gotoxy+printf("    ")    */
void printclear(int x,int y)
{
  int sum=x+y*80;
  
  text[sum++]=' '+COLOR0;
  text[sum++]=' '+COLOR0;
  text[sum++]=' '+COLOR0;
  text[sum++]=' '+COLOR0;
}
/****************************************************************************/
void DrawText()
{
  int y=0;
  SetText50();
  printc(0,y++,COLOR1,"ษอออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออหออออออออป");
  printc(0,y++,COLOR1,"บ MACEMU "VER"   ("COMP")  -  Copyright(c) 1996                         บ bout...บ");
  printc(0,y++,COLOR1,"ฬออออออออออออหอออออออออออออออหออออออออออออออออออออออออออออออออออออออออสออออออออน");
  printc(0,y++,COLOR1,"บ REGISTRI   บ STACK         บ MIR                                             บ");
  printc(0,y++,COLOR1,"ฬออออออออออออฮอออออออออออออออฮอออออออออออออออออออออออออออออออออออออออออออออออออน");
  printc(0,y++,COLOR1,"บ            บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ PC  :      บ               ฬอ|[][][]|||||[  ][  ][  ][      ]ออออออออออออออออน");
  printc(0,y++,COLOR1,"บ AC  :      บ               บ ณ ณ ณ ณณณณณณ  ณ   ณ   ณ     ณ                   บ");
  printc(0,y++,COLOR1,"บ SP  :      บ               บ ณ ณ ณ ณณณณณณ  C   B   A   ADDR                  บ");
  printc(0,y++,COLOR1,"บ IR  :      บ               บ A C A Sณณณณณ                                    บ");
  printc(0,y++,COLOR1,"บ TIR :      บ               บ M O L HณณณณภฤENC                                บ");
  printc(0,y++,COLOR1,"บ ZERO:      บ               บ U N U IณณณภฤฤWR    ฺฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤบ");
  printc(0,y++,COLOR1,"บ UNO :      บ               บ X D   FณณภฤฤฤRD    ณCOND 00 No jump             บ");
  printc(0,y++,COLOR1,"บ -UNO:      บ               บ       Tณณ          ณ     01 jump if n           บ");
  printc(0,y++,COLOR1,"บ AMSK:      บ               บ       EณภฤฤฤฤMAR   ณ     10 jump if z           บ");
  printc(0,y++,COLOR1,"บ SMSK:      บ               บ       RภฤฤฤฤฤฤBR   ณ     11 jump always         บ");
  printc(0,y++,COLOR1,"บ A   :      บ               ฬออออออออออออออออป   ณ                            บ");
  printc(0,y++,COLOR1,"บ B   :      บ               บKEYBOARD DEVICE บ   ณALU 00 sum SHIFTER 00 id    บ");
  printc(0,y++,COLOR1,"บ C   :      บ               ฬออออออออออออออออน   ณ    01 and         01 right บ");
  printc(0,y++,COLOR1,"บ D   :      บ               บBR :            บ   ณ    10 id          10 left  บ");
  printc(0,y++,COLOR1,"บ E   :      บ               บCSR:            บ   |    11 not         11 halt  บ");
  printc(0,y++,COLOR1,"บ F   :      บ               ฬออออออออออออออออสออออออออออออออออออออออออออออออออน");
  printc(0,y++,COLOR1,"บ            บ               บ                                                 บ");
  printc(0,y++,COLOR1,"ฬออออออออออออน               บ                                                 บ");
  printc(0,y++,COLOR1,"บ            บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ MAR :      บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ MBR :      บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ MPC :      บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ            บ               บ                                                 บ");
  printc(0,y++,COLOR1,"ฬออออออออออออน               บ                                                 บ");
  printc(0,y++,COLOR1,"บ BREAKPOINT บ               บ                                                 บ");
  printc(0,y++,COLOR1,"ฬออออออออออออน               บ                                                 บ");
  printc(0,y++,COLOR1,"บ            บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ Num   Addr บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ            บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ 01  :      บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ 02  :      บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ 03  :      บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ 04  :      บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ 05  :      บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ 06  :      บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ 07  :      บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ 08  :      บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ 09  :      บ               บ                                                 บ");
  printc(0,y++,COLOR1,"บ 10  :      บ    บ    บ     บ                                                 บ");
  printc(0,y++,COLOR1,"ฬออออออออออออนศอออสออออสออออผบ                                                 บ");
  printc(0,y++,COLOR1,"บREFRESH:    บ ADD      INST บ                                                 บ");
  printc(0,y++,COLOR1,"ฬออออออออออออสอออออออออออออออสออออออออออออออออออออออออหออออออออออออออออออออออออน");
  printc(0,y++,COLOR1,"บAlt+I=DEC/HEX  Alt+R=SetRefresh  Alt+J=Jump to       บPrinter:                บ");
  printc(0,y++,COLOR1,"ศอออออออออออออออออออออออออออออออออออออออออออออออออออออสออออออออออออออออออออออออผ");
  printc(71,1,COLOR6,"A");
  
  printc(31,7,COLOR1,"%c",24);
  printc(33,7,COLOR1,"%c",24);
  printc(35,7,COLOR1,"%c",24);
  printc(37,7,COLOR1,"%c",24);
  printc(38,7,COLOR1,"%c",24);
  printc(39,7,COLOR1,"%c",24);
  printc(40,7,COLOR1,"%c",24);
  printc(41,7,COLOR1,"%c",24);
  printc(42,7,COLOR1,"%c",24);
  printc(45,7,COLOR1,"%c",24);
  printc(49,7,COLOR1,"%c",24);
  printc(53,7,COLOR1,"%c",24);
  printc(59,7,COLOR1,"%c",24);
  
  if (memstack==MEMORY) printc(15,3,COLOR6,"MEMORY");
  else printc(15,3,COLOR6,"STACK ");
  
  printc(9,46,COLOR6,"%4d",ReadRates());
  
  Col_Win(30,22,78,46,0xb1,COLOR17);
}


/***************************************************************************/
/* restituisce in "pdata" un input di tipo "tipo" di lunghezza massima "elem" */
void Scanf(int x,int y,int tipo,int elem,void *pdata)
{
  int c=57;
  int k=0;
  char tmp[160];

  OnCursor();
  switch (tipo)
  {
    case HEX :  /* numeri HEX  */
    {
      for(k=0;k<elem;k++)
      printc(x+k,y,COLOR4," ");
      k=0;
      while((k<elem+1)&&(c!=ENTER))
      {
        if (k<elem)
        {
          Gotoxy(x+k,y);
          c=toupper(getch());
          if (((c>='0')&&(c<='9')) || ((c>='A') && (c<='F')))
          {
            tmp[k]=c;
            printc(x+k,y,COLOR4,"%c",c);
            k++;
          }
          if ((c==BACK)&&(k>0))
          {
            k--;
            printc(x+k,y,COLOR4," ");
          }
        }
        else
        {
          Gotoxy(x+k,y);
          c=toupper(getch());
          if (c==BACK)
          {
            k--;
            printc(x+k,y,COLOR4," ");
          }
        }
      }
      tmp[k]='\0';
      if (k==0) printc(x,y,COLOR4,"0");

      if (elem>4)                    /* v 3.01 */
      sscanf(tmp,"%lx",(long*)pdata);
      else
      sscanf(tmp,"%x",(word*)pdata);


      break;
    }
    case STR : /* stringa CHAR */
    {
      for(k=0;k<elem;k++)
      printc(x+k,y,COLOR4," ");
      k=0;
      while((k<elem+1)&&(c!=ENTER))
      {
        if (k<elem)
        {
          Gotoxy(x+k,y);
          c=toupper(getch());
          if (((c>='0')&&(c<='9')) || ((c>='A') && (c<='Z')) ||
          ((c==':') || (c=='\\') || (c=='.')))
          {
            ((char *)pdata)[k]=c;
            printc(x+k,y,COLOR4,"%c",c);
            k++;
          }
          if ((c==BACK)&&(k>0))
          {
            k--;
            printc(x+k,y,COLOR4," ");
          }
        }
        else
        {
          Gotoxy(x+k,y);
          c=toupper(getch());
          if (c==BACK)
          {
            k--;
            printc(x+k,y,COLOR4," ");
          }
        }
      }
      ((char *)pdata)[k]='\0';
      break;
    }
    case INT : /* numeri INT  */
    {
      for(k=0;k<elem;k++)
      printc(x+k,y,COLOR4," ");
      k=0;
      while((k<elem+1)&&(c!=ENTER))
      {
        if (k<elem)
        {
          Gotoxy(x+k,y);
          c=toupper(getch());
          if ((c>='0')&&(c<='9'))
          {
            ((char *)tmp)[k]=c;
            printc(x+k,y,COLOR4,"%c",c);
            k++;
          }
          if ((c==BACK)&&(k>0))
          {
            k--;
            printc(x+k,y,COLOR4," ");
          }
        }
        else
        {
          Gotoxy(x+k,y);
          c=toupper(getch());
          if (c==BACK)
          {
            k--;
            printc(x+k,y,COLOR4," ");
          }
        }
      }
      tmp[k]='\0';
      if (k==0) printc(x,y,COLOR4,"0");
      if (elem>4)  /* v4.0 */
      {
        *(long *)pdata=atoi(tmp);
      }
      else
      {
        *(int *)pdata=atoi(tmp);
        break;
      }
    }
  }
  OffCursor();
}
/******************** Funzione gotoxy in 80x50 *****************************/
void Gotoxy(int col, int rig)
{
  union REGS i,o;
  
  i.h.ah=0x02;
  i.h.dh=rig;
  i.h.dl=col;
  i.h.bh=0;
  #ifdef __386__
  int386(0x10,&i,&o);
  #else
  int86(0x10,&i,&o);
  #endif
}
/***************************************************************************/
int fastkbhit(void)
{
  #ifdef __386__
  short *head=(short *)0x41a;
  short *tail=(short *)0x41c;
  #else
  int far *head=(int far *)0x0040001AL;
  int far *tail=(int far *)0x0040001CL;
  #endif
  return (!(*head==*tail));
}
/***************************************************************************/
/* modalita' grafica  */
void SetGraph()
{
  union REGS i,o;
  
  #ifdef __386__
  i.w.ax=0x13;
  int386(0x10,&i,&o);
  #else
  i.x.ax=0x13;
  int86(0x10,&i,&o);
  #endif
}
/***************************************************************************/
/* modalita' testo */
void SetText()
{
  union REGS i,o;
  
  #ifdef __386__
  i.w.ax=0x03;
  int386(0x10,&i,&o);
  #else
  i.x.ax=0x03;
  int86(0x10,&i,&o);
  #endif
}
/***************************************************************************/
/* modalita' testo 80*50 */
void SetText50()
{
  union REGS i,o;
  
  SetText();
  #ifdef __386__
  i.w.ax=0x1112; /* Passa in modo 50 colonne */
  i.w.bx=0;
  int386(0x10,&i,&o);
  #else
  i.x.ax=0x1112;
  i.x.bx=0;
  int86(0x10,&i,&o);
  #endif
  OffCursor();
}
/***************************************************************************/
void OffCursor()
{
  #ifdef __386__
  union REGS i,o;
  i.w.ax=0x0100; /* Cancella il cursore */
  i.w.cx=0x2000;
  int386(0x10,&i,&o);
  #else
  _setcursortype(_NOCURSOR);
  #endif
}
/***************************************************************************/
void OnCursor()
{
  #ifdef __386__
  union REGS i,o;
  i.w.ax=0x0100;
  i.w.cx=0x0607;
  int386(0x10,&i,&o);
  #else
  _setcursortype(_NORMALCURSOR);
  #endif
}
/**************************************************************************/


