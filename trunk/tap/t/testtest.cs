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


// Looking for samples ? Don't look here. Look in samples.

using Taps;

using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;
using System.Linq;

class TestTest: TAP  {

    static string OkExpected=@"1..6
ok 1
ok 2 - a description for ok(true)
not ok 3
  ---
  severity: fail
/  file:     t[\\\/]testok\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestOk.Main
  ...
ok 4
not ok 5 - a desc for ok(()=>false)
  ---
  severity: fail
/  file:     t[\\\/]testok\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestOk.Main
  ...
not ok 6
  ---
  severity: fail
/  file:     t[\\\/]testok\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestOk.Main
  ...
# FAILED. 3/6 tests passed (50%)
/^# Wall clock time: .*$/";

    static string TerseOkExpected=@"1..6
ok 1
ok 2 - a description for ok(true)
not ok 3
/^#   failed test 3 \(t[\\\/]testok\.cs\.notcs at pos 30,9 in TestOk\.Main\)$/
ok 4
not ok 5 - a desc for ok(()=>false)
/^#   failed test 5 \(t[\\\/]testok\.cs\.notcs at pos 32,9 in TestOk\.Main\)$/
not ok 6
/^#   failed test 6 \(t[\\\/]testok\.cs\.notcs at pos 33,9 in TestOk\.Main\)$/
# FAILED. 3/6 tests passed (50%)
/^# Wall clock time: .*$/";

    static string VSOkExpected=@"1..6
ok 1
ok 2 - a description for ok(true)
not ok 3
/^t[\\\/]testok\.cs\.notcs\(30,9\): warning T3: $/
ok 4
not ok 5 - a desc for ok(()=>false)
/^t[\\\/]testok\.cs\.notcs\(32,9\): warning T5: a desc for ok\(\(\)=>false\)\. $/
not ok 6
/^t[\\\/]testok\.cs\.notcs\(33,9\): warning T6: $/
# FAILED. 3/6 tests passed (50%)
/^# Wall clock time: .*$/";

    static string IsExpected=@"1..16
ok 1
ok 2 - a description for Is(1,1)
ok 3
ok 4
not ok 5
  ---
  severity: fail
/  file:     t[\\\/]testis\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestIs.Main
  actual:   ~
  expected: a
  ...
not ok 6
  ---
  severity: fail
/  file:     t[\\\/]testis\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestIs.Main
  actual:   a
  expected: ~
  ...
not ok 7
  ---
  severity: fail
/  file:     t[\\\/]testis\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestIs.Main
  actual:   1
  expected: 2
  ...
not ok 8
  ---
  severity: fail
/  file:     t[\\\/]testis\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestIs.Main
  actual:   b
  expected: c
  ...
not ok 9
  ---
  severity:   fail
/  file:       t[\\\/]testis\.cs\.notcs/
/  line:       \d+/
  column:     9
  method:     TestIs.Main
  actual:     1
  expected:   1
  extensions: {cmp: !=}
  ...
not ok 10 - a description for Isnt(1,1)
  ---
  severity:   fail
/  file:       t[\\\/]testis\.cs\.notcs/
/  line:       \d+/
  column:     9
  method:     TestIs.Main
  actual:     1
  expected:   1
  extensions: {cmp: !=}
  ...
not ok 11
  ---
  severity:   fail
/  file:       t[\\\/]testis\.cs\.notcs/
/  line:       \d+/
  column:     9
  method:     TestIs.Main
  actual:     abc
  expected:   abc
  extensions: {cmp: !=}
  ...
not ok 12
  ---
  severity:   fail
/  file:       t[\\\/]testis\.cs\.notcs/
/  line:       \d+/
  column:     9
  method:     TestIs.Main
  actual:     ~
  expected:   ~
  extensions: {cmp: !=}
  ...
ok 13
ok 14
ok 15
ok 16
# FAILED. 8/16 tests passed (50%)
/^# Wall clock time: .*$/";

    static string TerseIsExpected=@"1..16
ok 1
ok 2 - a description for Is(1,1)
ok 3
ok 4
not ok 5
/^#   failed test 5 \(t[\\\/]testis\.cs\.notcs at pos 36,9 in TestIs\.Main\)$/
#        got: '(null)'
#   expected: 'a'
not ok 6
/^#   failed test 6 \(t[\\\/]testis\.cs\.notcs at pos 37,9 in TestIs\.Main\)$/
#        got: 'a'
#   expected: '(null)'
not ok 7
/^#   failed test 7 \(t[\\\/]testis\.cs\.notcs at pos 38,9 in TestIs\.Main\)$/
#        got: '1'
#   expected: '2'
not ok 8
/^#   failed test 8 \(t[\\\/]testis\.cs\.notcs at pos 39,9 in TestIs\.Main\)$/
#        got: 'b'
#   expected: 'c'
not ok 9
/^#   failed test 9 \(t[\\\/]testis\.cs\.notcs at pos 41,9 in TestIs\.Main\)$/
#   '1'
#   !=
#   '1'
not ok 10 - a description for Isnt(1,1)
/^#   failed test 10 \(t[\\\/]testis\.cs\.notcs at pos 42,9 in TestIs\.Main\)$/
#   '1'
#   !=
#   '1'
not ok 11
/^#   failed test 11 \(t[\\\/]testis\.cs\.notcs at pos 43,9 in TestIs\.Main\)$/
#   'abc'
#   !=
#   'abc'
not ok 12
/^#   failed test 12 \(t[\\\/]testis\.cs\.notcs at pos 44,9 in TestIs\.Main\)$/
#   '(null)'
#   !=
#   '(null)'
ok 13
ok 14
ok 15
ok 16
# FAILED. 8/16 tests passed (50%)
/^# Wall clock time: .*$/";

    static string VSIsExpected=@"1..16
ok 1
ok 2 - a description for Is(1,1)
ok 3
ok 4
not ok 5
/^t[\\\/]testis\.cs\.notcs\(36,9\): warning T5: got: '\(null\)' expected: 'a'$/
not ok 6
/^t[\\\/]testis\.cs\.notcs\(37,9\): warning T6: got: 'a' expected: '\(null\)'$/
not ok 7
/^t[\\\/]testis\.cs\.notcs\(38,9\): warning T7: got: '1' expected: '2'$/
not ok 8
/^t[\\\/]testis\.cs\.notcs\(39,9\): warning T8: got: 'b' expected: 'c'$/
not ok 9
/^t[\\\/]testis\.cs\.notcs\(41,9\): warning T9: '1' != '1'$/
not ok 10 - a description for Isnt(1,1)
/^t[\\\/]testis\.cs\.notcs\(42,9\): warning T10: a description for Isnt\(1,1\)\. '1' != '1'$/
not ok 11
/^t[\\\/]testis\.cs\.notcs\(43,9\): warning T11: 'abc' != 'abc'$/
not ok 12
/^t[\\\/]testis\.cs\.notcs\(44,9\): warning T12: '\(null\)' != '\(null\)'$/
ok 13
ok 14
ok 15
ok 16
# FAILED. 8/16 tests passed (50%)
/^# Wall clock time: .*$/";

    static string IsDeeplyExpected=@"1..6
ok 1 - single
not ok 2 - single but wrong
  ---
  severity: fail
/  file:     t[\\\/]testisdeeply\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestIsDeeply.Main
  actual:   1
  expected: 2
  ...
ok 3
not ok 4 - got is shorter
  ---
  severity: fail
/  file:     t[\\\/]testisdeeply\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestIsDeeply.Main
  actual:     # COUNT MISMATCH 2 vs 3
    [1, 2]
  expected:   # COUNT MISMATCH 3 vs 2
    [1, 2, 3]
    #      ^HERE
  ...
not ok 5 - expected is shorter
  ---
  severity: fail
/  file:     t[\\\/]testisdeeply\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestIsDeeply.Main
  actual:     # COUNT MISMATCH 3 vs 2
    [1, 2, 3]
    #      ^HERE
  expected:   # COUNT MISMATCH 2 vs 3
    [1, 2]
  ...
not ok 6 - 3rd element is different
  ---
  severity: fail
/  file:     t[\\\/]testisdeeply\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestIsDeeply.Main
  actual:   [1, 2, 3]
            #      ^HERE
  expected: [1, 2, 4]
            #      ^HERE
  ...
# FAILED. 2/6 tests passed (33%)
/^# Wall clock time: .*$/";

    static string TerseIsDeeplyExpected=@"1..6
ok 1 - single
not ok 2 - single but wrong
/^#   failed test 2 \(t[\\\/]testisdeeply\.cs\.notcs at pos 33,9 in TestIsDeeply\.Main\)$/
#        got: '1'
#   expected: '2'
ok 3
not ok 4 - got is shorter
/^#   failed test 4 \(t[\\\/]testisdeeply\.cs\.notcs at pos 35,9 in TestIsDeeply\.Main\)$/
#   got:
    ---
      # COUNT MISMATCH 2 vs 3
    [1, 2]
    ...
#   expected:
    ---
      # COUNT MISMATCH 3 vs 2
    [1, 2, 3]
    #      ^HERE
    ...
not ok 5 - expected is shorter
/^#   failed test 5 \(t[\\\/]testisdeeply\.cs\.notcs at pos 36,9 in TestIsDeeply\.Main\)$/
#   got:
    ---
      # COUNT MISMATCH 3 vs 2
    [1, 2, 3]
    #      ^HERE
    ...
#   expected:
    ---
      # COUNT MISMATCH 2 vs 3
    [1, 2]
    ...
not ok 6 - 3rd element is different
/^#   failed test 6 \(t[\\\/]testisdeeply\.cs\.notcs at pos 37,9 in TestIsDeeply\.Main\)$/
#   got:
    ---
    [1, 2, 3]
    #      ^HERE
    ...
#   expected:
    ---
    [1, 2, 4]
    #      ^HERE
    ...
# FAILED. 2/6 tests passed (33%)
/^# Wall clock time: .*$/";

    static string VSIsDeeplyExpected=@"1..6
ok 1 - single
not ok 2 - single but wrong
/^t[\\\/]testisdeeply\.cs\.notcs\(33,9\): warning T2: single but wrong\. got: '1' expected: '2'$/
ok 3
not ok 4 - got is shorter
/^t[\\\/]testisdeeply\.cs\.notcs\(35,9\): warning T4: got is shorter\. actual not as expected$/
  got:
    ---
      # COUNT MISMATCH 2 vs 3
    [1, 2]
    ...
  expected:
    ---
      # COUNT MISMATCH 3 vs 2
    [1, 2, 3]
    #      ^HERE
    ...
not ok 5 - expected is shorter
/^t[\\\/]testisdeeply\.cs\.notcs\(36,9\): warning T5: expected is shorter\. actual not as expected$/
  got:
    ---
      # COUNT MISMATCH 3 vs 2
    [1, 2, 3]
    #      ^HERE
    ...
  expected:
    ---
      # COUNT MISMATCH 2 vs 3
    [1, 2]
    ...
not ok 6 - 3rd element is different
/^t[\\\/]testisdeeply\.cs\.notcs\(37,9\): warning T6: 3rd element is different\. actual not as expected$/
  got:
    ---
    [1, 2, 3]
    #      ^HERE
    ...
  expected:
    ---
    [1, 2, 4]
    #      ^HERE
    ...
# FAILED. 2/6 tests passed (33%)
/^# Wall clock time: .*$/";

    static string LikeExpected=@"1..10
ok 1
ok 2 - with name
ok 3
not ok 4
  ---
  severity:   fail
/  file:       t[\\\/]testlike\.cs\.notcs/
/  line:       \d+/
  column:     9
  method:     TestLike.Main
  actual:     11
  expected:   ^1$
  extensions: {cmp: =~}
  ...
not ok 5 - avec nom
  ---
  severity:   fail
/  file:       t[\\\/]testlike\.cs\.notcs/
/  line:       \d+/
  column:     9
  method:     TestLike.Main
  actual:     abc
  expected:   ^b
  extensions: {cmp: =~}
  ...
not ok 6
  ---
  severity:   fail
/  file:       t[\\\/]testlike\.cs\.notcs/
/  line:       \d+/
  column:     9
  method:     TestLike.Main
  actual:     1
  expected:   ^1$
  extensions: {cmp: !~}
  ...
not ok 7 - with name
  ---
  severity:   fail
/  file:       t[\\\/]testlike\.cs\.notcs/
/  line:       \d+/
  column:     9
  method:     TestLike.Main
  actual:     1
  expected:   ^1$
  extensions: {cmp: !~}
  ...
not ok 8
  ---
  severity:   fail
/  file:       t[\\\/]testlike\.cs\.notcs/
/  line:       \d+/
  column:     9
  method:     TestLike.Main
  actual:     abc
  expected:   b
  extensions: {cmp: !~}
  ...
ok 9
ok 10 - avec nom
# FAILED. 5/10 tests passed (50%)
/^# Wall clock time: .*$/";

    static string TerseLikeExpected=@"1..10
ok 1
ok 2 - with name
ok 3
not ok 4
/^#   failed test 4 \(t[\\\/]testlike\.cs\.notcs at pos 35,9 in TestLike\.Main\)$/
#   '11'
#   =~
#   '^1$'
not ok 5 - avec nom
/^#   failed test 5 \(t[\\\/]testlike\.cs\.notcs at pos 36,9 in TestLike\.Main\)$/
#   'abc'
#   =~
#   '^b'
not ok 6
/^#   failed test 6 \(t[\\\/]testlike\.cs\.notcs at pos 37,9 in TestLike\.Main\)$/
#   '1'
#   !~
#   '^1$'
not ok 7 - with name
/^#   failed test 7 \(t[\\\/]testlike\.cs\.notcs at pos 38,9 in TestLike\.Main\)$/
#   '1'
#   !~
#   '^1$'
not ok 8
/^#   failed test 8 \(t[\\\/]testlike\.cs\.notcs at pos 39,9 in TestLike\.Main\)$/
#   'abc'
#   !~
#   'b'
ok 9
ok 10 - avec nom
# FAILED. 5/10 tests passed (50%)
/^# Wall clock time: .*$/";

    static string VSLikeExpected=@"1..10
ok 1
ok 2 - with name
ok 3
not ok 4
/^t[\\\/]testlike\.cs\.notcs\(35,9\): warning T4: '11' =~ '\^1\$'$/
not ok 5 - avec nom
/^t[\\\/]testlike\.cs\.notcs\(36,9\): warning T5: avec nom\. 'abc' =~ '\^b'$/
not ok 6
/^t[\\\/]testlike\.cs\.notcs\(37,9\): warning T6: '1' !~ '\^1\$'$/
not ok 7 - with name
/^t[\\\/]testlike\.cs\.notcs\(38,9\): warning T7: with name\. '1' !~ '\^1\$'$/
not ok 8
/^t[\\\/]testlike\.cs\.notcs\(39,9\): warning T8: 'abc' !~ 'b'$/
ok 9
ok 10 - avec nom
# FAILED. 5/10 tests passed (50%)
/^# Wall clock time: .*$/";

    static string CmpOkExpected=@"1..4
ok 1 - cmp11
ok 2
ok 3
not ok 4
  ---
  severity:   fail
/  file:       t[\\\/]testcmpok\.cs\.notcs/
/  line:       \d+/
  column:     9
  method:     TestCmpOk.Main
  actual:     1
  expected:   2
/^  extensions: \{cmp: TestCmpOk\.<Main>\w__2\}$/
  ...
# FAILED. 3/4 tests passed (75%)
/^# Wall clock time: .*$/";

    static string TerseCmpOkExpected=@"1..4
ok 1 - cmp11
ok 2
ok 3
not ok 4
/^#   failed test 4 \(t[\\\/]testcmpok\.cs\.notcs at pos 36,9 in TestCmpOk\.Main\)$/
#   '1'
/^#   TestCmpOk.<Main>\w__2$/
#   '2'
# FAILED. 3/4 tests passed (75%)
/^# Wall clock time: .*$/";

    static string VSCmpOkExpected=@"1..4
ok 1 - cmp11
ok 2
ok 3
not ok 4
/^t[\\\/]testcmpok\.cs\.notcs\(36,9\): warning T4: '1' TestCmpOk\.<Main>\w__2 '2'$/
# FAILED. 3/4 tests passed (75%)
/^# Wall clock time: .*$/";

    static string MiscExpected=@"1..24
ok 1 - phew
not ok 2 - uh oh
  ---
  severity: fail
/  file:     t[\\\/]testmisc\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestMisc.Main
  ...
# some comment
ok 3
not ok 4
  ---
  severity: fail
/  file:     t[\\\/]testmisc\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestMisc.Main
  ...
# so that went wrong, huh ?
ok 5
not ok 6
  ---
  severity: fail
/  file:     t[\\\/]testmisc\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestMisc.Main
  actual:   1
  expected: 2
  ...
# so that went wrong, huh ?
ok 7
not ok 8
  ---
  severity:   fail
/  file:       t[\\\/]testmisc\.cs\.notcs/
/  line:       \d+/
  column:     9
  method:     TestMisc.Main
  actual:     1
  expected:   1
  extensions: {cmp: !=}
  ...
# so that went wrong, huh ?
ok 9
not ok 10
  ---
  severity: fail
/  file:     t[\\\/]testmisc\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestMisc.Main
  actual:   1
  expected: 2
  ...
# so that went wrong, huh ?
ok 11
not ok 12
  ---
  severity: fail
/  file:     t[\\\/]testmisc\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestMisc.Main
  actual:   [1, 2, 3]
            #   ^HERE
  expected: [1, 3, 2]
            #   ^HERE
  ...
# so that went wrong, huh ?
ok 13
not ok 14
  ---
  severity:   fail
/  file:       t[\\\/]testmisc\.cs\.notcs/
/  line:       \d+/
  column:     9
  method:     TestMisc.Main
  actual:     1
  expected:   ^11$
  extensions: {cmp: =~}
  ...
# so that went wrong, huh ?
ok 15
not ok 16
  ---
  severity:   fail
/  file:       t[\\\/]testmisc\.cs\.notcs/
/  line:       \d+/
  column:     9
  method:     TestMisc.Main
  actual:     1
  expected:   ^1$
  extensions: {cmp: !~}
  ...
# so that went wrong, huh ?
ok 17
not ok 18
  ---
  severity:   fail
/  file:       t[\\\/]testmisc\.cs\.notcs/
/  line:       \d+/
  column:     9
  method:     TestMisc.Main
  actual:     1
  expected:   2
  extensions: {cmp: System.Object.Equals}
  ...
# so that went wrong, huh ?
ok 19 - this
not ok 20 - this
  ---
  severity: fail
/  file:     t[\\\/]testmisc\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestMisc.Main
  ...
# so that went wrong, huh ?
ok 21
not ok 22
  ---
  severity: fail
/  file:     t[\\\/]testmisc\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestMisc.Main
  actual:   System.String
  expected: System.Int32
  ...
# so that went wrong, huh ?
ok 23
not ok 24
  ---
  message:  the exception was not (a child) of the expected type
  severity: fail
/  file:     t[\\\/]testmisc\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestMisc.Main
  actual:   ApplicationException
  expected: SystemException
  ...
# so that went wrong, huh ?
# FAILED. 12/24 tests passed (50%)
/^# Wall clock time: .*$/";

    static string TerseMiscExpected=@"1..24
ok 1 - phew
not ok 2 - uh oh
/^#   failed test 2 \(t[\\\/]testmisc\.cs\.notcs at pos 33,9 in TestMisc\.Main\)$/
# some comment
ok 3
not ok 4
/^#   failed test 4 \(t[\\\/]testmisc\.cs\.notcs at pos 36,9 in TestMisc\.Main\)$/
# so that went wrong, huh ?
ok 5
not ok 6
/^#   failed test 6 \(t[\\\/]testmisc\.cs\.notcs at pos 38,9 in TestMisc\.Main\)$/
#        got: '1'
#   expected: '2'
# so that went wrong, huh ?
ok 7
not ok 8
/^#   failed test 8 \(t[\\\/]testmisc\.cs\.notcs at pos 40,9 in TestMisc\.Main\)$/
#   '1'
#   !=
#   '1'
# so that went wrong, huh ?
ok 9
not ok 10
/^#   failed test 10 \(t[\\\/]testmisc\.cs\.notcs at pos 42,9 in TestMisc\.Main\)$/
#        got: '1'
#   expected: '2'
# so that went wrong, huh ?
ok 11
not ok 12
/^#   failed test 12 \(t[\\\/]testmisc\.cs\.notcs at pos 44,9 in TestMisc\.Main\)$/
#   got:
    ---
    [1, 2, 3]
    #   ^HERE
    ...
#   expected:
    ---
    [1, 3, 2]
    #   ^HERE
    ...
# so that went wrong, huh ?
ok 13
not ok 14
/^#   failed test 14 \(t[\\\/]testmisc\.cs\.notcs at pos 46,9 in TestMisc\.Main\)$/
#   '1'
#   =~
#   '^11$'
# so that went wrong, huh ?
ok 15
not ok 16
/^#   failed test 16 \(t[\\\/]testmisc\.cs\.notcs at pos 48,9 in TestMisc\.Main\)$/
#   '1'
#   !~
#   '^1$'
# so that went wrong, huh ?
ok 17
not ok 18
/^#   failed test 18 \(t[\\\/]testmisc\.cs\.notcs at pos 50,9 in TestMisc\.Main\)$/
#   '1'
#   System.Object.Equals
#   '2'
# so that went wrong, huh ?
ok 19 - this
not ok 20 - this
/^#   failed test 20 \(t[\\\/]testmisc\.cs\.notcs at pos 52,9 in TestMisc\.Main\)$/
# so that went wrong, huh ?
ok 21
not ok 22
/^#   failed test 22 \(t[\\\/]testmisc\.cs\.notcs at pos 54,9 in TestMisc\.Main\)$/
#        got: 'System.String'
#   expected: 'System.Int32'
# so that went wrong, huh ?
ok 23
not ok 24
/^#   failed test 24 \(t[\\\/]testmisc\.cs\.notcs at pos 57,9 in TestMisc\.Main\)$/
#        got: 'ApplicationException'
#   expected: 'SystemException'
#   the exception was not (a child) of the expected type
# so that went wrong, huh ?
# FAILED. 12/24 tests passed (50%)
/^# Wall clock time: .*$/";

    static string VSMiscExpected=@"1..24
ok 1 - phew
not ok 2 - uh oh
/^t[\\\/]testmisc\.cs\.notcs\(33,9\): warning T2: uh oh\. $/
# some comment
ok 3
not ok 4
/^t[\\\/]testmisc\.cs\.notcs\(36,9\): warning T4: $/
# so that went wrong, huh ?
ok 5
not ok 6
/^t[\\\/]testmisc\.cs\.notcs\(38,9\): warning T6: got: '1' expected: '2'$/
# so that went wrong, huh ?
ok 7
not ok 8
/^t[\\\/]testmisc\.cs\.notcs\(40,9\): warning T8: '1' != '1'$/
# so that went wrong, huh ?
ok 9
not ok 10
/^t[\\\/]testmisc\.cs\.notcs\(42,9\): warning T10: got: '1' expected: '2'$/
# so that went wrong, huh ?
ok 11
not ok 12
/^t[\\\/]testmisc\.cs\.notcs\(44,9\): warning T12: actual not as expected$/
  got:
    ---
    [1, 2, 3]
    #   ^HERE
    ...
  expected:
    ---
    [1, 3, 2]
    #   ^HERE
    ...
# so that went wrong, huh ?
ok 13
not ok 14
/^t[\\\/]testmisc\.cs\.notcs\(46,9\): warning T14: '1' =~ '\^11\$'$/
# so that went wrong, huh ?
ok 15
not ok 16
/^t[\\\/]testmisc\.cs\.notcs\(48,9\): warning T16: '1' !~ '\^1\$'$/
# so that went wrong, huh ?
ok 17
not ok 18
/^t[\\\/]testmisc\.cs\.notcs\(50,9\): warning T18: '1' System\.Object\.Equals '2'$/
# so that went wrong, huh ?
ok 19 - this
not ok 20 - this
/^t[\\\/]testmisc\.cs\.notcs\(52,9\): warning T20: this\. $/
# so that went wrong, huh ?
ok 21
not ok 22
/^t[\\\/]testmisc\.cs\.notcs\(54,9\): warning T22: got: 'System\.String' expected: 'System\.Int32'$/
# so that went wrong, huh ?
ok 23
not ok 24
/^t[\\\/]testmisc\.cs\.notcs\(57,9\): warning T24: got: 'ApplicationException' expected: 'SystemException'$/
the exception was not (a child) of the expected type
# so that went wrong, huh ?
# FAILED. 12/24 tests passed (50%)
/^# Wall clock time: .*$/";

    static string SkipTodoExpected=@"1..8
ok 1 - see this
ok 2 - see this
ok 3 # SKIP ook zomaar
ok 4 # SKIP ook zomaar
ok 5 - gets run but is expected to fail # TODO a todo (unexpectedly succeeded)
not ok 6 - gets run but is expected to fail # TODO another todo
  ---
  severity:   todo
/  file:       t[\\\/]testskiptodo\.cs\.notcs/
/  line:       \d+/
  column:     33
/^  method:     TestSkipTodo\.<Main>\w__5$/
  extensions: {todo: another todo}
  ...
ok 7 # TODO a todo (unexpectedly succeeded)
not ok 8 # TODO another todo
  ---
  severity:   todo
/  file:       t[\\\/]testskiptodo\.cs\.notcs/
/  line:       \d+/
  column:     33
/^  method:     TestSkipTodo\.<Main>\w__7$/
  extensions: {todo: another todo}
  ...
# all OK. (8 tests)
#   2 todo tests succeeded unexpectedly.
/^# Wall clock time: .*$/";

    static string TerseSkipTodoExpected=@"1..8
ok 1 - see this
ok 2 - see this
ok 3 # SKIP ook zomaar
ok 4 # SKIP ook zomaar
ok 5 - gets run but is expected to fail # TODO a todo (unexpectedly succeeded)
not ok 6 - gets run but is expected to fail # TODO another todo
/^#   failed test 6 \(t[\\\/]testskiptodo\.cs\.notcs at pos \S+ in TestSkipTodo\.<Main>\w__5\)$/
ok 7 # TODO a todo (unexpectedly succeeded)
not ok 8 # TODO another todo
/^#   failed test 8 \(t[\\\/]testskiptodo\.cs\.notcs at pos \S+ in TestSkipTodo\.<Main>\w__7\)$/
# all OK. (8 tests)
#   2 todo tests succeeded unexpectedly.
/^# Wall clock time: .*$/";

    static string VSSkipTodoExpected=@"1..8
ok 1 - see this
ok 2 - see this
ok 3 # SKIP ook zomaar
ok 4 # SKIP ook zomaar
ok 5 - gets run but is expected to fail # TODO a todo (unexpectedly succeeded)
not ok 6 - gets run but is expected to fail # TODO another todo
/^t[\\\/]testskiptodo\.cs\.notcs\(\S+\): warning T6: gets run but is expected to fail\. \(todo\) $/
ok 7 # TODO a todo (unexpectedly succeeded)
not ok 8 # TODO another todo
/^t[\\\/]testskiptodo\.cs\.notcs\(\S+\): warning T8: \(todo\) $/
# all OK. (8 tests)
#   2 todo tests succeeded unexpectedly.
/^# Wall clock time: .*$/";    

    static string ExceptExpected=@"1..9
ok 1 - exact type
not ok 2 - wrong type
  ---
  message:  the exception was not (a child) of the expected type
  severity: fail
/  file:     t[\\\/]testexcept\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestExcept.Main
  actual:   ApplicationException
  expected: SystemException
  ...
ok 3
ok 4 - by text
not ok 5 - by text mismatch
  ---
  message:  the exception message did not match
  severity: fail
/  file:     t[\\\/]testexcept\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestExcept.Main
  actual:   OMFSMWTFBBQ
  expected: OMGWTFBBQ
  ...
not ok 6
  ---
  message:  the exception message did not match
  severity: fail
/  file:     t[\\\/]testexcept\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestExcept.Main
  actual:   OMFSMWTFBBQ
  expected: OMGWTFBBQ
  ...
ok 7 - by re
not ok 8 - by re mismatch
  ---
  message:  the exception message did not match
  severity: fail
/  file:     t[\\\/]testexcept\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestExcept.Main
  actual:   OMFSMWTFBBQ
  expected: GWT?BBQ
  ...
ok 9
# FAILED. 5/9 tests passed (55%)
/^# Wall clock time: .*$/";

    static string TerseExceptExpected=@"1..9
ok 1 - exact type
not ok 2 - wrong type
/^#   failed test 2 \(t[\\\/]testexcept\.cs\.notcs at pos 34,9 in TestExcept\.Main\)$/
#        got: 'ApplicationException'
#   expected: 'SystemException'
#   the exception was not (a child) of the expected type
ok 3
ok 4 - by text
not ok 5 - by text mismatch
/^#   failed test 5 \(t[\\\/]testexcept\.cs\.notcs at pos 40,9 in TestExcept\.Main\)$/
#        got: 'OMFSMWTFBBQ'
#   expected: 'OMGWTFBBQ'
#   the exception message did not match
not ok 6
/^#   failed test 6 \(t[\\\/]testexcept\.cs\.notcs at pos 42,9 in TestExcept\.Main\)$/
#        got: 'OMFSMWTFBBQ'
#   expected: 'OMGWTFBBQ'
#   the exception message did not match
ok 7 - by re
not ok 8 - by re mismatch
/^#   failed test 8 \(t[\\\/]testexcept\.cs\.notcs at pos 46,9 in TestExcept\.Main\)$/
#        got: 'OMFSMWTFBBQ'
#   expected: 'GWT?BBQ'
#   the exception message did not match
ok 9
# FAILED. 5/9 tests passed (55%)
/^# Wall clock time: .*$/";

    static string VSExceptExpected=@"1..9
ok 1 - exact type
not ok 2 - wrong type
/^t[\\\/]testexcept\.cs\.notcs\(34,9\): warning T2: wrong type\. got: 'ApplicationException' expected: 'SystemException'$/
the exception was not (a child) of the expected type
ok 3
ok 4 - by text
not ok 5 - by text mismatch
/^t[\\\/]testexcept\.cs\.notcs\(40,9\): warning T5: by text mismatch\. got: 'OMFSMWTFBBQ' expected: 'OMGWTFBBQ'$/
the exception message did not match
not ok 6
/^t[\\\/]testexcept\.cs\.notcs\(42,9\): warning T6: got: 'OMFSMWTFBBQ' expected: 'OMGWTFBBQ'$/
the exception message did not match
ok 7 - by re
not ok 8 - by re mismatch
/^t[\\\/]testexcept\.cs\.notcs\(46,9\): warning T8: by re mismatch\. got: 'OMFSMWTFBBQ' expected: 'GWT\?BBQ'$/
the exception message did not match
ok 9
# FAILED. 5/9 tests passed (55%)
/^# Wall clock time: .*$/";

    static string IsaExpected=@"1..4
ok 1
ok 2 - it's a string
not ok 3
  ---
  severity: fail
/  file:     t[\\\/]testisa\.cs\.notcs/
/  line:     \d+/
  column:   9
  method:   TestIsa.Main
  actual:   TestIsa
  expected: System.String
  ...
ok 4
# FAILED. 3/4 tests passed (75%)
/^# Wall clock time: .*$/";

    static string TerseIsaExpected=@"1..4
ok 1
ok 2 - it's a string
not ok 3
/^#   failed test 3 \(t[\\\/]testisa\.cs\.notcs at pos 34,9 in TestIsa\.Main\)$/
#        got: 'TestIsa'
#   expected: 'System.String'
ok 4
# FAILED. 3/4 tests passed (75%)
/^# Wall clock time: .*$/";

    static string VSIsaExpected=@"1..4
ok 1
ok 2 - it's a string
not ok 3
/^t[\\\/]testisa\.cs\.notcs\(34,9\): warning T3: got: 'TestIsa' expected: 'System\.String'$/
ok 4
# FAILED. 3/4 tests passed (75%)
/^# Wall clock time: .*$/";

    static string ExceptionExpected=@"1..3
ok 1
not ok 2
  ---
  message:   oops.
  severity:  fail
  method:    TestException.Crash
  backtrace: |2
/^    \s*at TestException.Crash\s*\(\)\s*\S* in .*[\\\/]tap[\\\/]t[\\\/]testexception.cs.notcs:(?:line )?\d+\s*$/
/^    \s*at TestException.Main\s*\(\)\s*\S* in .*[\\\/]tap[\\\/]t[\\\/]testexception.cs.notcs:(?:line )?\d+\s*$/
  ...
# FAILED. 1/2 test passed (50%)
#    Number of planned tests did not match number of tests.
#    planned: 3 run: 2
/^# Wall clock time: .*$/";

    static string TerseExceptionExpected=@"1..3
ok 1
not ok 2
/^#   failed test 2 \(in TestException\.Crash\)$/
#   oops.
/^#   \s*at TestException.Crash\s*\(\)\s*\S* in .*[\\\/]tap[\\\/]t[\\\/]testexception.cs.notcs:(?:line )?\d+\s*$/
/^#   \s*at TestException.Main\s*\(\)\s*\S* in .*[\\\/]tap[\\\/]t[\\\/]testexception.cs.notcs:(?:line )?\d+\s*$/
# FAILED. 1/2 test passed (50%)
#    Number of planned tests did not match number of tests.
#    planned: 3 run: 2
/^# Wall clock time: .*$/";

    static string VSExceptionExpected=@"1..3
ok 1
not ok 2
T2 : oops.
/^   \s*at TestException.Crash\s*\(\)\s*\S* in .*[\\\/]tap[\\\/]t[\\\/]testexception.cs.notcs:(?:line )?\d+\s*$/
/^   \s*at TestException.Main\s*\(\)\s*\S* in .*[\\\/]tap[\\\/]t[\\\/]testexception.cs.notcs:(?:line )?\d+\s*$/
# FAILED. 1/2 test passed (50%)
#    Number of planned tests did not match number of tests.
#    planned: 3 run: 2
/^# Wall clock time: .*$/";

    static string CultExpected=@"1..3
ok 1
ok 2
not ok 3
  ---
  severity: fail
/  file:     t[\\\/]testcult\.cs\.notcs/
/  line:     \d+/
  column:   13
  method:   TestCult.Main
  actual:   10000.12
  expected: 10000.13
  ...
# dump of should be .: 
---
10000.14
...
# FAILED. 2/3 tests passed (66%)
/^# Wall clock time: .*$/";

    static int NPlanned;

    static Regex TerseNoColumn=new Regex(@"( at pos \d+),\d+");
    static Regex VSNoColumn=new Regex(@"(cs\\\(\d+),\d+(\\\):)");
    
    static string PreProc(string s) {
        if(VM=="mono") {
            if(s.StartsWith("  column:")) return null;
            s=TerseNoColumn.Replace(s,"$1");
            s=VSNoColumn.Replace(s,"$1$2");
        }
        return s;
    }
    
    static int Main() {
        var metatests=new List<Action>{
            MkMetatest(OkExpected,"t\\testok.cs.notcs")
            ,MkMetatest(IsExpected,"t\\testis.cs.notcs")
            ,MkMetatest(IsDeeplyExpected,"t\\testisdeeply.cs.notcs")
            ,MkMetatest(LikeExpected,"t\\testlike.cs.notcs")
            ,MkMetatest(CmpOkExpected,"t\\testcmpok.cs.notcs")
            ,MkMetatest(MiscExpected,"t\\testmisc.cs.notcs")
            ,MkMetatest(SkipTodoExpected,"t\\testskiptodo.cs.notcs")
            ,MkMetatest(ExceptExpected,"t\\testexcept.cs.notcs")
            ,MkMetatest(IsaExpected,"t\\testisa.cs.notcs")
            ,MkMetatest(ExceptionExpected,"t\\testexception.cs.notcs")
            ,MkFrTest(CultExpected,"t\\testcult.cs.notcs")
        };
        var tersemetatests=new List<Action>{
            MkMetatest(TerseOkExpected,"t\\testok.cs.notcs")
            ,MkMetatest(TerseIsExpected,"t\\testis.cs.notcs")
            ,MkMetatest(TerseIsDeeplyExpected,"t\\testisdeeply.cs.notcs")
            ,MkMetatest(TerseLikeExpected,"t\\testlike.cs.notcs")
            ,MkMetatest(TerseCmpOkExpected,"t\\testcmpok.cs.notcs")
            ,MkMetatest(TerseMiscExpected,"t\\testmisc.cs.notcs")
            ,MkMetatest(TerseSkipTodoExpected,"t\\testskiptodo.cs.notcs")
            ,MkMetatest(TerseExceptExpected,"t\\testexcept.cs.notcs")
            ,MkMetatest(TerseIsaExpected,"t\\testisa.cs.notcs")
            ,MkMetatest(TerseExceptionExpected,"t\\testexception.cs.notcs")
        };
        var vsmetatests=new List<Action>{
            MkMetatest(VSOkExpected,"t\\testok.cs.notcs")
            ,MkMetatest(VSIsExpected,"t\\testis.cs.notcs")
            ,MkMetatest(VSIsDeeplyExpected,"t\\testisdeeply.cs.notcs")
            ,MkMetatest(VSLikeExpected,"t\\testlike.cs.notcs")
            ,MkMetatest(VSCmpOkExpected,"t\\testcmpok.cs.notcs")
            ,MkMetatest(VSMiscExpected,"t\\testmisc.cs.notcs")
            ,MkMetatest(VSSkipTodoExpected,"t\\testskiptodo.cs.notcs")
            ,MkMetatest(VSExceptExpected,"t\\testexcept.cs.notcs")
            ,MkMetatest(VSIsaExpected,"t\\testisa.cs.notcs")
            ,MkMetatest(VSExceptionExpected,"t\\testexception.cs.notcs")
        };
        Plan(NPlanned);
        foreach(var i in metatests) i();
        TAPApp.Format="terse";
        foreach(var i in tersemetatests) i();
        TAPApp.Format="vs";
        foreach(var i in vsmetatests) i();
        return 0;
    }

    static Action MkFrTest(string expstr,string source) {
        var action=MkMetatest(expstr,source);
        return ()=>{
            var Saved=Thread.CurrentThread.CurrentCulture;
            try {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
                action();
            } finally  {
                Thread.CurrentThread.CurrentCulture=Saved;
            }
        };
    }

    static Regex ReRe=new Regex(@"^/(.*)/$");

    static Action MkMetatest(string expstr,string source) {
        source=TAPApp.FixPathSep(source);
        string[] expected=expstr.Split(new []{"\r\n"},StringSplitOptions.None).Select<string,string>(PreProc).Where(x=>x!=null).ToArray();
        NPlanned+=expected.Length;
        return ()=>{
            var sw=new StringWriter();
            TAPApp.Out=sw;
            TAPApp.Subject="u";
            try {
                TAPRunner r=new TAPRunner();
                Diag(source);
                r.CompileAndRun(new []{new ScriptPath(source,null)});
                var sr=new StringReader(sw.GetStringBuilder().ToString());
                int idx=0;
                do {
                    string s=sr.ReadLine();
                    if(s==null) break;
                    if(idx>=expected.Length) {
                        Diag("output has more lines than expected-arr: "+s);
                    } else {
                        Match m=ReRe.Match(expected[idx]);
                        if(m.Success) {
                            string re=m.Groups[1].Value;
                            Like(s,re);
                        } else {
                            Is(s,expected[idx]);
                        }
                    }
                    ++idx;
                } while(true);
            } catch(Exception) {
                Diag("sw contains "+Regex.Replace(sw.GetStringBuilder().ToString(),"^","#>> ",RegexOptions.Multiline));
                throw;
            }
        };
    }
    
}

