using System;
using System.Threading.Tasks;
using Grpc.Core;

using System.Linq;
using Mb;
using System.Collections.Generic;
using System.IO;

namespace AssetTransfer
{
    public class AssetTransferServer
    {
        public string Host { get; private set; }
        public int Port { get; private set; }

        private List<BundleResponse> bundles;

        public AssetTransferServer(string host = "localhost", int port = 50051)
        {
            this.Host = host;
            this.Port = port;

            // Load directory into a bundle, using ints for this example because no one wants to type GUID's
            bundles = new List<BundleResponse>();
        }

        public void Start()
        {
            if (bundles.Contains(null))
            {
                Console.WriteLine("One or more asset directories do not exist. Exiting..");
                Console.ReadLine();
                return;
            }

            Server server = new Server
            {
                Services = { Bundle.BindService(new BundleImpl(bundles)), Asset.BindService(new AssetImpl()) },
                Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("Server listening on port " + Port);

            while (true)
            {
                Console.Write("Enter a bundle (ID,path): ");

                string bundleToLoad = Console.ReadLine();
                if (bundleToLoad.ToLower() == "exit")
                    break;
                else
                    LoadBundle(bundleToLoad);
            }

            server.ShutdownAsync().Wait();
        }

        private void LoadBundle(string input)
        {
            var sp = input.Split(',');

            int id;
            if (!int.TryParse(sp[0], out id))
            {
                Console.WriteLine("Could not load bundle, invalid ID");
                return;
            }

            string path = sp[1];
            if (!Directory.Exists(path))
            {
                Console.WriteLine("Could not load bundle, directory does not exist");
                return;
            }

            bundles.Add(FileManager.LoadAssets(id, path));
            Console.WriteLine("Serving bundle with id of " + id);
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
            BundleResponse reply = new BundleResponse { Id = -1 };
            try
            {
                reply = Bundles.Single(b => b.Id == request.Id);
            } catch (Exception)
            {

            }
            
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

                var response = new AssetResponse() {
                    AssetId = currentRequest.Id,
                    Content = Google.Protobuf.ByteString.CopyFrom(FileManager.Assets[currentRequest.Id])
                };
                
                await responseStream.WriteAsync(response);
            }
        }
    }
}
