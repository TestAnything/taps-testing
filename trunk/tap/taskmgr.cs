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
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Taps {

    public abstract class TaskMgrClient {
        public bool TapWasUpdated;

        public abstract void OutputReceived(string s,bool force);
        public abstract void ErrorReceived(string s,bool force);
    }

    class TaskMgr: IDisposable {

        int MaxTasks;

        Queue<Task> Tasks=new Queue<Task>();

        TaskMgrClient Client;

        AutoResetEvent TaskDone=new AutoResetEvent(false);

        public TaskMgr(int maxtasks,TaskMgrClient client) {
            MaxTasks=maxtasks;
            Client=client;
        }

        public void Dispose() {
            ((IDisposable)TaskDone).Dispose();
        }

        void AddTask(ScriptPath sp,bool single) {
            TAPApp.DLog(3,"Add task {0}",sp);
            var task=new Task(TaskDone,Client,sp,single);
            if(Tasks.Count==0) task.Head=true;
            task.Run();
            Tasks.Enqueue(task);
        }

        void ReapHead() {
            TAPApp.DLog(3,"ReapHead {0} tasks active",Tasks.Count);
            while(Tasks.Count!=0) {
                var task=Tasks.Peek();
                task.Head=true;
                if(task.Running) break;
                TAPApp.DLog(3,"finish task {0}",task.ScriptPath);                
                task.WriteBuffered();
                task.TaskComplete();
                Tasks.Dequeue();
            }
        }

        int CountRunningTasks() {
            return Tasks.Count(x=>x.Running);
        }

        public void Run(ScriptPath[] srcs) {
            int maxtasks=TAPApp.MaxTasks;
            TAPApp.VLog(3,"Max. {0} {1}.",maxtasks,maxtasks==1?"task":"parallel tasks");
            int k=0;
            bool single=srcs.Length==1;
            do {
                bool waiting=k!=srcs.Length;
                int running=CountRunningTasks();
                if(!waiting && running==0) TAPParser.EndTotal();
                ReapHead();
                if(waiting) {
                    running=CountRunningTasks();
                    while(running<maxtasks) {
                        if(k!=srcs.Length) {
                            AddTask(srcs[k],single);
                            ++k;
                            ++running;
                        } else  {
                            break;
                        }
                    }
                }
                if(Tasks.Count==0) break;
                TaskDone.WaitOne();
            } while(true);
            TAPApp.DLog(3,"TaskMgr done.");
        }
        
    }

}