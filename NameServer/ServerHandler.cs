using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

using System.Net;
using System.Net.Sockets;

namespace NameServer
{
    public class ServerHandler
    {
        internal static IList<string> ipDirectory = new List<string>();
        public Thread NetworkHandler(int port= 1040)
        {
            return new Thread((ThreadStart)delegate
            {
                IList<int> ports = new List<int>();                
                const byte _PLAYERS = 0, _SIZE = 1, _NAME = 2, _DIFFICULTY = 3, _SEED = 4, _PASS = 5;
                TcpListener tcp = new TcpListener(IPAddress.Any, port);
                tcp.Start(100);
                while (!Program.ShutDown)
                {
                    string path = "";
                    TcpClient client = tcp.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();

                    string ip = client.Client.RemoteEndPoint.ToString();
                    ip = ip.Substring(0, ip.IndexOf(":"));
                    if (!ipDirectory.Contains(ip))
                        ipDirectory.Add(ip);
                    else
                    {
                        Console.WriteLine(DateTime.Now.ToShortTimeString() + " " + ip + " has tried to start another server -- request denied.");
                        continue;
                    }

                    Process.Start("SystemResourceOutput.exe");
                    do
                    {
                        Thread.Sleep(3000);
                    } while (!File.Exists("ram.ini"));

                    string output = "";
                    using (StreamReader sr = new StreamReader("ram.ini"))
                    {
                        output = sr.ReadToEnd().Trim(new char[] { '\n', '\r' });
                    }
                    if (int.TryParse(output, out int ram))
                    {
                        if (ram < 700)
                        {
                            Console.WriteLine("RAM total -- " + ram + " is less that the required level for a server startup.");
                            continue;
                        }
                    }
                    else return;

                    if (Timeout(stream))
                        continue;

                    string start = Global.StringFromStream(stream);
                    string[] data = start.Split(",");

                    using (FileStream file = new FileStream("tshock.archive.zip", FileMode.Open, FileAccess.Read))
                    {
                        using (ZipArchive zip = new ZipArchive(file, ZipArchiveMode.Read))
                        {
                            zip.ExtractToDirectory(path = string.Format("{0}_{1}", data[_NAME], data[_PASS].GetHashCode().ToString().Trim('-')));
                            Console.WriteLine("Server created at " + path);
                        }
                    }

                    string directory = Directory.GetCurrentDirectory();
                    Directory.SetCurrentDirectory(directory + "\\" + path);

                    using (StreamWriter sw = new StreamWriter("serverconfig.txt"))
                    {
                        sw.WriteLine("difficulty=" + Difficulty(data[_DIFFICULTY]));
                        sw.Write("seed=" + data[_SEED]);
                    }

                    const int min = 6000, max = 9999;
                    int chosen = 0;
                    do
                    {
                        chosen = new Random().Next(min, max);
                    } while (ports.Contains(chosen));
                    ports.Add(chosen);

                    ProcessStartInfo info = new ProcessStartInfo(path + "\\TerrariaServer", string.Format("-port {0} -maxplayers {1} -world {2} -worldpath {3} -autocreate {4} -config {5}", new object[] { chosen, Math.Max(Math.Min(int.Parse(data[_PLAYERS]), 255), 8), data[_NAME], directory + "\\" + path + "\\worlds", WorldSize(data[_SIZE]), "serverconfig.txt" }));
                    info.FileName = "TerrariaServer.exe";
                    info.UseShellExecute = true;
                    info.WindowStyle = ProcessWindowStyle.Minimized;

                    ServerData server = new ServerData();
                    server.RemoteIP = ip;
                    server.directory = directory + "\\" + path;
                    server.Process = Process.Start(info);
                    server.Execute(new TimeSpan(0, 30, 0)).Start();

                    Console.Write(DateTime.Now.ToLongTimeString() + " Server creation successful.");

                    Directory.SetCurrentDirectory(directory);
                }
            });
        }

        private int Difficulty(string name)
        {
            switch (name)
            {
                case "Classic":
                    return 0;
                case "Expert":
                    return 1;
                case "Master":
                    return 2;
                case "Journey":
                    return 3;
                default:
                    return 0;
            }
        }
        private int WorldSize(string name)
        {
            switch (name)
            {
                case "Small":
                    return 1;
                case "Medium":
                    return 2;
                case "Large":
                    return 3;
                default:
                    return 1;
            }
        }

        private bool Timeout(NetworkStream stream)
        {
            int num = 0;
            const int max = 15;
            while (!stream.DataAvailable)
            {
                Thread.Sleep(1000);
                if (++num == max)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
