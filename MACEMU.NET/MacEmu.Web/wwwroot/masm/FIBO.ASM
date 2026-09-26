a =      4095
n =      10

start:  desp n
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
        loco a
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

fine:   insp 1
        halt
