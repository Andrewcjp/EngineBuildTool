using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    class CmakeGenerator
    {
        string OutputData = "";
        public static string SanitizePath(string input)
        {
            return input.Replace("\\", "/"); ;
        }
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
                output += GetFlagForConfig(bc, "MODULE_LINKER");
                output += GetFlagForConfig(bc, "SHARED_LINKER");
            }

            output += "set_property(GLOBAL PROPERTY DEBUG_CONFIGURATIONS " + GetConfigNames(Configs, true) + ")\n";
            return output;
        }
        void GenHeader(List<BuildConfig> buildConfigs)
        {
            OutputData += "cmake_minimum_required (VERSION 3.12.1)\n";
            OutputData += "set_property(GLOBAL PROPERTY USE_FOLDERS ON)\n";
            OutputData += "Project(" + "Engine" + ")\n";
            string OutputDir = SanitizePath(ModuleDefManager.GetBinPath());
            OutputData += "set(CMAKE_RUNTIME_OUTPUT_DIRECTORY " + OutputDir + ")\n";
            OutputData += "set(CMAKE_LIBRARY_OUTPUT_DIRECTORY  " + OutputDir + ")\n";
            OutputData += "set(CMAKE_MODULE_OUTPUT_DIRECTORY  " + OutputDir + ")\n";///NODEFAULTLIB:MSVCRT
            OutputData += "set(CMAKE_EXE_LINKER_FLAGS \"${CMAKE_EXE_LINKER_FLAGS} /SUBSYSTEM:WINDOWS /DEBUG:FASTLINK \")\n";
            OutputData += "set(CMAKE_CONFIGURATION_TYPES" + GetConfigNames(buildConfigs) + ")\n";
            OutputData += "set(CMAKE_SUPPRESS_REGENERATION true)\n";
            OutputData += GetConfigationStrings(buildConfigs);
            OutputData += "add_definitions(/MP)\n";
            //  OutputData += "add_definitions(/DEBUG:FASTLINK)\n";
            OutputData += "add_definitions(-DUNICODE)\nadd_definitions(-D_UNICODE)\nadd_definitions(/sdl)\n";
        }

        public void GenerateList(List<ModuleDef> Modules, ModuleDef CoreModule, List<BuildConfig> buildConfigs)
        {
            GenHeader(buildConfigs);
            ProcessModule(CoreModule);
            OutputData += "set_property(DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR} PROPERTY VS_STARTUP_PROJECT " + CoreModule.ModuleName + ")\n";
            foreach (ModuleDef M in Modules)
            {
                ProcessModule(M);
            }
            WriteToFile(ModuleDefManager.GetSourcePath());
        }
        static string ConvertStringArrayToStringJoin(string[] array)
        {
            // Use string Join to concatenate the string elements.
            string result = string.Join(" ", array);
            return result;
        }
        static string ArrayStringQuotes(string[] array)
        {
            // Use string Join to concatenate the string elements.
            string result = "";
            foreach (string s in array)
            {
                result += "\"" + s + "\"" + " ";
            }
            return result;
        }
        static string ListStringDefines(List<string> array)
        {
            // Use string Join to concatenate the string elements.
            string result = "";
            foreach (string s in array)
            {
                result += "-D" + s + ";  ";
            }
            return result;
        }

        static string ArrayStringQuotes(LibRef[] array)
        {
            // Use string Join to concatenate the string elements.
            string result = "";
            foreach (LibRef s in array)
            {
                if (s.Path.Length == 0)
                {
                    continue;
                }
                result += " " + s.BuildType + " \"" + s.Path + "\"" + " ";
            }
            return result;
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
            string AllSourceFiles = ArrayStringQuotes(Module.ModuleSourceFiles.ToArray());
            if (Module.ModuleOutputType == ModuleDef.ModuleType.ModuleDLL)
            {
                OutputData += "add_library( " + Module.ModuleName + " MODULE " + ArrayStringQuotes(Module.ModuleSourceFiles.ToArray()) + ")\n";
            }
            else if (Module.ModuleOutputType == ModuleDef.ModuleType.DLL)
            {
                OutputData += "add_library( " + Module.ModuleName + " SHARED " + ArrayStringQuotes(Module.ModuleSourceFiles.ToArray()) + ")\n";
            }
            else if (Module.ModuleOutputType == ModuleDef.ModuleType.LIB)
            {
                OutputData += "add_library( " + Module.ModuleName + " STATIC " + ArrayStringQuotes(Module.ModuleSourceFiles.ToArray()) + ")\n";
            }
            else if (Module.ModuleOutputType == ModuleDef.ModuleType.EXE)
            {
                OutputData += "add_executable( " + Module.ModuleName + " " + ArrayStringQuotes(Module.ModuleSourceFiles.ToArray()) + ")\n";
                OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES ENABLE_EXPORTS On)\n";
            }
            if (Module.SolutionFolderPath.Length == 0)
            {
                Module.SolutionFolderPath = "Engine/Modules";
            }
            OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES FOLDER " + Module.SolutionFolderPath + ")\n";

            if (Module.ModuleLibs.Count != 0)
            {
                OutputData += "target_link_libraries(" + Module.ModuleName + " " + ArrayStringQuotes(Module.ModuleLibs.ToArray()) + ")\n";
            }

            if (Module.ModuleDepends.Count != 0)
            {
                OutputData += "target_link_libraries(" + Module.ModuleName + " " + ArrayStringQuotes(Module.ModuleDepends.ToArray()) + ")\n";
            }

            List<string> Dirs = new List<string>();
            Module.GetIncludeDirs(ref Dirs);
            if (Dirs.Count != 0)
            {
                OutputData += "include_directories(" + Module.ModuleName + " " + ArrayStringQuotes(Dirs.ToArray()) + ")\n";
                Dirs.Clear();
            }
            OutputData += "source_group(TREE \"${CMAKE_CURRENT_SOURCE_DIR}\" FILES " + AllSourceFiles + ")\n";
            if (Module.UseCorePCH)
            {
                Module.PCH = ModuleDefManager.CoreModule.PCH;
            }
            if (Module.PCH.Length != 0)
            {
                string pchstring = "/FI" + Module.PCH + ".h";
                string SharedHeaderData = " /Yu" + Module.PCH + ".h ";
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
                    OutputData += "SET_SOURCE_FILES_PROPERTIES(\"" + Module.SourceFileSearchDir + "/" + Module.PCH + ".cpp\" COMPILE_FLAGS \"/Yc" + Module.PCH + ".h\" )\n";
                }

            }

            OutputData += "target_compile_definitions(" + Module.ModuleName + " PRIVATE " + ListStringDefines(Module.PreProcessorDefines) + ")\n";
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
        public void RunCmake()
        {
            const string Vs17Args = "\"Visual Studio 15 2017 Win64\"";
            const string Vs15Args = "\"Visual Studio 14 2015 Win64\"";
            string CmakeArgs = "-G  " + (UseVs17 ? Vs17Args : Vs15Args) + " \"" + ModuleDefManager.GetSourcePath() + "\"";

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            if (FindLocalCmake())
            {
                startInfo.FileName = CmakeLocalPath;
            }
            else
            {
                startInfo.FileName = "cmake";
            }
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
            Console.WriteLine("Exitcode: " + process.ExitCode);
        }
    }
}
