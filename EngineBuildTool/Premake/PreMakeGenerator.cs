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
        public static bool Use2019 = false;
        public override void ClearCache()
        {

        }
        public PreMakeGenerator()
        {
            DefinitionFile = StringUtils.SanitizePath(ModuleDefManager.GetSourcePath() + "/premake.lua");
        }


        public override void GenerateList(List<ModuleDef> Modules, ModuleDef CoreModule, List<BuildConfig> buildConfigs)
        {
            Console.WriteLine("Running Premake");
            ModuleDefManager.Instance.PatchPremakeFileHeader(ref outputdata);

            outputdata += "workspace 'Engine'\n";
            outputdata += " location \"" + StringUtils.SanitizePath(ModuleDefManager.GetIntermediateDir()) + "\"\n";
            string OutputDir = StringUtils.SanitizePath(ModuleDefManager.GetBinPath());
            outputdata += "     targetdir  \"" + OutputDir + "\"\n";
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
            PushPlatformFilter(PlatformDefinition.WindowsID);
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
        public override bool PushPlatformFilter(PlatformID[] Types, string extra = "")
        {
            string Platforms = "";

            for (int i = 0; i < Types.Length; i++)
            {

                if (Types[i] == PlatformID.Invalid)
                {
                    PopFilter();
                    return false;
                }
                if (PlatformDefinition.GetDefinition(Types[i]) == null)
                {
                    continue;
                }
                Platforms += PlatformDefinition.GetDefinition(Types[i]).Name;
                if (Types.Length > 1 && i != (Types.Length - 1))
                {
                    Platforms += " or ";
                }
            }
            outputdata += "filter{";
            if (extra.Length > 0)
            {
                outputdata += "\"" + extra + "\",";
            }
            outputdata += "\"platforms:" + Platforms;
            outputdata += "\"}\n";
            return true;
        }

        public override bool PushPlatformFilter(PlatformID Type, string extra = "")
        {
            if (Type == PlatformID.Invalid)
            {
                PopFilter();
                return false;
            }
            if (PlatformDefinition.GetDefinition(Type) == null)
            {
                return false;
            }
            outputdata += "filter{\"platforms:" + PlatformDefinition.GetDefinition(Type).Name + "\"}\n";
            return true;
        }
        public override bool PushFilter(PlatformID Type, string BuildType)
        {
            if (Type == PlatformID.Invalid)
            {
                PopFilter();
                return false;
            }
            if (PlatformDefinition.GetDefinition(Type) == null)
            {
                return false;
            }
            outputdata += "filter{\"platforms:" + PlatformDefinition.GetDefinition(Type).Name + "\", " + BuildType + "}\n";
            return true;
        }
        bool IsValid(PlatformID type)
        {
            return PlatformDefinition.GetDefinition(type) != null;
        }

        public override void PopFilter()
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
            string ALLFiles = AllSourceFiles + ", " + ExtraSourceFiles;

            outputdata += "\n--Begin Module " + m.ModuleName + "\n";
            outputdata += "group \"" + StringUtils.SanitizePath(m.SolutionFolderPath) + "\"\n";
            outputdata += "project '" + m.ModuleName + "' \n";
            if (m.ModuleOutputType == ModuleDef.ModuleType.EXE)
            {
                if (m.UseConsoleSubSystem)
                {
                    outputdata += "     kind \"ConsoleApp\"\n";
                }
                else
                {
                    outputdata += "     kind \"WindowedApp\"\n";
                }
            }
            else if (m.ModuleOutputType == ModuleDef.ModuleType.LIB)
            {
                outputdata += "     kind \"StaticLib\"\n";
            }
            else
            {
                outputdata += "     kind \"SharedLib\"\n";
            }

            outputdata += "     language \"" + ConvertLanguage(m) + "\"\n";
            outputdata += "     flags {\"NoImportLib\"}\n";
            outputdata += "     editandcontinue \"Off\" \n";
            outputdata += "     cppdialect \"C++17\"\n";

            PushPlatformFilter(PlatformDefinition.WindowsID);
            outputdata += "     buildoptions  {\"/bigobj\"}\n";
            outputdata += "     flags {\"NoImportLib\", \"MultiProcessorCompile\"}\n";
            PopFilter();


            ModuleDefManager.Instance.OnPreMakeWriteModule(m, ref outputdata);

            if (PushPlatformFilter(PlatformDefinition.AndroidID))
            {
                outputdata += "     buildoptions  {\"-frtti -fexceptions\" }\n";
                outputdata += "     cppdialect \"C++14\"\n";
                PopFilter();
            }

            if (m.PCH.Length > 0)
            {
                outputdata += "     pchheader \"" + m.PCH + ".h\"\n";
                outputdata += "     pchsource (\"" + StringUtils.SanitizePath(ModuleDefManager.GetSourcePath() + "/" + m.SourceFileSearchDir) + "/" + m.PCH + ".cpp\")\n";
                outputdata += "     forceincludes {\"" + m.PCH + ".h\"} \n";
            }
            outputdata += "     files {" + ALLFiles + "}\n";
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
            if (m.ExcludedFolders.Count > 0)
            {
                outputdata += "removefiles { " + StringUtils.ArrayStringQuotesComma(m.ExcludedFolders.ToArray()) + " }\n";
            } 
            if (m.ExcludeConfigs.Count > 0)
            {
                outputdata += "removeconfigurations{" + StringUtils.ArrayStringQuotesComma(m.ExcludeConfigs.ToArray()) + "};\n";
            }
            if (m.ExcludedFoldersNew.Count > 0)
            {
                foreach (FolderPlatformPair p in m.ExcludedFoldersNew)
                {
                    PushPlatformFilter(p.Platforms.ToArray(), "files:" + p.FolderName);
                    outputdata += "flags{ \"ExcludeFromBuild\" }\n ";
                    PopFilter();
                }
            }
            List<PlatformID> MergePlatoforms = new List<PlatformID>();
            foreach (PlatformDefinition PD in Platforms)
            {
                if (MergePlatoforms.Contains(PD.TypeId))
                {
                    continue;
                }
                List<PlatformID> AllOthers = new List<PlatformID>();
                PlatformDefinition.TryAddPlatfromsFromString("!" + PD.Name, ref AllOthers);
                for (int i = AllOthers.Count - 1; i >= 0; i--)
                {
                    if (PlatformDefinition.GetDefinition(AllOthers[i]) != null && PlatformDefinition.GetDefinition(AllOthers[i]).ExcludedPlatformFolder == PD.ExcludedPlatformFolder)
                    {
                        MergePlatoforms.Add(AllOthers[i]);
                        AllOthers.RemoveAt(i);
                    }
                }

                foreach (PlatformID i in AllOthers)
                {
                    if (PlatformDefinition.GetDefinition(i) != null)
                    {
                        PlatformID[] d = { i };
                        PushPlatformFilter(d, "files:" + PD.ExcludedPlatformFolder);
                        outputdata += "flags{\"ExcludeFromBuild\"}\n ";
                        PopFilter();
                    }
                }
            }
            //outputdata += "     filter{\"files:**.*\",\"platforms:Win64\"}\n  flags{\"ExcludeFromBuild\"}\n ";
            outputdata += "     filter{\"files:**.hlsl\"}\n  flags{\"ExcludeFromBuild\"}\n ";
            PopFilter();
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
                        PushFilter(PD.TypeId, "\"configurations:" + Bc.Name + "\"");
                        outputdata += "          links { " + Links + "}\n";
                        string OutputDir = StringUtils.SanitizePath(ModuleDefManager.GetBinPath()) + "/" + PD.Name + "/" + Bc.Name;
                        outputdata += "          targetdir  (\"" + OutputDir + "\")\n";
                        if (m.OutputObjectName.Length != 0)
                        {
                            outputdata += "          targetname \"" + m.OutputObjectName + "\"\n";
                        }
                        List<string> Defines = new List<string>();
                        List<string> LibDirs = new List<string>();
                        Defines.AddRange(m.PreProcessorDefines);
                        Defines.AddRange(Bc.Defines);
                        foreach (ExternalModuleDef ExtraMods in m.ExternalModules)
                        {
                            if (!ExtraMods.UnsupportedPlatformsTypes.Contains(PD.TypeId))
                            {
                                Defines.AddRange(ExtraMods.Defines);
                                LibDirs.AddRange(ExtraMods.LibDirs);
                            }
                        }
                        outputdata += "     defines{" + StringUtils.ArrayStringQuotesComma(Defines.ToArray()) + " }\n";
                        outputdata += "     libdirs{" + StringUtils.ArrayStringQuotesComma(LibDirs.ToArray()) + " }\n";
                        PopFilter();
                    }
                }
            }
            PopFilter();
            if (m.IsCoreModule)
            {
                outputdata += "prebuildcommands(\"$(MSBuildProjectDirectory)/../Scripts/WriteCommit.bat\")\n";
            }


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
                r.TargetPlatform = PlatformID.Invalid;
                string OutputDir = StringUtils.SanitizePath(ModuleDefManager.GetBinPath()) + "/" + PD.Name + "/" + BC.Name;
                r.Path = OutputDir + "/Core.lib";
                AllLibs.Add(r);
                AllLibs.AddRange(ModuleDefManager.CoreModule.ModuleLibs);
            }
            //new core
            List<LibDependency> StaticLibs = new List<LibDependency>();
            StaticLibs.AddRange(m.StaticLibraries);

            foreach (ExternalModuleDef ExtraMods in m.ExternalModules)
            {
                // if (!ExtraMods.UnsupportedPlatformsTypes.Contains(""))
                {
                    StaticLibs.AddRange(ExtraMods.StaticLibraries);
                }
            }
            foreach (LibDependency dep in StaticLibs)
            {
                if (dep.Platforms.Contains(PD.TypeId))
                {
                    DllOut += "\"" + dep.LibName + "\", ";
                }
            }
            foreach (LibRef r in AllLibs)
            {
                if (r.TargetPlatform != PlatformID.Invalid && r.TargetPlatform != PD.TypeId)
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
            ModuleDefManager.Instance.OnPreMakeAddLibs(m, BC, PD, ref DllOut);
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
            string Args = (Use2019 ? "vs2019" : "vs2017 ") + " --file=\"" + DefinitionFile + "\"";
            int code = -1;
            try
            {
                code = ProcessUtils.RunProcess(premakeExe, Args);
            }
            catch
            { }
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
                VisualStudioProjectEditor.ReplaceAllModule(m, "$(Console_Libs).lib", "$(Console_Libs)");
            }
            VisualStudioProjectEditor.ReplaceAllModule(CoreModule, "$(Console_Libs).lib", "$(Console_Libs)");
            //foreach (BuildConfig bc in ModuleDefManager.CurrentConfigs)
            //{
            //    string path = StringUtils.SanitizePath(ModuleDefManager.GetBinPath()/* + "\\" + PD.Name*/ + "\\" + bc.Name + "\\");
            //    VisualStudioProjectEditor.SetTargetOutput(BuildAllTarget, path, CoreModule.OutputObjectName, bc.Name);
            //}

        }
        void AddCustomTargets(List<ModuleDef> Modules, List<BuildConfig> buildConfigs)
        {
            List<PlatformDefinition> Platforms = PlatformDefinition.GetDefaultPlatforms();
            outputdata += "group \" Build/\"\n";
            outputdata += "project \"HeaderTool\"\n";
            outputdata += "kind (\"Makefile\")\n";

            string headertoolString = StringUtils.SanitizePath(ModuleDefManager.GetSourcePath() + "\\EngineHeaderTool.exe ");
            headertoolString += " -Name " + ModuleDefManager.CoreModule.GameModuleName;
            Console.WriteLine("Game Module is " + ModuleDefManager.CoreModule.GameModuleName);
            outputdata += "buildcommands {\" " + headertoolString + "  \"}\n";
            foreach (PlatformDefinition PD in Platforms)
            {
                if (PD == null)
                {
                    continue;
                }

                foreach (BuildConfig Bc in buildConfigs)
                {
                    if (Bc.CurrentPackageType == BuildConfiguration.PackageType.Package)
                    {
                        PushFilter(PD.TypeId, "\"configurations:" + Bc.Name + "\"");
                        outputdata += "     buildcommands{\"$(MSBuildProjectDirectory)/../Binaries/Win64/Release/StandaloneShaderComplier.exe " + PD.Name + "\"}\n";
                    }
                }
            }
            PopFilter();
            // outputdata += "buildoutputs {  '" + StringUtils.SanitizePath(ModuleDefManager.GetIntermediate Dir() + "\\Generated\\Core\\Core\\Components\\LightComponent.generated.h") + "' }\n";

        }
    }
}
