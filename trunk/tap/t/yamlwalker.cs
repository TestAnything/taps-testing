// Copyright 2009 Frank van Dijk
// This file is part of Taps.
//
// Taps is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Taps is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public
// License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Taps.  If not, see <http://www.gnu.org/licenses/>.
//
// You are granted an "additional permission" (as defined by section 7
// of the GPL) regarding the use of this software in automated test
// scripts; see the COPYING.EXCEPTION file for details.

using Taps;
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

class YAMLWalkerTest: TAP {
    
    static int Main() {
        Plan(29);
        Autorun(typeof(YAMLWalkerTest));
        return 0;
    }

#pragma warning disable 414

    class Fields {
        public enum Numbers {zero,one,two,three} 
        int a=1;
        public string b="Sally";
        Numbers n=Numbers.two;

        public Fields() {}
        public Fields(int i) {
            a=i;
        }
    }

    class A {
        public A otherA;
    }

    static void DumpNodes(List<YNode> nodes) {
        Diag("dumpnodes called");
        foreach(var i in nodes) {
            Diag(new string(' ',i.Depth*2)+"{0} -> {1} {2}",i.Key,i.O.ToString(),
                 i.Ref!=-1?string.Format("(ref {0})",i.Ref):"");
        }
    }

    static void TestInt() {
        var yw=new YAMLWalker(null);
        var nodes=yw.Walk("root",1);
        Is(nodes.Count,1);
        var node=nodes[0];
        Is(node.O,1);
    }

    static void TestFields() {
        var yw=new YAMLWalker(null);
        var nodes=yw.Walk("root",new Fields());
        IsDeeply(from x in nodes select x.Key,new []{"root","a","b","n"});
        IsDeeply((from x in nodes select x.O).Skip(1),new object[]{1,"Sally",Fields.Numbers.two});
    }

    static void TestNullField() {
        var yw=new YAMLWalker(null);
        var nodes=yw.Walk("root",new A());
        IsDeeply(from x in nodes select x.Key,new []{"root","otherA"});
        IsDeeply((from x in nodes select x.O).Skip(1),new object[]{null});
    }

    static void TestEnumerator() {
        var yw=new YAMLWalker(null);
        var nodes=yw.Walk("root",new[]{1,3,2});
        IsDeeply(from x in nodes select x.Key,new []{"root",null,null,null});
        IsDeeply((from x in nodes select x.O).Skip(1),new object[]{1,3,2});
    }

    static void TestNullEnumerator() {
        var yw=new YAMLWalker(null);
        var nodes=yw.Walk("root",new[]{"a",null,"b"});
        IsDeeply(from x in nodes select x.Key,new []{"root",null,null,null});
        IsDeeply((from x in nodes select x.O).Skip(1),new object[]{"a",null,"b"});
    }

    static void TestDic() {
        var dic=new Dictionary<string,int>(){{"b",1},{"a",2}};
        var yw=new YAMLWalker(null);
        var nodes=yw.Walk(null,dic);
        IsDeeply(from x in nodes select x.Key,new []{null,"a","b"});
        IsDeeply((from x in nodes select x.O).Skip(1),new object[]{2,1});
    }

    static void TestDicField() {
        var b=new Fields(1);
        var a=new Fields(2);
        var dic=new Dictionary<string,Fields>(){{"b",b},{"a",a}};
        var yw=new YAMLWalker(null);
        var nodes=yw.Walk("root",dic);
        IsDeeply(from x in nodes where x.Depth<=1 select x.Key,new []{"root","a","b"});
        IsDeeply((from x in nodes where x.Depth==1 select x.O),new object[]{a,b});
        IsDeeply((from x in nodes where x.Depth==2 select x.O),new object[]{
                2,"Sally",Fields.Numbers.two,1,"Sally",Fields.Numbers.two});
        //DumpNodes(yw);
    }

    static void TestRef() {
        var a=new Fields(2);
        var dic=new Dictionary<string,Fields>(){{"b",a},{"a",a}};
        var yw=new YAMLWalker(null);
        var nodes=yw.Walk("root",dic);
        IsDeeply(from x in nodes where x.Depth==1 select x.Flags,new []{YNode.Flag.ReferredTo|YNode.Flag.Dic,YNode.Flag.Dic});
        IsDeeply(from x in nodes where x.Depth<=1 select x.Key,new []{"root","a","b"});
        IsDeeply(from x in nodes where x.Depth==1 select x.O,new []{a,a});
        IsDeeply(from x in nodes where x.Depth==1 select x.Ref,new []{-1,1});
        // the fields of a appear after a
        Is(nodes[2].Depth,2);
        // the fields of a appear only once
        IsDeeply((from x in nodes where x.Depth==2 select x.O),new object[]{2,"Sally",Fields.Numbers.two});
        //DumpNodes(nodes);
    }

    static void TestCycle() {
        var yw=new YAMLWalker(null);
        A a=new A();
        A b=new A();
        a.otherA=b;
        b.otherA=a;
        var nodes=yw.Walk("root",a);
        IsDeeply(from x in nodes select x.Key,new []{"root","otherA","otherA"});
        IsDeeply(from x in nodes select x.Depth,new []{0,1,2});
        IsDeeply(from x in nodes select x.Ref,new []{-1,-1,0});
        IsDeeply(from x in nodes select x.Flags,new []{YNode.Flag.ReferredTo|YNode.Flag.Dic,YNode.Flag.Dic,YNode.Flag.Dic});
        //DumpNodes(nodes);
    }

    static void TestTightCycle() {
        var yw=new YAMLWalker(null);
        A a=new A();
        a.otherA=a;
        var nodes=yw.Walk("root",a);
        IsDeeply(from x in nodes select x.Key,new []{"root","otherA"});
        IsDeeply(from x in nodes select x.Depth,new []{0,1});
        IsDeeply(from x in nodes select x.Ref,new []{-1,0});
        IsDeeply(from x in nodes select x.Flags,new []{YNode.Flag.ReferredTo|YNode.Flag.Dic,YNode.Flag.Dic});
        //DumpNodes(nodes);
    }

}

