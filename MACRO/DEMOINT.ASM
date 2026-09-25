timersub = 254  / indirizzo che contiene la locazione di salto del timer utente
period   = 4    / la loc 4 contiene il periodo del timer
bell     = 7    / 7 e' il carattere bell per la stampante
printer  = 2    / e' la loc per scrivere sulla stampante

/ Il programma e' compilato dalla loc 256
        JUMP    start

sub:    LOCO    bell            / questa e' la routine utente del timer
        STOD    printer         / suona semplicemente
        RETN

start:  LODD    value   
        STOD    period          / setto il periodo del timer a 20000

        LOCO    257
        STOD    timersub        / setto il salto del timer alla routine sub:
        
loop2:  LOCO    4095            / questo e' un programma che cicla
loop:   SUBD    uno
        JNZE    loop
        JUMP    loop2
        HALT

uno:    1
value:  20000                   / il valore usato come periodo

