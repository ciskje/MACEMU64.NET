
/*********************************************************************/
/* KEYB.H                                                            */
/*********************************************************************/

#define IP  1
          /*юддд  IP=0x0001=0000 0000 0000 0001 */
#define IE  2
          /*юддд  IE=0x0002=0000 0000 0000 0010 */
#define RDY (1<<15)
          /* юддд RDY=0x8000=1000 0000 0000 0000 */
int  WriteKeyb(address addr,word *a);
int  ReadKeyb(address addr,word *a);
void Keyb(void);
int  IpKeyb(void);
void ResetKeyb(void);
void LoadKeyb(word c);
