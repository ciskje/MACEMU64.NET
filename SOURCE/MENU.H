
/*********************************************************************/
/* MENU.H                                                            */
/*********************************************************************/



#define INS 0
#define DEL 1

#define UNO 1
#define DUE 2

void Menu_Esecuzione(void);

void Win(int x0, int y0, int x1, int y1, int color1,char *titolo);
void Col_Win(int x0, int y0, int x1, int y1, int chartype, int color);
void Key(int x0,int y0, char *testo,int lettera);



int  Ask_Prog(void);
int  Ask_Cstore(void);
void Mod_Cstore(void);
void Mod_Registri(void);
void Mod_Memoria(void);
void InsBreakpoints(void);
void DelBreakpoints(void);
void ResetMac(void);
void LoadOS(void);


int  Visualizza_Bp(void);
int  Ricerca_Breakpoint(unsigned short add);


int  Gest_Key(int extinf, int extsup,
int rig, int col,
int pos,
char *elemento,
int num_elementi,
int ext_elementi,
int inc);

void Message_No_Display(void);
void Message_End_Prog(void);
void Message_Reset_Ok(void);
void Message_Never_Load(void);
void RequestRates(void);
void RequestLocationJump(void);
void About(void);
