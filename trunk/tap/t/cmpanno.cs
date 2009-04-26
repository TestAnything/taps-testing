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
using System.Diagnostics;
using System.Collections.Specialized;
using System.Collections.Generic;

class CmpAnnoTest: TAP {

    static int Main() {
        Plan(11);
        Autorun(typeof(CmpAnnoTest));
        return 0;
    }

    string Helper(object l,object r) {
        var dc=new DeepCmp();
        List<PathEntry> path;
        dc.Compare(l,r,out path);
        //        Dump("path",path);
        var sw=new StringWriter();
        sw.NewLine="\r\n";
        var cw=new YAMLCommentWriter(sw,null);
        var dic=new OrderedDictionary{{"a","blah"},{"actual",l},{"expected",r}};
        cw.WriteComment(0,dic,path,null);
        return sw.ToString();
    }

    void TestScalar() {
        // an annotation on the actual/expected key itself is rather silly
        Is(Helper(1,2),@"  ---
  {a: blah, actual: 1, expected: 2}
  ...
");
    }

    void TestArray() {
        Is(Helper(new[]{1,2},new[]{1,3}),@"  ---
  a:        blah
  actual:   [1, 2]
            #   ^HERE
  expected: [1, 3]
            #   ^HERE
  ...
");
        Is(Helper(new[]{1,2},new[]{1,2,3}),@"  ---
  a:        blah
  actual:     # COUNT MISMATCH 2 vs 3
    [1, 2]
  expected:   # COUNT MISMATCH 3 vs 2
    [1, 2, 3]
    #      ^HERE
  ...
");
        Is(Helper(new[]{1,2,3},new[]{1,2}),@"  ---
  a:        blah
  actual:     # COUNT MISMATCH 3 vs 2
    [1, 2, 3]
    #      ^HERE
  expected:   # COUNT MISMATCH 2 vs 3
    [1, 2]
  ...
");
    }

    void TestDic() {
        Is(Helper(new Dictionary<string,string>{{"tom","dick"},{"harry","sally"}},
                  new Dictionary<string,string>{{"tom","dick"},{"harry","saly"}}),@"  ---
  a:        blah
  actual:   {harry: sally, tom: dick}
            #^HERE
  expected: {harry: saly, tom: dick}
            #^HERE
  ...
");
        Is(Helper(new Dictionary<string,string>{{"tom","dick"},{"harry","sally"}},
                  new Dictionary<string,string>{{"tim","dick"},{"harry","sally"}}),@"  ---
  a:        blah
  actual:   {harry: sally, tom: dick}
            #              ^HERE
  expected: {harry: sally, tim: dick}
            #              ^HERE
  ...
");
        Is(Helper(new Dictionary<string,string>{{"tom","dick"},{"harry","sally"}},
                  new Dictionary<string,string>{{"tim","dick"},{"harry","sally"},{"zappa","frank"}}),@"  ---
  a:        blah
  actual:     # COUNT MISMATCH 2 vs 3
    {harry: sally, tom: dick}
    #              ^HERE
  expected:   # COUNT MISMATCH 3 vs 2
    {harry: sally, tim: dick, zappa: frank}
    #              ^HERE
  ...
");
        Is(Helper(new Dictionary<string,string>{{"tom","dick"},{"harry","sally"}},
                  new Dictionary<string,string>{{"tom","dick"},{"harry","sally"},{"zappa","frank"}}),@"  ---
  a:        blah
  actual:     # COUNT MISMATCH 2 vs 3
    {harry: sally, tom: dick}
  expected:   # COUNT MISMATCH 3 vs 2
    {harry: sally, tom: dick, zappa: frank}
    #                         ^HERE
  ...
");
    }

    void TestFalseDeep() {
        //make sure the greatest depth doesn't still have
        //actual[0].Counts[1] in path, printing a false here
        Is(Helper(new[]{new PathEntry(0,new int[]{1,1}),new PathEntry(1)},new[]{new PathEntry(0,new int[]{1,1}),new PathEntry(0)}),@"  ---
  a:        blah
  actual:   
    - Idx:    0
      Counts: [1, 1]
    -   # HERE
      Idx:    1  # HERE
      Counts: [0, 0]
  expected: 
    - Idx:    0
      Counts: [1, 1]
    -   # HERE
      Idx:    0  # HERE
      Counts: [0, 0]
  ...
");
    }

    class A  {
        public int B;
        public int C;
    }

    class D: A {
        public int E;
    }

    void TestFields() {
        Is(Helper(new A{B=1,C=2},new A{B=1,C=2}),@"  ---
  a:        blah
  actual:   {B: 1, C: 2}
  expected: {B: 1, C: 2}
  ...
");
        Is(Helper(new A{B=1,C=2},new D{B=1,C=2,E=3}),@"  ---
  a:        blah
  actual:     # COUNT MISMATCH 2 vs 3
    {B: 1, C: 2}
    #^HERE
  expected:   # COUNT MISMATCH 3 vs 2
    {E: 3, B: 1, C: 2}
    #^HERE
  ...
");
    }
    
}
