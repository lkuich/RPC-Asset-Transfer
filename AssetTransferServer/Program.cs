using System;
using System.Threading.Tasks;
using Grpc.Core;

using System.Linq;
using Mb;
using System.Collections.Generic;


namespace AssetTransferServer
{
    class TestData
    {
        public List<BundleResponse> ExampleBundles;
        public TestData() {
            ExampleBundles = new List<BundleResponse>()
            {
                new BundleResponse { Id = 1, AssetId = { 1, 2, 3 } },
                new BundleResponse { Id = 2, AssetId = { 10, 20, 30 } },
                new BundleResponse { Id = 3, AssetId = { 11, 12, 13 } }
            };
        }
    }

    // Runs server
    class Program
    {
        const int Port = 50051;

        public static void Main(string[] args)
        {
            Server server = new Server
            {
                Services = { Bundle.BindService(new BundleImpl()), Asset.BindService(new AssetImpl()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("Greeter server listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }

    // Simple RPC, server replys to a client request
    class BundleImpl : Bundle.BundleBase
    {
        public override Task<BundleResponse> GetBundle(BundleRequest request, ServerCallContext context)
        {
            // Get bundle by ID
            var testData = new TestData();
            var reply = testData.ExampleBundles.Single(b => b.Id == request.Id);
            
            return Task.FromResult(reply);
        }
    }

    // Bi-directional stream
    class AssetImpl : Asset.AssetBase
    {
        private List<AssetRequest> prevRequests;
        public AssetImpl()
        {
            prevRequests = new List<AssetRequest>();
        }
        
        public override async Task GetAssets(IAsyncStreamReader<AssetRequest> requestStream, IServerStreamWriter<AssetResponse> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                var currentRequest = requestStream.Current;
                prevRequests.Add(currentRequest);

                var response = new AssetResponse() { AssetId = currentRequest.Id, Content = "test" };
                await responseStream.WriteAsync(response);
            }
        }
    }
}
