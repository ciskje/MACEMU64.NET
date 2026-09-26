// FIBOINT.C - driver fibo(1..10)
// + bell del timer. Come FIBOINT.
#include "utility.h"
int n = 10;
int fib(int nn) {
    if (nn < 2) {
        return nn;
    }
    return fib(nn - 1) + fib(nn - 2);
}
void sub() {
    bell();
}
void main() {
    ontimer(20000, sub);
    int i = 1;
    int x = 0;
    while (i <= 9) {
        x = fib(i);
        i = i + 1;
    }
    x = fib(10);
    halt;
}
