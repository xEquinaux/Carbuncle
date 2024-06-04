using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using System.Net;
using System.Net.Sockets;

namespace RegServer
{
    public class ReceiveData
    {
        TcpListener tcp;
        TcpClient client;
        NetworkStream stream;
        public Thread NetworkHandler(int port = 1020)
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
                    const int max = 30;
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

                    string ip = "";
                    if (IPAddress.TryParse(ip = Global.StringFromStream(stream), out _))
                    {
                        if (!Global.Addr.Contains(ip))
                        {
                            Global.Addr.Add(ip);
                            Console.WriteLine(ip + " address added to directory.");
                        }
                    }
                }
            });
        }
    }
}
