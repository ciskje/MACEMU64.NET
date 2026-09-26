        DINT

        LODD    color1
        STOD    color
        LOCO    2064
        STOD    center
        LOCO    10
        STOD    radius
        CALL    circle

        LOCO    2080
        STOD    center
        LOCO    10
        STOD    radius
        CALL    circle

        LOCO    2096
        STOD    center
        LOCO    10
        STOD    radius
        CALL    circle

        LODD    color2
        STOD    color
        LOCO    2600
        STOD    center
        LOCO    10
        STOD    radius
        CALL    circle

        LOCO    2584
        STOD    center
        LOCO    10
        STOD    radius
        CALL    circle

        HALT
color1: 0x0707
color2: 0x4f4f
color:  0
menouno: 0xffff
/ ***** PROCEDURE *********************************************
/ ***** Disegna un cerchio di coordinate center e raggio radius
d:      0
x:      0
y:      0
temp:   0

uno:    1
sei:    6
dieci:  10

center:  0
radius:  0
circle:  LOCO   0
         STOD   x
         LODD   radius
         STOD   y
         LODD   radius
         ADDD   radius
         STOD   temp
         LOCO   3
         SUBD   temp
         STOD   d
      
main:    LODD   d
         JPOS   LAB1    / d>=0?

         LODD   x       / d<0
         ADDD   x
         ADDD   x
         ADDD   x
         ADDD   sei
         ADDD   d
         STOD   d       / d=d+(x*4)+6

	 JUMP	LAB2

LAB1:    LODD   x       / d>=0
         SUBD   y
         STOD   temp
         ADDD   temp
         ADDD   temp
         ADDD   temp
         ADDD   dieci
         ADDD   d
         STOD   d       / d=d+4*(x-y)+10

         LODD   y
         SUBD   uno
         STOD   y       / y=y-1

LAB2:    CALL   multy64
         LODD   temp
         ADDD   x
         ADDD   center    / ac=center+(+x+y*64)
         CALL   plot

         CALL   multy64
         LODD   temp
         SUBD   x       
         ADDD   center    / ac=center+(-x+y*64)
         CALL   plot

         CALL   multy64
         LODD   center
         SUBD   temp
         ADDD   x
         CALL   plot    / ac=center+(+x-y*64)

         CALL   multy64
         LODD   center
         SUBD   temp
         SUBD   x
         CALL   plot    / ac=center+(-x-y*64)



         CALL   multx64
         LODD   temp
         ADDD   y
         ADDD   center    / ac=center+(+y+x*64)
         CALL   plot

         CALL   multx64
         LODD   temp
         SUBD   y
         ADDD   center    / ac=center+(-y+x*64)
         CALL   plot

         CALL   multx64
         LODD   center
         SUBD   temp
         ADDD   y
         CALL   plot    / ac=center+(+y-x*64)

         CALL   multx64
         LODD   center
         SUBD   temp
         SUBD   y
         CALL   plot    / ac=center+(-y-x*64)



         LODD   x
         ADDD   uno
         STOD   x

         LODD   y       / x==y?
         SUBD   x
         JPOS   main

         RETN

/ ***** disegna il punto di coordinate ac
oldsp:	 0
plot:    SWAP
         STOD	oldsp
         LODD   color
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

/ ***** calcola x*64 e lo mette in temp
multx64: LODD   x
         ADDD   x
         ADDD   x
         ADDD   x
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












