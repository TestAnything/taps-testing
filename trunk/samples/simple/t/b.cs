using Taps;
using simple;

class BTest: TAP {

    static int Main() {
        Plan(1); 
        var b=new B();
        Is(b.c,2);
        return 0;
    }
}
