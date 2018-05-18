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
            StartServer();

            Console.ReadLine();
        }

        private static void StartServer()
        {
            var server = new AssetTransferServer("exit");
            server.Start();

            string bundleToLoad = @"1,C:\Users\Loren Kuich\Desktop\assets";
            server.LoadBundle(bundleToLoad);

            Console.WriteLine("Goodbye!");
        }

        private static void StartClient()
        {
            var client = new AssetTransferClient(@"C:\Users\Public\TestOutput");
            client.Start();
        }
    }
}
