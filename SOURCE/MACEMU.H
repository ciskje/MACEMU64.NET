
/***************************************************************************/
/* MACEMU.H                                                                */
/***************************************************************************/

#define NUM_ELEMENTI      8
#define LUNG_ELEMENTI     22
#define NUM_REGISTRI      16
#define LUNG_REGISTRI     11
#define LUNG_FILE         25
#define MAXBP             10

#define CARICA_PROGRAMMA  0
#define CARICA_CSTORE     1
#define MODIFICA_MEMORIA  2
#define MODIFICA_CSTORE   3
#define MENU_ESECUZIONE   4
#define RESET             6
#define RESTART           5
#define USCITA            7

#define VER "4.0b"

#ifdef __386__
#define COMP  "32bit"
#else
#define COMP  "16bit"
#endif

int  ReadRestart(void);
void WriteRestart(int a);
void WriteNeverLoad(int a);
int  ReadNeverLoad(void);
void SetRates(int rts);
int  ReadRates(void);
void SetHalt(void);
void ResetHalt(void);

void Principale(void);   /* menu primario   :  grafica + comandi  */
void Esecuzione(void);   /* menu esecuzione :  comandi            */

