using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssetTransfer;

namespace App
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Press 1 to start server, 2 for client");
            int selection = Int32.Parse(Console.ReadLine());

            if (selection == 1)
            {
                StartServer();
            } else if (selection == 2)
            {
                StartClient();
            }

            Console.ReadLine();
        }

        private static void StartServer()
        {
            var server = new AssetTransferServer("exit");
            server.Start();
            
            while (server.IsRunning())
            {
                Console.Write("Enter a bundle (ID,path): ");

                string bundleToLoad = Console.ReadLine();
                server.LoadBundle(bundleToLoad);
            }

            Console.WriteLine("Goodbye!");
        }

        private static void StartClient()
        {
            var client = new AssetTransferClient(@"D:\Desktop\mission_output");
            client.Start();
        }
    }
}
