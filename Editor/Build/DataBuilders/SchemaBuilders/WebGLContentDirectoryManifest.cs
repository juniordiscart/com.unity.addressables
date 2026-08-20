#if ENABLE_CONTENT_DIRECTORIES
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.AddressableAssets;

namespace UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders
{
    public partial class ContentDirectorySchemaBuilder
    {
        // Writes a WebGL preload manifest listing Addressables content directory artifacts
        // that need to be baked into the emscripten VFS for synchronous file access.
        internal static class WebGLContentDirectoryManifest
        {
            const string kManifestDir = "Library/PlayerDataCache/WebGLPreloadedStreamingAssets";
            const string kManifestFile = kManifestDir + "/addressables.manifest";

            internal static void WriteManifest(IEnumerable<string> contentDirectoryFilePaths)
            {
                string aaBuildPathFull = Path.GetFullPath(Addressables.BuildPath).Replace('\\', '/');

                // Stream entries straight to disk so we don't keep every relative path in memory at once.
                Directory.CreateDirectory(kManifestDir);
                using (StreamWriter writer = new StreamWriter(kManifestFile, false, new UTF8Encoding(false)))
                {
                    foreach (var file in contentDirectoryFilePaths)
                    {
                        string fileFull = Path.GetFullPath(file).Replace('\\', '/');
                        if (!fileFull.StartsWith(aaBuildPathFull + "/"))
                            continue;

                        string relative = fileFull.Substring(aaBuildPathFull.Length + 1);
                        writer.WriteLine("aa/" + relative);
                    }
                }
            }

            internal static void ClearManifest()
            {
                if (File.Exists(kManifestFile))
                    File.Delete(kManifestFile);
            }
        }
    }
}
#endif
