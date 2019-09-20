using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    class PlatformDefinition
    {
        public PlatformDefinition() { }
        public PlatformDefinition(string name, List<string> defines = null)
        {
            Name = name;
            if (defines != null)
            {
                Defines.AddRange(defines);
            }
            SystemVersion = ModuleDefManager.TargetRulesObject.GetWinSDKVer();
        }
        public string Name = "";
        public List<string> Defines = new List<string>();

        public string SystemType = "windows";
        public string SystemVersion = "";
        public static List<PlatformDefinition> GetDefaultPlatforms()
        {
            List<PlatformDefinition> Defs = new List<PlatformDefinition>();
            Defs.Add(new PlatformDefinition("Win64", new List<string>() { "PLATFROM_WINDOWS" }));
            Defs.Add(new PlatformDefinition("Win64_DX12", new List<string>() { "PLATFROM_WINDOWS", "SINGLERHI_DX12", "ALLOW_SINGLE_RHI" }));
            Defs.Add(new PlatformDefinition("Win64_VK", new List<string>() { "PLATFROM_WINDOWS", "SINGLERHI_VK", "ALLOW_SINGLE_RHI" }));

            Defs.Add(new PlatformDefinition("Linux", new List<string>() { "PLATFROM_LINUX" }));
            Defs[Defs.Count - 1].SystemType = "linux";
            return Defs;
        }
    }

}
