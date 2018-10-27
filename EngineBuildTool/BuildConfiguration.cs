using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    public class BuildConfiguration
    {
        public enum BuildType
        {
            Debug,
            Profile,
            Release,
        };
        public enum PackageType
        {
            Editor,
            Package,
            ShippingPackage
        };

        public static List<BuildConfig> GetDefaultConfigs()
        {
            List<BuildConfig> Configs = new List<BuildConfig>();
            Configs.Add(new BuildConfig("Debug", BuildType.Debug, PackageType.Editor));
            Configs.Add(new BuildConfig("Release", BuildType.Release, PackageType.Editor));
            Configs.Add(new BuildConfig("DebugPackage", BuildType.Debug, PackageType.Package));
            Configs.Add(new BuildConfig("ReleasePackage", BuildType.Release, PackageType.Package));
            Configs.Add(new BuildConfig("ShippingDebugPackage", BuildType.Debug, PackageType.ShippingPackage));
            Configs.Add(new BuildConfig("ShippingReleasePackage", BuildType.Release, PackageType.ShippingPackage));
            return Configs;
        }
    }
    public struct BuildConfig
    {
        public BuildConfig(string name, BuildConfiguration.BuildType type, BuildConfiguration.PackageType packageType, string[] inDefines = null)
        {
            Name = name;
            CurrentType = type;
            Defines = new List<string>();
            CurrentPackageType = packageType;
            if (packageType == BuildConfiguration.PackageType.Package)
            {
                Defines.Add("BUILD_GAME");
            }
            else if (packageType == BuildConfiguration.PackageType.ShippingPackage)
            {
                Defines.Add("BUILD_GAME");
                Defines.Add("BUILD_SHIP");
            }
            if (inDefines != null)
            {
                Defines.AddRange(inDefines);
            }
        }
        public string Name;
        public BuildConfiguration.BuildType CurrentType;
        public BuildConfiguration.PackageType CurrentPackageType;
        public List<string> Defines;
    }
}
