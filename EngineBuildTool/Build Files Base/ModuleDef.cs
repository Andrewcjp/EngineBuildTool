using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    public struct LibNameRef
    {
        public string LibName;
        public LibBuildConfig Config;
        public bool IsDLL;
        public LibNameRef(string name, LibBuildConfig Conf = LibBuildConfig.General, bool IsaDLL = false)
        {
            LibName = name;
            Config = Conf;
            IsDLL = IsaDLL;
        }
    }
    public class ModuleDef
    {
        public string ModuleName = "";
        public enum ModuleType { EXE, ModuleDLL, DLL, LIB };
        public enum ProjectType { CPP,ManagedCPP,CSharp };        
        public ProjectType LaunguageType = ProjectType.CPP;
        public ModuleType ModuleOutputType = ModuleType.ModuleDLL;
        public List<string> ModuleDepends = new List<string>();
        public string SolutionFolderPath = "";
        public string PCH = "";
        public string SourceFileSearchDir = "";
        public List<LibSearchPath> AdditonalLibSearchPaths = new List<LibSearchPath>();
        public List<LibNameRef> LibNameRefs = new List<LibNameRef>();
        public List<string> IncludeDirectories = new List<string>();
        public bool UseConsoleSubSystem = false;
        //Generated
        public List<string> ModuleSourceFiles = new List<string>();
        public List<string> ModuleExtraFiles = new List<string>();
        public List<LibRef> ModuleLibs = new List<LibRef>();
        public List<string> DelayedLoadDlls = new List<string>();
        public List<string> PreProcessorDefines = new List<string>();
        public List<string> StaticModuleDepends = new List<string>();
        public bool UseCorePCH = true;
        public bool NeedsCore = true;
        public bool UseUnity = false;
        public bool Processed { get; internal set; }
        public List<string> UnityBuildExcludedFolders = new List<string>();
        public string OutputObjectName = "";
        public bool IsGameModule = false;
        public bool IsCoreModule = false;
        public List<string> ThirdPartyModules = new List<string>();
        public List<ExternalModuleDef> ExternalModules = new List<ExternalModuleDef>();
        public List<LibNameRef> DLLs = new List<LibNameRef>();
        public List<string> SystemLibNames = new List<string>();
        public List<string> NuGetPackages = new List<string>();
        public List<string> NetReferences = new List<string>();
        public List<string> UnsupportedPlatforms = new List<string>();
        public bool IsOutputEXE = false;
        public ModuleDef(TargetRules Rules)
        { }
        public string GameModuleName = "TestGame";
        public void PostInit(TargetRules r)
        {
            PreProcessorDefines.Add(ModuleName.ToUpper() + "_EXPORT");
            if (ModuleOutputType == ModuleType.LIB)
            {
                PreProcessorDefines.Add("STATIC_MODULE");
            }
            foreach (string t in r.GlobalDefines)
            {
                PreProcessorDefines.Add(t);
            }
            string path = "//Intermediate//Generated//" + ModuleName + "//";
            IncludeDirectories.Add(path);
            IncludeDirectories.Add("//Source//" + ModuleName + "//");
            IncludeDirectories.Add("//Source//" + SourceFileSearchDir + "//");
            foreach (ExternalModuleDef EMD in ExternalModules)
            {
                EMD.Build();

                IncludeDirectories.Add(ModuleDefManager.GetThirdPartyDirRelative() + EMD.IncludeDir);
                DLLs.AddRange(EMD.DynamaicLibs);
                LibNameRefs.AddRange(EMD.StaticLibs);
                AdditonalLibSearchPaths.AddRange(EMD.LibrarySearchPaths);
                SystemLibNames.AddRange(EMD.SystemLibNames);
            }
            foreach (string s in SystemLibNames)
            {
                LibRef L = new LibRef();
                L.Path = s;
                L.BuildCFg = LibBuildConfig.General;
                L.BuildType = Library.BCToString(LibBuildConfig.General);
                ModuleLibs.Add(L);
            }
        }

        public void GetIncludeDirs(ref List<string> List)
        {
            List.AddRange(IncludeDirectories);
            for (int i = 0; i < List.Count; i++)
            {
                List[i] = StringUtils.SanitizePath(List[i]);
            }
        }
        bool IsBuildfile(string s)
        {
            return s.Contains("Build.cs");
        }
        public void GatherSourceFiles()
        {
            if (ModuleSourceFiles.Count != 0)
            {
                return;
            }
            if (LaunguageType == ProjectType.CSharp)
            {
                GetFiles("*.cs");
                ModuleSourceFiles.RemoveAll(IsBuildfile);
            }
            else
            {
                GetFiles("*.h");
                GetFiles("*.hpp");
                GetFiles("*.c");
                GetFiles("*.cpp");
                GetFiles("*.rc");
                GetFiles("*.cs", ModuleDefManager.GetSourcePath() + "\\" + SourceFileSearchDir, true);
                if (IsCoreModule)
                {
                    GetFiles("*.*", ModuleDefManager.GetRootPath() + "\\Shaders", false);
                    ModuleExtraFiles.Add(StringUtils.SanitizePath(ModuleDefManager.GetSourcePath() + "\\Core.Target.cs"));
                }
            }
        }
        void GetFiles(string Type)
        {
            string path = ModuleDefManager.GetSourcePath() + "\\" + SourceFileSearchDir;
            GetFiles(Type, path, true);
        }
        void GetFiles(string Type, string path, bool Source)
        {
            try
            {
                string[] files = Directory.GetFiles(path, Type, SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    files[i] = files[i].Replace(ModuleDefManager.GetSourcePath() + "\\", "");
                    files[i] = StringUtils.SanitizePath(files[i]);
                }
                if (Source)
                {
                    ModuleSourceFiles.AddRange(files);
                }
                else
                {
                    ModuleExtraFiles.AddRange(files);
                }
            }
            catch
            {
            }
        }

        public void GatherIncludes()
        {
            for (int i = 0; i < IncludeDirectories.Count; i++)
            {
                IncludeDirectories[i] = StringUtils.SanitizePath(ModuleDefManager.GetRootPath() + IncludeDirectories[i]);
            }
        }

    }
}
