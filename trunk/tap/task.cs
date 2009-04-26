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
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Linq;

namespace Taps {

    class Task {
        AutoResetEvent Done;
        TaskMgrClient Client;
        internal ScriptPath ScriptPath;
        public volatile bool Head;
        public volatile bool Running=true;
        bool WasHead;
        TAPParser TAPParser=new TAPParser();
        bool Single;

        public Task(AutoResetEvent done,TaskMgrClient client,ScriptPath path,bool single) {
            Done=done;
            Client=client;
            ScriptPath=path;
            Single=single;
        }

        struct OutBufLine {
            public bool Error;
            public bool Force;
            public string Data;
            public OutBufLine(bool error,bool force,string data) {
                Error=error;
                Force=force;
                Data=data;
            }
        }

        Queue<OutBufLine> OutBuf=new Queue<OutBufLine>();

        static Regex ExtEx=new Regex(@"(.+?)(?:\.\w*)?$");

        static string TapFromSource(ScriptPath s) {
            string rel=s.GetRelativePart();
            rel=rel.Replace(Path.DirectorySeparatorChar,'_');
            return Path.Combine(TAPApp.Subject,ExtEx.Replace(rel,m=>
                                                                    string.Concat("taps.",m.Groups[1].Value.Replace(".","")),1));
        }

        static string PdbFromTap(string tap) {
            return TAPApp.PdbFromExe(tap);
        }

        void Buffer(bool error,bool force,string s) {
            // xxx a lock isn't needed. other thread reads OutBuf
            // only after Running is set to false. A barrier should be
            // enough ?
            lock(OutBuf) {
                OutBuf.Enqueue(new OutBufLine(error,force,s));
            }
        }

        public void WriteBuffered() {
            lock(OutBuf) {
                foreach(var line in OutBuf) {
                    if(!line.Error) {
                        Client.OutputReceived(line.Data,line.Force);
                    } else {
                        Client.ErrorReceived(line.Data,line.Force);
                    }
                }
                OutBuf.Clear();
            }
        }

        void HandleOutput(string s,bool error,Action<string,bool> writedel) {
            if(s==null) return;
            TAPParser.ParseLine(s);
            ForwardOutput(s,error,false,writedel);
        }

        void ForwardOutput(string s,bool error,bool force,Action<string,bool> writedel) {
            if(!TAPApp.Unordered && !WasHead && Head) {
                WriteBuffered();
                WasHead=true;
            }
            if(TAPApp.Unordered || WasHead) {
                writedel(s,force);
            } else {
                Buffer(error,force,s);
            }
        }

        public void TaskComplete() {
            TAPApp.VLog(3,"TaskComplete");
            TAPParser.UpdateTotals();
            TAPParser.ShowSubtotals();
        }

        public int Run(string path) {
            using(Process proc=new Process()) {
                var startInfo=new ProcessStartInfo();
#if __MonoCS__
                startInfo.FileName="mono";
                startInfo.Arguments="--debug "+path;
#else
                startInfo.FileName=path;
#endif
                startInfo.CreateNoWindow=true;
                startInfo.UseShellExecute=false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                var env=startInfo.EnvironmentVariables;
                env["TAP_PWD"]=TAPApp.TapPwd;
                env["TAP_FORMAT"]=TAPApp.Format;
                if(TAPApp.Verbose!=0) env["TAP_VERBOSE"]=TAPApp.Verbose.ToString();
                if(TAPApp.Elapsed) env["TAP_ELAPSED"]=TAPApp.Elapsed.ToString();
                if(TAPApp.HorizontalThreshold!=-1) env["TAP_HTHRESH"]=TAPApp.HorizontalThreshold.ToString();
                proc.StartInfo=startInfo;
                proc.ErrorDataReceived+=(s,e)=>{
                    HandleOutput(e.Data,true,Client.ErrorReceived);
                };
                proc.OutputDataReceived+=(s,e)=>{
                    HandleOutput(e.Data,false,Client.OutputReceived);
                };
                proc.Start();
                proc.BeginErrorReadLine();
                proc.BeginOutputReadLine();
                proc.WaitForExit();
                return proc.ExitCode;
            }
        }

        static string[] GetSubjectAssemblies() {
            return (from x in new[]{"dll","exe"}
                from y in Directory.GetFiles(TAPApp.Subject,"*."+x)
                where !y.EndsWith(".vshost.exe")
                select y).ToArray();
        }

        static bool IsOutdated(string obj,string[] src) {
            string first=src.FirstOrDefault(x=>TAPApp.IsOutdated(obj,x));
            if(first!=null) {
                TAPApp.VLog(2,"{0} is newer than {1}",first,obj);
                return true;
            }
            return false;
        }

        string Compile(ScriptPath spath,bool tapwasupdated) {
            string path=spath.Path;
            if(!File.Exists(path)) {
                throw new FileNotFoundException("source file not found",path);
            }
            string outpath=TapFromSource(spath);
            string pdbpath=PdbFromTap(outpath);
            string[] sa=GetSubjectAssemblies(); // this includes tap.exe copied with CopyMe
            TAPApp.VLog(3,"subject assemblies:"+string.Join(",",sa.ToArray()));
            if(!tapwasupdated && !TAPApp.IsOutdated(outpath,path) && !TAPApp.IsOutdated(pdbpath,path)
                && !IsOutdated(outpath,sa)) {
                TAPApp.VLog(2,outpath+" is up to date");
                return outpath;
            }
            TAPApp.VLog(3,"building {0}",outpath);            
            using(CodeDomProvider prov=new CSharpCodeProvider(new Dictionary<string,string>{
                        {"CompilerVersion","v3.5"}})) { // maybe make configurable in a <system.codedom><compilers>...
                var cp=new CompilerParameters();
                cp.GenerateExecutable=true;
                cp.IncludeDebugInformation=true;
                cp.OutputAssembly=outpath;
                //cp.CompilerOptions+=String.Concat("/d:DEBUG /lib:\"",GetMyImagePath(),"\"");
#if __MonoCS__
                cp.CompilerOptions+="/d:DEBUG /nowarn:169";
#else
                cp.CompilerOptions+=string.Concat("/d:DEBUG /pdb:",pdbpath);
#endif                
                cp.ReferencedAssemblies.Add("System.dll");
                cp.ReferencedAssemblies.Add("System.Core.dll");

                cp.ReferencedAssemblies.AddRange(sa);
                cp.ReferencedAssemblies.AddRange(TAPApp.Refs.ToArray());
                CompilerResults cr=prov.CompileAssemblyFromFile(cp,new []{path});
                bool errors=cr.Errors.Count>0;
                if(errors) TAPApp.ELog("Errors building");
                if(errors || TAPApp.Verbose>1) {
                    foreach(string i in cr.Output) {
                        TAPApp.Log(i);
                    }
                }
                if(!errors) {
                    return cr.PathToAssembly;
                }
                return null;
            }
        }

        public void Runner(object o) {
            ScriptPath path=(ScriptPath)o;
            if(!Single) {
                ForwardOutput("# "+path.ToString(),false,true,Client.OutputReceived);
            }
            try  {
                int exit=1;
                string exe=Compile(path,Client.TapWasUpdated);
                if(exe!=null) {
                    exit=Run(exe);
                }
                TAPApp.VLog(2,"Exit code from {0}: {1}",path,exit);
            }
            catch(Exception e) {
                TAPApp.ELog("Exception running {0}: {1}",path,e.ToString());
            }
            finally {
                TAPApp.DLog(3,"{0} done.",path);
                TAPParser.End();
                Running=false;
                Done.Set();
            }
        }

        public void Run() {
            ThreadPool.QueueUserWorkItem(Runner,ScriptPath);
        }
        
    }
    
}