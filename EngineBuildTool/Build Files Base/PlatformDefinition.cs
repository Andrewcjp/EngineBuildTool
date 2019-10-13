using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    public class PlatformDefinition
    {
        public enum Platforms
        {
            Windows,
            Windows_DX12,
            Windows_VK,
            Linux,
            Android,
            Limit
        }
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
        public string ProcessorArch = "x64";
        public static List<PlatformDefinition> GetDefaultPlatforms()
        {
            List<PlatformDefinition> Defs = new List<PlatformDefinition>();
            foreach (PlatformDefinition d in Definitions)
            {
                if (d == null)
                {
                    continue;
                }
                Defs.Add(d);
            }
            return Defs;
        }
        public static void Init()
        {
            Definitions[(int)Platforms.Windows] = new PlatformDefinition("Win64", new List<string>() { "PLATFORM_WINDOWS" });
#if false
            Defs.Add(new PlatformDefinition("Win64_DX12", new List<string>() { "PLATFORM_WINDOWS", "SINGLERHI_DX12", "ALLOW_SINGLE_RHI" }));
            Defs.Add(new PlatformDefinition("Win64_VK", new List<string>() { "PLATFORM_WINDOWS", "SINGLERHI_VK", "ALLOW_SINGLE_RHI" }));
#endif
            Definitions[(int)Platforms.Linux] = new PlatformDefinition("Linux", new List<string>() { "PLATFORM_LINUX" });
            Definitions[(int)Platforms.Linux].SystemType = "linux";

            Definitions[(int)Platforms.Android] = new PlatformDefinition("Android", new List<string>() { "PLATFORM_ANDROID", "SINGLERHI_VK", "ALLOW_SINGLE_RHI" });
            Definitions[(int)Platforms.Android].SystemType = "android";
            Definitions[(int)Platforms.Android].ProcessorArch = "ARM";
        }
        void PrintPlatforms()
        {
            foreach (PlatformDefinition d in Definitions)
            {
                if (d == null)
                {
                    continue;
                }
                Console.WriteLine("Found platform " + d.Name);
            }
        }
        static PlatformDefinition[] Definitions = new PlatformDefinition[(int)Platforms.Limit];

        public static PlatformDefinition GetDefinition(Platforms type)
        {
            return Definitions[(int)type];
        }
    }

}
