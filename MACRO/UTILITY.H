// UTILITY.H - libreria di sistema
// per MiniC (#include "utility.h").
// Autocontenuto: nessun globale
// richiesto. Zero ASM nei programmi:
// il codice macchina sta qui.
// Riservati: prefissi Lc/Mc,
// nomi ontimer/funaddr, device
// 0..5, 254, 255.

// Geometria video (calcolata):
// schermo 320x200, la memoria
// (4096 word) occupa al centro
// 128x64 pixel a (96,68): ogni
// word = 2 pixel affiancati
// (byte basso a sx).
RESX = 128;
RESY = 64;
VIDX = 96;
VIDY = 68;
VIDW = RESX / 2;
VIDH = RESY;

// --- stampante (loc 2) ---
void bell() {
    print(7);
}

// --- attesa attiva ---
void delay(int n) {
    while (n != 0) {
        n = n - 1;
    }
}

// --- timer: periodo (la routine
// si aggancia con ontimer) ---
void setperiod(int p) {
    asm("LODL 1");
    asm("STOD 4");
}

// --- lettura/scrittura a
// indirizzo calcolato ---
int vidread(int a) {
    asm("LODL 1");
    asm("PSHI");
    asm("POP");
}
void vidwrite(int a, int v) {
    asm("LODL 2");
    asm("PUSH");
    asm("LODL 2");
    asm("POPI");
}

// --- plot: word (x, y) di
// colore c (= 2 pixel) ---
// x in 0..VIDW-1, y in 0..VIDH-1.
void plot(int x, int y, int c) {
    vidwrite(mult(y, VIDW) + x, c);
}

// --- scheduler cooperativo:
// nocciolo verbatim ---
// Protocollo originale: entra
// via interrupt (JUMP, non CALL),
// commuta con RETN. I task non
// devono mai ritornare.
int task = 0;
int st1 = 4095;
int st2 = 4028;
int ac1 = 0;
int ac2 = 0;
int pc1 = 0;
int pc2 = 0;
int tmret = 0;
int stack = 0;
void sched() {
    asm("SWAP");
    asm("STOD stack");
    asm("SWAP");
    asm("LODD task");
    asm("JZER tocca2");
    asm("tocca1:");
    asm("LODD stack");
    asm("STOD st2");
    asm("LODD st1");
    asm("STOD stack");
    asm("LODL 0");
    asm("STOD tmret");
    asm("LODL 1");
    asm("STOD ac2");
    asm("LODL 2");
    asm("STOD pc2");
    asm("SWAP");
    asm("LODD stack");
    asm("SWAP");
    asm("LODD tmret");
    asm("STOL 0");
    asm("LODD ac1");
    asm("STOL 1");
    asm("LODD pc1");
    asm("STOL 2");
    asm("JUMP switch");
    asm("tocca2:");
    asm("LODD stack");
    asm("STOD st1");
    asm("LODD st2");
    asm("STOD stack");
    asm("LODL 0");
    asm("STOD tmret");
    asm("LODL 1");
    asm("STOD ac1");
    asm("LODL 2");
    asm("STOD pc1");
    asm("SWAP");
    asm("LODD stack");
    asm("SWAP");
    asm("LODD tmret");
    asm("STOL 0");
    asm("LODD ac2");
    asm("STOL 1");
    asm("LODD pc2");
    asm("STOL 2");
    asm("JUMP switch");
    asm("switch:");
    asm("LODD task");
    asm("NOT");
    asm("STOD task");
    asm("RETN");
}
