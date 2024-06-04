using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using System.Net;
using System.Net.Sockets;

namespace RegServer
{
    public class SendData
    {
        TcpListener tcp;
        TcpClient client;
        NetworkStream stream;
        public Thread NetworkHandler(int port = 1030)
        {
            return new Thread((ThreadStart)delegate
            {
                tcp = new TcpListener(IPAddress.Any, port);
                tcp.Start(100);
                while (!Global.Quit)
                {
                    client = tcp.AcceptTcpClient();
                    stream = client.GetStream();

                    int num = -1;
                    const int max = 15;
                    bool go = true;
                    while (!stream.DataAvailable)
                    {
                        Thread.Sleep(1000);
                        if (++num == max)
                        {
                            go = false;
                            break;
                        }
                    }
                    if (!go) continue;

                    string data = Global.StringFromStream(stream);
                    if (data.StartsWith("GRAB"))
                    {
                        byte[] buffer = null;
                        int count = 0;
                        if (Global.Addr.Count != 0)
                        {
                            for (int i = 0; i < (count = Global.Addr.Count); i++)
                            {
                                string clone = Global.Addr[i].Clone().ToString();
                                if (i < count - 1)
                                    clone += ",";
                                buffer = Encoding.ASCII.GetBytes(clone);
                                stream.Write(buffer, 0, buffer.Length);
                            }
                        }
                        Console.WriteLine(client.Client.RemoteEndPoint.ToString() + " requested IP address directory.");
                    }
                }
            });
        }
    }
}
