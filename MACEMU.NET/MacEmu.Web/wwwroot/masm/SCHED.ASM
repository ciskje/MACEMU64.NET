/ *** Multi-Task 1.0
/ *** Dimostrativo delle eccezionali capacita' di MACEMU 4.0
/ *** Con due semplicissimi task

        JUMP    start
        JUMP    sched
        JUMP    task2

/ *** Lo scheduler 
/ ***********************************
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

/ *** Variabili locali
/ ***********************************
timerj  =       254
period  =       4

quanto: 1000

ac1:    0
pc1:    0
st1:    4095

ac2:    0
pc2:    258
st2:    4028

tmret:  0

stack:  0
task:   0

/ *** Starter
/ ***********************************
start:  LODD    quanto
        STOD    period

        LOCO    257
        STOD    timerj

        JUMP    task1

/ *** I due task
/ ***********************************

task1:  LOCO    1
        JUMP    task1

task2:  LOCO    16
        JUMP    task2
