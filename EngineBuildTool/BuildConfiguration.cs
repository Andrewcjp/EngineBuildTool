using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    class BuildConfiguration
    {
        public enum BuildType
        {
            Debug,
            Profile,
            Release,
        };


        public static List<BuildConfig> GetDefaultConfigs()
        {
            List<BuildConfig> Configs = new List<BuildConfig>();
            Configs.Add(new BuildConfig("Debug", BuildType.Debug));
            Configs.Add(new BuildConfig("Release", BuildType.Release));
            Configs.Add(new BuildConfig("DebugPackage", BuildType.Debug, new string[] { "BUILD_GAME" }));
            Configs.Add(new BuildConfig("ReleasePackage", BuildType.Release, new string[] { "BUILD_GAME" }));
            Configs.Add(new BuildConfig("ShippingDebugPackage", BuildType.Debug, new string[] { "BUILD_GAME", "BUILD_SHIP" }));
            Configs.Add(new BuildConfig("ShippingReleasePackage", BuildType.Release, new string[] { "BUILD_GAME", "BUILD_SHIP" }));
            return Configs;
        }
    }
    struct BuildConfig
    {

        public BuildConfig(string name, BuildConfiguration.BuildType type, string[] inDefines = null)
        {
            Name = name;
            CurrentType = type;
            Defines = new List<string>();
            if (inDefines != null)
            {
                Defines.AddRange(inDefines);
            }
        }
        public string Name;
        public BuildConfiguration.BuildType CurrentType;
        public List<string> Defines;
    }
}
