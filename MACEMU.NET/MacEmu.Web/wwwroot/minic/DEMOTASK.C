// DEMOTASK.C - scheduler + task1
// (fibo 8) + task2 (cerchi), come
// DEMOTASK.ASM. sched() e plot()
// da utility.h; task in C puro,
// zero ASM. Prologo task1 e
// vector-store omessi (scaffolding
// senza effetti osservabili).
#include "utility.h"
int quanto = 3000;
int color1 = 0x0707;
int color2 = 0x4f4f;
int color = 0;
int uno = 1;
int sei = 6;
int dieci = 10;
int fib(int n) {
    if (n < 2) {
        return n;
    }
    return fib(n - 1)
        + fib(n - 2);
}
void circle(int cx, int cy, int r) {
    int xx = 0;
    int yy = r;
    int dd = 3 - (r + r);
    while (yy >= xx) {
        if (dd < 0) {
            dd = dd + xx + xx;
            dd = dd + xx + xx + sei;
        }
        else {
            dd = dd + xx + xx;
            dd = dd + xx + xx;
            dd = dd - yy - yy;
            dd = dd - yy - yy + dieci;
            yy = yy - 1;
        }
        plot(cx + xx, cy + yy, color);
        plot(cx - xx, cy + yy, color);
        plot(cx + xx, cy - yy, color);
        plot(cx - xx, cy - yy, color);
        plot(cx + yy, cy + xx, color);
        plot(cx - yy, cy + xx, color);
        plot(cx + yy, cy - xx, color);
        plot(cx - yy, cy - xx, color);
        xx = xx + 1;
    }
}
void task1() {
    int f = fib(8);
    while (1) {
    }
    halt;
}
void task2() {
    color = color1;
    circle(16, 32, 10);
    circle(32, 32, 10);
    circle(48, 32, 10);
    color = color2;
    circle(40, 40, 10);
    circle(24, 40, 10);
    while (1) {
    }
    halt;
}
void main() {
    ontimer(quanto, sched);
    pc2 = funaddr(task2);
    task1();
    halt;
}
