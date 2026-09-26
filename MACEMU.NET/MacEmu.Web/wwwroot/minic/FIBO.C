// FIBO.C - fibo(1..10), poi HALT.
// Come FIBO.ASM (il driver ASM
// riempie anche un vettore in
// cima alla memoria: omesso,
// nessun effetto osservabile).
int fib(int n) {
    if (n < 2) {
        return n;
    }
    return fib(n - 1) + fib(n - 2);
}
void main() {
    int i = 1;
    int x = 0;
    while (i <= 9) {
        x = fib(i);
        i = i + 1;
    }
    x = fib(10);
    halt;
}
