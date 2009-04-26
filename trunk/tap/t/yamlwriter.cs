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
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

class YAMLWriterTest: TAP {

    static int Main() {
        Plan(44);
        Autorun(typeof(YAMLWriterTest));
        return 0;
    }

    public class A {
        public int B;
        public int C;
        public A(int b,int c) {
            B=b;
            C=c;
        }
    }

    public class B {
        public A A;
        public double D;
    }

    public class C {
        public Dictionary<string,object> Dic=new Dictionary<string,object>();
    }

    string RunWriter(int initialdepth,object o) {
        return RunWriter(initialdepth,o,HorizontalThreshold,GetBBTs(),null);
    }

    string RunWriter(int initialdepth,object o,Func<int,YNode,string> anno) {
        return RunWriter(initialdepth,o,HorizontalThreshold,GetBBTs(),anno);
    }

    string RunWriter(int initialdepth,object o,int horizontalthreshold) {
        return RunWriter(initialdepth,o,horizontalthreshold,GetBBTs(),null);
    }

    string RunWriter(int initialdepth,object o,int horizontalthreshold,Func<int,YNode,string> anno) {
        return RunWriter(initialdepth,o,horizontalthreshold,GetBBTs(),anno);
    }

    string RunWriter(int initialdepth,object o,IDictionary<Type,bool> bbt) {
        return RunWriter(initialdepth,o,HorizontalThreshold,bbt,null);
    }
    
    string RunWriter(int initialdepth,object o,int horizontalthreshold,IDictionary<Type,bool> bbt) {
        return RunWriter(initialdepth,o,horizontalthreshold,bbt,null);
    }

    string RunWriter(int initialdepth,object o,int horizontalthreshold,IDictionary<Type,bool> bbt,Func<int,YNode,string> anno) {
        var w=new YAMLWriter(new StringWriter(),initialdepth,horizontalthreshold,bbt);
        w.Annotate=anno;
        return w.Write(o).ToString();
    }

    string ToLocalEOL(string s) {
        string localeol=new StringWriter().NewLine;
        // this assumes the .cs file has \r\n and that the compiler
        // doesn't convert that to the platform default for @ strings.
        return s.Replace("\r\n",localeol)
            // yamlwriter terminates lines according to the stream's
            // NewLine property. But if the <CR> and/or <LF> is part of
            // the string it is outputting, it outputs exactly what's in the string.
            // therefore use <CR> and <LF> in the 'expected' string if we
            // really want a \r or \n,
            .Replace("<CR>","\r").Replace("<LF>","\n");
    }

    string YString(string s,params int[] ps) {
        string d="";
        if(ps!=null && ps.Length!=0) d=new string(' ',ps[0]);
        return ToLocalEOL(string.Concat(d,@"---
",s,@"
",d,@"...
"));
    }

    void TestEscape() {
        var yw=new YAMLWriter(Console.Out,0,60);
        Is(yw.Escape("\u0087"),"\"\\u0087\"");
        Is(yw.Escape("\ufffe"),"\"\\ufffe\"");
        Is(yw.Escape("\ud900"),"\"\\ud900\"");
        Is(yw.Escape("\u0085\uffff"),"\"\\N\\uffff\"");
    }

    void TestScalar() {
        Is(RunWriter(2,1),YString("  1",2));
        Is(RunWriter(0,"hi!"),YString("hi!"));
        Is(RunWriter(0,"a\u0085b"),YString("|2\r\n  a\u0085  b"));
        Is(RunWriter(0,"\0hi!"),YString("\"\\0hi!\""));
        Is(RunWriter(0,"hi\nthere!"),YString("|2\r\n  hi\n  there!"));
        Is(RunWriter(0,"hi\rthere!\r\n"),YString("|2\r\n  hi<CR>  there!<CR>\n"));
        Is(RunWriter(0,'\t'),YString("\"\\t\""));
        Is(RunWriter(0,'\n'),YString("|2\r\n  \n"));
        Is(RunWriter(0,null),YString("~"));
    }

    void TestArray() {
        Is(RunWriter(0,new []{1,2,3}),YString("[1, 2, 3]"));
        string lng=new string('a',70);
        Is(RunWriter(2,new []{lng,"a\u0085b","c\u001ed"}),YString("  - "+lng+@"
  - |2
    a"+"\u0085"+@"    b
  - ""c\x1ed""",2));
        Is(RunWriter(2,new []{"a","a\u0085b","c\u001ed"}),YString(@"  [a, ""a\Nb"", ""c\x1ed""]",2));
        Is(RunWriter(2,new []{"a",null}),YString(@"  [a, ~]",2));
    }

    void TestDic() {
        var sdic=new Dictionary<string,string>{{"a","b"},{"c","d"},{"e\tt","f\r\ng"}};
        Is(RunWriter(0,sdic),YString(@"{a: b, c: d, ""e\tt"": ""f\r\ng""}"));
        string lng=new string('b',68);
        sdic["a"]=lng;
        Is(RunWriter(0,sdic),YString("a:      "+lng+@"
c:      d
""e\tt"": |2
  f<CR><LF>  g"));
        // we always tostring() keys for now. "? : " yaml syntax not implemented yet
        var adic=new Dictionary<A,string>{{new A(3,4),"a"}};
        Is(RunWriter(0,adic),YString("{YAMLWriterTest+A: a}"));
        var ndic=new Dictionary<string,string>{{"dick","tom"},{"sally",null}};
        Is(RunWriter(0,ndic),YString(@"{dick: tom, sally: ~}"));        
    }

    void TestComplex() {
        // array of map
        Is(RunWriter(0,new A[]{new A(5,6),new A(5,6),new A(6,7)}),
           YString(@"- {B: 5, C: 6}
- {B: 5, C: 6}
- {B: 6, C: 7}"));
        Is(RunWriter(0,new A[]{new A(5,6),new A(5,6),new A(6,7)},3),
           YString(@"- B: 5
  C: 6
- B: 5
  C: 6
- B: 6
  C: 7"));
        // map of array
        Is(RunWriter(0,new Dictionary<int,int[]>{{2,new []{1,2}},{1,new []{3,2}}}),
           YString(@"1: [3, 2]
2: [1, 2]"));
    }

    void TestFields() {
        var a=new A(5,6);
        Is(RunWriter(0,a),YString(@"{B: 5, C: 6}"));
        Is(RunWriter(0,a,3),YString(@"B: 5
C: 6"));
        Is(RunWriter(0,new B{A=new A(7,8), D=(double)1.23}),YString(@"A: {B: 7, C: 8}
D: 1.23"));
        Is(RunWriter(0,new B{A=new A(7,8), D=(double)1.23},3),YString("A: "+
@"
  B: 7
  C: 8
D: 1.23"));
    }

    void TestRef() {
        var a=new A(4,9);
        A[] aas=new A[]{a,a,a};
        Is(RunWriter(0,aas),YString(@"- &id1
  {B: 4, C: 9}
- *id1
- *id1"));
        Is(RunWriter(0,aas,3),YString(@"- &id1
  B: 4
  C: 9
- *id1
- *id1"));
    }

    void TestCycle() {
        var c=new C();
        var c2=new C();
        c2.Dic["a"]=c2;
        c.Dic["x"]=c2;
        Is(RunWriter(0,c),YString("Dic: \r\n  x: &id2\r\n"+
"    Dic: "+
"\r\n      a: *id2"));
        Is(RunWriter(0,c2),YString(@"&id0
Dic: "+
"\r\n  a: *id0"));
    }

    void TestBlackBox() {
        var dic=new Dictionary<Type,bool>{{typeof(int),true}};
        Is(RunWriter(0,new A(1,2),dic),YString(@"{B: 1, C: 2}"));
        dic.Add(typeof(A),true);
        Is(RunWriter(0,new B{A=new A(1,2),D=1.5},dic),YString(@"{A: YAMLWriterTest+A, D: 1.5}"));
    }

    void TestRE() {
        // a regex is a pretty deep graph with Pointers, It had better be caught by the default BBD
        Skip("depends on .net internals",1,()=>VM==".net",()=>{
                Regex re=new Regex("1");
                Like(RunWriter(0,re),new Regex(@"\bSystem\.Reflection\.Pointer\b"));
            });
    }

    string Annotator(int i,YNode n) {
        return string.Format("node {0} depth {1}",i,n.Depth);        
    }

    void TestAnnotate() {
        Is(RunWriter(0,1,Annotator),YString("1  # node 0 depth 0"));
        Is(RunWriter(0,new[]{1,2},3,Annotator),YString(@"  # node 0 depth 0
- 1  # node 1 depth 1
- 2  # node 2 depth 1"));
        Is(RunWriter(0,new A(1,2),3,Annotator),YString(@"  # node 0 depth 0
B: 1  # node 1 depth 1
C: 2  # node 2 depth 1"));
        var a=new A(4,9);
        A[] aas=new A[]{a,a,a};
        Is(RunWriter(0,aas,3,Annotator),YString(@"  # node 0 depth 0
- &id1  # node 1 depth 1
  B: 4  # node 2 depth 2
  C: 9  # node 3 depth 2
- *id1  # node 4 depth 1
- *id1  # node 5 depth 1"));
        Is(RunWriter(0,new A[]{new A(5,6),new A(5,6)},3,Annotator),
           YString(@"  # node 0 depth 0
-   # node 1 depth 1
  B: 5  # node 2 depth 2
  C: 6  # node 3 depth 2
-   # node 4 depth 1
  B: 5  # node 5 depth 2
  C: 6  # node 6 depth 2"));
    }

    void TestCompactAnnotate() {
        Is(RunWriter(0,new[]{1,2},Annotator),YString(@"  # node 0 depth 0
[1, 2]
#^node 1 depth 1
#   ^node 2 depth 1"));
    Is(RunWriter(0,new A(1,2),Annotator),YString(@"  # node 0 depth 0
{B: 1, C: 2}
#^node 1 depth 1
#      ^node 2 depth 1"));
        var a=new A(4,9);
        A[] aas=new A[]{a,a,a};
        Is(RunWriter(0,aas,Annotator),YString(@"  # node 0 depth 0
- &id1  # node 1 depth 1
  {B: 4, C: 9}
  #^node 2 depth 2
  #      ^node 3 depth 2
- *id1  # node 4 depth 1
- *id1  # node 5 depth 1"));
        Is(RunWriter(0,new A[]{new A(5,6),new A(5,6)},Annotator),
           YString(@"  # node 0 depth 0
-   # node 1 depth 1
  {B: 5, C: 6}
  #^node 2 depth 2
  #      ^node 3 depth 2
-   # node 4 depth 1
  {B: 5, C: 6}
  #^node 5 depth 2
  #      ^node 6 depth 2"));
    }

}
