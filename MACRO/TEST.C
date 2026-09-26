// TEST.C - legge n, calcola
// v[i]=i+v[i]*v[i], somma in k.
// Come TEST.ASM: n elementi
// (0..n-1): 2->k=2, 3->k=8.
int uno = 1;
int quanti = 4;
int i = 0;
int n = 0;
int v[8];
int k = 0;
int sum(int nn) {
    k = 0;
    for (i = nn; i >= 0; i = i - 1) {
        k = k + v[i];
    }
    return k;
}
void main() {
    n = readint();
    if (n == 0) {
        halt;
    }
    n = n - 1;
    if (n >= quanti) {
        halt;
    }
    for (i = n; i >= 0; i = i - 1) {
        v[i] = i;
    }
    for (i = n; i >= 0; i = i - 1) {
        v[i] = i + mult(v[i], v[i]);
    }
    k = sum(n);
    halt;
}
