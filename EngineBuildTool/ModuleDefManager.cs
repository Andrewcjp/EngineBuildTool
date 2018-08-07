﻿using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

namespace EngineBuildTool
{
    class ModuleDefManager
    {
        Library Projectdata;
        public string SourceDir = "";
        public List<string> ModuleExcludeList = new List<string>();
        string BuildAssembly = "BuildCsFiles.dll";
        string BuildCsString = ".Build";
        string TargetCsString = ".Target";
        List<string> ModuleNames = new List<string>();
        TargetRules TargetRulesObject;
        List<ModuleDef> ModuleObjects = new List<ModuleDef>();
        const string DefaultTargetRulesName = "CoreTargetRules";
        public string TargetRulesName = "";


        public static string GetSourcePath()
        {
            return Directory.GetCurrentDirectory() + "\\Source";
        }
        public static string GetRootPath()
        {
            return Directory.GetCurrentDirectory();
        }
        public static string GetBinPath()
        {
            return Directory.GetCurrentDirectory() + "\\Binaries";
        }
        public static string GetLibPath()
        {
            return Directory.GetCurrentDirectory() + "\\lib";
        }
        public static string GetIncludePath()
        {
            return Directory.GetCurrentDirectory() + "\\include";
        }
        public static string GetIntermediateDir()
        {
            return Directory.GetCurrentDirectory() + "\\Intermediate";
        }
        public ModuleDefManager()
        {
            SourceDir = GetSourcePath();
            Projectdata = new Library();
        }

        void LogStage(string stagename)
        {
            const int LineSize = 20;
            int Length = LineSize - stagename.Length;
            string logstring = "";
            for (int i = 0; i < Length / 2; i++)
            {
                logstring += "-";
            }
            logstring += stagename;
            for (int i = 0; i < Length / 2; i++)
            {
                logstring += "-";
            }
            Console.WriteLine(logstring);
        }
        public void Clean()
        {
            LogStage("Clean Stage");
            if (Directory.Exists(GetIntermediateDir()))
            {
                Directory.Delete(GetIntermediateDir(), true);
            }
            if (File.Exists(BuildAssembly))
            {
                File.Delete(BuildAssembly);
            }
        }

        void GatherModuleFiles()
        {
            List<string> SourceFiles = new List<string>();
            string[] files = Directory.GetFiles(SourceDir, "*" + BuildCsString + ".cs", SearchOption.AllDirectories);
            SourceFiles.AddRange(files);
            const string ModuleSufix = "Module";
            foreach (string s in files)
            {
                string filename = Path.GetFileNameWithoutExtension(s);
                string outi = filename.Replace(BuildCsString, "");
                ModuleNames.Add(outi);
            }
            ModuleExcludeList.Add("VulkanRHI");
            foreach (string s in ModuleExcludeList)
            {
                if (ModuleNames.Contains(s))
                {
                    ModuleNames.Remove(s);
                    Console.WriteLine("Excluded Module " + s);
                }
            }


            string[] Targetfiles = Directory.GetFiles(SourceDir, "*" + TargetCsString + ".cs", SearchOption.AllDirectories);
            SourceFiles.AddRange(Targetfiles);

            Assembly CompiledAssembly = CompileAssembly("BuildCsFiles.dll", SourceFiles);
            AppDomain.CurrentDomain.Load(CompiledAssembly.GetName());

            Type RulesObjectType;
            foreach (string module in ModuleNames)
            {
                RulesObjectType = CompiledAssembly.GetType(module + ModuleSufix);
                if (RulesObjectType != null)
                {
                    ModuleDef RulesObject;
                    RulesObject = (ModuleDef)FormatterServices.GetUninitializedObject(RulesObjectType);
                    ConstructorInfo Constructor = RulesObjectType.GetConstructor(Type.EmptyTypes);
                    if (Constructor != null)
                    {
                        Constructor.Invoke(RulesObject, new object[] { });
                        ModuleObjects.Add(RulesObject);
                    }
                }
            }
            if (TargetRulesName.Length == 0)
            {
                TargetRulesName = DefaultTargetRulesName;
            }
            RulesObjectType = CompiledAssembly.GetType(TargetRulesName);
            if (RulesObjectType == null)
            {
                Console.WriteLine("Failed to File Target Rules \"" + TargetRulesName + "\" falling back to default");
                TargetRulesName = DefaultTargetRulesName;
                RulesObjectType = CompiledAssembly.GetType(TargetRulesName);
            }
            if (RulesObjectType != null)
            {
                TargetRulesObject = (TargetRules)FormatterServices.GetUninitializedObject(RulesObjectType);
                ConstructorInfo Constructor = RulesObjectType.GetConstructor(Type.EmptyTypes);
                if (Constructor != null)
                {
                    Constructor.Invoke(TargetRulesObject, new object[] { });
                }
            }
        }
        ModuleDef CoreModule = null;
        public void Run()
        {
            LogStage("Generate Stage");
            Directory.CreateDirectory(GetIntermediateDir());
            GatherModuleFiles();
            CmakeGenerator gen = new CmakeGenerator();
            //core module Is Special
            CoreModule = TargetRulesObject.GetCoreModule();
            Projectdata.LibSearchPaths.AddRange(TargetRulesObject.LibSearchPaths);
            PreProcessModules();
            Projectdata.PopulateLibs();
            ProcessModules();

            Console.WriteLine("Running CMake");
            gen.GenerateList(ModuleObjects, CoreModule);
            gen.RunCmake();
            FileUtils.CreateShortcut("EngineSolution.sln", GetRootPath(),GetIntermediateDir()+ "\\Engine.sln");
            LogStage("Complete");
        }

        void PreProcessModules()
        {
            CoreModule.PostInit();
            foreach (ModuleDef def in ModuleObjects)
            {
                def.PostInit();
                if (def.ModuleOuputType == ModuleDef.ModuleType.LIB)
                {
                    CoreModule.ModuleDepends.Add(def.ModuleName);
                }
                if (def.AdditonalLibSearchPaths.Count != 0)
                {
                    Projectdata.LibSearchPaths.AddRange(def.AdditonalLibSearchPaths);
                }
            }
        }

        void ProcessModules()
        {
            foreach (ModuleDef def in ModuleObjects)
            {
                Projectdata.AddLibsForModule(def);
            }
            Projectdata.AddLibsForModule(CoreModule,true);
        }

        static Assembly CompileAssembly(string OutputAssemblyPath, List<string> SourceFileNames, List<string> ReferencedAssembies = null, List<string> PreprocessorDefines = null, bool TreatWarningsAsErrors = false)
        {

            CompilerParameters CompileParams = new CompilerParameters();
            CompileParams.OutputAssembly = OutputAssemblyPath;

            Assembly UnrealBuildToolAssembly = Assembly.GetExecutingAssembly();
            CompileParams.ReferencedAssemblies.Add(UnrealBuildToolAssembly.Location);
            CompilerResults CompileResults;
            try
            {
                // Enable .NET 4.0 as we want modern language features like 'var'
                Dictionary<string, string> ProviderOptions = new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } };
                CSharpCodeProvider Compiler = new CSharpCodeProvider(ProviderOptions);
                CompileResults = Compiler.CompileAssemblyFromFile(CompileParams, SourceFileNames.ToArray());
            }
            catch (Exception Ex)
            {
                throw new Exception("Failed to launch compiler to compile assembly from source files '{0}' (Exception: {1})", Ex);
            }
            if (CompileResults.Errors.Count > 0)
            {
                foreach (CompilerError CurError in CompileResults.Errors)
                {
                    Console.WriteLine(CurError.ToString());
                }
                throw new Exception();
            }
            Console.WriteLine("Complie Sucessful");

            return CompileResults.CompiledAssembly;
        }
    }
}
