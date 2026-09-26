// LENTO.C - legge tasti in loop
// con attesa (eco nel campo
// Printer). Come LENTO.ASM.
int max = 500;
int i = 0;
int uno = 1;
void main() {
    while (1) {
        getc();
        i = max;
        while (i >= 0) {
            i = i - 1;
        }
    }
    halt;
}
