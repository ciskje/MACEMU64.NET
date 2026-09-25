
/***********************************************************************/
/* TIMER.H                                                             */
/***********************************************************************/

#define IP  1
          /*ÀÄÄÄ  IP=0x0001=0000 0000 0000 0001 */

int  WriteTimer(address addr,word *t);
int  IpTimer(void);
void ResetTimer(void);
void Timer(void);
