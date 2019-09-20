using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    class CmakeCSharpProject
    {

        public static string GetModule(ModuleDef Module)
        {
            string OutputData = "";
            string AllSourceFiles = StringUtils.ArrayStringQuotes(Module.ModuleSourceFiles.ToArray());
            OutputData += "add_library( " + Module.ModuleName + " SHARED " + AllSourceFiles + ")\n";
            // OutputData += "add_executable( " + Module.ModuleName + " " + AllSourceFiles + ")\n";
            OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES LINKER_LANGUAGE CSharp)\n";
            if (Module.SolutionFolderPath.Length == 0)
            {
                Module.SolutionFolderPath = "Engine/Modules";
            }
            OutputData += "set_target_properties(" + Module.ModuleName + " PROPERTIES FOLDER " + Module.SolutionFolderPath + ")\n";
            if (Module.ModuleDepends.Count > 0)
            {
                OutputData += "add_dependencies(" + Module.ModuleName + " " + StringUtils.ArrayStringQuotes(Module.ModuleDepends.ToArray()) + ")\n";
            }
            OutputData += "set_property(TARGET " + Module.ModuleName + " PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION \"v4.6.1\")\n";
            OutputData += "set_property(TARGET " + Module.ModuleName + " PROPERTY VS_DOTNET_REFERENCES \n  \"System\"  \n  \"System.Core\" \n   \"System.Data\" \"System.Data.DataSetExtensions\" \"System.Windows.Forms\" \"CSharpBridge\"  " + StringUtils.ArrayStringQuotes(Module.NetReferences.ToArray()) + ")\n";
            return OutputData;
        }
    }
}
