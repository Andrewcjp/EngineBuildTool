using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace EngineBuildTool
{
    class CmakeGenerator
    {
        const bool EnableFastLink = true;
        string OutputData = "";
        public static string SanitizePath(string input)
        {
            return input.Replace("\\", "/"); ;
        }
        public static string SanitizePathToDoubleBack(string input)
        {
            return input.Replace("/", "\\");
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
        const string SDKVersion = "10.0.17763.0";
        void GenHeader(List<BuildConfig> buildConfigs)
        {
            OutputData += "cmake_minimum_required (VERSION 3.12.1)\n";
            OutputData += "set(CMAKE_SYSTEM_VERSION " + SDKVersion + " CACHE TYPE INTERNAL FORCE)\n";
            OutputData += "message(\"Detected CMAKE_SYSTEM_VERSION = '${CMAKE_SYSTEM_VERSION}'\")\n";
            OutputData += "set_property(GLOBAL PROPERTY USE_FOLDERS ON)\n";
            OutputData += "Project(" + "Engine" + ")\n";
            OutputData += "set(CMAKE_SYSTEM_VERSION " + SDKVersion + " CACHE TYPE INTERNAL FORCE)\n";
            string OutputDir = SanitizePath(ModuleDefManager.GetBinPath());
            OutputData += "set(CMAKE_RUNTIME_OUTPUT_DIRECTORY \"" + OutputDir + "\")\n";
            OutputData += "set(CMAKE_LIBRARY_OUTPUT_DIRECTORY  \"" + OutputDir + "\")\n";
            OutputData += "set(CMAKE_MODULE_OUTPUT_DIRECTORY  \"" + OutputDir + "\")\n";///NODEFAULTLIB:MSVCRT
            OutputData += "set(CMAKE_EXE_LINKER_FLAGS \"${CMAKE_EXE_LINKER_FLAGS} /SUBSYSTEM:WINDOWS \")\n";
            OutputData += "set(CMAKE_EXE_LINKER_CONSOLE_FLAGS \"${CMAKE_EXE_LINKER_FLAGS} /SUBSYSTEM:CONSOLE  \")\n";
            if (EnableFastLink)
            {
                OutputData += "set(CMAKE_EXE_LINKER_FLAGS_DEBUG \" /INCREMENTAL /debug:fastlink \")\n";
                OutputData += "set(CMAKE_MODULE_LINKER_FLAGS_DEBUG \" /INCREMENTAL /debug:fastlink \")\n";
            }
            OutputData += "set(CMAKE_CONFIGURATION_TYPES" + GetConfigNames(buildConfigs) + ")\n";
            OutputData += "set(CMAKE_SUPPRESS_REGENERATION true)\n";
            OutputData += GetConfigationStrings(buildConfigs);
            OutputData += "add_definitions(/MP)\n";
            OutputData += "add_definitions(-DUNICODE)\nadd_definitions(-D_UNICODE)\nadd_definitions(/sdl)\n";
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
        public void RunPostStep(List<ModuleDef> Modules, ModuleDef CoreModule)
        {
            EnableUnityBuild(CoreModule);
            foreach (ModuleDef m in Modules)
            {
                EnableUnityBuild(m);
            }
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
            string ExtraSourceFiles = ArrayStringQuotes(Module.ModuleExtraFiles.ToArray());
            string ALLFiles = AllSourceFiles + ExtraSourceFiles;
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
                    string OutputDir = SanitizePath(ModuleDefManager.GetBinPath() + "\\Tools\\" + bc.Name + "\\");
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
                OutputData += "target_link_libraries(" + Module.ModuleName + " " + ArrayStringQuotes(Module.ModuleLibs.ToArray()) + ")\n";
            }

            //if (Module.IsGameModule)
            //{
            //    OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES COMPILE_OPTIONS  \"${}\")\n";
            //    OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES COMPILE_OPTIONS  \"${CMAKE_EXE_LINKER_FLAGS_DEBUG}\")\n";
            //    OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES CMAKE_MODULE_LINKER_DEVELOPMENT \"${CMAKE_MODULE_LINKER_FLAGS_DEBUG}\")\n";
            //    OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES CMAKE_SHARED_LINKER_FLAGS_DEVELOPMENT \"${CMAKE_SHARED_LINKER_FLAGS_DEBUG}\")\n";
            //}

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
            OutputData += "source_group(TREE \"" + SanitizePath(ModuleDefManager.GetRootPath()) + "\" FILES " + ALLFiles + ")\n";
            OutputData += "set_source_files_properties(" + ExtraSourceFiles + " PROPERTIES HEADER_FILE_ONLY ON)\n";
            if (Module.UseCorePCH)
            {
                Module.PCH = ModuleDefManager.CoreModule.PCH;
            }
            if (Module.PCH.Length != 0)
            {
                string PCHString = "Source/" + Module.SourceFileSearchDir + "/" + Module.PCH;
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
            if (CanModuleUnity(Module))
            {
                Module.PreProcessorDefines.Add("WITH_UNITY");
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
        public static bool AllowUnityBuild = true;

        public bool CanModuleUnity(ModuleDef md)
        {
            if (!md.UseUnity || !AllowUnityBuild || !UseVs17)
            {
                return false;
            }
            return true;
        }

        void EnableUnityBuild(ModuleDef md)
        {
            if (!CanModuleUnity(md))
            {
                return;
            }
            Console.WriteLine("Experimental VS Unity Build running on module " + md.ModuleName);
            string VxprojPath = ModuleDefManager.GetIntermediateDir() + "\\" + md.ModuleName + ".vcxproj";
            if (!File.Exists(VxprojPath))
            {
                Console.WriteLine("Error: No project file!");
                return;
            }
            XmlDocument doc = new XmlDocument();
            doc.Load(VxprojPath);
            XmlNode target = doc.SelectSingleNode("//EnableUnitySupport");
            if (target == null)
            {
                XmlNode newnode = doc.CreateNode(XmlNodeType.Element, "PropertyGroup", doc.DocumentElement.NamespaceURI);
                doc.DocumentElement.InsertAfter(newnode, doc.DocumentElement.FirstChild);
                XmlNode a = doc.CreateNode(XmlNodeType.Element, "EnableUnitySupport", doc.DocumentElement.NamespaceURI);
                a.InnerText = "true";
                newnode.AppendChild(a);
                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("a", "http://schemas.microsoft.com/developer/msbuild/2003");
                XmlNodeList complies = doc.SelectNodes("//a:ClCompile", nsmgr);
                foreach (XmlNode x in complies)
                {
                    XmlNode value = doc.CreateNode(XmlNodeType.Element, "IncludeInUnityFile", doc.DocumentElement.NamespaceURI);
                    value.InnerText = "true";//<IncludeInUnityFile>true</IncludeInUnityFile>
                    x.AppendChild(value);
                }

                ProcessExpections(doc, nsmgr, md);

                doc.Save(VxprojPath);
            }
        }

        private static void ProcessExpections(XmlDocument doc, XmlNamespaceManager nsmgr, ModuleDef md)
        {
            List<string> Excludes = new List<string>();
            foreach (string path in md.UnityBuildExcludedFolders)
            {
                Excludes.AddRange(FileUtils.GetFilePaths(ModuleDefManager.GetSourcePath() + "\\" + md.ModuleName + "\\" + path, "*.cpp", true));
                Console.WriteLine("Excluded " + path + " from unity build for module " + md.ModuleName);
            }

            XmlNodeList cl = doc.SelectNodes("//a:ItemGroup", nsmgr);
            foreach (string s in Excludes)
            {
                string parsed = SanitizePathToDoubleBack(s);
                XmlNodeList cc = doc.SelectNodes("//a:ClCompile[*]", nsmgr);
                foreach (XmlNode nn in cc)
                {
                    if (nn.Attributes.Count > 0)
                    {
                        if (nn.Attributes[0].Value == parsed)
                        {
                            nn.FirstChild.InnerText = "false";
                        }
                    }
                }
            }
        }
    }
}
