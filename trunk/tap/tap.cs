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
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Linq;

namespace Taps {

    class CommentDictionaries {
        public OrderedDictionary Dic=new OrderedDictionary();
        public OrderedDictionary Ext=null;

        public void Add(string key,object value) {
            Dic.Add(key,value);
        }

        public object this[string key] {
            get {return Dic[key];}
            set {Dic[key]=value;}
        }
        
        public void AddExtension(string key,object value) {
            if(Ext==null) Ext=new OrderedDictionary();
            Ext.Add(key,value);
        }
        
    }

    public class WithInvariantCulture: IDisposable {

        CultureInfo Saved;
        
        public WithInvariantCulture() {
            Saved=Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }

        public void Dispose() {
            Thread.CurrentThread.CurrentCulture=Saved;
        }
    }

    public abstract class CommentWriter {

        protected IDictionary<Type,bool> BBD;
        protected TextWriter Tw;

        public CommentWriter(TextWriter tw,IDictionary<Type,bool> bbd) {
            BBD=bbd;
            Tw=tw;
        }

        public abstract void WriteComment(int nr,OrderedDictionary dic,List<PathEntry> path,string name);

        protected string GetAnnotation(int idx,YNode node,List<PathEntry> path,int side,int rootdepth) {
            int depth=node.Depth;
            if(path!=null && depth<path.Count) {
                PathEntry p=path[depth];
                if(p.Idx==idx) {
                    if(p.Counts[0]!=p.Counts[1]) {
                        string notcoll=node.IsLeaf?" (not even a countable)":"";
                        return string.Format("COUNT MISMATCH {0} vs {1}{2}",p.Counts[0^side],p.Counts[1^side],notcoll);
                    } else {
                        // saying 'here' at the root is stating the obvious 
                        if(depth!=rootdepth) {
                            return "HERE";
                        }
                    }
                }
            }
            return null;
        }

    }

    public class YAMLCommentWriter: CommentWriter {

        public YAMLCommentWriter(TextWriter tw,IDictionary<Type,bool> bbd): base(tw,bbd) {
        }

        public override void WriteComment(int nr,OrderedDictionary dic,List<PathEntry> path,string name) {
            if(path!=null) path.Insert(0,new PathEntry(-1));
            var yw=new YAMLWriter(Tw,2,TAP.HorizontalThreshold,BBD);
            int rootidx=-1;
            int side=0;
            yw.Annotate=(i,n)=>{
                int depth=n.Depth;
                if(depth==1)  {
                    var k=n.Key as string;
                    if(k!=null) {
                        if(k=="actual") {
                            side=0;
                            rootidx=i;
                        } else if(k=="expected") {
                            side=1;
                            rootidx=i;
                        }
                    } else {
                        rootidx=-1;
                    }
                }
                if(rootidx!=-1) {
                    return GetAnnotation(i-rootidx,n,path,side,1);
                }
                return null;
            };
            yw.Write(dic);
        }

    }

    class TerseCommentWriter: CommentWriter {

        public TerseCommentWriter(TextWriter tw,IDictionary<Type,bool> bbd): base(tw,bbd) {
        }

        void WriteBaseComment(int nr,OrderedDictionary dic) {
            Tw.Write("#   failed test {0} (",nr);
            var filename=(string)dic["file"];
            if(filename!=null) {
                Tw.Write("{0} at pos {1},{2} ",filename,dic["line"],dic["column"]);
            }
            Tw.WriteLine("in {0})",dic["method"]);
        }

        static string SafeToString(object s) {
            if(s==null) return "(null)";
            return s.ToString();
        }

        static string ValOrNull(OrderedDictionary dic,string key) {
            return SafeToString(dic[key]);
        }

        void WriteCmpComment(OrderedDictionary dic,string cmp) {
            Tw.WriteLine("#   '{0}'",ValOrNull(dic,"actual"));
            Tw.WriteLine("#   {0}",cmp);
            Tw.WriteLine("#   '{0}'",ValOrNull(dic,"expected"));
        }

        void WriteIsVal(OrderedDictionary dic,string label,string key,List<PathEntry> path,int side) {
            var val=dic[key];
            if(path==null || YAMLWriter.IsLeafType(val)) {
                Tw.WriteLine("#{0,11}: '{1}'",label,SafeToString(val));
            } else {
                Tw.WriteLine("#   {0}:",label);
                var yw=new YAMLWriter(Tw,4,TAP.HorizontalThreshold,BBD);
                yw.Annotate=(i,n)=> {
                    return GetAnnotation(i,n,path,side,0);
                };
                yw.Write(val);
            }
        }

        void WriteIsComment(OrderedDictionary dic,List<PathEntry> path) {
            WriteIsVal(dic,"got","actual",path,0);
            WriteIsVal(dic,"expected","expected",path,1);
        }

        public override void WriteComment(int nr,OrderedDictionary dic,List<PathEntry> path,string name) {
            WriteBaseComment(nr,dic);
            var ext=(OrderedDictionary)dic["extensions"];
            if(ext!=null) {
                if(ext.Contains("cmp")) {
                    WriteCmpComment(dic,(string)ext["cmp"]);
                }
            } else if(dic.Contains("actual")) {
                WriteIsComment(dic,path);
            }
            var msg=(string)dic["message"];
            if(msg!=null) {
                Tw.WriteLine("#   "+msg);
            }
            var backtrace=(string)dic["backtrace"];
            if(backtrace!=null) {
                string estring=Regex.Replace(backtrace,@"^","# ",RegexOptions.Multiline);
                Tw.WriteLine(estring);

            }
        }

    }

    class VSCommentWriter: CommentWriter {

        public VSCommentWriter(TextWriter tw,IDictionary<Type,bool> bbd): base(tw,bbd) {
        }

        void WriteBaseComment(int nr,OrderedDictionary dic,string name) {
            var filename=(string)dic["file"];
            if(name==null) name=""; else name+=". ";
            string todo=TAP.InTodo!=null?"(todo) ":"";
            if(filename!=null) {
                Tw.Write("{0}({1},{2}): warning T{3}: {4}{5}",filename,dic["line"],dic["column"],nr,name,todo);
            } else {
                Tw.Write("T{0} : {1}{2}",nr,name,todo);
            }
        }

        static string SafeToString(object s) {
            if(s==null) return "(null)";
            return s.ToString();
        }

        static string ValOrNull(OrderedDictionary dic,string key) {
            return SafeToString(dic[key]);
        }

        void WriteCmpComment(OrderedDictionary dic,string cmp) {
            Tw.WriteLine("'{0}' {1} '{2}'",ValOrNull(dic,"actual"),cmp,ValOrNull(dic,"expected"));
        }

        void WriteIsVal(bool leaf,OrderedDictionary dic,string label,string key,List<PathEntry> path,int side) {
            var val=dic[key];
            if(leaf) {
                Tw.Write("{0}: '{1}'",label,SafeToString(val));
            } else {
                Tw.WriteLine("  {0}:",label);
                var yw=new YAMLWriter(Tw,4,TAP.HorizontalThreshold,BBD);
                yw.Annotate=(i,n)=> {
                    return GetAnnotation(i,n,path,side,0);
                };
                yw.Write(val);
            }
        }

        void WriteIsComment(OrderedDictionary dic,List<PathEntry> path) {
            bool leaf=path==null || (YAMLWriter.IsLeafType(dic["actual"]) && YAMLWriter.IsLeafType(dic["expected"]));
            if(!leaf) Tw.WriteLine("actual not as expected");
            WriteIsVal(leaf,dic,"got","actual",path,0);
            if(leaf) Tw.Write(" ");
            WriteIsVal(leaf,dic,"expected","expected",path,1);
            if(leaf) Tw.WriteLine();
        }

        public override void WriteComment(int nr,OrderedDictionary dic,List<PathEntry> path,string name) {
            WriteBaseComment(nr,dic,name);
            bool wrotestuff=false;
            var ext=(OrderedDictionary)dic["extensions"];
            if(ext!=null) {
                if(ext.Contains("cmp")) {
                    WriteCmpComment(dic,(string)ext["cmp"]);
                    wrotestuff=true;
                }
            } else if(dic.Contains("actual")) {
                WriteIsComment(dic,path);
                wrotestuff=true;
            }
            var msg=(string)dic["message"];
            if(msg!=null) {
                Tw.WriteLine(msg);
                wrotestuff=true;
            }
            var backtrace=(string)dic["backtrace"];
            if(backtrace!=null) {
                string estring=Regex.Replace(backtrace,@"^","  ",RegexOptions.Multiline);
                Tw.WriteLine(estring);
                wrotestuff=true;
            }
            if(!wrotestuff) {
                Tw.WriteLine();
            }
        }

    }

    
    public class TAP {

        static int Tests;
        static int Cur;
        static object WriteLock=new object();
        [ThreadStatic] static Stopwatch Stopwatch;

        [ThreadStatic] internal static string InTodo;

        static Dictionary<Type,bool> BlackBoxTypes=new Dictionary<Type,bool>{
            {typeof(Pointer),true}
        };

        static int Verbose=int.Parse(Environment.GetEnvironmentVariable("TAP_VERBOSE")??"0");
        public static int HorizontalThreshold=int.Parse(Environment.GetEnvironmentVariable("TAP_HTHRESH")??"60");
        static bool Elapsed=bool.Parse(Environment.GetEnvironmentVariable("TAP_ELAPSED")??"false");

        static string Format=Environment.GetEnvironmentVariable("TAP_FORMAT");

        public IDictionary<Type,bool> GetBBTs() {
            return new Dictionary<Type,bool>(BlackBoxTypes);
        }
            
        //[SecurityPermission(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlAppDomain)]
        static TAP() {
            AppDomain.CurrentDomain.UnhandledException+=UncaughtHandler;
        }

        static void UncaughtHandler(object sender, UnhandledExceptionEventArgs args) {
            Exception e = (Exception) args.ExceptionObject;
            if(e is TargetInvocationException && e.InnerException!=null) {
                e=e.InnerException;
            }
            using(new WithInvariantCulture()) {
                lock(WriteLock) {
                    ReportNotOk(null);
                    var dic=new CommentDictionaries();
                    dic.Add("message",e.Message);
                    dic.Add("severity","fail");
                    var meth=e.TargetSite;
                    if(meth!=null) dic.Add("method",string.Format("{0}.{1}",meth.DeclaringType.FullName,meth.Name));
                    dic.Add("backtrace",e.InnerException==null?e.StackTrace:e.ToString());
                    WriteComment(dic,null,null);
                }
            }
            Environment.Exit(1);
        }

        public static void Dump(string name,object o) {
            using(new WithInvariantCulture()) {
                lock(WriteLock) {
                    Diag("dump of "+name+": ");
                    var yw=new YAMLWriter(Console.Out,0,TAP.HorizontalThreshold,null);
                    yw.Write(o);
                }
            }
        }

        static void ReportOkness(string name,bool ok) {
            TimeSpan time=TimeSpan.MinValue;
            if(Stopwatch!=null) time=Stopwatch.Elapsed;
            ++Cur;
            if(!ok) Console.Write("not ");
            Console.Write("ok {0}",Cur);
            if(name!=null) Console.Write(" - {0}",name);
            string intodo=InTodo;
            if(intodo!=null) {
                if(ok) {
                    Console.Write(" # TODO {0} (unexpectedly succeeded)",intodo);
                } else {
                    Console.Write(" # TODO {0}",intodo);
                }
            } else if(Elapsed && time!=TimeSpan.MinValue) {
                if(time.TotalSeconds>=0.001) {
                    Console.Write(" # {0:F6}s",time.TotalSeconds);
                } else {
                    Console.Write(" # {0:F1}us",time.Ticks/10.0);
                }
            }
            Console.WriteLine();
        }

        static void ReportOk(string name) {
            ReportOkness(name,true);
        }

        static void ReportNotOk(string name) {
            ReportOkness(name,false);
        }
        
        static string MakeRelative(string filename) {
            string here=Environment.GetEnvironmentVariable("TAP_PWD");
            if(here==null) {
                here=Directory.GetCurrentDirectory();
            }
            Path.GetFullPath(here);
            if(!here.EndsWith("\\")) here+="\\";
            if(filename.StartsWith(here,StringComparison.OrdinalIgnoreCase)) {
                return filename.Substring(here.Length);
            }
            return filename;
        }

        static StackFrame GetStackFrame() {
            var st=new StackTrace(true);
            Assembly thisass=typeof(TAP).Assembly;
            for(int k=1;k<st.FrameCount;++k) {
                var sf=st.GetFrame(k);
                MethodBase meth=sf.GetMethod();
                if(meth.DeclaringType.Assembly!=thisass) return sf;
            }
            throw new ApplicationException("tap: no calling stack frame found");
        }

        static void WriteComment(CommentDictionaries dic,List<PathEntry> path,string name) {
            if(dic.Ext!=null) {
                dic.Dic.Add("extensions",dic.Ext);
            }
            CommentWriter cw;
            switch(Format) {
            case "yaml":
                cw=new YAMLCommentWriter(Console.Out,BlackBoxTypes);
                break;
            case "vs":
                cw=new VSCommentWriter(Console.Out,BlackBoxTypes);
                break;
            default:
                cw=new TerseCommentWriter(Console.Out,BlackBoxTypes);
                break;
            }
            cw.WriteComment(Cur,dic.Dic,path,name);
        }

        static void WriteIsComment(bool res,string name,string msg,StackFrame sf,object got,object expected,string cmp) {
            var dic=MkCommentDic(res,name,msg,sf);
            dic.Add("actual",got);
            dic.Add("expected",expected);
            if(cmp!=null) dic.AddExtension("cmp",cmp);
            WriteComment(dic,null,name);
        }

        static bool ReportCommon(bool res,string name,object got,object expected,string cmp)  {
            using(new WithInvariantCulture()) {
                lock(WriteLock) {
                    ReportOkness(name,res);
                    if(!res || Verbose>=4) {
                        WriteIsComment(res,name,null,GetStackFrame(),got,expected,cmp);
                    }
                }
            }
            TimerReset();
            return res;
        }

        static void AddExtension(CommentDictionaries dic,string key,object val) {
            dic.AddExtension(key,val);
        }

        static CommentDictionaries MkCommentDic(bool result,string name,string msg,StackFrame sf) {
            var dic=new CommentDictionaries();
            if(msg!=null) dic.Add("message",msg);
            if(InTodo!=null) {
                dic.Add("severity","todo");
                dic.AddExtension("todo",InTodo);
            } else {
                dic.Add("severity",result?"success":"fail");
            }
            MethodBase meth=sf.GetMethod();
            var filename=sf.GetFileName();
            if(filename!=null) {
                dic.Add("file",MakeRelative(filename));
                var line=sf.GetFileLineNumber();
                dic.Add("line",line);
                var col=sf.GetFileColumnNumber();
                dic.Add("column",col);
            }
            //if(name!=null) dic.Add("name",name);
            dic.Add("method",string.Format("{0}.{1}",meth.DeclaringType.FullName,meth.Name));
            return dic;
        }

        static public void Autorun(Type t) {
            Autorun(t,(Regex[])null);
        }

        static public void Autorun(Type t,params string[] ps) {
            Autorun(t,ps.Select(x=>new Regex(x,RegexOptions.CultureInvariant|RegexOptions.IgnoreCase)).ToArray());
        }

        static public void Autorun(Type t,params Regex[] ps) {
            if(ps==null || ps.Length==0) ps=new []{new Regex("^test",RegexOptions.CultureInvariant|RegexOptions.IgnoreCase)};
            MethodInfo[] mis=t.GetMethods(BindingFlags.DeclaredOnly|BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
            object instance=null;
            try {
                foreach(var mi in mis) {
                    string name=mi.Name;
                    if(name.Contains('<')) continue; // to skip lambda's (is there a better way to detect them ?)
                    if(name=="Main") continue;
                    if(mi.ReturnParameter.ParameterType!=typeof(void)) continue;
                    if(mi.GetParameters().Length!=0) continue;
                    if(!ps.Any(x=>x.IsMatch(name))) continue;
                    VDiag(2,name+"()");
                    if(mi.IsStatic) {
                        mi.Invoke(null,null);
                    } else {
                        if(instance==null) {
                            var ctor=t.GetConstructor(Type.EmptyTypes);
                            instance=ctor.Invoke(null);
                        }
                        mi.Invoke(instance,null);
                    }
                }
            }
            finally {
                var disp=instance as IDisposable;
                if(disp!=null) disp.Dispose();
            }
        }

        static public void Plan(int tests) {
            Tests=tests;
            using(new WithInvariantCulture()) {
                Console.WriteLine("1..{0}",tests);
            }
            TimerReset();
        }

        public static void TimerReset() {
            if(Stopwatch==null) Stopwatch=new Stopwatch();
            Stopwatch.Reset();
            Stopwatch.Start();
        }

        static public bool Ok(bool res,string name) {
            return OkCommon(res,name);
        }
        
        static public bool Ok(bool res) {
            return OkCommon(res,null);
        }

        static public bool Ok(Func<bool> del,string name) {
            return OkCommon(del(),name);
        }

        static public bool Ok(Func<bool> del) {
            return OkCommon(del(),null);
        }
        
        static bool OkCommon(bool res,string name) {
            using(new WithInvariantCulture()) {
                lock(WriteLock) {
                    ReportOkness(name,res);
                    if(!res || Verbose>=4) {
                        WriteComment(MkCommentDic(res,name,null,GetStackFrame()),null,name);
                    }
                }
            }
            TimerReset();
            return res;
        }

        static public bool Is<T>(T got,T expected,string name) {
            return IsCommon(got,expected,name,false);
        }

        static public bool Is<T>(T got,T expected) {
            return IsCommon(got,expected,null,false);
        }

        static public bool Isnt<T>(T got,T expected,string name) {
            return IsCommon(got,expected,name,true);
        }

        static public bool Isnt<T>(T got,T expected) {
            return IsCommon(got,expected,null,true);
        }

        static bool IsCommon(object got,object expected,string name,bool not) {
            return ReportCommon(object.Equals(got,expected)^not,name,got,expected,not?"!=":null);
        }

        static public bool IsDeeply(object got,object expected,string name) {
            return IsDeeplyCommon(got,expected,name);
        }

        static public bool IsDeeply(object got,object expected) {
            return IsDeeplyCommon(got,expected,null);
        }

        static bool IsDeeplyCommon(object got,object expected,string name) {
            List<PathEntry> path;
            DeepCmp.Result res=new DeepCmp(BlackBoxTypes).Compare(got,expected,out path);
            bool bres=res==DeepCmp.Result.Eq;
            using(new WithInvariantCulture()) {
                lock(WriteLock) {
                    ReportOkness(name,bres);
                    if(!bres || Verbose>=4) {
                        var comdic=MkCommentDic(bres,name,null,GetStackFrame());
                        comdic.Add("actual",got);
                        comdic.Add("expected",expected);
                        WriteComment(comdic,path,name);
                    }
                }
            }
            TimerReset();
            return bres;
        }

        static public bool Like<T>(T got,string expected,string name) {
            return LikeCommon<T>(got,new Regex(expected,RegexOptions.CultureInvariant),name,false);
        }

        static public bool Like<T>(T got,string expected) {
            return LikeCommon<T>(got,new Regex(expected,RegexOptions.CultureInvariant),null,false);
        }

        static public bool Unlike<T>(T got,string expected,string name) {
            return LikeCommon<T>(got,new Regex(expected,RegexOptions.CultureInvariant),name,true);
        }

        static public bool Unlike<T>(T got,string expected) {
            return LikeCommon<T>(got,new Regex(expected,RegexOptions.CultureInvariant),null,true);
        }

        static public bool Like<T>(T got,Regex expected,string name) {
            return LikeCommon<T>(got,expected,name,false);
        }

        static public bool Like<T>(T got,Regex expected) {
            return LikeCommon<T>(got,expected,null,false);
        }

        static public bool Unlike<T>(T got,Regex expected,string name) {
            return LikeCommon<T>(got,expected,name,true);
        }

        static public bool Unlike<T>(T got,Regex expected) {
            return LikeCommon<T>(got,expected,null,true);
        }

        static bool LikeCommon<T>(T got,Regex expected,string name,bool not) {
            return ReportCommon((got!=null && expected.IsMatch(got.ToString()))^not,name,got,
                expected.ToString(),not?"!~":"=~");
        }

        static public bool CmpOk<T,U>(T got,Func<T,U,bool> cmp,U expected,string name) {
            return CmpOkCommon(got,cmp,expected,name);
        }

        static public bool CmpOk<T,U>(T got,Func<T,U,bool> cmp,U expected) {
            return CmpOkCommon(got,cmp,expected,null);            
        }

        static string CmpName(Delegate cmp) {
            var meth=cmp.Method;
            return string.Concat(meth.DeclaringType.FullName,".",meth.Name);
        }
        
        static public bool CmpOkCommon<T,U>(T got,Func<T,U,bool> cmp,U expected,string name) {
            return ReportCommon(cmp(got,expected),name,got,expected,CmpName(cmp));
        }

        static public bool Pass(string name) {
            return OkCommon(true,name);
        }

        static public bool Fail(string name) {
            return OkCommon(false,name);
        }

        static public bool Diag(string fmt,params object[] ps) {
            using(new WithInvariantCulture()) {
                lock(WriteLock) {
                    Console.Write("# ");
                    if(ps==null || ps.Length==0)  {
                        Console.WriteLine(fmt);
                    } else {
                        Console.WriteLine(fmt,ps);
                    }
                }
            }
            return true;
        }

        static public bool VDiag(int v,string fmt,params object[] ps) {
            if(v<=Verbose) {
                return Diag(fmt,ps);
            }
            return true;
        }

        static public bool Skip(string why,int n,Func<bool> unless,Action del) {
            if(!unless()) {
                using(new WithInvariantCulture()) {
                    lock(WriteLock) {
                        while(n--!=0) {
                            ++Cur;
                            Console.WriteLine("ok {0} # SKIP {1}",Cur,why);
                        }
                    }
                }
                return true;
            } else {
                del();
                return false;
            }
        }

        static public void Todo(string why,Action del) {
            string oldInTodo=InTodo;
            try {
                InTodo=why;
                del();
            } finally {
                InTodo=oldInTodo;
            }
        }

        static public bool Except(Action f,Type exceptiontype,string name) {
            return ExceptCommon(f,exceptiontype,null,name);
        }

        static public bool Except(Action f,Type exceptiontype) {
            return ExceptCommon(f,exceptiontype,null,null);
        }

        static public bool Except(Action f,string errtext,string name) {
            return ExceptCommon(f,null,errtext,name);
        }

        static public bool Except(Action f,string errtext) {
            return ExceptCommon(f,null,errtext,null);
        }

        static public bool Except(Action f,Regex errtext,string name) {
            return ExceptCommon(f,null,errtext,name);
        }

        static public bool Except(Action f,Regex errtext) {
            return ExceptCommon(f,null,errtext,null);
        }

        static bool ExceptCommon(Action f,Type exceptiontype,object errtext,string name) {
            Exception e=null;
            string msg=null;
            try {
                f();
            }
            catch(Exception ee) {
                e=ee;
                msg=e.Message;  // get the message in current culture
            }
            using(new WithInvariantCulture()) {
                if(e==null) {
                    lock(WriteLock) {
                        ReportNotOk(name);
                        WriteComment(MkCommentDic(false,name,"expected exception but got none",GetStackFrame()),null,name);
                    }
                } else {
                    string type=e.GetType().Name;
                    if(exceptiontype!=null && !(exceptiontype.IsInstanceOfType(e)))  {
                        lock(WriteLock) {
                            ReportNotOk(name);
                            WriteIsComment(false,name,"the exception was not (a child) of the expected type",GetStackFrame(),type,exceptiontype.Name,null);
                        }
                    } else if((errtext is string && (string)errtext!=msg)
                              || (errtext is Regex && !((Regex)errtext).IsMatch(msg))) {
                        lock(WriteLock) {
                            ReportNotOk(name);
                            WriteIsComment(false,name,"the exception message did not match",GetStackFrame(),msg,errtext.ToString(),null);
                        }
                    }
                    else {
                        lock(WriteLock) {
                            ReportOk(name);
                            VDiag(3,"exception of type {0} thrown as expected: {1}",type,msg);
                        }
                        return true;
                    }
                }
            }
            TimerReset();
            return false;
        }

        public static bool Isa(object o,Type t,string name) {
            return IsaCommon(o,t,name);
        }

        public static bool Isa(object o,Type t) {
            return IsaCommon(o,t,null);
        }

        static bool IsaCommon(object o,Type t,string name) {
            return ReportCommon(t.IsInstanceOfType(o),name,o.GetType().FullName,t.FullName,null);
        }

    }

}

