using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    public class ExternalModuleDef
    {
        //helpers for build config files
        protected List<string> DebugLibs = new List<string>();
        protected List<string> ReleaseLibs = new List<string>();
        protected List<string> CommonLibs = new List<string>();
        protected List<string> DLLs = new List<string>();
        protected List<string> DebugDLLs = new List<string>();
        protected List<string> ReleaseDLLs = new List<string>();
        public List<string> SystemLibNames = new List<string>();
        protected List<LibSearchPath> AddFolderOfLibs = new List<LibSearchPath>();
        protected string ModuleRoot = "";
        //public outputs 
        public List<LibSearchPath> LibrarySearchPaths = new List<LibSearchPath>();
        public List<LibNameRef> StaticLibs = new List<LibNameRef>();
        public List<LibNameRef> DynamaicLibs = new List<LibNameRef>();
        public string IncludeDir = "";
        public void Build()
        {
            if (IncludeDir.Length == 0)
            {
                IncludeDir = ModuleRoot + "include\\";
            }
            AddLibs(CommonLibs, LibBuildConfig.General);
            AddLibs(DebugLibs, LibBuildConfig.Debug);
            AddLibs(ReleaseLibs, LibBuildConfig.Optimized);

            AddLibs(DLLs, LibBuildConfig.General, true);
            AddLibs(DebugDLLs, LibBuildConfig.Debug, true);
            AddLibs(ReleaseDLLs, LibBuildConfig.Optimized, true);

            foreach (LibSearchPath p in AddFolderOfLibs)
            {
                List<string> files = p.GetFiles();
                AddLibs(files, p.LibBuildConfig, p.IsLibaryDll);
            }
        }
        void AddLibs(List<string> names, LibBuildConfig CFG, bool DLL = false)
        {
            foreach (string s in names)
            {
                if (DLL)
                {
                    DynamaicLibs.Add(new LibNameRef(s, CFG, DLL));
                }
                else
                {
                    StaticLibs.Add(new LibNameRef(s, CFG, DLL));
                }
            }
        }
        protected void AddLibSearch(ref List<LibSearchPath> target, string folder, LibBuildConfig CFG, bool IsDLL)
        {
            target.Add(new LibSearchPath(ModuleDefManager.GetThirdPartyDir() + ModuleRoot + "\\" + folder, CFG, IsDLL, true));
        }
        protected void AddStandardFolders(bool IncludeDLLs = true)
        {
            AddLibSearch(ref LibrarySearchPaths, "\\Lib\\Debug", LibBuildConfig.Debug, false);
            AddLibSearch(ref LibrarySearchPaths, "\\Lib\\Release", LibBuildConfig.Optimized, false);
            AddLibSearch(ref LibrarySearchPaths, "\\Lib\\General", LibBuildConfig.General, false);

            AddLibSearch(ref AddFolderOfLibs, "\\Lib\\Debug", LibBuildConfig.Debug, false);
            AddLibSearch(ref AddFolderOfLibs, "\\Lib\\Release", LibBuildConfig.Optimized, false);
            AddLibSearch(ref AddFolderOfLibs, "\\Lib\\General", LibBuildConfig.General, false);

            if (IncludeDLLs)
            {
                AddLibSearch(ref LibrarySearchPaths, "\\DLLs\\Debug", LibBuildConfig.Debug, true);
                AddLibSearch(ref LibrarySearchPaths, "\\DLLs\\Release", LibBuildConfig.Optimized, true);
 
                AddLibSearch(ref AddFolderOfLibs, "\\DLLs\\Debug", LibBuildConfig.Debug, true);
                AddLibSearch(ref AddFolderOfLibs, "\\DLLs\\Release", LibBuildConfig.Optimized, true);
            }
        }
    }
}
