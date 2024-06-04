using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using System.Net.Sockets;

namespace NameServer
{
    public class Global
    {
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
