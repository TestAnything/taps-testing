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
using System.Collections;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Taps {

    public abstract class GWalker {

        public class Node {
            public object Key;
            public object O;
            public int Dupidx;
            public int Count;
            public bool Leaf;
            public Node(object key,object o,int dupidx,int count,bool leaf) {
                Key=key;
                O=o;
                Dupidx=dupidx;
                Count=count;
                Leaf=leaf;
            }
            public override string ToString() {
                return string.Format("key {0} o {1} dupidx {2} count {3} leaf {4}",Key,O,Dupidx,Count,Leaf);
            }
        }

        public class ReferenceEqualityComparer: IEqualityComparer<object> {

            bool IEqualityComparer<object>.Equals(object l,object r) {
                return object.ReferenceEquals(l,r);
            }

            int IEqualityComparer<object>.GetHashCode(object l) {
                return l.GetHashCode(); // should be object.GetHashCode, never derived, but how ?
            }
        }

        const int MaxDepth=1000;

        protected int Depth=0;
        protected int NodeCount=0;
        Dictionary<object,int> DuplicateDic=new Dictionary<object,int>(new ReferenceEqualityComparer());
        IDictionary<Type,bool> BlackBoxTypes;

        public GWalker(IDictionary<Type,bool> blackBoxTypes) {
            BlackBoxTypes=blackBoxTypes;
        }
        
        Node CheckDuplicate(object key,object o,bool leaf) {
            int v;
            if(DuplicateDic.TryGetValue(o,out v)) {
                NodeCount++;
                return new Node(key,o,v,0,leaf);
            }
            return null;
        }

        Node ProcRefNode(object key,object o,int count,bool leaf) {
            DuplicateDic.Add(o,NodeCount);
            NodeCount++;
            return new Node(key,o,-1,count,leaf);
        }

        Node ProcNode(object key,object o,bool leaf) {
            NodeCount++;
            return new Node(key,o,-1,0,leaf);
        }

        IEnumerable<Node> WalkDictionary(IDictionary dic) {
            ICollection coll=dic.Keys;
            var arr=new object[coll.Count];
            coll.CopyTo(arr,0);
            if(!(dic is IOrderedDictionary)) {
                Array.Sort(arr);
            }
            return from i in arr from j in Walk(i,dic[i]) select j;
        }

        static int CountEnum(IEnumerable enm) {
            int n=0;
            var etor=enm.GetEnumerator();
            while(etor.MoveNext()) ++n;
            return n;
        }
        
        IEnumerable<Node> WalkEnumerable(IEnumerable enu) {
             return from i in Enumerable.Cast<object>(enu)
                 from j in Walk(null,i)
                 select j;
        }

        IEnumerable<Node> WalkObject(FieldInfo[] fi,object o) {
            return from i in fi from j in Walk(i.Name,i.GetValue(o)) select j;
        }

        public IEnumerable<Node> Walk(object key,object o) {
            if(Depth==MaxDepth) {
                yield return ProcNode(key,"maxdepth reached",true);
            } else if(o==null) {
                yield return ProcNode(key,o,true);
            } else {
                Type t=o.GetType();
                Node d=null;
                if(IsLeafType(o,t) || IsBlackBoxType(t)) {
                    if(t.IsClass) {
                        if((d=CheckDuplicate(key,o,true))!=null) {
                            yield return d;
                        } else {
                            yield return ProcRefNode(key,o,0,true);
                        }
                    } else {
                        yield return ProcNode(key,o,true);
                    }
                } else if(o is IDictionary)  {
                    if((d=CheckDuplicate(key,o,false))!=null) {
                        yield return d;
                    } else {
                        var dic=(IDictionary)o;
                        yield return ProcRefNode(key,o,dic.Count,false);
                        ++Depth;
                        foreach(var i in WalkDictionary(dic)) {
                            yield return i;
                        }
                        --Depth;
                    }
                } else if(o is IEnumerable) {
                    if((d=CheckDuplicate(key,o,false))!=null) {
                        yield return d;
                    } else {
                        var enm=(IEnumerable)o;
                        yield return ProcRefNode(key,o,CountEnum(enm),false);
                        ++Depth;
                        foreach(var i in WalkEnumerable(enm)) {
                            yield return i;
                        }
                        --Depth;
                    }
                } else {
                    if(t.IsClass && (d=CheckDuplicate(key,o,false))!=null) {
                        yield return d;
                    } else {
                        FieldInfo[] fi=t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                        yield return ProcRefNode(key,o,fi.Length,false);
                        ++Depth;
                        foreach(var i in WalkObject(fi,o)) {
                            yield return i;
                        }
                        --Depth;
                    }
                }
            }
        }

        public abstract bool IsLeafType(object o,Type t);

        bool IsBlackBoxType(Type t) {
            bool isbb=false;
            if(BlackBoxTypes!=null) BlackBoxTypes.TryGetValue(t,out isbb);
            return isbb;
        }

    }

}
