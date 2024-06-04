using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;

using CirclePrefect.Dotnet;

namespace NameServer
{
    public class Client
    {
        public static IList<Client> Clients = new List<Client>();
        internal IList<string> ignore = new List<string>();
        private IList<string> cmdList = new List<string>() { "ignore", "whisper", "me" };
        internal static IList<string> Filter = new List<string>(); 
        public string Color { get; set; }
        public string Name { get; set; }
        public string uID { get; set; }
        private bool clear;
        public StreamReader read;
        public StreamWriter write;
        public TcpClient client;
        public NetworkStream stream;
        private IList<string> servers = new List<string>();
        private bool active = true;
        private System.Timers.Timer update;
        private bool skip;
        
        const byte NAME = 1, COLOR = 2, FIRST = 3, INFO = 4;
        public void Initialize()
        {
            read = new StreamReader(client.GetStream());
            write = new StreamWriter(client.GetStream());
            stream = client.GetStream();
            (update = Update()).Start();

            Block block = LoginHandler.Data.GetBlock(Name.ToLower());
            string[] array = block.GetValue("ignore").Split(';');
            foreach (string name in array)
            {
                if (string.IsNullOrEmpty(name))
                    ignore.Add(name);
            }
        }

        private System.Timers.Timer Update()
        {
            var update = new System.Timers.Timer(5000)
            {
                AutoReset = true,
                Enabled = true
            };
            update.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
                if (!active)
                {
                    update.Stop();
                    update.Dispose();
                    return;
                }
             
                clear = true;
                servers.Clear();

                foreach (Process process in GetRunningServers())
                {
                    string title;
                    servers.Add(title = GetServerTitle(process));
                    byte[] buffer = null;
                    if (clear)
                    {
                        buffer = Encoding.ASCII.GetBytes("CLEAR");
                        stream.Write(buffer, 0, buffer.Length);
                        clear = false;
                    }
                    buffer = Encoding.ASCII.GetBytes("LIST," + title + ",");
                    stream.Write(buffer, 0, buffer.Length);
                }
            };
            return update;
        }
        private Process[] GetRunningServers()
        {
            return Process.GetProcessesByName("TerrariaServer"); ;
        }
        private string GetServerTitle(Process process)
        {
            return process.MainWindowTitle;
        }

        public Thread NetworkHandler()
        {
            return new Thread((ThreadStart)delegate
            {
                while (active)
                {
                    while (!stream.DataAvailable)
                        Thread.Sleep(200);

                    string buf = StringFromStream(stream);
                    Console.WriteLine(buf);

                    string message = ExtractMessage(buf, ' ', out string[] data);
                    if (WordFilter(message))
                        continue;

                    if (cmdList.Contains(data[FIRST].ToLower().Substring(1)) && data.Length > 3 && message.Length > 3)
                    {
                        if (CommandHandler(data[FIRST], message.Substring(data[FIRST].Length + 1), data[INFO], data[NAME], out byte[] buffer))
                        {
                            stream.Write(buffer, 0, buffer.Length);
                            continue;
                        }
                    }

                    foreach (Client c in Clients)
                    {
                        try
                        {
                            if (c.active && !c.ignore.Contains(data[NAME].ToLower()))
                            {
                                byte[] b = Encoding.ASCII.GetBytes(buf);
                                c.stream.Write(b, 0, b.Length);
                            }
                        }
                        catch
                        {
                            Disconnect(c);
                            continue;
                        }
                    }
                }
            });
        }


        private bool WordFilter(string message)
        {
            foreach (string s in Filter)
            {
                if (message.ToLower().Contains(s))
                {
                    string error = "MESSAGE Server #000000 Word found in filter therefore message ignored.";
                    byte[] b = Encoding.ASCII.GetBytes(error);
                    stream.Write(b, 0, b.Length);
                    return true;
                }
            }
            return false;
        }

        private bool CommandHandler(string command, string content, string name, string username, out byte[] output)
        {
            bool flag = false;
            string user = content.Clone().ToString();
            if (!command.StartsWith("/") && !command.StartsWith("!"))
            {
                output = null;
                return flag;
            }
            command = command.Substring(1);
            content = content.ToLower();

            if (content.Contains(' '))
                content = content.Split(' ')[0];
            bool added = true;
            switch (command.ToLower())
            {
                case "ignore":
                    Block block = LoginHandler.Data.GetBlock(Name);
                    string list = block.GetValue("ignore");
                    
                    if (list.StartsWith('0'))
                        list = list.TrimStart('0');
                    
                    if (ignore.Contains(content))
                    {
                        list = list.Replace(content + ";", "");
                        ignore.Remove(content);
                        added = false;
                    }
                    else
                    {
                        list += content + ";";
                        ignore.Add(content);
                    }
                    block.WriteValue("ignore", list);
                    LoginHandler.Data.WriteToFile();

                    output = Encoding.ASCII.GetBytes(string.Format("MESSAGE Server #000000 User {0} {1} using ignore list.", user, added ? "added" : "removed"));
                    flag = true;
                    break;
                case "whisper":
                    if (user.Length > name.Length + 1)
                    {
                        string message = user.Substring(name.Length + 1);
                        Client c = Clients.Where(n => n.Name.ToLower() == name.ToLower()).FirstOrDefault();
                        if (c != null && c.Name != null && c.active && !c.ignore.Contains(username.ToLower()))
                        {
                            try
                            {
                                byte[] n = Encoding.ASCII.GetBytes(string.Format("MESSAGE {0} {1} (whisper) {2}", Name, Color, message));
                                c.stream.Write(n, 0, n.Length);
                            }
                            catch
                            {
                                Disconnect(c);
                            }
                            output = Encoding.ASCII.GetBytes(string.Format("MESSAGE {0} {1} (whispered) {2}", Name, Color, message));
                            flag = true;
                            break;
                        }
                        else
                        {
                            output = Encoding.ASCII.GetBytes("_");
                        }
                        flag = true;
                        break;
                    }
                    flag = false;
                    goto default;
                case "me":
                    byte[] buffer = new byte[10];
                    foreach (Client n in Clients)
                    {
                        if (n != null && n.active && !n.ignore.Contains(username.ToLower()))
                        {
                            try
                            {
                                buffer = Encoding.ASCII.GetBytes(string.Format("EMOTE {0} {1} {2}", Name, Color, content));
                                n.stream.Write(buffer, 0, buffer.Length);
                            }
                            catch
                            {
                                Disconnect(n);
                                continue;
                            }
                        }
                    }
                    flag = true;
                    goto default;
                default:
                    output = Encoding.ASCII.GetBytes("");
                    break;
            }
            return flag;
        }

        private void Disconnect(Client c)
        {
            Console.WriteLine(c.Name + " disconnected.");
            c.update.Stop();
            c.active = false;
        }
        private string ExtractMessage(string buffer, char separator, out string[] split)
        {
            int index = 0;
            var data = buffer.Split(separator);
            for (int i = 0; i < COLOR + 1; i++)
            {
                index += data[i].Length + 1;
            }
            split = data;
            return buffer.Substring(index);
        }
        private string StringFromStream(NetworkStream stream)
        {
            string buf = "";
            byte[] buffer = new byte[256];
            using (MemoryStream mem = new MemoryStream())
            {
                int bytesRead = 0;
                while (stream.DataAvailable)
                {
                    bytesRead += stream.Read(buffer, 0, buffer.Length);
                }
                buf = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            }
            return buf;
        }
    }
}
