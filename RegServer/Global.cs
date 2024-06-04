using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Net;
using System.Net.Sockets;

namespace RegServer
{
    public class Global
    {
		public static IList<string> Addr = new List<string>();
		public static bool Quit = false;
		public static string StringFromStream(NetworkStream stream)
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
