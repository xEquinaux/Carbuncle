using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using CirclePrefect.Dotnet;

namespace NameServer
{
	public class LoginHandler
	{
		public static LoginHandler Instance;
		public int Port { get; set; }
		internal static DataStore Data;
		public LoginHandler(int port = 1010, string filename = "data")
		{
			Instance = this;
			Port = port;
			InitData(filename);
			Start().Start();
		}
		private void InitData(string filename)
        {
			Data = new DataStore(filename);
        }
		const byte NAME = 1, CODE = 2, COLOR = 3, UID = 4;
		public Thread Start()
        {
			var thread = new Thread((ThreadStart) => 
			{
				TcpListener tcp = new TcpListener(IPAddress.Any, Port);
				tcp.Start(100);

				while (!Program.ShutDown)
				{
					try
					{
						var tcpClient = tcp.AcceptTcpClient();
						Console.WriteLine(tcpClient.Client.RemoteEndPoint.ToString() + " attempting sign in!");

						var stream = tcpClient.GetStream();
						var read = GetReader(stream);
						var write = GetWriter(stream);

						string[] data = null;
						string buffer = "";
						while (!stream.DataAvailable)
							Thread.Sleep(1000);

						buffer = StringFromStream(stream);
						Console.WriteLine(buffer);

						if (Program.ShutDown)
							return;

						if (buffer.Contains("LOGIN"))
						{
							data = buffer.Split(' ');
							if (Data.BlockExists(data[NAME].ToLower()))
							{
								Block block = Data.GetBlock(data[NAME].ToLower());
								if (block.GetValue("code") != data[CODE])
								{
									continue;
								}
								else
								{
									Client client = null;
									Client.Clients.Add(client = new Client()
									{
										Name = data[NAME],
										Color = data[COLOR],
										uID = data[UID],
										client = tcpClient
									});
									client.Initialize();

									write.WriteLine("SUCCESS");
									client.NetworkHandler().Start();

									continue;
								}
							}
							else
							{
								Block block = Data.NewBlock(new string[] { "code", "ignore" }, data[NAME].ToLower());
								block.WriteValue("code", data[CODE]);
								Data.WriteToFile();

								Client client = null;
								Client.Clients.Add(client = new Client()
								{
									Name = data[NAME],
									Color = data[COLOR],
									uID = data[UID],
									client = tcpClient
								});
								client.Initialize();

								write.WriteLine("SUCCESS");
								client.NetworkHandler().Start();

								continue;
							}
						}
						else
						{
							continue;
						}
					}
					catch (Exception e)
                    {
						tcp.Stop();
						Console.WriteLine(e.Message);
						break;
                    }
				}
				Start().Start();
			});
			return thread;
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

		private StreamReader GetReader(NetworkStream stream)
        {
			return new StreamReader(stream);
        }
		private StreamWriter GetWriter(NetworkStream stream)
        {
			return new StreamWriter(stream) { AutoFlush = true };
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
