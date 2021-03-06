﻿using System;
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
    public class LibDependency
    {
        public string LibName;
        public List<PlatformID> Platforms = new List<PlatformID>();
        public bool NeedsDll = true;
        public LibDependency(string name, string[] inplatforms = null)
        {
            LibName = name;
            if (inplatforms != null)
            {
                foreach (string s in inplatforms)
                {
                    Platforms.Add(PlatformDefinition.ParseString(s));
                }
            }
        }

        public LibDependency(string name, string platfrom = null)
        {
            LibName = name;
            if (platfrom != null)
            {
                if (platfrom.ToLower() == "all")
                {
                    Platforms.AddRange(PlatformID.Ids);
                }
                else
                {
                    Platforms.Add(PlatformDefinition.ParseString(platfrom));
                }
            }
        }
    }
    public class FolderPlatformPair
    {
        public string FolderName;
        public List<PlatformID> Platforms = new List<PlatformID>();
        public FolderPlatformPair(string name, string platfrom = null)
        {
            FolderName = name;
            if (platfrom != null)
            {
                if (platfrom.ToLower() == "all")
                {
                    Platforms.AddRange(PlatformID.Ids);
                }
                else
                {
                    PlatformDefinition.TryAddPlatfromsFromString(platfrom, ref Platforms);
                }
            }
        }      
    }
    public class ModuleDef
    {
        public string ModuleName = "";
        public enum ModuleType { EXE, ModuleDLL, DLL, LIB };
        public enum ProjectType { CPP, ManagedCPP, CSharp };
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
        public List<string> ExcludeConfigs = new List<string>();
        public List<string> ExcludedFolders = new List<string>();
        public List<FolderPlatformPair> ExcludedFoldersNew = new List<FolderPlatformPair>();
        public List<LibDependency> StaticLibraries = new List<LibDependency>();
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
            foreach (LibDependency l in StaticLibraries)
            {
                if (l.NeedsDll)
                {
                    LibNameRef lref;
                    lref.Config = LibBuildConfig.General;
                    lref.IsDLL = true;
                    lref.LibName = l.LibName.Replace(".lib",".dll");
                    DLLs.Add(lref);
                }
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
                    GetFiles("*.hlsl", ModuleDefManager.GetRootPath() + "\\Shaders", false);
                    GetFiles("*.h", ModuleDefManager.GetRootPath() + "\\Shaders", false);
                    ModuleExtraFiles.Add(StringUtils.SanitizePath(ModuleDefManager.GetSourcePath() + "\\Core.Target.cs"));
                }
            }
            for (int i = 0; i < ModuleSourceFiles.Count; i++)
            {
                foreach (string folder in ExcludedFolders)
                {
                    string Safe = folder.Replace("*", "");
                    if (ModuleSourceFiles[i].Contains(Safe))
                    {
                        ModuleSourceFiles[i] = "";
                    }
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
                    files[i] = files[i].Replace(ModuleDefManager.GetRootPath() + "\\", "../");
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
                if (IncludeDirectories[i].Contains("$"))
                {
                    continue;
                }
                IncludeDirectories[i] = StringUtils.SanitizePath(ModuleDefManager.GetRootPath() + IncludeDirectories[i]);
            }
        }
    }
}
