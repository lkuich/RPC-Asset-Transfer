using System;
using Grpc.Core;

using Mb;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AssetTransferClient
{
    class Program
    {
        private static Bundle.BundleClient bundleClient;
        private static Asset.AssetClient assetClient;
        private const int Port = 50051;

        public static void Main(string[] args)
        {
            Channel channel = new Channel("127.0.0.1:" + Port, ChannelCredentials.Insecure);

            bundleClient = new Bundle.BundleClient(channel);
            assetClient = new Asset.AssetClient(channel);
            
            while (channel.State != ChannelState.Shutdown)
            {
                Console.WriteLine("Enter a bundle ID: ");
                var bundleId = int.Parse(Console.ReadLine());

                Recieve(assetClient, GetAssets(bundleId));
            }
            
            channel.ShutdownAsync().Wait();
        }

        private static IEnumerable<AssetRequest> GetAssets(int bundleId)
        {
            foreach (int assetId in bundleClient.GetBundle(new BundleRequest { Id = bundleId }).AssetId)
            {
                yield return new AssetRequest { Id = assetId };
            }
        }
        
        private static async Task Recieve(Asset.AssetClient assetClient, IEnumerable<AssetRequest> requests)
        {
            // Now that we have the asset ID's we're after, send each asset asynchronously
            using (var call = assetClient.GetAssets())
            {
                var responseReaderTask = Task.Run(async () =>
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        var response = call.ResponseStream.Current;
                        Console.WriteLine("Received " + response.AssetId);
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
