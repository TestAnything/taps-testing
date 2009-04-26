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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace Taps {

    public class TAPParser {

        internal class TAPCounters {
            public bool FirstLine=true;
            public DateTime Start=DateTime.UtcNow;
            public DateTime End;
            public int NPlanned;
            public int NOk;
            public int NNotOk;
            public int NTodo;
            public int NTodoSucc;
            public int NSkipped;
            public int NRunFailed;
            public int NScripts;
            public bool Mismatch;

            string TestTests(int n) {
                return n==1?"test":"tests";
            }
            
            public int Show() {
                int all=NOk+NNotOk;
                int exit=1;
                string scripts=NScripts==1?"script":"scripts";
                if(!FirstLine && !Mismatch && NNotOk==0 && NRunFailed==0) {
                    TAPApp.Log("# all OK. ({0} {1})",NOk,TestTests(NOk));
                    if(NTodoSucc!=0) {
                        TAPApp.Log("#   {0} todo {1} succeeded unexpectedly.",NTodoSucc,TestTests(NTodoSucc));                            
                    }
                    exit=0;
                } else {
                    string pfix="FAILED.";
                    if(FirstLine) {
                        TAPApp.Log("# {0} No output.",pfix);
                    }
                    if(NRunFailed!=0) {
                        int nrunok=NScripts-NRunFailed;
                        TAPApp.Log("# {0} {1}/{2} {3} run ({4:D0}%)",pfix,nrunok,NScripts,scripts,(int)(((double)nrunok/NScripts)*100));
                        pfix="  ";
                    }
                    if(NNotOk!=0) {
                        TAPApp.Log("# {0} {1}/{2} {3} passed ({4:D0}%)",pfix,NOk,all,TestTests(NOk),(int)(((double)NOk/all)*100));
                        pfix="  ";
                    }
                    if(Mismatch) {
                        TAPApp.Log("# {0} Number of planned tests did not match number of tests.",pfix);
                        pfix="  ";                        
                        TAPApp.Log("# {0} planned: {1} run: {2}",pfix,NPlanned,all);
                    }
                } 
                TAPApp.Log("# Wall clock time: {0}",End-Start);
                return exit;
            }

            public void Wrapup() {
                Mismatch=NPlanned!=0 && NPlanned!=NOk+NNotOk;
            }
            
        };

        TAPCounters Counters;
        static internal TAPCounters Total;
        static public int Exit;

        static Regex PlanRE=new Regex(@"\d+\.\.(\d+)");
        static Regex ResultRE=new Regex(@"^(ok|not ok)\s+(\d+)(|\s+.+?)(?:\s+#\s+(SKIP|TODO)\s*(.*)?)?$");

        static TAPParser() {
            Total=new TAPCounters();
            Total.FirstLine=false;
        }

        public TAPParser() {
            Counters=new TAPCounters();
        }

        public static void UpdateExit(int exit) {
            if(exit!=0) Exit=exit;
        }

        public void ShowSubtotals() {
            UpdateExit(Counters.Show());
        }

        static public void ShowTotals(List<string> paths) {
            if(Total.NScripts==0) {
                string instr="";
                if(paths.Count!=0)  {
                    instr=string.Join(", ",paths.ToArray());
                }
                TAPApp.Log("not ok 1 - No matching test scripts. Paths: {0}.",instr);
                Exit=1;
            }
            else if(Total.NScripts>1) {
                TAPApp.Log("# result after {0} scripts:",Total.NScripts);
                UpdateExit(Total.Show());
                TAPApp.DLog(2,"exit is {0}",Exit);
            }
        }

        public void UpdateTotals() {
            Counters.Wrapup();
            ++Total.NScripts;
            if(Counters.FirstLine) ++Total.NRunFailed;
            Total.NPlanned+=Counters.NPlanned;
            Total.NOk+=Counters.NOk;
            Total.NNotOk+=Counters.NNotOk;
            Total.NTodo+=Counters.NTodo;
            Total.NTodoSucc+=Counters.NTodoSucc;
            Total.NSkipped+=Counters.NSkipped;
            Total.Mismatch|=Counters.Mismatch;
        }

        public string[] Match(Regex re,string s) {
            Match m=re.Match(s);
            if(m.Success) {
                return (from x in m.Groups.Cast<Group>().Skip(1) select x.ToString()).ToArray();
            } else {
                return null;
            }
        }

        public void Plan(int n) {
            Counters.NPlanned=n;
        }

        public void End() {
            Counters.End=DateTime.UtcNow;
        }

        static public void EndTotal() {
            Total.End=DateTime.UtcNow;
            TAPApp.DLog(3,"Stopped the clock.");
        }

        public void ParseLine(string line) {
            if(Counters.FirstLine) {
                Counters.FirstLine=false;
                string[] p=Match(PlanRE,line);
                if(p!=null) {
                    Plan(int.Parse(p[0]));
                    return;
                }
            }
            string[] m=Match(ResultRE,line);
            if(m!=null) {
                bool ok=m[0]=="ok";
                bool todo=m[3]=="TODO";
                if(todo) ++Counters.NTodo;
                if(ok)  {
                    ++Counters.NOk;
                    if(todo) {
                        ++Counters.NTodoSucc;
                    }
                } else {
                    if(todo) {
                        ++Counters.NOk;
                    } else {
                        ++Counters.NNotOk;
                    }
                }
                int expectedidx=Counters.NOk+Counters.NNotOk;
                int idx=int.Parse(m[1]);
                if(idx!=expectedidx) {
                    TAPApp.Log("# tapparser: unexpected index number {0}; expected {1}.",idx,expectedidx);
                }
                if(m[3]=="SKIP") {
                    ++Counters.NSkipped;
                }
            }
        }
        
    }
    
}
