using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mb
{
    public static class FileManager
    {
        public static Dictionary<string, byte[]> Assets = new Dictionary<string, byte[]>();

        // Load all assets into memory as a bundle
        public static BundleResponse LoadAssets(int bundleId, string dir)
        {
            if (Directory.Exists(dir))
            {
                var response = new BundleResponse { Id = bundleId };
                Parallel.ForEach(Directory.GetFiles(dir), (filename) =>
                {
                    string id = filename.Replace(dir, "").Replace("\\", "");
                    response.AssetId.Add(id);
                    Assets.Add(
                        id,
                        File.ReadAllBytes(filename)
                    );
                });
                
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
