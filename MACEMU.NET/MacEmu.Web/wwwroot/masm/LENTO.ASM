getc    = 17

loop2:  CALL    getc
        LODD    max
        STOD    i

loop:   LODD    i
        SUBD    uno
        STOD    i

        JPOS    loop

        JUMP    loop2

max:    500
i:      0
uno:    1


