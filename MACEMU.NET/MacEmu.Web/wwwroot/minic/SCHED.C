// SCHED.C - scheduler cooperativo
// a 2 task su interrupt timer,
// come SCHED.ASM. sched() sta in
// utility.h; qui task e start in
// C puro, zero ASM.
#include "utility.h"
int quanto = 1000;
void main() {
    ontimer(quanto, sched);
    pc2 = funaddr(task2);
    int q = 0;
    while (1) {
        q = 1;
    }
    halt;
}
void task2() {
    int q = 0;
    while (1) {
        q = 16;
    }
    halt;
}
