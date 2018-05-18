using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Grpc.Core;
using Mb;

namespace AssetBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AssetTransferClient Client { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Client = new AssetTransferClient(@"C:\Users\Loren Kuich\Desktop\assets");
        }

        public void NavigateToStream(System.IO.Stream stream)
        {
            Browser.Dispatcher.Invoke(() =>
            {
                Browser.NavigateToStream(stream);
            });
        }

        private void Go_Click(object sender, RoutedEventArgs e)
        {
            int id = 0;
            int.TryParse(IdSearch.Text, out id);
            if (id != 0)
            {
                Client.RequestBundle(id, stream =>
                {
                    NavigateToStream(stream);
                });
            }
        }
    }


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

            this.channel = new Channel(this.Host + ":" + this.Port, ChannelCredentials.Insecure);
            this.bundleClient = new Bundle.BundleClient(channel);
            this.assetClient = new Asset.AssetClient(channel);
        }
        
        public void RequestBundle(int bundleId, Action<System.IO.Stream> OnRecieved)
        {
            var assets = GetAssets(bundleId);
            if (!Enumerable.Contains(assets, null))
            {
                Recieve(assetClient, assets, bundleId, OnRecieved);
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

        private async Task Recieve(Asset.AssetClient assetClient, IEnumerable<AssetRequest> requests, int bundleId, Action<System.IO.Stream> OnRecieved)
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
                        
                        OnRecieved(new System.IO.MemoryStream(response.Content.ToByteArray()));
                        // response.Content.ToByteArray());
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
