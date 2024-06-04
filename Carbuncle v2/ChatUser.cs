using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
    public class ChatUser
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public string uID
        {
            get { return "#" + (Name + Color).GetHashCode().ToString().Substring(0, 4); }
        }
        public TcpClient client;
        private NetworkStream stream;
        public StreamReader read;
        public StreamWriter write;

        public Thread userThread;

        const byte NAME = 1, COLOR = 2, FIRST = 3;
        public static bool Interrupt;

        const string CMD_Symbol = "/";

        public void SendNetworkMessage(string message)
        {
            byte[] buffer = null;
            string text = string.Format("MESSAGE {0} {1} {2}", Name, Color, message);
            buffer = Encoding.ASCII.GetBytes(text);

            stream.Write(buffer, 0, buffer.Length);
        }

        public Thread NetworkLoop()
        {
            return new Thread((ThreadStart)delegate
            {
                while (!Interrupt)
                {
                    try
                    {
                        while (stream?.DataAvailable == false)
                            Thread.Sleep(500);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                        return;
                    }

                    string buffer = StringFromStream(stream);
                    string message = null;

                    if (buffer.StartsWith("MESSAGE"))
                    {
                        message = ExtractMessage(buffer, ' ', out string[] data);
                        MainWindow.Instance.DisplayMessage(data[NAME], data[COLOR], message);
                    }
                    if (buffer.StartsWith("EMOTE"))
                    {
                        message = ExtractMessage(buffer, ' ', out string[] data);
                        MainWindow.Instance.DisplayMessage(data[NAME], data[COLOR], message, true);
                    }
                    if (buffer.Contains("CLEAR"))
                    {
                        MainWindow.Instance.listbox_servers.Dispatcher.Invoke((Action)delegate
                        {
                            MainWindow.Instance.listbox_servers.Items.Clear();
                        });
                    }
                    if (buffer.Contains("LIST"))
                    {
                        MainWindow.Instance.listbox_servers.Dispatcher.Invoke((Action)delegate
                        {
                            string[] array = buffer.Split(',');
                            if (array.Length > 1)
                            {
                                for (int i = 1; i < array.Length; i++)
                                {
                                    if (array[i].Length > 6)
                                    {
                                        MainWindow.Instance.listbox_servers.Items.Add(array[i]);
                                    }
                                }
                            }
                        });
                    }
                }
            });
        }
        public bool RemoteRegister(string code, int port = 1010)
        {
            try
            {
                client = new TcpClient();
                client.Connect(IPAddress.Parse(MainWindow.Instance.textbox_ipstart.Text), port);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }
            write = new StreamWriter(client.GetStream());
            read = new StreamReader(client.GetStream());
            stream = client.GetStream();

            byte[] buffer = Encoding.ASCII.GetBytes(string.Format("LOGIN {0} {1} {2} {3}", Name, code, Color, uID));
            stream.Write(buffer, 0, buffer.Length);

            int num = -1;
            const int max = 10;
            if (client.Connected)
            {
                while (!stream.DataAvailable)
                {
                    Thread.Sleep(1000);
                    if (++num == max)
                    {
                        MainWindow.Instance.label_counter.Content = num + "/" + max;
                        return false;
                    }
                }
                if (MainWindow.Instance.StringFromStream(stream).Contains("SUCCESS"))
                    return true;
            }
            return false;
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
        private string ReadWithPeek(StreamReader read)
        {
            string s = "";
            while (read.Peek() != -1)
            {
                s += char.ConvertFromUtf32(read.Read());
            }
            return s;
        }
    }
}
