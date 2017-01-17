using System;
using Grpc.Core;

using Mb;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Diagnostics;

namespace AssetTransfer
{
    public class AssetTransferClient
    {
        public string Host { get; private set; }
        public int Port { get; private set; }
        public string WorkingDir { get; set; }

        private Bundle.BundleClient bundleClient;
        private Asset.AssetClient assetClient;

        private Channel channel;

        public AssetTransferClient(string workingDir, string host = "localhost", int port = 50051)
        {
            this.WorkingDir = workingDir;
            this.Host = host;
            this.Port = port;
        }

        public void Start()
        {
            this.channel = new Channel(this.Host + ":" + this.Port, ChannelCredentials.Insecure);

            this.bundleClient = new Bundle.BundleClient(channel);
            this.assetClient = new Asset.AssetClient(channel);

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
                    break;
                }
            }

            channel.ShutdownAsync().Wait();
        }

        public void RequestBundle(int id)
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
            }
        }

        /// <summary>
        /// Closes the connection
        /// </summary>
        public void Close()
        {
            channel.ShutdownAsync().Wait();
        }
        
        private IEnumerable<AssetRequest> GetAssets(int bundleId)
        {
            if (bundleId == -1)
                yield return null;

            foreach (string assetId in bundleClient.GetBundle(new BundleRequest { Id = bundleId }).AssetId)
            {
                yield return new AssetRequest { Id = assetId };
            }
        }

        private async Task Recieve(Asset.AssetClient assetClient, IEnumerable<AssetRequest> requests, int bundleId)
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
                        
                        FileManager.WriteAsset(WorkingDir, bundleId, assetId, response.Content.ToByteArray());

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