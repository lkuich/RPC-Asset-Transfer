using System;
using Grpc.Core;

using Mb;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AssetTransferClient
{
    class Program
    {
        private const string WORKING_DIR = @"D:\Desktop\mission_output";
        private const int PORT = 50051;

        private static FileManager fileManager;

        private static Bundle.BundleClient bundleClient;
        private static Asset.AssetClient assetClient;

        public static void Main(string[] args)
        {
            fileManager = new FileManager();
            Channel channel = new Channel("127.0.0.1:" + PORT, ChannelCredentials.Insecure);

            bundleClient = new Bundle.BundleClient(channel);
            assetClient = new Asset.AssetClient(channel);

            Console.Write("Enter a bundle ID: ");
            while (channel.State != ChannelState.Shutdown)
            {
                int bundleId;
                if (int.TryParse(Console.ReadLine(), out bundleId))
                    Recieve(assetClient, GetAssets(bundleId), bundleId);
                else
                {
                    Console.WriteLine("Couldn't parse input.\nPress any key to stop the server...");
                    Console.ReadLine();
                    break;
                }
            }
            
            channel.ShutdownAsync().Wait();
        }

        private static IEnumerable<AssetRequest> GetAssets(int bundleId)
        {
            foreach (string assetId in bundleClient.GetBundle(new BundleRequest { Id = bundleId }).AssetId)
            {
                yield return new AssetRequest { Id = assetId };
            }
        }
        
        private static async Task Recieve(Asset.AssetClient assetClient, IEnumerable<AssetRequest> requests, int bundleId)
        {
            // Now that we have the asset ID's we're after, send each asset asynchronously
            using (var call = assetClient.GetAssets())
            {
                var responseReaderTask = Task.Run(async () =>
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        var response = call.ResponseStream.Current;
                        string assetId = response.AssetId;

                        Console.WriteLine("Received " + assetId);
                        fileManager.WriteAsset(WORKING_DIR, bundleId, assetId, response.Content.ToByteArray());
                    }
                });

                foreach (AssetRequest request in requests)
                {
                    await call.RequestStream.WriteAsync(request);
                }
                await call.RequestStream.CompleteAsync();
                await responseReaderTask;
            }
        }
    }
}
