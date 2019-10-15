using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace EngineBuildTool
{
    public class VisualStudioProjectEditor
    {
        public static bool AllowUnityBuild = true;

        public static bool CanModuleUnity(ModuleDef md)
        {
            if (!md.UseUnity || !AllowUnityBuild /*|| !UseVs17*/)
            {
                return false;
            }
            return true;
        }

        public static void EnableUnityBuild(ModuleDef md)
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
        public static void SetTargetOutput(string Targetname, string outdir, string TargetNamestr, string config)
        {

            string VxprojPath = ModuleDefManager.GetIntermediateDir() + "\\" + Targetname + ".vcxproj";
            if (!File.Exists(VxprojPath))
            {
                Console.WriteLine("Error: No project file!");
                return;
            }
            XmlDocument doc = new XmlDocument();
            doc.Load(VxprojPath);
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("a", "http://schemas.microsoft.com/developer/msbuild/2003");
            //  string Conditionstr = "'$(Configuration)|$(Platform)'=='" + config + "Win64|x64'";
            string Conditionstr = "'$(Configuration)|$(Platform)'=='" + config + "|x64'";
            string Querry = "//a:PropertyGroup[@Condition=\"" + Conditionstr + "\"]";
            XmlNode target = doc.SelectSingleNode(Querry, nsmgr);
            if (target == null)
            {
                XmlNode newnode = doc.CreateElement("PropertyGroup", doc.DocumentElement.NamespaceURI);
                doc.DocumentElement.InsertAfter(newnode, doc.DocumentElement.FirstChild);
                XmlAttribute attrib = doc.CreateAttribute("Condition");
                attrib.Value = Conditionstr;
                newnode.Attributes.Append(attrib);
                target = newnode;
            }

            XmlNode OutDir = doc.CreateElement("OutDir", doc.DocumentElement.NamespaceURI);
            OutDir.InnerText = outdir;
            target.AppendChild(OutDir);
            XmlNode TargetName = doc.CreateElement("TargetName", doc.DocumentElement.NamespaceURI);
            TargetName.InnerText = TargetNamestr;
            target.AppendChild(TargetName);

            doc.Save(VxprojPath);
        }
        public static void ProcessFile(ModuleDef md)
        {
            string VxprojPath = ModuleDefManager.GetIntermediateDir() + "\\" + md.ModuleName + ".vcxproj";
            if (!File.Exists(VxprojPath))
            {
                Console.WriteLine("Error: No project file!");
                return;
            }
            XmlDocument doc = new XmlDocument();
            doc.Load(VxprojPath);
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("a", "http://schemas.microsoft.com/developer/msbuild/2003");
            PostProcessStepAddNuGet(doc, nsmgr, md);

            doc.Save(VxprojPath);
        }
        public static void ProcessExpections(XmlDocument doc, XmlNamespaceManager nsmgr, ModuleDef md)
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
                string parsed = StringUtils.SanitizePathToDoubleBack(s);
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

        public static void ProcessNuGetPacks(ModuleDef M)
        {
            if (M.NuGetPackages.Count == 0)
            {
                return;
            }
            // OutputData += "find_program(NUGET nuget)\nif (NOT NUGET) \n   message(FATAL \"CMake could not find the nuget command line tool. Please install it!\")\nendif()\n";
            foreach (string Pack in M.NuGetPackages)
            {
                //M.ModuleSourceFiles
                //OutputData += "execute_process(COMMAND NUGET install " + Pack + " -OutputDirectory " + SanitizePath(ModuleDefManager.GetIntermediateDir() + "\\packages")+
                //    "  \n WORKING_DIRECTORY " + SanitizePath(ModuleDefManager.GetRootPath() + "/Scripts/") + " )\n";
            }
        }
        public static void PostProcessStepAddNuGet(XmlDocument doc, XmlNamespaceManager nsmgr, ModuleDef md)
        {
            if (md.NuGetPackages.Count == 0)
            {
                return;
            }
            foreach (string Pack in md.NuGetPackages)
            {
                //  string data = "<Import Project=\"packages\\WinPixEventRuntime.1.0.190604001\\build\\WinPixEventRuntime.targets\" Condition=\"Exists('packages\\WinPixEventRuntime.1.0.190604001\\build\\WinPixEventRuntime.targets')\" />";
                XmlNodeList cl = doc.SelectNodes("//a:ImportGroup", nsmgr);
                //  foreach (XmlNode nn in cl)
                {
                    XmlNode nn = cl[cl.Count - 1];
                    XmlNode value = doc.CreateNode(XmlNodeType.Element, "Import", doc.DocumentElement.NamespaceURI);
                    XmlAttribute A = doc.CreateAttribute("Project");
                    A.Value = "packages\\WinPixEventRuntime.1.0.190604001\\build\\WinPixEventRuntime.targets";// Condition=\"Exists('packages\\WinPixEventRuntime.1.0.190604001\\build\\WinPixEventRuntime.targets')\";
                    value.Attributes.Append(A);
                    XmlAttribute B = doc.CreateAttribute("Condition");
                    B.Value = "Exists('packages\\WinPixEventRuntime.1.0.190604001\\build\\WinPixEventRuntime.targets')";
                    value.Attributes.Append(B);
                    //  value.InnerText = "true";//<IncludeInUnityFile>true</IncludeInUnityFile>
                    nn.AppendChild(value);

                }
            }
        }
        public static void ReplaceAllModule(ModuleDef md, string target, string replacement)
        {
            string VxprojPath = ModuleDefManager.GetIntermediateDir() + "\\" + md.ModuleName + ".vcxproj";
            if (!File.Exists(VxprojPath))
            {
                Console.WriteLine("Error: No project file!");
                return;
            }
            ReplaceAll(VxprojPath, target, replacement);
        }
        public static void ReplaceAll(string VxprojPath, string target, string replacement)
        {
            string[] lines = File.ReadAllLines(VxprojPath);
            bool changed = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(target))
                {
                    lines[i] = lines[i].Replace(target, replacement);
                    changed = true;
                }
            }
            if (changed)
            {
                File.WriteAllLines(VxprojPath, lines);
            }
        }
    }
}
