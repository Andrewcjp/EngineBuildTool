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
        public string Path = "";
        public string Name = "";
    }
    class Library
    {
        public Library()
        {

        }
        public List<LibSearchPath> LibSearchPaths = new List<LibSearchPath>();
        public void PopulateLibs()
        {
            foreach (LibSearchPath path in LibSearchPaths)
            {
                List<string> files = FileUtils.GetFilePaths(ModuleDefManager.GetLibPath() + path.Path, "*.lib", true, SearchOption.TopDirectoryOnly);
                AddModulePaths(files, path.LibBuildConfig);
            }
        }
        List<LibRef> FoundLibs = new List<LibRef>();
        public bool GetLib(string name, out LibRef Output)
        {
            foreach (LibRef r in FoundLibs)
            {
                if (r.Name == name)
                {
                    Output = r;
                    return true;
                }
            }
            Output = null;
            return false;
        }

        static string BCToString(LibBuildConfig config)
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

        void AddModulePaths(List<string> paths, LibBuildConfig buildconfig)
        {
            foreach (string s in paths)
            {
                LibRef Newref = new LibRef();
                Newref.BuildType = BCToString(buildconfig);
                Newref.Path = CmakeGenerator.SanitizePath(s);
                Newref.Name = Path.GetFileNameWithoutExtension(s);
                FoundLibs.Add(Newref);
            }
        }
        public void AddLibsForModule(ModuleDef m, bool All = false)
        {
            if (All)
            {
                m.ModuleLibs.AddRange(FoundLibs);
            }
            else
            {
                foreach (string LibName in m.LibNames)
                {
                    LibRef outputlib = null;
                    if (GetLib(LibName, out outputlib))
                    {
                        m.ModuleLibs.Add(outputlib);
                    }
                }
            }
        }
    }
}
