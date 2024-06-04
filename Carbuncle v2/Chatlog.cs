using System;
using System.Collections.Generic;
using System.Text;

namespace Carbuncle_v2
{
    public class Chatlog
    {
        public static Chatlog Instance;
        public Chatlog()
        {
            Instance = this;
        }
        public List<string> Log = new List<string>();
        public static void NewMessage(string message)
        {
            Instance.Log.Add(message);
        }
    }
}
