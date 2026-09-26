                DINT

                LOCO 0x707
                STOD color
                LOCO 1023
                STOD x

cont:           LODD x
                CALL plot

                LODD x
                SUBD uno
                STOD x
                SUBD block
                JPOS cont

                LOCO 31
                STOD x

loop:           LOCO 0
                STOD color
                LODD pre
                CALL plot
                LOCO 0x707
                STOD color
                CALL multy64
                LODD temp
                ADDD x
                STOD pre

                SWAP
                STOD oldsp
                LODL 0
                JZER ok5
                LOCO 0
                STOL 0
                LOCO 0
                SUBD yi
                STOD yi
                LODD y
                ADDD yi
                STOD y
ok5:            LODD oldsp
                SWAP

                LODD pre
                CALL plot
                
                LODD x
                ADDD xi
                STOD x

                LODD y
                ADDD yi
                STOD y

                LODD maxx
                SUBD x
                JPOS ok1

                LOCO 0
                SUBD xi
                STOD xi
                LOCO 7
                STOD 2

ok1:            LODD x
                SUBD minx
                JPOS ok2

                LOCO 0
                SUBD xi
                STOD xi
                LOCO 7
                STOD 2

ok2:            LODD maxy
                SUBD y
                JPOS ok3

                LOCO 0
                SUBD yi
                STOD yi
                LOCO 7
                STOD 2

ok3:            LODD y
                SUBD miny
                JPOS ok4

                LOCO 0
                SUBD yi
                STOD yi
                LOCO 7
                STOD 2

ok4:            JUMP loop

uno:            1
block:          640
pre:            0
x:              31
y:              27
xi:             1
yi:             1
maxx:           61
maxy:           61
minx:           1
miny:           10
incc:           1

/ ***** disegna il punto di coordinate ac
color:   0x0707

temp:    0
oldsp:	 0
plot:    SWAP
         STOD	oldsp
         LODD color
         STOL	0
         LODD	oldsp
         SWAP

         RETN

/ ***** calcola y*64 e lo mette in temp
multy64: LODD   y
         ADDD   y
         ADDD   y
         ADDD   y
         STOD   temp    / temp=4*y
         ADDD   temp    / temp=8*y
         STOD   temp
         ADDD   temp    / temp=16*y
         STOD   temp
         ADDD   temp    / temp=32*y
         STOD   temp
         ADDD   temp    / temp=64*y
         STOD   temp
         RETN


