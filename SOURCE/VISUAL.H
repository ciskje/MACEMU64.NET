
/***************************************************************************/
/* VISUAL.H                                                                */
/***************************************************************************/

/* COSTANTI
************************************************************************/

#define ON        1
#define OFF       0
#define MAXLENWAV 100000L

/* CODICI DEI TASTI
************************************************************************/

#define UP     0x48
#define DOWN   0x50
#define LEFT   0x4b
#define RIGHT  0x4d
#define PGUP   0x49
#define PGDOWN 0x51
#define ESC    0x1b
#define ENTER  0x0d
#define F1     0x3b
#define F2     0x3c
#define F3     0x3d
#define F4     0x3e
#define F5     0x3f
#define F6     0x40
#define F7     0x41
#define F8     0x42
#define F9     0x43
#define F10    0x44
#define F11    0x45

#define ZERO   0x00
#define BACK   0x08
#define SPACE  0x20
#define ALT    0x1e
#define ALT_A  0x1e
#define ALT_I  0x17
#define ALT_R  0x13
#define ALT_J  0x24


#define NIL 4097

#define HEX    0
#define STR    1
#define INT    2
#define DEC    3

#define STACK  0
#define MEMORY 1


/* MODI DISPLAY
************************************************************************/
#define NODISPLAY      0
#define TEXTDISPLAY    1
#define MEMDISPLAY     2


/* COLORI TESTO
************************************************************************/

	     /* BT ===>> B=Background, T=text */
	     /* || */
	     /* \/ */

#define COLOR0  0x1700
		   /* Gray on Black	               */
#define COLOR1  0x1f00
		   /* White on blue  ok            */
#define COLOR2  0x7100
		   /* Blue on Gray                 */
#define COLOR3  0x9a00
		   /* Blinking Light Green on Blue */
#define COLOR4  0x1b00
		   /* Cyan on Blue   	  ok */
#define COLOR5  0x0300
		   /* Cyan on Black   	  ok */
#define COLOR6  0x1c00
		   /* Red on blue     	  ok */
#define COLOR7  0x2b00
		   /* Green on Cyan   	  ok */
#define COLOR8  0x1a00
		   /* Light Green on Blue 	     */
#define COLOR9  0x1e00
		   /* Yellow on Blue    	  ok */
#define COLOR10 0x1e00
		   /* Blinking Yellow on Blue      */
#define COLOR11 0x0200
		   /* Black on Blue  	     */
#define COLOR12 0x3100
		   /* Blue on Cyan     	  ok */
#define COLOR13 0x3f00
		   /* White on Cyan ok  	     */
#define COLOR14 0x0c00
		   /* Red on Black         	  ok */
#define COLOR15 0x2e00
		   /* Yellow on Green 	     */
#define COLOR16 0x2c00
		   /* Red on Green         	  ok */
#define COLOR17 0x0f00
		   /* White on Black	     */
#define COLOR18 0x0700
		   /* Gray on Black 	     */
#define COLOR19 0x0100
		   /* Black on blue      	  ok */
#define COLOR20 0xb100
		   /* Blue on Cyan        	  ok */

/* PROTOTIPI
************************************************************************/

void SetMode1(void);
void SwapMemStack(void);
int  ReadMemStack(void);
int  ReadVideoMode(void);

void DownMov(void);
void UpMov(void);
void WriteMov(int addr);

void Scanf(int x,int y,int tipo,int elem,void *pdata);
void Gotoxy(int,int);
int  fastkbhit(void);
void SetGraph(void);
void SetText(void);
void SetText50(void);
void OffCursor(void);
void OnCursor(void);

void BlankScreen(void);
void LoadPic(char *fname);
void Blinking(void);

void printc(int x,int y,unsigned int color,char *format,...);
void printh(int x,int y,int value);
void printclear(int x,int y);
void printb(int x,int y,unsigned long a);

void WriteVisual(int m);
void Visual(void);
void DrawText(void);
