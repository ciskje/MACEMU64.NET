getc = 17

      DINT
      LOCO 2000
loop: STOD i
      SUBD uno
      JNZE loop
      EINT
      CALL getc
      NOT
      HALT


i:    0
uno:  1
