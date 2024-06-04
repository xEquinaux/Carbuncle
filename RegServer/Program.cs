using System;

namespace RegServer
{
    class Program
    {
        static void Main(string[] args)
        {
            new ReceiveData().NetworkHandler().Start();
            new SendData().NetworkHandler().Start();

            Console.WriteLine("Carbuncle IP Exchange Server\n© 2020 Circle Prefect");
            Console.Write(":");
            while (Console.ReadLine() != "exit")
                Console.Write(":");

            Global.Quit = true;
        }
    }
}
