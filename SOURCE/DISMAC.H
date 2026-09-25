
/****************************************************************/
/* DISMAC.H                                                     */
/****************************************************************/

#define    TWELVE_BIT_INSTR_MASK         0xf000
#define    EIGHT_BIT_INSTR_MASK          0xff00
#define    FOUR_BIT_INSTR_MASK           0xfff0
#define    TWELVE_BIT_OPERAND_MASK       0x0fff
#define    EIGHT_BIT_OPERAND_MASK        0x00ff
#define    FOUR_BIT_OPERAND_MASK         0x000f
#define    FOUR_BIT_SIGN_EXTEND_MASK     0xfffffff0L
#define    SIXTEEN_BIT_SIGN_EXTEND_MASK  0xffff0000L
#define    MIN_4_BIT_NEG_VALUE           0x00000008L
#define    MIN_16_BIT_NEG_VALUE          0x00008000L


char * disassemble(word hexcode);
long sign_extend(long temphex,int number_length);
