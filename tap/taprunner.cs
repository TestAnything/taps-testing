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
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("taps")]
[assembly: AssemblyTitle("tap.exe")]
[assembly: AssemblyCopyright("Copyright © 2009 Frank van Dijk")]

namespace Taps {


    public class TAPApp {

        public static TextWriter Out=Console.Out;

        public static List<string> Paths=new List<string>();
        public static string Subject="bin\\Debug";
        public static List<string> Refs=new List<string>();
        
        internal static string TapPwd=Directory.GetCurrentDirectory();

        internal static string[] SupportedFormats=new[]{"yaml","terse","vs"};
        public static string Format="yaml";
        public static int Verbose=1;
        public static int DebugVerbose;
        public static int MaxTasks=1;
        public static int HorizontalThreshold=-1;
        public static bool Elapsed;
        public static bool Unordered;
        public static bool Zero;
        
        public static void Log(string s,params object[] ps) {
            if(s==null) Out.WriteLine("(Log called with null)");
            if(ps==null || ps.Length==0) Out.WriteLine(s); else Out.WriteLine(s,ps);
        }

        public static void ELog(string s,params object[] ps) {
            if(s==null) Console.Error.WriteLine("(Elog called with null)");
            if(ps==null || ps.Length==0) Console.Error.WriteLine(s); else Console.Error.WriteLine(s,ps);
        }

        public static void VLog(int v,string s,params object[] ps) {
            if(v<=Verbose) {
                Log("## "+s,ps);
            }
        }

        public static void DLog(int v,string s,params object[] ps) {
            if(v<=DebugVerbose) {
                string pfix=string.Format("## {0:O} {1} ",DateTime.UtcNow,Thread.CurrentThread.ManagedThreadId);
                ELog(pfix+s,ps);
            }
        }

        static void WriteTemplate(string name) {
            if(string.IsNullOrEmpty(name)) name="Sally";
            using(var wr=File.CreateText(name.ToLower()+".cs")) {
                wr.Write(@"using Taps;
using System;

class "+name+@"Test: TAP {

    static int Main() {
        //Plan(1);
        //Autorun(typeof("+name+@"Test));
        Ok(1==1,""hello!"");
        return 0;
    }
}
");
            }
        }

        static void OutputHelp() {
            Log(@"tap.exe [switches] [paths]

-e, -elapsed
  Show elapsed time with every test.
-f, -format
  'yaml', 'terse' or 'vs'. Write info on failed tests in a YAML
  document, a terse diagnostic, or in a format recognized by
  visual studio's error list.
-h
  This.
-j:<thresh>, -horizontalthresh:<thresh>
  Write collections JSON-style if the resulting text is below
  this threshold. Default 60.
-p:<n>, -parallel:<n>
  Max number of scripts to run in parallel. Default is 1, default
  if switch is provided without value is number of cores.
-r:<name>, -reference:<name>
  Additional assemblies to reference.
-s:<subject>, -subject:<subject>
  Dir where test subject is to be found. Default 'bin\Debug'.
-t:<name>, -template:<name>
  Write a file <name>.cs with a test script template.
-u, -unordered
  When running parallel tests, allow their printed output to mix.
-v:<level>, -verbose:<level>
  A higher number means more output that may be useful for
  diagnosing a problem.
-z, -zero
  Return 0 even if some tests fail (some TAP consumers like that).

If no switches and no paths are supplied tap runs test scripts
in directory 't/' on subjects in 'bin/debug'.

If a path is a dir tap recurses into subdirectories of that dir
looking for test scripts.

This is free software.  You may redistribute copies of it under the
terms of the GNU General Public License <http://www.gnu.org/licenses/>
and the additional terms laid out in the file COPYING.EXCEPTION.
There is NO WARRANTY, to the extent permitted by law.
");
        }
        
        static void ReadArgs(string[] args) {
            if(args.Length!=0 && args[0]=="-le") {
                Environment.Exit(0);
            }
            foreach(var i in args) {
                Match m=Regex.Match(i,@"^[/-](\w+)(?::(.+))?");
                if(m.Success) {
                    string key=m.Groups[1].ToString().ToLower();
                    string val=m.Groups[2].ToString();
                    bool hasval=!string.IsNullOrEmpty(val);
                    switch(key) {
                    case "r":
                    case "reference":
                        if(hasval) Refs.Add(val);
                        break;
                    case "t":
                    case "template":
                        WriteTemplate(val);
                        Environment.Exit(0);
                        break;
                    case "s":
                    case "subject":
                        Subject=hasval?val:".";
                        break;
                    case "f":
                    case "format":
                        val=val.ToLowerInvariant();
                        if(hasval && SupportedFormats.Contains(val)) {
                            Format=val;
                        } else {
                            Log("unrecognized format.");
                            Environment.Exit(1);
                        }
                        break;
                    case "e":
                    case "elapsed":
                        Elapsed=true;
                        break;
                    case "p":
                    case "parallel":
                        if(hasval) {
                            MaxTasks=int.Parse(val);
                        } else {
                            MaxTasks=Environment.ProcessorCount;
                        }
                        break;
                    case "u":
                    case "unordered":
                        Unordered=true;
                        break;
                    case "j":
                    case "horizontalthresh":
                        if(hasval) HorizontalThreshold=int.Parse(val);
                        break;
                    case "z":
                    case "zero":
                        Zero=true;
                        break;
                    case "v":
                    case "verbose":
                        Verbose=hasval?int.Parse(val):2;
                        break;
                    case "d":
                    case "debugverbose":
                        DebugVerbose=hasval?int.Parse(val):0;
                        break;
                    case "h":
                    case "help":
                        OutputHelp();
                        Environment.Exit(1);
                        break;
                    }
                } else {
                    Paths.Add(i);
                }
            }
        }

        internal static bool IsOutdated(string obj,string src) {
            // returns true if file is outdated or doesn't exist
            // (write time doesn't change with copy (on ntfs at least))
            var objtime=File.GetLastWriteTimeUtc(obj);
            var srctime=File.GetLastWriteTimeUtc(src);
            return objtime<srctime;
        }

        public static int Main(string[] args) {
            try {
                ReadArgs(args);
                var tr=new TAPRunner();
                tr.CompileAndRun(tr.Expand(Paths));
                tr.ShowTotals();
                VLog(2,"EXIT {0}",tr.Exit);
                return Zero?0:tr.Exit;
            } catch(Exception e) {
                ELog(e.ToString());
                return 1;
            }
        }

    }

    

    internal class ScriptPath {
            
        public string Path;
        public string Basepath;

        static Regex NonEmptyWithoutBSlash=new Regex(@"[^\\]$");

        public ScriptPath(string path,string basepath) {
            Path=path;
            if(basepath!=null && NonEmptyWithoutBSlash.IsMatch(basepath)) {
                basepath+="\\";
            }
            Basepath=basepath;
        }
        public override string ToString() {
            return Path;
        }

        public string GetRelativePart() {
            if(!string.IsNullOrEmpty(Basepath)) {
                if(Path.StartsWith(Basepath)) {
                    return Path.Substring(Basepath.Length);
                }
            }
            return Path;
        }
    }

    public class TAPRunner: TaskMgrClient {

        public TAPRunner() {
        }

        static string GetMyImagePath() {
            return new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath;
        }

        bool CopyMe(string targetpath) {
            string me=GetMyImagePath();
            string to=Path.Combine(targetpath,Path.GetFileName(me));
            if(TAPApp.IsOutdated(to,me)) {
                TAPApp.VLog(2,"copy from {0} to {1}",me,to);
                File.Copy(me,to,true);
                // this can be removed once all the bugs are fixed :-)
                string pdb=Regex.Replace(me,@"\.exe$",".pdb");
                string pdbto=Regex.Replace(to,@"\.exe$",".pdb");
                File.Copy(pdb,pdbto,true);
                return true;
            }
            return false;
        }

        internal void ShowTotals() {
            TAPParser.ShowTotals(TAPApp.Paths);
        }

        internal int Exit {
            get {
                return TAPParser.Exit;
            }
        }

        static IEnumerable<ScriptPath> GetScriptsInPath(string path) {
            return Directory.GetFiles(path,"*.cs",SearchOption.AllDirectories).Select(x=>new ScriptPath(x,path));
        }

        static IEnumerable<ScriptPath> GetByWildcard(string pw) {
            var path=Path.GetDirectoryName(pw);
            if(string.IsNullOrEmpty(path)) path=".";
            return Directory.GetFiles(path,Path.GetFileName(pw),SearchOption.TopDirectoryOnly).Select(x=>new ScriptPath(x,path));
        }

        internal IEnumerable<ScriptPath> Expand(ICollection<string> paths) {
            if(paths.Count==0)  {
                return Directory.GetFiles("t","*.cs",SearchOption.AllDirectories).Select(x=>new ScriptPath(x,"t"));
            } else {
                return from i in paths.Select(x=>x.Replace('/','\\'))
                    from j in Directory.Exists(i)?GetScriptsInPath(i):GetByWildcard(i)
                    select j;
            }
        }

        internal void CompileAndRun(IEnumerable<ScriptPath> srcs) {
            TapWasUpdated=CopyMe(TAPApp.Subject);
            using(TaskMgr tm=new TaskMgr(TAPApp.MaxTasks,this)) {
                tm.Run(srcs.ToArray());
            }
        }

        public override void ErrorReceived(string s,bool force) {
            if(s!=null && (force || TAPApp.Verbose>0)) TAPApp.ELog(s);
        }

        public override void OutputReceived(string s,bool force) {
            if(s!=null) {
                TAPApp.DLog(4,"received {0}.",s);
                if(force || TAPApp.Verbose>0) TAPApp.Log(s);
            }
        }

    }
    
}
