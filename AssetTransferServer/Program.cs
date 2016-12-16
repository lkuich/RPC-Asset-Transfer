using System;
using System.Threading.Tasks;
using Grpc.Core;

using System.Linq;
using Mb;
using System.Collections.Generic;

namespace AssetTransferServer
{
    // Runs server
    class Program
    {
        private const int PORT = 50051;

        public static void Main(string[] args)
        {
            var fileManager = new FileManager();

            // Load directory into a bundle, using ints for this example because no one wants to type GUID's
            var bundles = new List<BundleResponse>()
            {
                fileManager.LoadAssets(1, @"D:\Desktop\mission1"),
                fileManager.LoadAssets(2, @"D:\Desktop\mission2")
            };
            if (bundles.Contains(null))
            {
                Console.WriteLine("One or more asset directories do not exist. Exiting..");
                Console.ReadLine();
                return;
            }
            
            Server server = new Server
            {
                Services = { Bundle.BindService(new BundleImpl(bundles)), Asset.BindService(new AssetImpl(fileManager)) },
                Ports = { new ServerPort("localhost", PORT, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("Server listening on port " + PORT);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }

    // Simple RPC, server replys to a client request
    class BundleImpl : Bundle.BundleBase
    {
        public IEnumerable<BundleResponse> Bundles { get; private set; }
        public BundleImpl(IEnumerable<BundleResponse> bundles)
        {
            Bundles = bundles;           
        }

        public override Task<BundleResponse> GetBundle(BundleRequest request, ServerCallContext context)
        {
            // Get bundle by ID
            var reply = Bundles.Single(b => b.Id == request.Id);
            
            return Task.FromResult(reply);
        }
    }

    // Bi-directional stream
    class AssetImpl : Asset.AssetBase
    {
        private List<AssetRequest> prevRequests;
        private Dictionary<string, byte[]> assets;

        public AssetImpl(FileManager fileManager)
        {
            prevRequests = new List<AssetRequest>();
            assets = fileManager.Assets;
        }
        
        public override async Task GetAssets(IAsyncStreamReader<AssetRequest> requestStream, IServerStreamWriter<AssetResponse> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                var currentRequest = requestStream.Current;
                prevRequests.Add(currentRequest);

                var response = new AssetResponse() {
                    AssetId = currentRequest.Id,
                    Content = Google.Protobuf.ByteString.CopyFrom(assets[currentRequest.Id])
                };
                
                await responseStream.WriteAsync(response);
            }
        }
    }
}
