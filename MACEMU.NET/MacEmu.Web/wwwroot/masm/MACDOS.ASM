/ *******************************
/ MACDOS 4.0
/ *******************************
/ getc    = 17
/ mult    = 18
/ readint = 19
/ *******************************

/ Locazione iniziale di gestione interrupt...

        JUMP    int

        JUMP    getc             / Vettore delle Trap del S.O.
        JUMP    mult
        JUMP    readint

int:    STOD    regf             / regf=f
        PUSH
        LODD    ipkeyb
        ANDL    0
        STOD    tmp
        POP                      / tmp=regf&ipkeyb
        LODD    tmp
        JZER    cont4
        CALL    keybrd
        RETI

cont4:  LODD    regf
        PUSH
        LODD    iptmr
        ANDL    0
        STOD    tmp
        POP                      / tmp=regf&iptmr
        LODD    tmp
        JZER    cont5
        CALL    timer
        RETI

cont5:  HALT                     / Gestore del prossimo dispositivo

regf:   0
tmp:    0
ipkeyb: 2
iptmr:  8
ermask: 0x7ffc
sbuf:   220
ebuf:   236
dist:   15
testa:  220
coda:   220

/ ******************************  DRIVER  TASTIERA
keybrd: LODD    1               / Carica CSR
        PUSH                  
        LODD    ermask            
        ANDL    0               
        STOD    tmp             
        POP                   
        LODD    tmp             
        JNZE    buferr          / Overrun sul buffer hardware?

cont1:  LODD    testa         
        SUBD    coda
        SUBD    uno
        JZER    buferr          / Buffer Pieno coda+1=testa

        LODD    coda
        SUBD    testa           
        SUBD    dist    
        JZER    buferr          / * Buffer Pieno coda-testa=dist

        LOCO    0               / Sposta il BR nello stack
        PSHI

        LODD    coda            
        POPI                    / e lo mette in coda

        LODD    coda            
        ADDD    uno             
        STOD    coda            / coda++

        SUBD    ebuf
        JNEG    ok1             / ok se coda<ebuf
        LODD    sbuf            / coda=ebuf allora coda=sbuf
        STOD    coda

ok1:    LOCO    0               / azzera la tastiera
        STOD    1               / resetta il CSR della tastiera
        RETN

buferr:
        LOCO    0               / Reset Keyb
        STOD    0
        STOD    1
        LOCO    7               / Bell
        STOD    2               / Printer
        RETN

/ **************************************** timer
timersub = 254

timer:  LOCO    0
        STOD    5               / resetta il CSR del timer
        
        LODD    timersub        / timersub e' l'indizizzo della routine 
        JZER    end2            / del timer utente

        PUSH
        RETN                    / equivalente ad JUMP [timersub]
        
end2:   RETN

/ **************************************** getc
getc:   LODD    testa
        SUBD    coda
        JZER    getc            / Se testa=coda allora buffer vuoto, cicla!

        LODD    testa
        PSHI
        POP
        STOD    tmp             / tmp = carattere preso dalla testa

        LOCO    0               / Metto uno 0 dopo aver preso il carattere
        PUSH                    / Cosi' posso veder graficamente i car presi
        LODD    testa
        POPI

        LODD    testa
        ADDD    uno
        STOD    testa           / testa++

        SUBD    ebuf            
        JNEG    ok2             / ok se testa<ebuf
        LODD    sbuf            / senno testa=sbuf
        STOD    testa

ok2:    LODD    tmp
        STOD    2               / stampa sulla stampante il carattere preso
        RETN

/ **************************************** mult
mult:   LOCO    0
        STOD    sum
        LODL    1
        SUBL    2
        JPOS    dos1
        LODL    1
        JZER    dos3
        STOD    j
        LODL    2
        STOD    k
        JUMP    dos2
dos1:   LODL    2
        JZER    dos3
        STOD    j
        LODL    1
        STOD    k
dos2:   LODD    k
        ADDD    sum
        STOD    sum
        LODD    j
        SUBD    uno
        STOD    j
        JNZE    dos2
dos3:   POP
        STOD    oldpc
        POP
        LODD    oldpc
        STOL    0
        LODD    sum
        RETN
sum:    0
oldpc:  0
j:      0
k:      0

/ **************************************** readint

readint:
        LOCO    0
        STOD    i
        STOD    res
        LOCO    1
        STOD    power
dos4:   CALL    getc
        STOD    key
        PUSH
        LODD    pos
        ADDD    i
        POPI
        LODD    i
        ADDD    uno
        STOD    i
        SUBD    sette
        JPOS    err

        LODD    key
        SUBD    enter
        JNZE    dos4
        LODD    i
        SUBD    uno
        SUBD    uno
        STOD    i
dos5:   LODD    pos
        ADDD    i
        PSHI
        POP
        SUBD    kkey
        PUSH

        JNEG    err     / n < 0
        SUBD    dieci   
        JPOS    err     / n-10 >= 0   n>=10

        LODD    power
        PUSH
        CALL    mult
        ADDD    res
        STOD    res
        LODD    power
        PUSH
        LOCO    10
        PUSH
        CALL    mult
        STOD    power
        LODD    i
        SUBD    uno
        STOD    i
        JPOS    dos5
        LODD    res     / ac=res
        RETN

err:    POP
        LOCO    0
        RETN
        
enter:  0x0d
pos:    240
power:  0
key:    0
res:    0
kkey:   0x30
i:      0
dieci:  10
sette:  7

/ **************************************** Global variable
uno:    1
