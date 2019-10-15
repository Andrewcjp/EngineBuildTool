using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace EngineBuildTool
{
    class CmakeGenerator : GeneratorBase
    {
        bool PreBuild_HeaderTool = true;
        bool UseAllBuildWorkAround = false;
        const bool EnableFastLink = true;
        string OutputData = "";

        string GetConfigNames(List<BuildConfig> Configs, bool DebugOnly = false)
        {
            string output = "";
            foreach (BuildConfig bc in Configs)
            {
                if (DebugOnly && bc.CurrentType != BuildConfiguration.BuildType.Debug)
                {
                    continue;
                }
                output += " " + bc.Name;
            }
            return output;
        }
        string GetFlagForConfig(BuildConfig bc, string flag)
        {
            string output = "";
            output += "SET(CMAKE_" + flag + "_FLAGS_" + bc.Name.ToUpper() + " ";
            if (bc.CurrentType == BuildConfiguration.BuildType.Debug)
            {
                output += "\"${CMAKE_" + flag + "_FLAGS_DEBUG}";
            }
            else if (bc.CurrentType == BuildConfiguration.BuildType.Release)
            {
                output += "\"${CMAKE_" + flag + "_FLAGS_RELWITHDEBINFO}";
            }
            foreach (string define in bc.Defines)
            {
                output += " /D" + define;
            }
            output += "\"";
            output += ") \n";
            return output;
        }

        string GetConfigationStrings(List<BuildConfig> Configs)
        {
            string output = "";
            foreach (BuildConfig bc in Configs)
            {
                output += GetFlagForConfig(bc, "CXX");
                output += GetFlagForConfig(bc, "EXE_LINKER");
                output += GetFlagForConfig(bc, "EXE_LINKER_CONSOLE");
                output += GetFlagForConfig(bc, "MODULE_LINKER");
                output += GetFlagForConfig(bc, "SHARED_LINKER");
            }

            output += "set_property(GLOBAL PROPERTY DEBUG_CONFIGURATIONS " + GetConfigNames(Configs, true) + ")\n";
            return output;
        }
        string AppendConfigFlags(BuildConfig bc, string flag, string value)
        {
            string configflagname = "CMAKE_" + flag + "_FLAGS_" + bc.Name.ToUpper();
            return "SET(" + configflagname + " \"${" + configflagname + "} " + value + "\")\n";
        }
        //const string SDKVersion = "10.0.17763.0";
        // const string SDKVersion = "10.0.18362.0";
        void GenHeader(List<BuildConfig> buildConfigs)
        {
            string SDKVersion = ModuleDefManager.TargetRulesObject.GetWinSDKVer();
            OutputData += "cmake_minimum_required (VERSION 3.12.1)\n";
            OutputData += "message(\"Detected CMAKE_SYSTEM_VERSION = '${CMAKE_SYSTEM_VERSION}'\")\n";
            OutputData += "set_property(GLOBAL PROPERTY USE_FOLDERS ON)\n";
            OutputData += "Project(" + "Engine" + " CSharp C CXX )\n";
            string OutputDir = StringUtils.SanitizePath(ModuleDefManager.GetBinPath());
            OutputData += "set(CMAKE_RUNTIME_OUTPUT_DIRECTORY \"" + OutputDir + "\")\n";
            OutputData += "set(CMAKE_LIBRARY_OUTPUT_DIRECTORY  \"" + OutputDir + "\")\n";
            OutputData += "set(CMAKE_MODULE_OUTPUT_DIRECTORY  \"" + OutputDir + "\")\n";///NODEFAULTLIB:MSVCRT
            OutputData += "set(CMAKE_EXE_LINKER_FLAGS \"${CMAKE_EXE_LINKER_FLAGS} /SUBSYSTEM:WINDOWS /IGNORE:4099 \")\n";
            OutputData += "set(CMAKE_EXE_LINKER_CONSOLE_FLAGS \"${CMAKE_EXE_LINKER_FLAGS} /SUBSYSTEM:CONSOLE  \")\n";

            if (EnableFastLink)
            {
                OutputData += "set(CMAKE_EXE_LINKER_FLAGS_DEBUG \" /INCREMENTAL /debug:fastlink \")\n";
                OutputData += "set(CMAKE_MODULE_LINKER_FLAGS_DEBUG \" /INCREMENTAL /debug:fastlink \")\n";
                OutputData += "set(CMAKE_SHARED_LINKER_FLAGS_DEBUG \" /INCREMENTAL /debug:fastlink \")\n";
            }
            OutputData += "set(CMAKE_CONFIGURATION_TYPES" + GetConfigNames(buildConfigs) + ")\n";
            OutputData += "set(CMAKE_SUPPRESS_REGENERATION true)\n";
            OutputData += GetConfigationStrings(buildConfigs);
            OutputData += "add_definitions(/MP)\n";
            OutputData += "add_definitions(-DUNICODE)\nadd_definitions(-D_UNICODE)\n add_definitions(/sdl)\n";//add_definitions(/sdl)\n
            foreach (BuildConfig b in buildConfigs)
            {
                if (b.CurrentPackageType == BuildConfiguration.PackageType.ShippingPackage && b.CurrentType == BuildConfiguration.BuildType.Release)
                {
                    OutputData += AppendConfigFlags(b, "EXE_LINKER", "/LTCG");
                    OutputData += AppendConfigFlags(b, "MODULE_LINKER", "/LTCG");
                    OutputData += AppendConfigFlags(b, "CXX", "/Ob2 /Ot /Oi /GL /arch:AVX2");
                }
                else if (b.CurrentType == BuildConfiguration.BuildType.Release)
                {
                    //OutputData += AppendConfigFlags(b, "EXE_LINKER", "/LTCG");
                    //OutputData += AppendConfigFlags(b, "MODULE_LINKER", "/LTCG");
                    OutputData += AppendConfigFlags(b, "CXX", "/Ob2 /Ot /Oi");
                }
            }
            OutputData += "message(\"Detected CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION = '${CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION}'\")\n";

        }
        const string BuildAllTarget = "BuildAll";
        const string HeaderToolTarget = "HeaderTool";
        public override void GenerateList(List<ModuleDef> Modules, ModuleDef CoreModule, List<BuildConfig> buildConfigs)
        {
            Console.WriteLine("Targeting Platform " + SingleTargetPlatform.Name);
            GenHeader(buildConfigs);
            ProcessModule(CoreModule);

            foreach (ModuleDef M in Modules)
            {
                ProcessModule(M);
            }
            {//Header tool project
                OutputData += "add_custom_target(" + HeaderToolTarget + " DEPENDS  always_rebuild)\n";
                string headertoolString = StringUtils.SanitizePath(ModuleDefManager.GetSourcePath() + "/EngineHeaderTool.exe \" ");
                Console.WriteLine("Game Module is " + CoreModule.GameModuleName);
                OutputData += "add_custom_command(TARGET " + HeaderToolTarget + "  PRE_BUILD  \nCOMMAND \"" + headertoolString +
                    " -Name " + CoreModule.GameModuleName + " )\n";
                OutputData += "set_target_properties(" + HeaderToolTarget + " PROPERTIES FOLDER " + "Build/" + ")\n";
            }
            if (UseAllBuildWorkAround)
            {
                OutputData += "add_custom_target(" + BuildAllTarget + " ALL)\n";
                if (PreBuild_HeaderTool)
                {
                    OutputData += "add_dependencies(" + BuildAllTarget + " " + HeaderToolTarget + ")\n";
                }
                foreach (ModuleDef M in Modules)
                {
                    OutputData += "add_dependencies(" + BuildAllTarget + " " + M.ModuleName + ")\n";
                }
                //A Workaround is used here to set the correct directories for running the project
                OutputData += "set_target_properties(" + BuildAllTarget + " PROPERTIES FOLDER " + "Targets/" + ")\n";

                OutputData += "set_property(DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR} PROPERTY VS_STARTUP_PROJECT " + BuildAllTarget + ")\n";

            }
            else
            {

                ModuleDef Temp_GMOut = null;
                foreach (ModuleDef M in Modules)
                {
                    if (M.ModuleOutputType == ModuleDef.ModuleType.EXE)
                    {
                        Temp_GMOut = M;
                        break;
                    }
                }
                foreach (ModuleDef M in Modules)
                {
                    OutputData += "add_dependencies(" + Temp_GMOut.ModuleName + " " + M.ModuleName + ")\n";
                }
                OutputData += "set_property(DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR} PROPERTY VS_STARTUP_PROJECT " + Temp_GMOut.ModuleName + ")\n";
            }

            WriteToFile(ModuleDefManager.GetSourcePath());

        }
        public override void RunPostStep(List<ModuleDef> Modules, ModuleDef CoreModule)
        {
            string replacement = " Win64|x64";
            string ConfigToken = "</Configuration>";
            string ConfigreplaceToken = " Win64</Configuration>";
            VisualStudioProjectEditor.EnableUnityBuild(CoreModule);
            VisualStudioProjectEditor.ReplaceAllModule(CoreModule, "|x64", replacement);
            VisualStudioProjectEditor.ReplaceAllModule(CoreModule, ConfigToken, ConfigreplaceToken);
            foreach (ModuleDef m in Modules)
            {
                VisualStudioProjectEditor.EnableUnityBuild(m);
                VisualStudioProjectEditor.ProcessFile(m);
                VisualStudioProjectEditor.ReplaceAllModule(m, "|x64", replacement);
                VisualStudioProjectEditor.ReplaceAllModule(m, ConfigToken, ConfigreplaceToken);
            }
            string buildall = ModuleDefManager.GetIntermediateDir() + "\\ALL_Build.vcxproj";
            VisualStudioProjectEditor.ReplaceAll(buildall, "|x64", replacement);
            VisualStudioProjectEditor.ReplaceAll(buildall, ConfigToken, ConfigreplaceToken);
            string HeaderTool = ModuleDefManager.GetIntermediateDir() + "\\HeaderTool.vcxproj";
            VisualStudioProjectEditor.ReplaceAll(HeaderTool, "|x64", replacement);
            VisualStudioProjectEditor.ReplaceAll(HeaderTool, ConfigToken, ConfigreplaceToken);
            string SLNpath = ModuleDefManager.GetIntermediateDir() + "\\Engine.sln";

            foreach (BuildConfig bc in ModuleDefManager.CurrentConfigs)
            {

                string token = "|x64.ActiveCfg = " + bc.Name + "|x64";
                string repalcementtoken = "|Win64.ActiveCfg = " + bc.Name + " Win64|x64";
                VisualStudioProjectEditor.ReplaceAll(SLNpath, token, repalcementtoken);
                token = "|x64.Build.0 = " + bc.Name + "|x64";
                repalcementtoken = "|Win64.Build.0 = " + bc.Name + " Win64|x64";
                VisualStudioProjectEditor.ReplaceAll(SLNpath, token, repalcementtoken);
                token = "|x64 = " + bc.Name + "|x64";
                repalcementtoken = "|Win64 = " + bc.Name + "|Win64";
                VisualStudioProjectEditor.ReplaceAll(SLNpath, token, repalcementtoken);
            }
            if (UseAllBuildWorkAround)
            {
                foreach (BuildConfig bc in ModuleDefManager.CurrentConfigs)
                {
                    string path = StringUtils.SanitizePath(ModuleDefManager.GetBinPath() + "\\" + bc.Name + "\\");
                    VisualStudioProjectEditor.SetTargetOutput(BuildAllTarget, path, CoreModule.OutputObjectName, bc.Name);
                }
            }

        }

        public void ProcessModule(ModuleDef Module)
        {
            if (Module.Processed)
            {
                return;
            }
            OutputData += "#-------------Module Start " + Module.ModuleName + "----------------\n";
            Module.GatherSourceFiles();
            Module.GatherIncludes();
            if (Module.LaunguageType == ModuleDef.ProjectType.CSharp)
            {
                OutputData += CmakeCSharpProject.GetModule(Module);
                return;
            }
            Module.PreProcessorDefines.AddRange(SingleTargetPlatform.Defines);
            string AllSourceFiles = StringUtils.ArrayStringQuotes(Module.ModuleSourceFiles.ToArray());
            string ExtraSourceFiles = StringUtils.ArrayStringQuotes(Module.ModuleExtraFiles.ToArray());
            string ALLFiles = StringUtils.RelativeToABS(Module.ModuleSourceFiles) + ExtraSourceFiles;
            if (Module.ModuleOutputType == ModuleDef.ModuleType.ModuleDLL)
            {
                OutputData += "add_library( " + Module.ModuleName + " MODULE " + ALLFiles + ")\n";
            }
            else if (Module.ModuleOutputType == ModuleDef.ModuleType.DLL)
            {
                OutputData += "add_library( " + Module.ModuleName + " SHARED " + ALLFiles + ")\n";
            }
            else if (Module.ModuleOutputType == ModuleDef.ModuleType.LIB)
            {
                OutputData += "add_library( " + Module.ModuleName + " STATIC " + ALLFiles + ")\n";
            }
            else if (Module.ModuleOutputType == ModuleDef.ModuleType.EXE)
            {
                OutputData += "add_executable( " + Module.ModuleName + " " + ALLFiles + ")\n";
                OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES ENABLE_EXPORTS On)\n";
            }

            if (Module.UseConsoleSubSystem)
            {
                OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES LINK_FLAGS ${CMAKE_EXE_LINKER_CONSOLE_FLAGS})\n";

                foreach (BuildConfig bc in ModuleDefManager.CurrentConfigs)
                {
                    string OutputDir = StringUtils.SanitizePath(ModuleDefManager.GetBinPath() + "\\Tools\\" + bc.Name + "\\");
                    OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES RUNTIME_OUTPUT_DIRECTORY_" + bc.Name.ToUpper() + " \"" + OutputDir + "\")\n";
                }
            }
            if (Module.OutputObjectName.Length > 0)
            {
                OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES OUTPUT_NAME \"" + Module.OutputObjectName + "\")\n";
            }
            if (Module.SolutionFolderPath.Length == 0)
            {
                Module.SolutionFolderPath = "Engine/Modules";
            }
            OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES FOLDER " + Module.SolutionFolderPath + ")\n";

            if (Module.ModuleLibs.Count != 0)
            {
                OutputData += "target_link_libraries(" + Module.ModuleName + " " + StringUtils.ArrayStringQuotes(Module.ModuleLibs.ToArray()) + ")\n";
            }
            if (PreBuild_HeaderTool)
            {
                OutputData += "add_dependencies(" + Module.ModuleName + " " + HeaderToolTarget + ")\n";
            }
            if (Module.LaunguageType == ModuleDef.ProjectType.ManagedCPP)
            {
                //Module.ModuleDepends.Add("CSharpCore");
            }
            if (Module.ModuleDepends.Count != 0)
            {
                OutputData += "target_link_libraries(" + Module.ModuleName + " " + StringUtils.ArrayStringQuotes(Module.ModuleDepends.ToArray()) + ")\n";
            }
            VisualStudioProjectEditor.ProcessNuGetPacks(Module);
            List<string> Dirs = new List<string>();
            Module.GetIncludeDirs(ref Dirs);
            if (Module != ModuleDefManager.CoreModule)
            {
                ModuleDefManager.CoreModule.GetIncludeDirs(ref Dirs);
            }
            if (Dirs.Count > 0)
            {
#if true
                OutputData += "target_include_directories(" + Module.ModuleName + " PRIVATE " + StringUtils.ArrayStringQuotes(Dirs.ToArray()) + ")\n";
#else
                OutputData += "include_directories(" + Module.ModuleName + " " + ArrayStringQuotes(Dirs.ToArray()) + ")\n";
#endif
                Dirs.Clear();
            }
            OutputData += "source_group(TREE \"" + StringUtils.SanitizePath(ModuleDefManager.GetRootPath()) + "\" REGULAR_EXPRESSION \"*.h\" FILES " + ALLFiles + ")\n";

            OutputData += "set_source_files_properties(" + ExtraSourceFiles + " PROPERTIES HEADER_FILE_ONLY ON)\n";
            OutputData += "set_target_properties( " + Module.ModuleName + " PROPERTIES GHS_NO_SOURCE_GROUP_FILE OFF)\n";
            if (Module.UseCorePCH)
            {
                Module.PCH = ModuleDefManager.CoreModule.PCH;
            }
            if (Module.PCH.Length != 0)
            {
                string PCHString = /*"Source/" +*/ /*Module.SourceFileSearchDir + "/"+*/  Module.PCH;
                string pchstring = "/FI" + PCHString + ".h";
                string SharedHeaderData = " /Yu" + PCHString + ".h ";
                if (Module.UseCorePCH)
                {
                    SharedHeaderData = "";
                }
#if false
                if (Module.UseCorePCH)
                {
                    SharedHeaderData = "/Fp" + ModuleDefManager.CoreModule.ModuleName + ".dir" + "/$(Configuration)/" + ModuleDefManager.CoreModule.ModuleName + ".pch";
                }
#endif
                OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES COMPILE_FLAGS \"" + SharedHeaderData + pchstring + "\" )\n";

                if (!Module.UseCorePCH)
                {
                    OutputData += "SET_SOURCE_FILES_PROPERTIES(\"" + Module.SourceFileSearchDir + "/" + Module.PCH + ".cpp\" COMPILE_FLAGS \"/Yc" + PCHString + ".h\" )\n";
                }

            }

            if (VisualStudioProjectEditor.CanModuleUnity(Module))
            {
                Module.PreProcessorDefines.Add("WITH_UNITY");
            }
            OutputData += "target_compile_definitions(" + Module.ModuleName + " PRIVATE " + StringUtils.ListStringDefines(Module.PreProcessorDefines) + ")\n";
            ///WHOLEARCHIVE
            if (Module.ModuleDepends.Count > 0 && Module.ModuleOutputType == ModuleDef.ModuleType.LIB)
            {
                string WholeDataString = "";
                foreach (string s in Module.ModuleDepends)
                {
                    WholeDataString += "/WHOLEARCHIVE:" + s + " ";
                }
                OutputData += "SET_TARGET_PROPERTIES(" + Module.ModuleName + " PROPERTIES LINK_FLAGS_DEBUG " + WholeDataString + " )\n";
            }
            if (Module.NeedsCore && Module != ModuleDefManager.CoreModule)
            {
                OutputData += "add_dependencies(" + Module.ModuleName + " Core )\n";
            }
            if (Module.IsCoreModule)
            {
                string VersionGetterString = StringUtils.SanitizePath(ModuleDefManager.GetRootPath() + "/Scripts/WriteCommit.bat ");
                OutputData += "add_custom_command(TARGET " + Module.ModuleName + "  PRE_BUILD  \nCOMMAND \"" + VersionGetterString + "\" )\n";
            }
            if (Module.LaunguageType == ModuleDef.ProjectType.ManagedCPP)
            {
                //Imported_common_language_runtime
                OutputData += "set_property(TARGET " + Module.ModuleName + " PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION \"v4.6.1\")\n";
                OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES COMMON_LANGUAGE_RUNTIME \"\")\n";

                //OutputData += "SET (MANAGEDFLAGS \"${CMAKE_CXX_FLAGS}\")\n";
                //OutputData += "SET (MANAGEDFLAGS_D \"${CMAKE_CXX_FLAGS_DEBUG}\")\n";
                //OutputData += "STRING(REPLACE \"/EHsc\" \"/EHa\" MANAGEDFLAGS ${MANAGEDFLAGS}) \n STRING(REPLACE \"/RTC1\" \"\" MANAGEDFLAGS_D ${MANAGEDFLAGS_D})\n";
                //OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES COMPILE_FLAGS \"${CMAKE_CXX_FLAGS}" + "/clr" + "\" )\n";
                OutputData += "set_property(TARGET " + Module.ModuleName + " PROPERTY VS_DOTNET_REFERENCES  \"System\" " + StringUtils.ArrayStringQuotes(Module.NetReferences.ToArray()) + " )\n";
            }
            Module.Processed = true;
        }

        void WriteToFile(string dir)
        {
            File.WriteAllText(dir + "/CmakeLists.txt", OutputData);
        }
        public static bool UseVs17 = true;
        string CmakeLocalPath = "";
        //this allows a cmake install to be placed in a folder called CmakeLocal
        bool FindLocalCmake()
        {
            CmakeLocalPath = ModuleDefManager.GetRootPath() + "\\CmakeLocal\\CMake\\bin";
            if (!Directory.Exists(CmakeLocalPath))
            {
                return false;
            }
            CmakeLocalPath += "\\cmake.exe";
            if (!File.Exists(CmakeLocalPath))
            {
                return false;
            }
            return true;
        }



        public override void Execute()
        {
            string SDKVersion = ModuleDefManager.TargetRulesObject.GetWinSDKVer();
            string Arg = " -DCMAKE_SYSTEM_VERSION=" + SDKVersion + " -DCMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION=" + SDKVersion;
            string Vs17Args = "\"Visual Studio 15 2017 Win64\"" + Arg;
            string Vs15Args = "\"Visual Studio 14 2015 Win64\"" + Arg;
            string CmakeArgs = "-G  " + (UseVs17 ? Vs17Args : Vs15Args) + " \"" + ModuleDefManager.GetSourcePath() + "\"";
            string Cmakeexe = "cmake";
            if (FindLocalCmake())
            {
                Cmakeexe = CmakeLocalPath;
            }
            int code = ProcessUtils.RunProcess(Cmakeexe, CmakeArgs);

            Console.WriteLine("Cmake finished with Code: " + code);
        }

        public override void ClearCache()
        {
            string CMakeDir = ModuleDefManager.GetIntermediateDir() + "\\CMakeFiles";
            if (Directory.Exists(CMakeDir))
            {
                Directory.Delete(CMakeDir, true);
            }
        }
    }
}
