using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace NameServer
{
    public class ServerData
    {
        public string directory { get; internal set; }
        public Process Process { get; internal set; }
        public string RemoteIP { get; internal set; }
        public Timer Execute(TimeSpan timeSpan) 
        {
            Timer timer = new Timer(timeSpan.TotalMilliseconds)
            {
                Enabled = true
            };
            timer.Elapsed += (object sender, ElapsedEventArgs e) => 
            {
                Process.Kill();
                Process.Dispose();
                Directory.Delete(directory, true);
                ServerHandler.ipDirectory.Remove(RemoteIP);
                Console.WriteLine(DateTime.Now.ToLongDateString() + " Server process at " + directory + " stopped and directory has been deleted.");
            };
            return timer;
        }
    }
}
