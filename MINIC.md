# MiniC — manuale del compilatore C minimale per MAC-1

MiniC traduce un sottoinsieme del C in assembly MAC
(`MACRO/*.C` → `.ASM` → `.MAC` via `MacAssembler`,
verificato byte-identico agli originali).
Implementazione: `MACEMU.NET/MacEmu.Core/Asm/MiniC.cs`.
Libreria di sistema: `MACRO/UTILITY.H` (via `#include`).

## 1. L'ambiente (la macchina che gira sotto)

- CPU MIC-1 a 4 fasi, 16 registri a 16 bit (`PC AC SP IR ...`),
  4K word di memoria (indirizzi 0..4095), stack da 4095 a scendere.
- Istruzioni: `LODD STOD ADDD SUBD` (dirette), `LODL STOL ADDL SUBL`
  (via SP), `LOCO` (costante 12 bit), `PUSH POP PSHI POPI`,
  `CALL RETN RETI`, `JPOS JZER JNEG JNZE JUMP`,
  `DINT EINT NOT SWAP HALT`. Niente `MUL/DIV/SHIFT` in hardware.
- Mappa memoria: 0/1 tastiera (BR/CSR), 2 stampante, 4/5 timer
  (periodo/CSR), 8..15 trap, 17/18/19 `getc/mult/readint` (MACDOS),
  254 salto timer utente, 255 vettore interrupt.
- Convenzione MiniC (stile `FIBO.ASM`, caller-cleanup):
  `DESP n` in apertura, locali a `LODL 0..`, parametri a `LODL`
  dopo `ret`, `INSP` + `RETN` in uscita, risultato in AC.
  Il chiamante spinge gli argomenti in ordine inverso e pulisce.
  `main` termina con `HALT`.

## 2. Testo vs grafica (i due canali d'uscita)

- TESTO: la stampante (loc 2) mostra 16 caratteri nel campo
  `Printer:` in basso a destra; il carattere 7 suona il bell.
  In C: `print('A')`, `bell()`. I programmi che leggono la
  tastiera vedono l'eco qui (è `getc` di MACDOS a stamparli).
- GRAFICA: la memoria E' lo schermo (F9 MEMDISPLAY).
  `plot(x, y, c)` scrive una word di colore `c`.
  Vedi sotto per le coordinate.

## 3. Geometria video (calcolata, non 320x200)

Lo schermo grafico è 320x200, ma i 4096 word occupano al
centro solo 128x64 pixel a (96,68): ogni word = 2 pixel
affiancati (byte basso a sinistra). Quindi:

```
RESX = 128;   // pixel reali del display memoria
RESY = 64;
VIDX = 96;    // offset finestra sullo schermo
VIDY = 68;
VIDW = RESX / 2;   // colonne word: 64
VIDH = RESY;       // righe word: 64
```

`plot(x, y, c)`: `x` in 0..VIDW-1 (colonna word = 2 pixel),
`y` in 0..VIDH-1. Indirizzo = `y*64+x` (calcolato con
`mult`, la macchina non ha shift/divisioni).

## 4. Il linguaggio

```
int / void, globali (+array: int v[8]), locali, parametri,
ricorsione, if/else, while, for(init;cond;incr), return,
+ - * (via trap mult), -x e ~x unari, < <= > >= == !=,
print(), halt, dint()/eint(), asm("..."), // commenti,
'A' letterali carattere, 0x1F esadecimali,
NAME = espressione-costante; (simboli, define-before-use),
#include "file" (da MACRO/).
```

Builtin (niente dichiarazioni): `getc()`, `mult(a,b)`,
`readint()`, `ontimer(periodo, funz)`, `funaddr(funz)`
(indirizzo di funzione, solo ASM può), `print(c)`.
Da `UTILITY.H`: `bell()`, `delay(n)`, `setperiod(p)`,
`vidread(a)`/`vidwrite(a,v)`, `plot(x,y,c)`, `sched()`
(nocciolo scheduler verbatim per SCHED/DEMOTASK).

Limiti voluti: niente `/ %` (commento/divisione assenti:
`/` apre i commenti), niente `& |` logici, condizioni solo
relazionali o costanti, array solo globali, `LOCO` a 12 bit
(i letterali grandi sono sommati a chunk), niente `break`,
`switch`, `struct`, puntatori, float. Riservati: prefissi
`Lc/Mc`, nomi `ontimer/funaddr`, device 0..5, 254, 255.

## 5. Programmi demo (tutti con differenziale .C vs .MAC)

| programma | fa | I/O |
|---|---|---|
| HELLO | scrive HELLO WORLD | stampante+HALT |
| FIBO | fibo(1..10), AC=55 | HALT (driver senza vector) |
| TEST | n=readint, v[i]=i+v[i]², k | tastiera; k in mem |
| INSTR | attesa DINT, getc, NOT | AC=~tasto |
| LENTO | getc in loop + attesa | eco Printer |
| DEMOINT | bell a ogni tick timer | audio, infinito |
| FIBOINT | driver fibo + bell timer | audio+HALT |
| CIRCLES | 5 cerchi Bresenham | grafica+HALT |
| FLOWERS | 5 cerchi r=15 (x++ solo se d<0!) | grafica+HALT |
| ARCANOID | riga + pallina, bell ai muri | grafica+audio |
| SCHED | 2 task su timer | nessuno (task flip) |
| DEMOTASK | fibo(8) + cerchi in task | grafica |

Quirk fedeli: FLOWERS somma `dieci` (=10) anche se il commento
dice +15; ARCANOID legge il pixel (blocco originale =
se occupato cancella e rimbalza); TEST fa `n--` (n elementi);
il driver FIBO originale riempie un vettore in cima alla
memoria (omesso: scaffolding). `TEST.MAC` rigenerato il
2026-09-26 (era pre-rework readint).

## 6. Compilare ed eseguire

```
LOAD PROGRAM → HELLO.MAC → RUNNING MENU → F1
```

Da C: `MiniC.Compile(src, inc)` → `MacAssembler` →
`.MAC`. I test (`MiniCTests`, 29) compilano, assemblano ed
eseguono headless ogni demo confrontandolo con l'originale.

## 7. Roadmap

Fatto: milestone M1-M5 + `for` + `utility.h` + `funaddr` +
`=` costanti + char literal. Futuro possibile: `&& || !`,
`break/continue`, stringhe, `&` via ANDL.
