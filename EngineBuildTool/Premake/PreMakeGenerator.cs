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
                outputdata += "     architecture \"" + PDef.ProcessorArch + "\"\n";
                outputdata += "     defines{" + StringUtils.ArrayStringQuotesComma(PDef.Defines.ToArray()) + " }\n";
                outputdata += "     systemversion \"" + PDef.SystemVersion + "\"\n";
            }
            PopFilter();
            PushPlatformFilter(PlatformDefinition.Platforms.Windows);
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
            PopFilter();
            AddCustomTargets(Modules, buildConfigs);
            CoreModule.ModuleDepends.Add("HeaderTool");
            AddModule(CoreModule, buildConfigs);
            foreach (ModuleDef m in Modules)
            {
                if (m.IsOutputEXE && m.ModuleOutputType == ModuleDef.ModuleType.EXE)
                {
                    foreach (ModuleDef mn in Modules)
                    {
                        m.ModuleDepends.Add(mn.ModuleName);
                    }
                }
                AddModule(m, buildConfigs);
            }

            File.WriteAllText(DefinitionFile, outputdata);
        }
        void PushPlatformFilter(PlatformDefinition.Platforms Type)
        {
            if (Type == PlatformDefinition.Platforms.Limit)
            {
                PopFilter();
                return;
            }
            outputdata += "filter{\"platforms:" + PlatformDefinition.GetDefinition(Type).Name + "\"}\n";
        }
        void PopFilter()
        {
            outputdata += "filter {}\n";
        }
        void AddModule(ModuleDef m, List<BuildConfig> buildConfigs)
        {
            List<PlatformDefinition> Platforms = PlatformDefinition.GetDefaultPlatforms();
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
            outputdata += "     flags {\"NoImportLib\"}\n";

            PushPlatformFilter(PlatformDefinition.Platforms.Windows);
            outputdata += "     buildoptions  {\"/bigobj\" }\n";
            PopFilter();
            PushPlatformFilter(PlatformDefinition.Platforms.Android);
            outputdata += "     buildoptions  {\"-frtti -fexceptions\" }\n";
            outputdata += "     cppdialect \"C++14\"\n";
            PopFilter();

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
            if (m.ModuleDepends.Count > 0)
            {
                outputdata += "dependson {" + StringUtils.ArrayStringQuotesComma(m.ModuleDepends.ToArray()) + "}\n";
            }
           
            foreach (PlatformDefinition PD in Platforms)
            {
                if (PD == null)
                {
                    continue;
                }
                foreach (BuildConfig Bc in buildConfigs)
                {
                    string Links = CreateLibs(m, Bc, PD);
                    if (Links.Length > 0)
                    {
                        PushPlatformFilter(PD.Type);
                        outputdata += "  filter{\"configurations:" + Bc.Name + "\"}\n";
                        outputdata += "          links { " + Links + "}\n";
                        string OutputDir = StringUtils.SanitizePath(ModuleDefManager.GetBinPath()) + "/" + Bc.Name;
                        outputdata += "          targetdir  (\"" + OutputDir + "\")\n";
                        if (m.OutputObjectName.Length != 0)
                        {
                            outputdata += "          targetname \"" + m.OutputObjectName + "\"\n";
                        }
                        PopFilter();
                    }
                }
            }
            PopFilter();

        }
        string CreateLibs(ModuleDef m, BuildConfig BC, PlatformDefinition PD)
        {
            string linkout = "";
            if (m.UnsupportedPlatforms.Contains(PD.Name))
            {
                return "";
            }
            string DllOut = "";
            if (m != ModuleDefManager.CoreModule)
            {
                DllOut += "\"" + ModuleDefManager.CoreModule.ModuleName + "\", ";
            }
            List<LibRef> AllLibs = new List<LibRef>();
          
            AllLibs.AddRange(m.ModuleLibs);
            if (m != ModuleDefManager.CoreModule)
            {
                LibRef r = new LibRef();
                r.TargetPlatform = PlatformDefinition.Platforms.Limit;
                string OutputDir = StringUtils.SanitizePath(ModuleDefManager.GetBinPath()) + "/" + BC.Name;
                r.Path = OutputDir + "/Core.lib";
                AllLibs.Add(r);
                AllLibs.AddRange(ModuleDefManager.CoreModule.ModuleLibs);
            }

            foreach (LibRef r in AllLibs)
            {
                if (r.TargetPlatform != PlatformDefinition.Platforms.Limit && r.TargetPlatform != PD.Type)
                {
                    continue;
                }
                if (r.BuildCFg == BC.GetLibType() || r.BuildCFg == LibBuildConfig.General)
                {
                    DllOut += "\"" + r.Path + "\", ";
                }
            }

            if (m.LaunguageType == ModuleDef.ProjectType.CSharp || m.LaunguageType == ModuleDef.ProjectType.ManagedCPP)
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
            // outputdata += "buildoutputs {  '" + StringUtils.SanitizePath(ModuleDefManager.GetIntermediateDir() + "\\Generated\\Core\\Core\\Components\\LightComponent.generated.h") + "' }\n";

        }
    }
}
