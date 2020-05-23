using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Setup.Configuration;
using System.Runtime.InteropServices;

namespace EngineBuildTool
{
    class ProcessUtils
    {
        public static int RunProcess(string File, string CmakeArgs)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = File;
            startInfo.Arguments = CmakeArgs;
            startInfo.WorkingDirectory = ModuleDefManager.GetIntermediateDir();
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            process.StartInfo = startInfo;
            process.Start();
            while (!process.HasExited)
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.Write(result);
                }
            }
            process.WaitForExit();
            return process.ExitCode;
        }
    }
    class FileUtils
    {
        public static List<string> GetFilePaths(string dir, string type, bool AsFullPath = false, SearchOption opt = SearchOption.AllDirectories)
        {
            if (!Directory.Exists(dir))
            {
                return new List<string>();
            }
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
                files[i] = StringUtils.SanitizePath(files[i]);
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
                    if (Newfile.Exists)
                    {
                        if (Newfile.LastAccessTime >= SrcFile.LastAccessTime)
                        {
                            FileCopyCount++;
                            continue;
                        }
                    }
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
        public static bool FindVSVersion()
        {
            try
            {
                SetupConfiguration Setup = new SetupConfiguration();
                IEnumSetupInstances Enumerator = Setup.EnumAllInstances();

                ISetupInstance[] Instances = new ISetupInstance[1];
                for (; ; )
                {
                    int NumFetched;
                    Enumerator.Next(1, Instances, out NumFetched);

                    if (NumFetched == 0)
                    {
                        break;
                    }

                    ISetupInstance2 Instance = (ISetupInstance2)Instances[0];
                    if ((Instance.GetState() & InstanceState.Local) == InstanceState.Local)
                    {
                        string VersionString = Instance.GetDisplayName();
                        if (VersionString.Contains("19"))
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
            }
            return true;
        }
        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(
        string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        enum SymbolicLink
        {
            File = 0x0,
            Directory = 0x1,
            dev = 0x1 | 0x2
        }
        public static void CreateSymbolicLink(string LinkPath, string TargetPath, bool SilentExistError = false)
        {
            CreateSymbolicLink(TargetPath, LinkPath, SymbolicLink.dev);
            int returncode = Marshal.GetLastWin32Error();
            if (returncode == 0)
            {
                returncode = Marshal.GetLastWin32Error();
            }
            if (returncode == 1314)
            {
                Console.WriteLine("Error: Failed to create SymLink with error ERROR_PRIVILEGE_NOT_HELD");
            }
            else if (returncode == 183 || returncode == 18)//18 == ERROR_NO_MORE_FILES
            {
                if (!SilentExistError)
                {
                    Console.WriteLine("Error: Failed to create SymLink with error ERROR_ALREADY_EXISTS");
                }
            }
            else if (returncode != 0)
            {
                Console.WriteLine("Error: Failed to create SymLink with error code:" + returncode);
            }
        }
        public static void DeleteDirectory(string name)
        {
            try
            {
                if (Directory.Exists(name))
                {
                    Directory.Delete(name, true);
                }
            }
            catch
            {
                Console.WriteLine("Failed to Delete " + name);
            }
        }
        public static void DeleteFile(string name)
        {
            try
            {
                if (System.IO.File.Exists(name))
                {
                    System.IO.File.Delete(name);
                }
            }
            catch
            {
                Console.WriteLine("Failed to Delete " + name);
            }
        }
    }

}
