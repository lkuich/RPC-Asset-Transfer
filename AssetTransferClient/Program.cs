using System;
using Grpc.Core;

using Mb;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Diagnostics;

namespace AssetTransferClient
{
    class Program
    {
        private const string HOST = "localhost";
        private const int PORT = 50051;
        private const string WORKING_DIR = @"D:\Desktop\mission_output";
        
        private static Bundle.BundleClient bundleClient;
        private static Asset.AssetClient assetClient;

        public static void Main(string[] args)
        {
            Channel channel = new Channel(HOST + ":" + PORT, ChannelCredentials.Insecure);

            bundleClient = new Bundle.BundleClient(channel);
            assetClient = new Asset.AssetClient(channel);

            Console.Write("Enter a bundle ID: ");
            while (channel.State != ChannelState.Shutdown)
            {
                int bundleId;
                bool validId = int.TryParse(Console.ReadLine(), out bundleId);
                var assets = GetAssets(bundleId);

                if (validId && !System.Linq.Enumerable.Contains(assets, null))
                {
                    Recieve(assetClient, assets, bundleId);
                }
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
            if (bundleId == -1)
                yield return null;

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
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();

                        var response = call.ResponseStream.Current;
                        string assetId = response.AssetId;
                        
                        FileManager.WriteAsset(WORKING_DIR, bundleId, assetId, response.Content.ToByteArray());

                        stopwatch.Stop();

                        double ticks = stopwatch.ElapsedTicks;
                        double milliseconds = (ticks / Stopwatch.Frequency) * 1000;
                        double nanoseconds = (ticks / Stopwatch.Frequency) * 1000000000;

                        Console.WriteLine(string.Format("Received and Wrote {0} in {1}ms/{2}ns", assetId, Math.Round(milliseconds, 2), Math.Round(nanoseconds, 2)));
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