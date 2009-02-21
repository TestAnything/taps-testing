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
using System.Collections.Generic;

class DeepCmpTest: TAP {

    static int Main() {
        Plan(48);
        Autorun(typeof(DeepCmpTest));
        return 0;
    }

    void TestScalar() {
        var dc=new DeepCmp();
        List<PathEntry> path;
        Is(dc.Compare(1,1,out path),DeepCmp.Result.Eq);
        Is(path,null);
        Is(dc.Compare(1,2,out path),DeepCmp.Result.ValNe);
        IsDeeply(path,new PathEntry[]{new PathEntry{Idx=0}});
        Is(dc.Compare("a","a",out path),DeepCmp.Result.Eq);
        Is(path,null);
        Is(dc.Compare("a","b",out path),DeepCmp.Result.ValNe);
        IsDeeply(path,new PathEntry[]{new PathEntry{Idx=0}});
    }

    void TestNullScalar() {
        var dc=new DeepCmp();
        List<PathEntry> path;
        Is(dc.Compare("a",null,out path),DeepCmp.Result.ValNe);
        IsDeeply(path,new PathEntry[]{new PathEntry{Idx=0}});
        Is(dc.Compare(null,"b",out path),DeepCmp.Result.ValNe);
        IsDeeply(path,new PathEntry[]{new PathEntry{Idx=0}});
        Is(dc.Compare(null,null,out path),DeepCmp.Result.Eq);
        IsDeeply(path,null);
    }

    void TestArray() {
        var dc=new DeepCmp();
        List<PathEntry> path;
        Is(dc.Compare(new[]{1,2},new[]{1,2},out path),DeepCmp.Result.Eq);
        Is(path,null);
        Is(dc.Compare(new[]{1,2},new[]{1,3},out path),DeepCmp.Result.ValNe);
        IsDeeply(path,new[]{new PathEntry(0,new[]{2,2}),new PathEntry(2)});
        Is(dc.Compare(new[]{1,2},new[]{2,2},out path),DeepCmp.Result.ValNe);
        IsDeeply(path,new[]{new PathEntry(0,new[]{2,2}),new PathEntry(1)});
    }

    void TestNullArray() {
        var dc=new DeepCmp();
        List<PathEntry> path;
        Is(dc.Compare(null,new[]{1,2},out path),DeepCmp.Result.ValNe);
        IsDeeply(path,new[]{new PathEntry(0,new[]{0,2})});
        Is(dc.Compare(new[]{null,"b"},new[]{"a","b"},out path),DeepCmp.Result.ValNe);
        IsDeeply(path,new[]{new PathEntry(0,new[]{2,2}),new PathEntry(1)});
    }

    void TestDic() {
        var dc=new DeepCmp();
        List<PathEntry> path;
        Is(dc.Compare(new Dictionary<int,int>{{1,2}},new SortedDictionary<int,int>{{1,2}},out path),DeepCmp.Result.Eq);
        Is(path,null);
        Is(dc.Compare(new Dictionary<int,int>{{1,2}},new SortedDictionary<int,int>{{1,3}},out path),DeepCmp.Result.ValNe);
        IsDeeply(path,new[]{new PathEntry(0,new int[]{1,1}),new PathEntry(1)});
        Is(dc.Compare(new Dictionary<int,int>{{1,2},{0,1}},new SortedDictionary<int,int>{{0,1},{2,3}},out path),DeepCmp.Result.KeyNe);
        IsDeeply(path,new[]{new PathEntry(0,new int[]{2,2}),new PathEntry(2)});
    }

    void TestNullDic() {
        var dc=new DeepCmp();
        List<PathEntry> path;
        Is(dc.Compare(null,new Dictionary<string,string>{{"a","b"}},out path),DeepCmp.Result.ValNe);
        IsDeeply(path,new[]{new PathEntry(0,new[]{0,1})});
        Is(dc.Compare(new Dictionary<string,string>{{"a",null}},new Dictionary<string,string>{{"a","b"}},out path),DeepCmp.Result.ValNe);
        IsDeeply(path,new[]{new PathEntry(0,new[]{1,1}),new PathEntry(1)});
    }
    
    class A {
        public int B;
        public A Aref;
        public A(int b,A aref) {
            B=b;
            Aref=aref;
        }
    }

    void TestFields() {
        var dc=new DeepCmp();
        List<PathEntry> path;
        Is(dc.Compare(new A(1,null),new A(1,null),out path),DeepCmp.Result.Eq);
        Is(path,null);
        Is(dc.Compare(new A(1,null),new A(2,null),out path),DeepCmp.Result.ValNe);
        IsDeeply(path,new[]{new PathEntry(0,new[]{2,2}),new PathEntry(1)});
        Is(dc.Compare(new A(1,new A(2,null)),new A(1,new A(2,null)),out path),DeepCmp.Result.Eq);
        Is(path,null);
        Is(dc.Compare(new A(1,new A(2,null)),new A(1,new A(3,null)),out path),DeepCmp.Result.ValNe);
        IsDeeply(path,new[]{new PathEntry(0,new[]{2,2}),new PathEntry(2,new[]{2,2}),new PathEntry(3)});
    }

    void TestNest() {
        var dc=new DeepCmp();
        List<PathEntry> path;
        var a=new A(1,null);
        a.Aref=a;
        var b=new A(1,null);
        b.Aref=b;
        Is(dc.Compare(a,b,out path),DeepCmp.Result.Eq);
        Is(path,null);
        // 'got' and 'expected' referring to each other
        a.Aref=b;
        b.Aref=a;
        Is(dc.Compare(a,b,out path),DeepCmp.Result.Eq);
        Is(path,null);
        a.B=2;
        Is(dc.Compare(a,b,out path),DeepCmp.Result.ValNe);
        IsDeeply(path,new[]{new PathEntry(0,new[]{2,2}),new PathEntry(1)});
    }
    
}
