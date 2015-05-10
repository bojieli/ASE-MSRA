#include<stdio.h>
int main() {
    int i;
    double s = 0;
    for (i=0; i<220; i++) {
        s += (1-i/220.0)*(1-i/220.0)*(1-i/220.0) * i * 4 / 220;
    }
    printf("%lf\n", s);
}
