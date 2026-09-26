/ HELLO: hello world sulla stampante (loc 2), poi HALT
/ "HELLO WORLD" = 11 caratteri, ci sta nel carrello da 16
        JUMP    start

start:  LOCO    72              / H
        STOD    2
        LOCO    69              / E
        STOD    2
        LOCO    76              / L
        STOD    2
        LOCO    76              / L
        STOD    2
        LOCO    79              / O
        STOD    2
        LOCO    32              / spazio
        STOD    2
        LOCO    87              / W
        STOD    2
        LOCO    79              / O
        STOD    2
        LOCO    82              / R
        STOD    2
        LOCO    76              / L
        STOD    2
        LOCO    68              / D
        STOD    2
        HALT
