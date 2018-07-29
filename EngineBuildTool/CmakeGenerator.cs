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
        void GenHeader()
        {
            OutputData += "cmake_minimum_required (VERSION 3.10)\n";
            OutputData += "set_property(GLOBAL PROPERTY USE_FOLDERS ON)\n";
            OutputData += "Project(" + "Engine" + ")\n";
            string OutputDir = SanitizePath(ModuleDefManager.GetBinPath());
            OutputData += "set(CMAKE_RUNTIME_OUTPUT_DIRECTORY " + OutputDir + ")\n";
            OutputData += "set(CMAKE_LIBRARY_OUTPUT_DIRECTORY  " + OutputDir + ")\n";
            OutputData += "set(CMAKE_MODULE_OUTPUT_DIRECTORY  " + OutputDir + ")\n";
            OutputData += "set(CMAKE_EXE_LINKER_FLAGS \"${CMAKE_EXE_LINKER_FLAGS} /SUBSYSTEM:WINDOWS\")\n";
            //OutputData += "set(CMAKE_CONFIGURATION_TYPES \"Debug; Release; \" CACHE STRING \"\" FORCE)\n";
            //OutputData += "set(CMAKE_C_FLAGS_Release /o2)\n";
        }
        public void GenerateList(List<ModuleDef> Modules, ModuleDef CoreModule)
        {
            GenHeader();
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
            OutputData += "#-------------Module Start " + Module.ModuleName + "----------------\n";
            Module.GatherSourceFiles();
            Module.GatherIncludes();
            string AllSourceFiles = ArrayStringQuotes(Module.ModuleSourceFiles.ToArray());
            if (Module.ModuleOuputType == ModuleDef.ModuleType.DLL)
            {
                OutputData += "add_library( " + Module.ModuleName + " MODULE " + ArrayStringQuotes(Module.ModuleSourceFiles.ToArray()) + ")\n";
            }
            else if (Module.ModuleOuputType == ModuleDef.ModuleType.LIB)
            {
                OutputData += "add_library( " + Module.ModuleName + " STATIC " + ArrayStringQuotes(Module.ModuleSourceFiles.ToArray()) + ")\n";
            }
            else if (Module.ModuleOuputType == ModuleDef.ModuleType.EXE)
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

            if (Module.PCH.Length != 0)
            {
                OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES COMPILE_FLAGS \"/Yu" + Module.PCH + ".h\" )\n";
                OutputData += "add_definitions(/FI\"" + Module.PCH + ".h\")\n";

                OutputData += "SET_SOURCE_FILES_PROPERTIES(\"Core/" + Module.PCH + ".cpp\" COMPILE_FLAGS \"/Yc" + Module.PCH + ".h\" )\n";
            }
            OutputData += "add_definitions(/MP)\n";
            OutputData += "add_definitions(-DUNICODE)\nadd_definitions(-D_UNICODE)\n";
        }

        void WriteToFile(string dir)
        {
            File.WriteAllText(dir + "/CmakeLists.txt", OutputData);
        }

        public void RunCmake()
        {
            string CmakeArgs = "-G \"Visual Studio 15 2017 Win64\"  \"" + ModuleDefManager.GetSourcePath() + "\"";

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmake";
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
