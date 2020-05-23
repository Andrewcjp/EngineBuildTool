using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EngineBuildTool
{

    public class PlatformID
    {
        public static List<PlatformID> Ids = new List<PlatformID>();
        public static int GetIdCount() { return Ids.Count; }
        public static PlatformID Register(string name)
        {
            foreach (PlatformID p in Ids)
            {
                if (p.Name.ToLower() == name.ToLower())
                {
                    return p;
                }
            }
            PlatformID id = new PlatformID();
            id.Value = Ids.Count;
            id.Name = name;
            Ids.Add(id);
            return id;
        }
        public PlatformID() { }
        public string GetName() { return Name; }

        public override bool Equals(object obj)
        {
            PlatformID rhs = (PlatformID)obj;
            if (rhs != null)
            {
                return rhs.Value == Value;
            }
            return false;
        }
        int Value = -1;
        string Name = "";
        public static PlatformID Invalid = new PlatformID();
    }
    public class PlatformDefinition
    {
        public static PlatformID WindowsID = PlatformID.Register("Win64");
        public static PlatformID LinuxID = PlatformID.Register("Linux");
        public static PlatformID AndroidID = PlatformID.Register("Android");

        public PlatformDefinition() { }
        public PlatformDefinition(string name, List<string> defines, PlatformID type)
        {
            Name = name;
            if (defines != null)
            {
                Defines.AddRange(defines);
            }
            SystemVersion = ModuleDefManager.TargetRulesObject.GetWinSDKVer();
            TypeId = type;
        }
        public string Name = "";
        public string DisplayName = "";
        public List<string> Defines = new List<string>();
        //public Platforms Type = Platforms.Limit;
        public PlatformID TypeId = PlatformID.Invalid;
        public string SystemType = "windows";
        public string SystemVersion = "";
        public string ProcessorArch = "x64";
        public string ExcludedPlatformFolder = "**windows/**";
        public static List<PlatformDefinition> GetDefaultPlatforms()
        {
            return Definitions;
        }
        public static void Init(List<PlatformSupportInterface> Interfaces)
        {
            Definitions.Add(new PlatformDefinition("Win64", new List<string>() { "PLATFORM_WINDOWS" }, WindowsID));
#if false
            Defs.Add(new PlatformDefinition("Win64_DX12", new List<string>() { "PLATFORM_WINDOWS", "SINGLERHI_DX12", "ALLOW_SINGLE_RHI" }));
            Defs.Add(new PlatformDefinition("Win64_VK", new List<string>() { "PLATFORM_WINDOWS", "SINGLERHI_VK", "ALLOW_SINGLE_RHI" }));
            Definitions[(int)Platforms.Linux] = new PlatformDefinition("Linux", new List<string>() { "PLATFORM_LINUX" }, Platforms.Limit);
            Definitions[(int)Platforms.Linux].SystemType = "linux";

            Definitions[(int)Platforms.Android] = new PlatformDefinition("Android", new List<string>() { "PLATFORM_ANDROID", "SINGLERHI_VK", "ALLOW_SINGLE_RHI" }, Platforms.Android);
            Definitions[(int)Platforms.Android].SystemType = "android";
            Definitions[(int)Platforms.Android].ProcessorArch = "ARM";
#endif
            foreach (PlatformSupportInterface i in Interfaces)
            {
                i.AddPlatforms(ref Definitions);
            }
            Console.WriteLine("Target Platforms:");
            foreach(PlatformDefinition pd in Definitions)
            {
                Console.WriteLine(pd.Name);
            }
            Console.WriteLine("");
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
        static List<PlatformDefinition> Definitions = new List<PlatformDefinition>();

        public static PlatformDefinition GetDefinition(PlatformID type)
        {
            foreach (PlatformDefinition p in Definitions)
            {
                if (p.TypeId == type)
                {
                    return p;
                }
            }
            return null;
        }
        public static PlatformDefinition GetDefinition(string type)
        {
            foreach (PlatformDefinition p in Definitions)
            {
                if (p.Name.ToLower() == type.ToLower())
                {
                    return p;
                }
            }
            return null;
        }
        public static PlatformID ParseString(string name)
        {
            return PlatformID.Register(name);            
        }
        public static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", ".") + "$";
        }
        public static void TryAddPlatfromsFromString(string data, ref List<PlatformID> list)
        {
            data = data.ToLower();
            bool Flip = data.Contains("!");
            data = data.Replace("!", "");
            Regex reg = new Regex(WildcardToRegex(data));
            
            for (int i = 0; i < PlatformID.GetIdCount(); i++)
            {          
                if (reg.IsMatch(PlatformID.Ids[i].GetName().ToLower()) == !Flip)
                {
                    list.Add(PlatformID.Ids[i]);
                }
            }
        }
    }

}
