using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    class PreMakeGenerator : GeneratorBase
    {
        string DefinitionFile = "";
        string outputdata = "";
        public override void ClearCache()
        {

        }
        public PreMakeGenerator()
        {
            DefinitionFile = StringUtils.SanitizePath(ModuleDefManager.GetSourcePath() + "/premake.lua");
        }


        public override void GenerateList(List<ModuleDef> Modules, ModuleDef CoreModule, List<BuildConfig> buildConfigs)
        {
            outputdata += "workspace 'Engine'\n";
            outputdata += " location \"" + StringUtils.SanitizePath(ModuleDefManager.GetIntermediateDir()) + "\"\n";
            string OutputDir = StringUtils.SanitizePath(ModuleDefManager.GetBinPath());
            outputdata += "     targetdir  \"" + OutputDir + "\"";
            string Configurations = "";
            foreach (BuildConfig B in buildConfigs)
            {
                Configurations += "\"" + B.Name + "\",";
            }
            outputdata += " configurations {" + Configurations + " }\n";


            outputdata += "--Platform Definitions\n";
            List<PlatformDefinition> Platforms = PlatformDefinition.GetDefaultPlatforms();
            string PlatformData = "";
            foreach (PlatformDefinition PDef in Platforms)
            {
                PlatformData += "\"" + PDef.Name + "\",";
            }
            outputdata += " platforms { " + PlatformData + " }\n";


            foreach (PlatformDefinition PDef in Platforms)
            {
                outputdata += "filter{\"platforms:" + PDef.Name + "\"}\n";
                outputdata += "     system \"" + PDef.SystemType + "\"\n";
                outputdata += "     architecture \"x64\"\n";
                outputdata += "     defines{" + StringUtils.ArrayStringQuotesComma(PDef.Defines.ToArray()) + " }\n";
                outputdata += "     systemversion \"" + PDef.SystemVersion + "\"";
            }

            foreach (BuildConfig B in buildConfigs)
            {
                outputdata += "filter{\"configurations:" + B.Name + "\"}\n";
                if (B.CurrentType == BuildConfiguration.BuildType.Debug)
                {
                    outputdata += "     defines { \"DEBUG\" } \n symbols \"On\"\n";
                }
                else if (B.CurrentType == BuildConfiguration.BuildType.Release)
                {
                    outputdata += "     defines { \"NDEBUG\" } \n  optimize   \"On\" \n";
                }
            }

            AddCustomTargets(Modules, buildConfigs);

            AddModule(CoreModule, buildConfigs);
            foreach (ModuleDef m in Modules)
            {
                AddModule(m, buildConfigs);
            }

            File.WriteAllText(DefinitionFile, outputdata);
        }

        void AddModule(ModuleDef m, List<BuildConfig> buildConfigs)
        {
            m.GatherSourceFiles();
            m.GatherIncludes();
            string AllSourceFiles = StringUtils.ArrayStringQuotesComma(m.ModuleSourceFiles.ToArray());
            string ExtraSourceFiles = StringUtils.ArrayStringQuotesComma(m.ModuleExtraFiles.ToArray());
            List<string> ABSSourceFiles = m.ModuleSourceFiles;
            string ALLFiles = StringUtils.RelativeToABS(ABSSourceFiles) + ExtraSourceFiles;

            outputdata += "\n--Begin Module " + m.ModuleName + "\n";
            outputdata += "group \"" + StringUtils.SanitizePath(m.SolutionFolderPath) + "\"\n";
            outputdata += "project '" + m.ModuleName + "' \n";
            if (m.ModuleOutputType == ModuleDef.ModuleType.EXE)
            {
                outputdata += "     kind \"WindowedApp\"\n";
            }
            else if (m.ModuleOutputType == ModuleDef.ModuleType.LIB)
            {
                outputdata += "     kind \"StaticLib\"\n";
            }
            else
            {
                outputdata += "     kind \"SharedLib\"\n";
            }

            outputdata += "     language \"" + ConvertLanguage(m) + "\"";
            outputdata += "     defines{" + StringUtils.ArrayStringQuotesComma(m.PreProcessorDefines.ToArray()) + " }\n";
            outputdata += "     buildoptions  {\"/bigobj\" }\n";
            outputdata += "     flags {\"NoImportLib\"}\n";
            // outputdata += "defaultplatform \"Win64\"\n";
            if (m.PCH.Length > 0)
            {
                outputdata += "     pchheader \"" + m.PCH + ".h\"\n";
                outputdata += "     pchsource (\"" + StringUtils.SanitizePath(ModuleDefManager.GetSourcePath() + "/" + m.SourceFileSearchDir) + "/" + m.PCH + ".cpp\")\n";
                outputdata += "     forceincludes {\"" + m.PCH + ".h\"} \n";
            }
            outputdata += "     files {" + AllSourceFiles + "}\n";
            if (m.PCH.Length > 0)
            {
                outputdata += "     pchsource (\"" + StringUtils.SanitizePath(ModuleDefManager.GetSourcePath() + "/" + m.SourceFileSearchDir) + "/" + m.PCH + ".cpp\")\n";
            }
            List<string> Dirs = new List<string>();
            m.GetIncludeDirs(ref Dirs);
            if (m != ModuleDefManager.CoreModule)
            {
                ModuleDefManager.CoreModule.GetIncludeDirs(ref Dirs);
            }
            outputdata += "     includedirs {" + StringUtils.ArrayStringQuotesComma(Dirs.ToArray()) + "}\n";
            if (m.LaunguageType == ModuleDef.ProjectType.ManagedCPP)
            {
                outputdata += " clr \"on\"\n";
            }
            if (m.UnsupportedPlatforms.Count > 0)
            {
                outputdata += "removeplatforms  { " + StringUtils.ArrayStringQuotesComma(m.UnsupportedPlatforms.ToArray()) + "}\n";
            }
            
            foreach (BuildConfig Bc in buildConfigs)
            {
                string Links = CreateLibs(m, Bc);
                if (Links.Length > 0)
                {
                    outputdata += "  filter{\"configurations:" + Bc.Name + "\"}\n";
                    outputdata += "          links { " + Links + "}\n";
                    string OutputDir = StringUtils.SanitizePath(ModuleDefManager.GetBinPath()) + "/" + Bc.Name;
                    outputdata += "          targetdir  \"" + OutputDir + "\"\n";
                    if (m.OutputObjectName.Length != 0)
                    {
                        outputdata += "          targetname \"" + m.OutputObjectName + "\"";
                    }
                }
            }
        }
        string CreateLibs(ModuleDef m, BuildConfig BC)
        {
            string linkout = "";
            if (m.ModuleDepends.Count > 0)
            {
                linkout = StringUtils.ArrayStringQuotesComma(m.ModuleDepends.ToArray());
            }
            List<LibRef> AllLibs = new List<LibRef>();
            string DllOut = "";
            if (m.LaunguageType != ModuleDef.ProjectType.CSharp)
            {
                AllLibs.AddRange(m.ModuleLibs);
                if (m != ModuleDefManager.CoreModule)
                {
                    LibRef r = new LibRef();
                    string OutputDir = StringUtils.SanitizePath(ModuleDefManager.GetBinPath()) + "/" + BC.Name;
                    r.Path = OutputDir + "/BleedOut.lib";
                    AllLibs.Add(r);
                    AllLibs.AddRange(ModuleDefManager.CoreModule.ModuleLibs);
                }


                foreach (LibRef r in AllLibs)
                {
                    if (r.BuildCFg == BC.GetLibType() || r.BuildCFg == LibBuildConfig.General)
                    {
                        DllOut += "\"" + r.Path + "\", ";
                    }
                }
            }
            else
            {
                foreach (string s in m.NetReferences)
                {
                    DllOut += "\"" + s + "\", ";
                }

            }

            return linkout + DllOut;
        }
        string ConvertLanguage(ModuleDef m)
        {
            if (m.LaunguageType == ModuleDef.ProjectType.CPP || m.LaunguageType == ModuleDef.ProjectType.ManagedCPP)
            {
                return "C++";
            }
            else if (m.LaunguageType == ModuleDef.ProjectType.CSharp)
            {
                return "C#";
            }
            return "";
        }
        public override void Execute()
        {
            string premakeExe = ModuleDefManager.GetRootPath() + "\\Scripts\\premake5.exe";
            string Args = "vs2017 --file=\"" + DefinitionFile + "\"";
            int code = ProcessUtils.RunProcess(premakeExe, Args);

            Console.WriteLine("PreMake finished with Code: " + code);
        }
        string BuildAllTarget = "BuildAll";
        public override void RunPostStep(List<ModuleDef> Modules, ModuleDef CoreModule)
        {
            VisualStudioProjectEditor.EnableUnityBuild(CoreModule);
            foreach (ModuleDef m in Modules)
            {
                VisualStudioProjectEditor.EnableUnityBuild(m);
                VisualStudioProjectEditor.ProcessFile(m);
            }
            foreach (BuildConfig bc in ModuleDefManager.CurrentConfigs)
            {
                string path = StringUtils.SanitizePath(ModuleDefManager.GetBinPath() + "\\" + bc.Name + "\\");
                VisualStudioProjectEditor.SetTargetOutput(BuildAllTarget, path, CoreModule.OutputObjectName, bc.Name);
            }

        }
        void AddCustomTargets(List<ModuleDef> Modules, List<BuildConfig> buildConfigs)
        {
            outputdata += "group \" Build/\"\n";
            outputdata += "project \"HeaderTool\"\n";
            outputdata += "kind (\"Makefile\")\n";

            string headertoolString = StringUtils.SanitizePath(ModuleDefManager.GetSourcePath() + "\\EngineHeaderTool.exe ");
            headertoolString += " -Name " + ModuleDefManager.CoreModule.GameModuleName;
            Console.WriteLine("Game Module is " + ModuleDefManager.CoreModule.GameModuleName);
            outputdata += "buildcommands {\" " + headertoolString + "  \"}\n";
            //outputdata += "buildoutputs {  'test.c' }\n";

            outputdata += "group \"Targets/\"\n";
            outputdata += "project \"" + BuildAllTarget + "\"\n";
            outputdata += "kind (\"ConsoleApp\")\n";
            string Links = "\"" + ModuleDefManager.CoreModule.ModuleName + "\",";
            foreach (ModuleDef m in Modules)
            {
                Links += "\"" + m.ModuleName + "\" ,";
            }
            outputdata += "  links { " + Links + "}\n";

            foreach (BuildConfig Bc in buildConfigs)
            {
                // outputdata += "  filter{\"configurations:" + Bc.Name + "\"}\n";
                string OutputDir = StringUtils.SanitizePath(ModuleDefManager.GetBinPath()) + "/" + Bc.Name;
                outputdata += "          targetdir  \"" + OutputDir + "\"\n";
                //outputdata += "          targetname \"" + ModuleDefManager.CoreModule.OutputObjectName + "\"";
                //outputdata += "          targetextension  \"" + ".exe" + "\"\n";

            }

        }
    }
}
