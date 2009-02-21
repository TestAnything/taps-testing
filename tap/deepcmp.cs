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

using System;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;

namespace Taps {

    public class PathEntry {
        public int Idx;
        public int[] Counts;
        public PathEntry(int idx):this(idx,new int[2]) {
        }
        public PathEntry(int idx,int[] counts) {
            Idx=idx;
            Counts=counts;
        }
        public PathEntry():this(0,new int[2]) {}
    }
    
    public class CmpWalker: GWalker {

        public List<PathEntry> Path=null;

        static MethodInfo ObjectEquals=((Func<object,bool>)new object().Equals).Method;

        struct DummyVT {};

        static MethodInfo ValueTypeEquals=((Func<object,bool>)new DummyVT().Equals).Method;

        int Participant;

        public CmpWalker(int participant,IDictionary<Type,bool> bbd):base(bbd) {
            Participant=participant;
        }
        
        public override bool IsLeafType(object o,Type t) {
            MethodInfo ometh=((Func<object,bool>)o.Equals).Method;
            if(t.IsClass) {
                return ObjectEquals!=ometh || t==typeof(object);
            }
            return ValueTypeEquals!=ometh;
        }

        public IEnumerable<Node> Walk(object key,object o,List<PathEntry> path) {
            foreach(Node i in base.Walk(key,o)) {
                int curnode=NodeCount-1;
                if(Depth==path.Count) {
                    path.Add(new PathEntry());
                } else {
                    int k=path.Count-Depth-1;
                    if(k>0) path.RemoveRange(Depth,k);
                }
                PathEntry p=path[Depth];
                p.Idx=curnode;
                p.Counts[Participant]=i.Count;
                yield return i;
            }
        }

    }

    public class DeepCmp {

        public enum Result {Eq,KeyNe,ValNe,LeftLonger,RightLonger};

        IDictionary<Type,bool> BBD;

        public DeepCmp(IDictionary<Type,bool> bbd) {
            BBD=bbd;
        }

        public DeepCmp() {
        }
        
        public Result CmpLists(IEnumerable<GWalker.Node> l,IEnumerable<GWalker.Node> r) {
            IEnumerator<GWalker.Node> le=l.GetEnumerator();
            IEnumerator<GWalker.Node> re=r.GetEnumerator();
            do {
                bool lp=le.MoveNext();
                bool rp=re.MoveNext();
                if(!lp && !rp) return Result.Eq;
                if(!rp) return Result.LeftLonger;
                if(!lp) return Result.RightLonger;
                var ln=le.Current;
                var rn=re.Current;
                if(!object.Equals(ln.Key,rn.Key)) return Result.KeyNe;
                if(ln.Leaf^rn.Leaf) return Result.ValNe;
                if(ln.Leaf && !object.Equals(ln.O,rn.O)) return Result.ValNe;
            } while(true);
        }

        public Result Compare(object l,object r,out List<PathEntry> path) {
            path=new List<PathEntry>();;
            var lw=new CmpWalker(0,BBD);
            var rw=new CmpWalker(1,BBD);
            var result=CmpLists(lw.Walk(null,l,path),rw.Walk(null,r,path));
            if(result==Result.Eq) path=null;
            return result;
        }
        
    }
    
}