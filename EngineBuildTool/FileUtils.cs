using IWshRuntimeLibrary;
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
        public static void CopyAllFromPath(string SrcDir, string type, string DestDir)
        {
            int FileCopyCount = 0;
            try
            {
                List<string> files = new List<string>(Directory.GetFiles(SrcDir, type, SearchOption.AllDirectories));
                for (int i = files.Count - 1; i >= 0; i--)
                {
                    string DestPath = files[i].Replace(SrcDir, DestDir);
                    FileInfo SrcFile = new FileInfo(SrcDir);
                    FileInfo Newfile = new FileInfo(DestPath);
                    Directory.CreateDirectory(Newfile.DirectoryName);
                    try
                    {
                        System.IO.File.Copy(files[i], DestPath, true);
                        FileCopyCount++;
                    }
                    catch { }
                }
            }
            catch
            {
                Console.WriteLine("Failed to find Folder: " + SrcDir);
            }
            Console.WriteLine("Copied " + FileCopyCount + " files");
        }
        public static void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation)
        {
            string shortcutLocation = Path.Combine(shortcutPath, shortcutName + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);
            shortcut.TargetPath = targetFileLocation;                 // The path of the file that will launch when the shortcut is run
            shortcut.Save();                                          // Save the shortcut
        }
    }
}
