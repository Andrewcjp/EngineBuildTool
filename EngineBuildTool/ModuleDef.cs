﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{

    public class ModuleDef
    {
        public string ModuleName = "";
        public enum ModuleType { EXE, ModuleDLL, DLL, LIB };
        public ModuleType ModuleOutputType = ModuleType.ModuleDLL;
        public List<string> ModuleDepends = new List<string>();
        public string SolutionFolderPath = "";
        public string PCH = "";
        public string SourceFileSearchDir = "";
        public List<LibSearchPath> AdditonalLibSearchPaths = new List<LibSearchPath>();
        public List<string> LibNames = new List<string>();
        public List<string> IncludeDirectories = new List<string>();
        public bool UseConsoleSubSystem = false;
        //Generated
        public List<string> ModuleSourceFiles = new List<string>();
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
        public ModuleDef()
        { }
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
        }
        public void GetIncludeDirs(ref List<string> List)
        {
            List.AddRange(IncludeDirectories);
            for (int i = 0; i < List.Count; i++)
            {
                List[i] = CmakeGenerator.SanitizePath(List[i]);
            }
        }

        public void GatherSourceFiles()
        {
            if (ModuleSourceFiles.Count != 0)
            {
                return;
            }
            GetFiles("*.h");
            GetFiles("*.hpp");
            GetFiles("*.c");
            GetFiles("*.cpp");
        }

        void GetFiles(string Type)
        {
            string path = ModuleDefManager.GetSourcePath() + "\\" + SourceFileSearchDir;
            try
            {
                string[] files = Directory.GetFiles(path, Type, SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    files[i] = files[i].Replace(ModuleDefManager.GetSourcePath() + "\\", "");
                    files[i] = CmakeGenerator.SanitizePath(files[i]);
                }
                ModuleSourceFiles.AddRange(files);
            }
            catch
            {
            }
        }

        public void GatherIncludes()
        {
            for (int i = 0; i < IncludeDirectories.Count; i++)
            {
                IncludeDirectories[i] = CmakeGenerator.SanitizePath(ModuleDefManager.GetRootPath() + IncludeDirectories[i]);
            }
        }

    }
}
