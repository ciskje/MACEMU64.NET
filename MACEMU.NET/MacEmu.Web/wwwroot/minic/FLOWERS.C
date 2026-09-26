// FLOWERS.C - 5 cerchi raggio 15
// come FLOWERS.ASM. Struttura
// diversa da CIRCLES: disegna,
// poi x++ SOLO se d<0 (altrimenti
// solo y--), esce su y==x.
// NOTA LAB1: somma dieci (=10)
// anche se il commento dice +15.
#include "utility.h"
int color1 = 0x0707;
int color2 = 0x4f4f;
int color = 0;
int uno = 1;
int sei = 6;
int dieci = 10;
void circle(int cx, int cy, int r) {
    int xx = 0;
    int yy = r;
    int dd = 3 - (r + r);
    while (1) {
        plot(cx + xx, cy + yy, color);
        plot(cx - xx, cy + yy, color);
        plot(cx + xx, cy - yy, color);
        plot(cx - xx, cy - yy, color);
        plot(cx + yy, cy + xx, color);
        plot(cx - yy, cy + xx, color);
        plot(cx + yy, cy - xx, color);
        plot(cx - yy, cy - xx, color);
        if (dd < 0) {
            dd = dd + xx + xx;
            dd = dd + xx + xx + sei;
            xx = xx + 1;
        }
        else {
            dd = dd + xx + xx;
            dd = dd + xx + xx;
            dd = dd - yy - yy;
            dd = dd - yy - yy + dieci;
            yy = yy - 1;
        }
        if (yy == xx) {
            return;
        }
    }
}
void main() {
    dint();
    color = color1;
    circle(16, 32, 15);
    circle(32, 32, 15);
    circle(48, 32, 15);
    color = color2;
    circle(40, 40, 15);
    circle(24, 40, 15);
    halt;
}
