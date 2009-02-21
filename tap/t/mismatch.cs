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
using System.Text.RegularExpressions;
using System.Linq;

class MismatchTest: TAP {

    static int Main() {
        Plan(4);
        var paths=new string[] {"t\\12many.cs.notcs","t\\12few.cs.notcs"};
        var sw=new StringWriter();
        TAPApp.Out=sw;
        TAPApp.Subject="u";
        try {
            TAPRunner r=new TAPRunner();
            r.CompileAndRun(paths.Select(x=>new ScriptPath(x,null)));
            var cs=TAPParser.Total;
            Is(cs.NPlanned,2);
            Is(cs.NOk,2);
            Is(cs.Mismatch,true);
            r.ShowTotals();
            Like(sw.ToString(),@"did not match number of tests\.
.*planned: 2 run: 2\b");
        } catch(Exception) {
            Dump("mismatch sw",sw);
            throw;
        }
        return 0;
    }

}
