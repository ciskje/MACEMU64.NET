// DEMOINT.C - il timer suona
// il bell, il main brucia CPU.
// Come DEMOINT.ASM.
// il main brucia CPU in loop infinito.
#include "utility.h"
void sub() {
    bell();
}
void main() {
    ontimer(20000, sub);
    while (1) {
        delay(4095);
    }
    halt;
}
