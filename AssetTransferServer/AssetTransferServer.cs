using System;
using System.Threading.Tasks;
using Grpc.Core;

using System.Linq;
using Mb;
using System.Collections.Generic;
using System.IO;

namespace AssetTransfer
{
    public static class GrpcServerHelper
    {
        private static bool ServerRunning { get; set; }

        public static void SpecialStart(this Server server)
        {
            server.Start();
            ServerRunning = true;
        }

        public async static Task SpecialStop(this Server server)
        {
            await server.ShutdownAsync();
            ServerRunning = false;
        }

        public static bool IsRunning(this Server server)
        {
            return ServerRunning;
        }
    }

    public class AssetTransferServer
    {
        public string Host { get; private set; }
        public int Port { get; private set; }

        private List<BundleResponse> bundles;

        private Server server;
        private string Killword { get; set; }

        public AssetTransferServer(string killword, string host = "localhost", int port = 50051)
        {
            this.Host = host;
            this.Port = port;
            this.Killword = killword;

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

            server = new Server
            {
                Services = { Bundle.BindService(new BundleImpl(bundles)), Asset.BindService(new AssetImpl()) },
                Ports = { new ServerPort(Host, Port, ServerCredentials.Insecure) }
            };
            server.SpecialStart();

            Console.WriteLine("Server listening on port " + Port);
        }

        public bool IsRunning()
        {
            return server.IsRunning();
        }

        public void LoadBundle(string input)
        {
            if (input == Killword)
            {
                server.SpecialStop().Wait();
                return;
            }

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
                    Content = FileManager.Assets[currentRequest.Id]
                };
                
                await responseStream.WriteAsync(response);
            }
        }
    }
}

namespace Mb
{
    public static class FileManager
    {
        public static Dictionary<string, string> Assets = new Dictionary<string, string>();

        // Load all assets into memory as a bundle
        public static BundleResponse LoadAssets(int bundleId, string dir)
        {
            if (Directory.Exists(dir))
            {
                var response = new BundleResponse { Id = bundleId };
                foreach (string filename in Directory.GetFiles(dir))
                {
                    string id = filename.Replace(dir, "").Replace("\\", "");
                    response.AssetId.Add(id);
                    Assets.Add(
                        id,
                        File.ReadAllText(filename)
                    );
                }

                return response;
            }

            return null;
        }

        /// <summary>
        /// Creates bundle dir and writes files to their bundle dir
        /// </summary>
        /// <param name="path">Working directory, this should already exist</param>
        /// <param name="bundleId">Bundle ID, directory will be created in the root</param>
        /// <param name="assetId">ID of the asset to write, will be filename</param>
        /// <param name="content">Bytes to write</param>
        public static void WriteAsset(string workingDir, int bundleId, string assetId, byte[] content)
        {
            workingDir.TrimEnd('\\'); workingDir += "\\" + bundleId;

            if (!Directory.Exists(workingDir))
                Directory.CreateDirectory(workingDir);

            File.WriteAllBytes(workingDir + "\\" + assetId, content);
        }

        public static bool ReadFile(string path, out string output)
        {
            bool exists = File.Exists(path);
            output = "";

            if (exists)
                output = File.ReadAllText(path);

            return exists;
        }
    }
}
