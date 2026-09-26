// ARCANOID.C - riga in alto,
// pallina che rimbalza (bell).
// Come ARCANOID.ASM. La collisione
// legge il pixel (blocco originale
// equivalente). incc omesso.
#include "utility.h"
int uno = 1;
int x = 31;
int y = 27;
int xi = 1;
int yi = 1;
int maxx = 61;
int maxy = 61;
int minx = 1;
int miny = 10;
int color = 0x707;
int ox = 0;
int oy = 0;
void main() {
    dint();
    color = 0x707;
    int yy = miny;
    while (yy <= miny + 5) {
        int xx = VIDW - 1;
        while (xx >= 0) {
            plot(xx, yy, color);
            xx = xx - 1;
        }
        yy = yy + 1;
    }
    x = 31;
    while (1) {
        plot(ox, oy, 0);
        ox = x;
        oy = y;
        if (vidread(mult(y, VIDW)
            + x) != 0) {
            plot(x, y, 0);
            yi = -yi;
            y = y + yi;
        }
        x = x + xi;
        y = y + yi;
        if (maxx - x < 0) {
            xi = -xi;
            bell();
        }
        if (x - minx < 0) {
            xi = -xi;
            bell();
        }
        if (maxy - y < 0) {
            yi = -yi;
            bell();
        }
        if (y - miny < 0) {
            yi = -yi;
            bell();
        }
    }
    halt;
}
