/ *** Multi-Task 1.0
/ *** Dimostrativo delle eccezionali capacita' di MACEMU 4.0
/ ***
        JUMP    start
        JUMP    sched
        JUMP    task2

/ *** Lo scheduler ****************************
sched:  SWAP
        STOD    stack
        SWAP

        LODD    task
        JZER    tocca2

tocca1: LODD    stack
        STOD    st2
        LODD    st1
        STOD    stack

        LODL    0
        STOD    tmret
        LODL    1
        STOD    ac2
        LODL    2
        STOD    pc2

        SWAP
        LODD    stack
        SWAP

        LODD    tmret
        STOL    0
        LODD    ac1
        STOL    1
        LODD    pc1
        STOL    2

        JUMP    switch

tocca2: LODD    stack
        STOD    st1
        LODD    st2
        STOD    stack

        LODL    0
        STOD    tmret
        LODL    1
        STOD    ac1
        LODL    2
        STOD    pc1

        SWAP
        LODD    stack
        SWAP

        LODD    tmret
        STOL    0
        LODD    ac2
        STOL    1
        LODD    pc2
        STOL    2

        JUMP    switch

switch: LODD    task
        NOT
        STOD    task
        RETN

/ *** Variabili locali **************************
timerj  =       254
period  =       4

quanto: 3000

ac1:    0
pc1:    0
st1:    4095

ac2:    0
pc2:    258
st2:    4028

tmret:  0

stack:  0
task:   0

/ *** Starter ***********************************
start:  LODD    quanto
        STOD    period

        LOCO    257
        STOD    timerj

        JUMP    task1

















/ *** I due task ***********************************

/ *** FIBO.ASM
n       =       8
task1:  dint
        swap
        stod a
        swap
        eint

        desp n
        loco n
        push
        desp 1

ciclo:  lodl 1
        subd c1
        stol 1
        loco n
        subl 1
        push
        call fibo
        insp 1
        stol 0
        loco n
        subl 1
        push
        lodd a
        subl 0
        stol 0
        pop
        addd c2
        popi
        push
        loco n
        subl 1
        addd c1
        jpos ciclo
        jzer ciclo
        jump fine


fibo:   desp 1
        lodl 2
        jzer base0
        subd c1
        jzer base1
        push
        call fibo
        insp 1
        stol 0
        lodl 2
        subd c2
        push
        call fibo
        insp 1
        addl 0
        insp 1
        retn
base0:  loco 0
        insp 1
        retn
base1:  loco 1
        insp 1
        retn

c1:     1
c2:     2
a:      0

fine:   insp 1
loop:   jump loop















/ *** CIRCLES.ASM
task2:  LODD    color1
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

loop2:  JUMP    loop2

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
addr:    0
plot:    STOD   addr
         LODD   color
         PUSH
         LODD   addr
         POPI

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

