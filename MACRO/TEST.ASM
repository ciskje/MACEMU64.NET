getc    = 17
mult    = 18
readint = 19

        CALL    readint         / n=readint() v4.0
        STOD    n
										  / valore restituito dalla readint
        JZER    exit            / se il valore inserito e' 0 => ERRORE

        SUBD    uno             / n--
        STOD    n
        SUBD    quanti
        JPOS    error           / Salta ad error se n>=quanti
        LODD    n
        STOD    i               / i=n

loop1:  LODD    i
        PUSH
        LODD    v
        ADDD    i
        POPI                    / v[i]=i
        LODD    i
        SUBD    uno
        STOD    i               / i--
        JPOS    loop1           / Salta se i>=0

        LODD    n               / i=n
        STOD    i

loop2:  LODD    v
        ADDD    i
        PSHI
        PSHI
        CALL    mult            / v[i]*v[i]
        ADDD    i               / i+v[i]*v[i]
        PUSH
        LODD    v
        ADDD    i
        POPI                    / v[i]=
        LODD    i
        SUBD    uno
        STOD    i               / i--
        JPOS    loop2           / Salta se i>=0

        LODD    v
        PUSH
        LODD    n
        PUSH
        CALL    sum

exit:   HALT

error:  LOCO    0
        HALT        

uno:    1
i:      0
quanti: 4
v:      3968                    / contenuto di un indirizzo
n:      0
        									  / da fissarsi in compilazione
sum:    LODL    1               / n
        STOD    i               / i=n
        LODL    2               / v
        STOD    vect            / vect=v
loop3:  LODD    vect
        ADDD    i
        PSHI
        POP
        ADDD    k
        STOD    k               / k=k+vect[i]
        LODD    i
        SUBD    uno
        STOD    i               / i--
        JPOS    loop3

        LODL    0               / oldpc
        STOL    2
        POP
        POP                     / Pulisce lo stack dai parametri
        LODD    k
        RETN
vect:   0
k:      0

