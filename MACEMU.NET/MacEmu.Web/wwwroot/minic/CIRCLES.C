// CIRCLES.C - 5 cerchi Bresenham
// come CIRCLES.ASM (3 color1,
// 2 color2). Coordinate word
// 0..VIDW-1, 0..VIDH-1.
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
void main() {
    dint();
    color = color1;
    circle(16, 32, 10);
    circle(32, 32, 10);
    circle(48, 32, 10);
    color = color2;
    circle(40, 40, 10);
    circle(24, 40, 10);
    halt;
}
