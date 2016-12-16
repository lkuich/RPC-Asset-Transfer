using System.Collections.Generic;
using System.IO;

namespace Mb
{
    public class FileManager
    {
        public Dictionary<string, byte[]> Assets { get; private set; }

        public FileManager(string workingDir = null)
        {
            Assets = new Dictionary<string, byte[]>();
        }

        // Load all assets into memory as a bundle
        public BundleResponse LoadAssets(int bundleId, string dir)
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
                        File.ReadAllBytes(filename)
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
        public void WriteAsset(string workingDir, int bundleId, string assetId, byte[] content)
        {
            workingDir.TrimEnd('\\'); workingDir += "\\" + bundleId;
            
            if (!Directory.Exists(workingDir))
                Directory.CreateDirectory(workingDir);

            File.WriteAllBytes(workingDir + "\\" + assetId, content);
        }
    }
}
