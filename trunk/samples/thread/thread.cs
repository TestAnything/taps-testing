using Taps;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

class ThreadTest: TAP {

    static Queue<int> Queue=new Queue<int>(Enumerable.Range(0,100));

    static void Runner(object o) {
        int n=(int)o;
        Thread.Sleep(500);
        int element=-1;
        lock(Queue) {
            element=Queue.Dequeue();
        }
        Isnt(element,-1,string.Format("thread {0} got element {1}",n,element));
    }

    static int Main() {
        Plan(100);
        foreach(var i in Enumerable.Range(0,100)) {
            var t=new Thread(Runner);
            t.Start(i);
        }
        return 0;
    }
}
