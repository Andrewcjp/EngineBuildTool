using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

namespace EngineBuildTool
{
    public class ModuleDefManager
    {
        Library Projectdata;
        public static string SourceDir = "";

        string BuildAssembly = "BuildCsFiles.dll";
        const string BuildCsString = ".Build";
        const string TargetCsString = ".Target";
        List<string> ModuleNames = new List<string>();
        public static TargetRules TargetRulesObject;
        List<ModuleDef> NonCoreModuleObjects = new List<ModuleDef>();
        const string DefaultTargetRulesName = "CoreTargetRules";
        public string TargetRulesName = "";
        List<ModuleDef> ALLModules = new List<ModuleDef>();
        public static bool USEPREMAKE = false;
        public static ModuleDefManager Instance;
        public static bool IsDebug()
        {
            return false;
        }
        public static string GetSourcePath()
        {
            return SourceDir;
        }
        public static string GetRootPath()
        {
            return Directory.GetCurrentDirectory();
        }
        public static string GetThirdPartyDir()
        {
            return Directory.GetCurrentDirectory() + GetThirdPartyDirRelative();
        }
        public static string GetThirdPartyDirRelative()
        {
            return "\\Source\\ThirdParty\\";
        }
        public static string GetBinPath()
        {
            return Directory.GetCurrentDirectory() + "\\Binaries";
        }
        public static string GetStaticLibPath()
        {
            return Directory.GetCurrentDirectory() + "\\lib\\Static";
        }
        public static string GetDynamicLibPath()
        {
            return Directory.GetCurrentDirectory() + "\\lib\\Dynamic";
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
            Instance = this;
            SourceDir = Directory.GetCurrentDirectory() + "\\Source"; ;
#if DEBUG 
            SourceDir = "C:\\Users\\AANdr\\Documents\\Dev\\Engine\\Engine\\Repo\\GraphicsEngine\\Source\\";
            if (Directory.Exists(SourceDir))
            {
                USEPREMAKE = true;             
            }
            else
            {
                SourceDir = Directory.GetCurrentDirectory() + "\\Source"; ;
            }
#endif
            Projectdata = new Library();
            SettingCache.Load();
        }

        void LogStage(string stagename)
        {
            const int LineSize = 40;
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
            FileUtils.DeleteDirectory(GetIntermediateDir());
            FileUtils.DeleteDirectory(GetRootPath() + "\\DerivedDataCache");
            FileUtils.DeleteDirectory(GetBinPath());
            FileUtils.DeleteDirectory(GetRootPath() + "\\x64");
            FileUtils.DeleteDirectory(GetRootPath() + "\\Build");
            FileUtils.DeleteDirectory(GetRootPath() + "\\Packed");
            FileUtils.DeleteFile(BuildAssembly);
            FileUtils.DeleteFile("EngineSolution.sln");
        }
        Assembly CompiledAssembly = null;
        void GatherModuleFiles()
        {
            List<string> SourceFiles = new List<string>();
            string[] files = Directory.GetFiles(SourceDir, "*" + BuildCsString + ".cs", SearchOption.AllDirectories);
            SourceFiles.AddRange(files);
            FindPlatfromInterface(ref SourceFiles);
            const string ModuleSufix = "Module";
            foreach (string s in files)
            {
                string filename = Path.GetFileNameWithoutExtension(s);
                string outi = filename.Replace(BuildCsString, "");
                ModuleNames.Add(outi);
            }
            string[] Targetfiles = Directory.GetFiles(SourceDir, "*" + TargetCsString + ".cs", SearchOption.AllDirectories);
            SourceFiles.AddRange(Targetfiles);

            try
            {
                CompiledAssembly = CompileAssembly("BuildCsFiles.dll", SourceFiles);
            }
            catch (Exception E)
            {
                Console.WriteLine("Compile Failed with error " + E.Message);
                Console.Read();
                Environment.Exit(1);
            }
            AppDomain.CurrentDomain.Load(CompiledAssembly.GetName());
            SetupPlatfromInterface();

            if (TargetRulesName.Length == 0)
            {
                TargetRulesName = DefaultTargetRulesName;
            }

            Type RulesObjectType = CompiledAssembly.GetType(TargetRulesName);
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
                TargetRulesObject.Resolve();
            }
            Console.WriteLine("Targeting Windows 10 Version " + TargetRulesObject.WindowTenVersionTarget + " (" + TargetRulesObject.GetWinSDKVer() + ")");
            foreach (string s in TargetRulesObject.ModuleExcludeList)
            {
                if (ModuleNames.Contains(s))
                {
                    ModuleNames.Remove(s);
                    Console.WriteLine("Excluded Module " + s);
                }
            }
            const string ModuleEnd = "Module";
            for (int i = 0; i < ModuleNames.Count; i++)
            {
                if (ModuleNames[i].EndsWith(ModuleEnd))
                {
                    ModuleNames[i] = ModuleNames[i].Remove(ModuleNames[i].Length - ModuleEnd.Length, ModuleEnd.Length);
                }
            }
            InitObjectsOfType(ModuleNames, ref NonCoreModuleObjects, CompiledAssembly, ModuleSufix);
            ALLModules.AddRange(NonCoreModuleObjects);

        }

        void InitObjectsOfType<T>(List<string> Names, ref List<T> ConstructedObjects, Assembly CompiledAssembly, string Sufix = "")
        {
            Type ObjectType;
            foreach (string name in Names)
            {
                ObjectType = CompiledAssembly.GetType(name + Sufix);
                if (ObjectType != null)
                {
                    T RulesObject;
                    RulesObject = (T)FormatterServices.GetUninitializedObject(ObjectType);
                    ConstructorInfo Constructor;
                    if (typeof(T) == typeof(ModuleDef))
                    {
                        Constructor = ObjectType.GetConstructor(new Type[] { typeof(TargetRules) });
                        if (Constructor != null)
                        {
                            Constructor.Invoke(RulesObject, new object[] { TargetRulesObject });
                        }
                    }
                    else
                    {
                        Constructor = ObjectType.GetConstructor(new Type[] { });
                        if (Constructor != null)
                        {
                            Constructor.Invoke(RulesObject, new object[] { });
                        }
                    }
                    ConstructedObjects.Add(RulesObject);
                }
            }
        }

        public static ModuleDef CoreModule = null;
        public static List<BuildConfig> CurrentConfigs = new List<BuildConfig>();
        const bool LogDebug = true;
        GeneratorBase gen;
        public void Run()
        {
            LogStage("Generate Stage");
            CurrentConfigs = BuildConfiguration.GetDefaultConfigs();
            Directory.CreateDirectory(GetIntermediateDir());
            GatherModuleFiles();
            PlatformDefinition.Init(Interfaces);

            if (USEPREMAKE)
            {
                gen = new PreMakeGenerator();
            }
            else
            {
                gen = new CmakeGenerator();
            }
            //core module Is Special!
            CoreModule = TargetRulesObject.GetCoreModule();
            for (int i = ALLModules.Count - 1; i >= 0; i--)
            {
                if (ALLModules[i].IsGameModule && ALLModules[i].ModuleName != CoreModule.GameModuleName)
                {
                    ALLModules.RemoveAt(i);
                }
            }
            ALLModules.Add(CoreModule);
            if (LogDebug)
            {
                foreach (ModuleDef M in ALLModules)
                {
                    Console.WriteLine("Found module " + M.ModuleName);
                }
            }
            Projectdata.LibSearchPaths.AddRange(TargetRulesObject.LibSearchPaths);
            PreProcessModules();
            Projectdata.PopulateLibs();
            ProcessModules();
            LogStage("Generate project files stage");
            if (!SettingCache.IsCacheValid())
            {
                gen.ClearCache();
                Console.WriteLine("Cache Is Invalid, clearing...");
            }

            gen.SingleTargetPlatform = PlatformDefinition.GetDefinition(PlatformDefinition.WindowsID);
            gen.GenerateList(ALLModules, CoreModule, CurrentConfigs);
            gen.Execute();
            LogStage("Post Gen");
            gen.RunPostStep(NonCoreModuleObjects, CoreModule);
            LogStage("Copy DLLs");
            FileUtils.CreateShortcut("EngineSolution.sln", GetRootPath(), GetIntermediateDir() + "\\Engine.sln");
            Projectdata.CopyDllsToConfig(PlatformDefinition.GetDefaultPlatforms(), CurrentConfigs, ALLModules);
            LinkDirectiories();
            SettingCache.Save();
            LogStage("Complete");

        }
        void LinkPackageDir(string directoryname, string configname, PlatformDefinition PD)
        {
            string SRC = GetRootPath() + "\\" + directoryname;
            string Target = GetBinPath() + "\\" + PD.Name + "\\" + configname + "\\" + directoryname;
            FileUtils.CreateSymbolicLink(SRC, Target, true);
        }
        void LinkDirectiories()
        {
            foreach (BuildConfig bc in CurrentConfigs)
            {
                if (bc.CurrentPackageType != BuildConfiguration.PackageType.Editor)
                {
                    LinkPackageDir("DerivedDataCache", bc.Name, PlatformDefinition.GetDefinition(PlatformDefinition.WindowsID));
                    LinkPackageDir("Content", bc.Name, PlatformDefinition.GetDefinition(PlatformDefinition.WindowsID));
                }
            }
        }
        public static string GetConfigPathName(BuildConfiguration.BuildType type)
        {
            if (type == BuildConfiguration.BuildType.Debug)
            {
                return "\\Debug";
            }
            else
            {
                return "\\Release";
            }
        }

        void PreProcessModules()
        {
            InitObjectsOfType(CoreModule.ThirdPartyModules, ref CoreModule.ExternalModules, CompiledAssembly);
            CoreModule.PostInit(TargetRulesObject);
            Projectdata.LibSearchPaths.AddRange(CoreModule.AdditonalLibSearchPaths);
            foreach (ModuleDef def in NonCoreModuleObjects)
            {
                InitObjectsOfType(def.ThirdPartyModules, ref def.ExternalModules, CompiledAssembly);
                def.PostInit(TargetRulesObject);
                if (def.ModuleOutputType == ModuleDef.ModuleType.LIB)
                {
                    // CoreModule.ModuleDepends.Add(def.ModuleName);
                }
                if (def.NeedsCore)
                {
                    def.ModuleDepends.Add(CoreModule.ModuleName);
                }
                if (def.AdditonalLibSearchPaths.Count > 0)
                {
                    Projectdata.LibSearchPaths.AddRange(def.AdditonalLibSearchPaths);
                }
                if (def.LaunguageType == ModuleDef.ProjectType.CSharp)
                {
                    //add the system links
                    def.NetReferences.Add("System.Windows.Forms");
                }
            }

        }

        void ProcessModules()
        {
            foreach (ModuleDef def in NonCoreModuleObjects)
            {
                Projectdata.AddLibsForModule(def);
            }
            Projectdata.AddLibsForModule(CoreModule, true);
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
            Console.WriteLine("Compile Successful");

            return CompileResults.CompiledAssembly;
        }
        void FindPlatfromInterface(ref List<string> SourceFiles)
        {
            string targetdir = SourceDir + "\\ExtraPlatforms\\";
            if (!Directory.Exists(targetdir))
            {
                return;
            }
            string[] files = Directory.GetFiles(targetdir, "*" + "Platform" + ".cs", SearchOption.AllDirectories);
            SourceFiles.AddRange(files);
            foreach (string s in files)
            {
                string name = Path.GetFileName(s).ToLower().Replace(".platform.cs", "");
                PlatfromModuleNames.Add(name);
            }
        }
        List<string> PlatfromModuleNames = new List<string>();
        List<PlatformSupportInterface> Interfaces = new List<PlatformSupportInterface>();
        void SetupPlatfromInterface()
        {
            InitObjectsOfType(PlatfromModuleNames, ref Interfaces, CompiledAssembly, "platform");
        }
        public void OnPreMakeAddLibs(ModuleDef m, BuildConfig BC, PlatformDefinition PD, ref string Dllout)
        {
            foreach (PlatformSupportInterface i in Interfaces)
            {
                i.OnPreMakeAddLibs(m, BC, PD, ref Dllout);
            }

        }
        public void OnPreMakeWriteModule(ModuleDef n, ref string PremakeFile)
        {
            foreach (PlatformSupportInterface i in Interfaces)
            {
                i.OnPreMakeCreateModule(n, ref PremakeFile, gen);
            }
        }
        public void PatchPremakeFileHeader(ref string premakefile)
        {
            foreach (PlatformSupportInterface i in Interfaces)
            {
                i.PatchPremakeFileHeader(ref premakefile);
            }
        }
    }
}
