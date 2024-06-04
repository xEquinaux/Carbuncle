using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Carbuncle_v2
{
    public class LocalServer
    {
        public static LocalServer Instance;
        public static IList<LocalServer> Servers = new List<LocalServer>();
        public int Port
        {
            get; private set;
        }
        public IPAddress IP;
        public string Name
        {
            get; private set;
        }
        public string Address
        {
            get { return IP.ToString(); }
            set { IPAddress.TryParse(value, out IP); }
        }
        public int Index
        {
            get; set;
        }
        
        public LocalServer()
        {
            Instance = this;
            
        }

        public static Process[] GetRunningServers()
        {
            return Process.GetProcessesByName("TerrariaServer"); ;
        }
        public static string GetServerTitle(Process process)
        {
            return process.MainWindowTitle;
        }
    }
}
