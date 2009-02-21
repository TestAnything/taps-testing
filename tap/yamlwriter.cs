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
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Taps {

    public class YNode {
        public int Depth;
        public object Key;
        public object O;
        public int Ref;
        public enum Flag {None,ReferredTo=1,Dic=2,Leaf=4}
        public Flag Flags;

        public bool IsDic {
            get {return (Flags&Flag.Dic)!=0;}
        }

        public bool IsLeaf {
            get {return (Flags&Flag.Leaf)!=0;}
        }

        public YNode(int depth,object key,object o,int rf,Flag flags) {
            Depth=depth;
            Key=key;
            O=o;
            Ref=rf;
            Flags=flags;
        }
    }

    public class YAMLWalker: GWalker {

        public YAMLWalker(IDictionary<Type,bool> bbd): base(bbd) {
        }
        
        public override bool IsLeafType(object o,Type t) {
            return YAMLWriter.IsLeafType(o,t);
        }

        static bool IsMap(Node n) {
            if(n.Leaf) return false;
            if(n.O is IDictionary) return true;
            return !(n.O is IEnumerable);
        }

        public new List<YNode> Walk(object key,object o) {
            var nodes=new List<YNode>();
            foreach(Node i in base.Walk(key,o)) {
                int dupidx=i.Dupidx;
                var ynode=new YNode(Depth,i.Key,i.O,dupidx,
                                    IsMap(i)?YNode.Flag.Dic:YNode.Flag.None);
                if(dupidx!=-1) nodes[dupidx].Flags|=YNode.Flag.ReferredTo;
                if(i.Leaf) ynode.Flags|=YNode.Flag.Leaf;
                nodes.Add(ynode);
            }
            return nodes;
        }
        
    }

    public class YAMLWriter {

        class LineWriter {
            public bool Sameline {
                get {return Column!=0;}
            }
            public int Column;
            public TextWriter Tw;
            public LineWriter(TextWriter tw,int column) {
                Tw=tw;
                Column=column;
            }

            public void Write(string s) {
                Tw.Write(s);
                Column+=s.Length;
            }
        }

        string Depth;
        int HorizontalThresh;
        LineWriter Lw;
        List<YNode> List;
        IDictionary<Type,bool> BBD;

        public Func<int,YNode,string> Annotate;

        public YAMLWriter(TextWriter tw,int initialdepth,int horizontalthresh,IDictionary<Type,bool> bbd) {
            Depth=new string(' ',initialdepth);
            HorizontalThresh=horizontalthresh;
            Lw=new LineWriter(tw,0);
            BBD=bbd;
            VertWriter=new LeafWriter(this,VertNeedsQuotes,s=>LineBreak.IsMatch(s));
            HorizontalArrWriter=new LeafWriter(this,HArrNeedsQuotes,s=>false);
            HorizontalDicWriter=new LeafWriter(this,HDicNeedsQuotes,s=>false);
        }

        public YAMLWriter(TextWriter tw,int initialdepth,int horizontalthresh)
            : this(tw,initialdepth,horizontalthresh,null) {
        }

        public static bool IsLeafType(object o,Type t) {
            return t.IsPrimitive || (o is string) || t.IsEnum;
        }

        public static bool IsLeafType(object o) {
            return IsLeafType(o,o.GetType());
        }

        void WriteDepthSpace(int depth) {
            if(Lw.Sameline) return;
            Lw.Write(Depth);
            // all docs except the ones containing a single scalar
            // have a root node that is not printed and shouldn't
            // contribute to the indent
            if(depth>0) depth--;
            Lw.Write(new string(' ',depth*2));
        }
        
        void Write(int depth,string fmt,params object[] ps) {
            WriteDepthSpace(depth);
            string s=(ps==null || ps.Length==0)? fmt: string.Format(fmt,ps);
            Lw.Write(s);
        }

        void WriteLine(int depth,string fmt,params object[] ps) {
            WriteDepthSpace(depth);
            if(ps==null || ps.Length==0) Lw.Tw.WriteLine(fmt); else Lw.Tw.WriteLine(fmt,ps);
            Lw.Column=0;
        }

        void EndLine() {
            if(Lw.Sameline) WriteLine(0,"");
        }

        public string Escape(string s) {
            return VertWriter.Escape(s);
        }

        class LeafWriter {

            static char[] CtrlEscapes=new char[32]{'0','\0','\0','\0','\0','\0','\0','a',
                                                   'b','t','n','v','f','r','\0','\0',
                                                   '\0','\0','\0','\0','\0','\0','\0','\0'
                                                   ,'\0','\0','\0','e','\0','\0','\0','\0'};
            static Dictionary<char,string> OtherEscapes=new Dictionary<char,string> {
                {'"',"\\\""},{'\\',"\\\\"},{'\u0085',"\\N"},{'\u00a0',"\\_"},{'\u2028',"\\L"},
                {'\u2029',"\\P"},{'\ufffe',"\\ufffe"},{'\uffff',"\\uffff"}
            };

            public LeafWriter(YAMLWriter yamlWriter,Regex needsQuotes,Func<string,bool> lineBreakp) {
                NeedsQuotes=needsQuotes;
                LineBreakp=lineBreakp;
                YAMLWriter=yamlWriter;
            }

            YAMLWriter YAMLWriter;
            Func<string,bool> LineBreakp;
            Regex NeedsQuotes;
            
            static Regex LineBreaker=new Regex(@".*?(?:\r\n|[\r\n\u0085\u2028\u2029])|[^\r\n\u0085\u2028\u2029]+$",RegexOptions.CultureInvariant|RegexOptions.Singleline);

            public string Escape(string s) {
                if(s.Length>0 && !NeedsQuotes.IsMatch(s)) return s;
                var sb=new StringBuilder();
                sb.Append('"');
                foreach(char c in s) {
                    if(c<32) {
                        sb.Append('\\');
                        char e=CtrlEscapes[c];
                        if(e==0) {
                            sb.AppendFormat("x{0:x2}",(int)c);
                        }
                        else {
                            sb.Append(e);
                        }
                    } else  {
                        string e;
                        if(OtherEscapes.TryGetValue(c,out e)) {
                            sb.Append(e);
                        } else if((c>='\u007f' && c<'\u00a1') || (c>='\ud800' && c<'\ue000'))  {
                            sb.AppendFormat("\\u{0:x4}",(int)c);
                        }
                        else {
                            sb.Append(c);
                        }
                    }
                }
                sb.Append('"');
                return sb.ToString();
            }
            
            // returns false if not a leaf
            public bool WriteLeaf(YNode n) {
                return WriteLeaf(n,int.MaxValue);
            }

            // returns false if not a leaf or strlen(n.O)>maxlen
            public bool WriteLeaf(YNode n,int maxlen) {
                var o=n.O;
                if(o==null) {
                    YAMLWriter.Write(n.Depth,"~");
                    return true;
                } else if(n.IsLeaf) {
                    var s=n.O.ToString();
                    if(s.Length>maxlen) return false;
                    if(n.O is string || n.O is char) {
                        if(LineBreakp(s)) {
                            var matches=LineBreaker.Matches(s);
                            YAMLWriter.WriteLine(n.Depth,"|2");
                            var olddepth=YAMLWriter.Depth;
                            YAMLWriter.Depth+="  ";
                            foreach(Match i in matches) {
                                YAMLWriter.Lw.Column=0;
                                YAMLWriter.Write(n.Depth,i.Groups[0].Value);
                            }
                            YAMLWriter.Depth=olddepth;
                        } else {
                            YAMLWriter.Write(n.Depth,Escape(s));
                        }
                    } else {
                        YAMLWriter.Write(n.Depth,s);
                    }
                    return true;
                }
                return false;
            }

        }

        static Regex LineBreak=new Regex(@"[\r\n\u0085\u2028\u2029]",RegexOptions.CultureInvariant);
        static Regex VertNeedsQuotes=new Regex(@"[\x00-\x1f""#\u007f-\u00a0\u2028\u2029\ud800-\udfff\ufffe\uffff]",RegexOptions.CultureInvariant);
        static Regex HArrNeedsQuotes=new Regex(@"[\x00-\x1f""\[\],#\u007f-\u00a0\u2028\u2029\ud800-\udfff\ufffe\uffff]",RegexOptions.CultureInvariant);
        static Regex HDicNeedsQuotes=new Regex(@"[\x00-\x1f""{}:,#\u007f-\u00a0\u2028\u2029\ud800-\udfff\ufffe\uffff]",RegexOptions.CultureInvariant);

        LeafWriter VertWriter;

        LeafWriter HorizontalArrWriter;
        LeafWriter HorizontalDicWriter;

        string GetAnnotation(int idx,YNode n) {
            if(Annotate==null) return null;
            return Annotate(idx,n);
        }

        bool WriteAnnotation(int idx,YNode n) {
            string s=GetAnnotation(idx,n);
            if(s!=null) {
                Write(n.Depth,"  # {0}",s);
                return true;
            }
            return false;
        }
        
        int WriteValue(int idx) {
            var n=List[idx];
            if(VertWriter.WriteLeaf(n)) {
                WriteAnnotation(idx,n);
                WriteLine(n.Depth,"");
                ++idx;
            } else {
                if(n.Ref!=-1)  {
                    Write(n.Depth,"*id{0}",n.Ref);
                    WriteAnnotation(idx,n);
                    WriteLine(n.Depth,"");
                    ++idx;
                } else {
                    if((n.Flags&YNode.Flag.ReferredTo)!=0) {
                        Write(n.Depth,"&id{0}",idx);
                        WriteAnnotation(idx,n);
                        WriteLine(n.Depth,"");
                    } else {
                        if(WriteAnnotation(idx,n)) WriteLine(n.Depth,"");
                    }
                    idx=WriteChildren(idx,idx!=0 && n.Key!=null);
                }
            }
            return idx;
        }

        bool HasChildren(int idx) {
            int cidx=idx+1;
            if(cidx>=List.Count) return false;
            return List[cidx].Depth>List[idx].Depth;
        }

        bool HasDepth(int idx,int depth) {
            if(idx>=List.Count) return false;
            return List[idx].Depth==depth;
        }

        int WriteChildren(int idx,bool asmapvalue) {
            if(List[idx].IsDic) {
                return WriteDicChildren(idx,asmapvalue);
            } else {
                return WriteArrChildren(idx,asmapvalue);
            }
        }

        bool WriteHorizontalCollection(ref int idx,string open,string close,Func<YNode,bool> write) {
            if(!List[idx].IsLeaf) return false;
            int i=idx;
            int childdepth=List[i].Depth;
            List<string> annotations=null;
            var tmp=Lw;
            string res;
            try {
                var sw=new StringWriter();
                Lw=new LineWriter(sw,0);
                var sb=sw.GetStringBuilder();
                Lw.Write(open); // to avoid WriteDepthSpace
                var sep="";
                do {
                    var r=List[i];
                    if(!r.IsLeaf) return false;
                    Write(0,sep);
                    sep=", ";
                    string s=GetAnnotation(i,r);
                    if(s!=null) {
                        int len=sb.Length;
                        if(annotations==null) annotations=new List<string>();
                        annotations.Add(string.Concat("#",new string(' ',len-1),"^",s));
                    }
                    if(!write(r)) return false;
                    if(sb.Length>=HorizontalThresh) return false;
                    ++i;
                } while(HasDepth(i,childdepth));
                Write(0,close);
                idx=i;
                res=sb.ToString();
            }
            finally  {
                Lw=tmp;
            }
            WriteDepthSpace(childdepth);
            int curcolumn=Lw.Column;
            Write(childdepth,res);
            if(annotations!=null) {
                foreach(string s in annotations) {
                    EndLine();
                    Lw.Write(new string(' ',curcolumn));
                    Lw.Write(s);
                }
            }
            return true;
        }

        bool WriteHorizontalArray(ref int idx) {
            return WriteHorizontalCollection(ref idx,"[","]",n=>
                                          HorizontalArrWriter.WriteLeaf(n,HorizontalThresh));
        }

        int WriteArrChildren(int idx,bool asmapvalue) {
            if(HasChildren(idx)) {
                ++idx;
                if(WriteHorizontalArray(ref idx)) {
                    WriteLine(0,"");
                } else {
                    if(asmapvalue) EndLine();
                    int childdepth=List[idx].Depth;
                    do {
                        var r=List[idx];
                        Write(r.Depth,"- ");
                        idx=WriteValue(idx);
                    } while(HasDepth(idx,childdepth));
                }
            } else {
                WriteLine(List[idx].Depth,"");
                ++idx;
            }
            return idx;
        }

        bool WriteHorizontalDic(ref int idx,int maxkey) {
            if(maxkey>HorizontalThresh) return false;
            return WriteHorizontalCollection(ref idx,"{","}",n=>{
                    string key=HorizontalDicWriter.Escape(n.Key.ToString())+": ";
                    Write(0,key);
                    return HorizontalDicWriter.WriteLeaf(n,HorizontalThresh);
                });
        }

        int GetMaxKey(LeafWriter lw,int idx,int childdepth) {
            int m=0;
            do {
                var r=List[idx];
                if(r.Depth==childdepth) {
                    if(r.Key!=null) {
                        int l=lw.Escape(r.Key.ToString()).Length;
                        if(m<l) m=l;
                    }
                }
                ++idx;
            } while(idx!=List.Count && List[idx].Depth>=childdepth);
            return m>20?20:m;
        }

        int WriteDicChildren(int idx,bool asmapvalue) {
            if(HasChildren(idx)) {
                ++idx;
                int childdepth=List[idx].Depth;
                int m=GetMaxKey(VertWriter,idx,childdepth)+2;
                if(WriteHorizontalDic(ref idx,m)) {
                    WriteLine(0,"");
                } else {
                    if(asmapvalue) EndLine();
                    do {
                        var r=List[idx];
                        // complex data types as map keys (yaml "? k : v")
                        // not done yet, we always ToString() them for
                        // now.
                        string key=VertWriter.Escape(r.Key.ToString())+": ";
                        Write(r.Depth,key);
                        if(key.Length<m) Write(r.Depth,new string(' ',m-key.Length));
                        idx=WriteValue(idx);
                    } while(HasDepth(idx,childdepth));
                }
            } else {
                WriteLine(List[idx].Depth,"");
                ++idx;
            }
            return idx;
        }

        public TextWriter Write(object o) {
            var walker=new YAMLWalker(BBD);
            List=walker.Walk(null,o);
            WriteLine(0,"---");
            WriteValue(0);
            WriteLine(0,"...");
            return Lw.Tw;
        }
    }
}
