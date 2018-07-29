using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    class FileUtils
    {
        public static List<string> GetFilePaths(string dir, string type, bool AsFullPath = false, SearchOption opt = SearchOption.AllDirectories)
        {
            List<string> files = new List<string>(Directory.GetFiles(dir, type, opt));
            for (int i = files.Count - 1; i >= 0; i--)
            {
                if (!AsFullPath)
                {
                    files[i] = files[i].Replace(dir + "\\", "");
                }
                if (!Path.HasExtension(files[i]))
                {
                    files.RemoveAt(i);
                }
                files[i] = CmakeGenerator.SanitizePath(files[i]);
            }
            return files;
        }
    }
}
