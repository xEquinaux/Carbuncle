using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NameServer
{
   class Program
   {
      public static bool ShutDown;
      [MTAThread]
      static void Main(string[] args)
      {
         Console.WriteLine("Carbuncle Login & Lobby Server\n© 2020 Circle Prefect");

         if (!File.Exists("filter.ini"))
         {
            using (File.CreateText("filter.ini")) ;
         }
         else
         {
            using (StreamReader sr = new StreamReader("filter.ini"))
            {
               foreach (string s in sr.ReadToEnd().Split(','))
               {
                  if (!string.IsNullOrWhiteSpace(s) && !string.IsNullOrWhiteSpace(s))
                        Client.Filter.Add(s.ToLower().Replace(" ", ""));
               }
            }
         }

         if (!File.Exists("./ip_server.ini"))
         {
            using (File.CreateText("./ip_server.ini")) ;
         }
         SendAddress().Start();

         new ServerHandler().NetworkHandler().Start();
         LoginHandler handler = new LoginHandler();
            
         while (Console.ReadLine() != "exit") ;
         ShutDown = true;
      }

      private static Thread SendAddress()
      {
         return new Thread((ThreadStart)delegate
         {
            TcpClient client;
            NetworkStream stream;

            string text = "";
            using (StreamReader read = new StreamReader("./ip_server.ini"))
               text = read.ReadToEnd();

            if (!IPAddress.TryParse(text, out IPAddress addr))
            {
               addr = Dns.GetHostAddresses(text)[0];
            }
            try
            {
               client = new TcpClient();
               client.Connect(addr, 1020);
               stream = client.GetStream();

               if (!client.Connected)
                     return;

               string local = System.Environment.MachineName;
               IPHostEntry host = Dns.GetHostEntry(local);
               string ip = Convert.ToString(host.AddressList.Where(i => i.AddressFamily == AddressFamily.InterNetwork).First());

               byte[] buffer = Encoding.ASCII.GetBytes(ip);
               stream.Write(buffer, 0, buffer.Length);
            }
            catch
            {
               Console.WriteLine("Provide a legitimate IP address for the IP address directory server in ip_server.ini, then restart this program.");
               Console.WriteLine("Press any key to close this process...");
               Console.ReadKey();
               Process.GetCurrentProcess().Kill();
               return;
            }
         });
      }
   }
}
