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
using System.Reflection;

class DeepTest: TAP {

    static int Main() {
        Plan(1);
        //Autorun(typeof(DeepTest));
        string loc=Assembly.GetCallingAssembly().Location;
        Diag(loc);
        // if tap was run with path t, not if tap was run with path t/sub
        Is(Path.GetFileName(loc),"taps.sub_deep"); 
        return 0;
    }
}
