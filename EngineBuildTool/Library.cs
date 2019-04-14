using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    public class LibRef
    {
        public string BuildType = "";
        public LibBuildConfig BuildCFg = LibBuildConfig.General;
        public string Path = "";
        public string Name = "";
    }
    class Library
    {
        public Library()
        {

        }
        public List<LibSearchPath> LibSearchPaths = new List<LibSearchPath>();
        public List<LibSearchPath> DynmaicLibSearchPaths = new List<LibSearchPath>();
        public void PopulateLibs()
        {
            foreach (LibSearchPath path in LibSearchPaths)
            {
                List<string> files = path.GetFiles();
                if (path.IsLibaryDll)
                {
                    AddModulePaths(files, path.LibBuildConfig, true);
                }
                else
                {
                    AddModulePaths(files, path.LibBuildConfig, false);
                }
            }
        }
        List<LibRef> FoundLibs = new List<LibRef>();
        List<LibRef> FoundDLLs = new List<LibRef>();
        List<string> BinaryDirectories = new List<string>();
        public static bool IsValidConfig(LibBuildConfig Current, LibBuildConfig other)
        {
            if (Current == LibBuildConfig.General || other == LibBuildConfig.General)
            {
                return true;
            }
            return Current == other;
        }
        public bool GetLib(string name, out LibRef Output, LibBuildConfig CFG, bool IsDLL = false)
        {
            name = Path.GetFileNameWithoutExtension(name);
            Output = null;
            if (IsDLL)
            {
                foreach (LibRef r in FoundDLLs)
                {
                    if (r.Name == name)
                    {
                        Output = r;
                        return true;
                    }
                }
            }
            else
            {
                foreach (LibRef r in FoundLibs)
                {
                    if (r.Name == name && r.BuildCFg == CFG)
                    {
                        Output = r;
                        return true;
                    }
                }
            }
            return false;
        }
        public void CopyDllsToConfig(List<BuildConfig> configs, List<ModuleDef> ALLModules)
        {
            foreach (BuildConfig bc in configs)
            {
                Console.WriteLine("Copying Files for: '" + bc.Name + "' Of type " + bc.CurrentType.ToString());
                List<string> DLLsForConfig = new List<string>();
                foreach (ModuleDef M in ALLModules)
                {
                    foreach (LibNameRef LNR in M.DLLs)
                    {
                        LibRef DLLref = null;
                        GetLib(LNR.LibName, out DLLref, LibBuildConfig.General, true);
                        if (DLLref == null)
                        {
                            Console.WriteLine("Error Failed to find DLL " + LNR.LibName);
                            continue;
                        }
                        if (!IsValidConfig(DLLref.BuildCFg, bc.GetLibType()))
                        {
                            continue;
                        }
                        string filepath = ModuleDefManager.GetBinPath() + "\\" + bc.Name + "\\" + Path.GetFileName(LNR.LibName);
                        Directory.CreateDirectory(Path.GetDirectoryName(filepath));
                        File.Copy(DLLref.Path, filepath, true);
                        if (ModuleDefManager.IsDebug())
                        {
                            Console.WriteLine("Copied " + Path.GetFileName(LNR.LibName) + " to output dir");
                        }
                    }
                }
            }

        }
        public static string BCToString(LibBuildConfig config)
        {
            switch (config)
            {
                case LibBuildConfig.Debug:
                    return "debug";
                case LibBuildConfig.Optimized:
                    return "optimized";
                case LibBuildConfig.General:
                    return "general";
            }
            return "-1";
        }

        void AddModulePaths(List<string> paths, LibBuildConfig buildconfig, bool DLL)
        {
            foreach (string s in paths)
            {
                LibRef Newref = new LibRef();
                Newref.BuildType = BCToString(buildconfig);
                Newref.Path = CmakeGenerator.SanitizePath(s);
                Newref.Name = Path.GetFileNameWithoutExtension(s);
                Newref.BuildCFg = buildconfig;
                if (DLL)
                {
                    FoundDLLs.Add(Newref);
                }
                else
                {
                    FoundLibs.Add(Newref);
                }
            }
        }
        public void AddLibsForModule(ModuleDef m, bool All = false)
        {
            if (m.LibNameRefs.Count > 0)
            {
                foreach (LibNameRef LibName in m.LibNameRefs)
                {
                    LibRef outputlib = null;
                    string CleanLibName = Path.GetFileNameWithoutExtension(LibName.LibName);
                    if (GetLib(CleanLibName, out outputlib, LibName.Config))
                    {
                        m.ModuleLibs.Add(outputlib);
                    }
                    else
                    {
                        Console.WriteLine("Error: failed to find lib " + LibName.LibName);
                    }
                }
            }
        }
    }
}
