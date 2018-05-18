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

            Client = new AssetTransferClient();
        }

        public void NavigateToStream(string src)
        {
            Browser.Dispatcher.Invoke(() =>
            {
                Browser.NavigateToString(src);
            });
        }

        private void Go_Click(object sender, RoutedEventArgs e)
        {
            var url = IdSearch.Text.Split('/');

            int id = 0;
            int.TryParse(url[0], out id);

            string item = url[1];

            if (id != 0)
            {
                Client.RequestBundle(id, (name, src) =>
                {
                    if (name == item)
                        NavigateToStream(src);
                });
            }
        }
    }


    public class AssetTransferClient
    {
        public string Host { get; private set; }
        public int Port { get; private set; }

        private Bundle.BundleClient bundleClient;
        private Asset.AssetClient assetClient;

        private Channel channel;

        public AssetTransferClient(string host = "localhost", int port = 50051)
        {
            this.Host = host;
            this.Port = port;

            this.channel = new Channel(this.Host + ":" + this.Port, ChannelCredentials.Insecure);
            this.bundleClient = new Bundle.BundleClient(channel);
            this.assetClient = new Asset.AssetClient(channel);
        }
        
        public void RequestBundle(int bundleId, Action<string, string> OnRecieved)
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

        private async Task Recieve(Asset.AssetClient assetClient, IEnumerable<AssetRequest> requests, int bundleId, Action<string, string> OnRecieved)
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
                        
                        OnRecieved(assetId, response.Content);
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
