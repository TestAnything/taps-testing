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
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;

class CmpWalkerTest: TAP {

    static int Main() {
        Plan(22);
        Autorun(typeof(CmpWalkerTest));
        return 0;
    }

    bool IsLeafType(CmpWalker cw,object o) {
        return cw.IsLeafType(o,o.GetType());
    }

#pragma warning disable 659
    
    class Leaf {
        public int A;
        public int B;
        public override bool Equals(object other) {
            return true;
        }
    }

    class Node {
        public int A;
        public int B;
    }

    struct LeafS {
        public int A;
        public int B;
        public override bool Equals(object other) {
            return true;
        }
    }

    struct NodeS {
        public int A;
        public int B;
    }

    void TestLeafType() {
        var cw=new CmpWalker(0,null);
        Ok(IsLeafType(cw,new object()));
        Ok(IsLeafType(cw,1));
        Ok(IsLeafType(cw,"a"));
        Ok(IsLeafType(cw,1.23));
        Ok(IsLeafType(cw,'a'));
        Ok(IsLeafType(cw,new Leaf()));
        Ok(!IsLeafType(cw,new Node()));
        Ok(IsLeafType(cw,new Node().A));
        Ok(IsLeafType(cw,new LeafS()));
        Ok(!IsLeafType(cw,new NodeS()));
        Ok(!IsLeafType(cw,new []{1,2}));
        Ok(!IsLeafType(cw,new Dictionary<int,int>{{1,2},{2,3}}));
    }

    void TestWalkHelper(GWalker.Node[] l,GWalker.Node[] r) {
        DumpNodes(l);
        IsDeeply(l.Select(x=>x.Key),r.Select(x=>x.Key));
        IsDeeply(l.Select(x=>x.O),r.Select(x=>x.O));
        IsDeeply(l.Select(x=>x.Dupidx),r.Select(x=>x.Dupidx));
        IsDeeply(l.Select(x=>x.Leaf),r.Select(x=>x.Leaf));
    }

    void DumpNodes(IEnumerable<GWalker.Node> nodes) {
        foreach(var i in nodes) {
            Diag(i.ToString());
        }
    }

    void TestWalk() {
        var cw=new CmpWalker(0,null);
        var path=new List<PathEntry>();
        TestWalkHelper(cw.Walk(null,1,path).ToArray(),new GWalker.Node[]{new GWalker.Node(null,1,-1,0,true)});
        IsDeeply(path,new[]{new PathEntry(0,new[]{0,0})});

        cw=new CmpWalker(0,null);
        var ass=new int[]{1,2};
        path=new List<PathEntry>();
        TestWalkHelper(cw.Walk(null,ass,path).ToArray(),new GWalker.Node[]{
                new GWalker.Node(null,ass,-1,2,false),new GWalker.Node(null,1,-1,0,true),
                new GWalker.Node(null,2,-1,0,true)});
        IsDeeply(path,new[]{new PathEntry(0,new[]{2,0}),new PathEntry(2)});
    }

}
