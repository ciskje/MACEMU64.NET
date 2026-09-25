
/***************************************************************************/
/* MIR.H                                                                   */
/***************************************************************************/

typedef enum
{
  ADDR, A, B, C, ENC, WR, RD, MAR, MBR, SH, ALU, COND, AMUX
} campimir;


unsigned int ReadMIR(int campo);
void LoadMIR(void);
