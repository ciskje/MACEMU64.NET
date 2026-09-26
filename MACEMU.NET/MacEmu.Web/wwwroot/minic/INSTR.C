// INSTR.C - attesa con interrupt
// spenti, legge un tasto, lo
// nega. Come INSTR.ASM.
// lo nega e termina (AC = ~tasto).
int i = 0;
int uno = 1;
void main() {
    dint();
    i = 2000;
    while (i != 0) {
        i = i - 1;
    }
    eint();
    int c = getc();
    c = ~c;
    halt;
}
