using Taps;
using simple;

using System.Reflection;

class ATest: TAP {

    static int Main() {
        Plan(2);
        var a=new A();
        Is(a.b,1);
        var b = new B();
        Is(b.c,1);
        IsDeeply(a, b,"apples and oranges");
        return 0;
    }
}
