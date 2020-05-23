using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    public enum LibBuildConfig { Debug, Optimized, General }
    public class LibSearchPath
    {
        public LibSearchPath(string p, LibBuildConfig conf, bool IsDll = false, bool IsABS = false)
        {
            Path = p;
            LibBuildConfig = conf;
            IsLibaryDll = IsDll;
            ISABS = IsABS;
        }
        public bool ISABS = false;
        public bool IsLibaryDll = false;
        public string Path = "";
        public LibBuildConfig LibBuildConfig = LibBuildConfig.General;
        public bool IsValidForBuild(BuildConfiguration.BuildType bc)
        {
            if (bc == BuildConfiguration.BuildType.Debug)
            {
                return (LibBuildConfig != LibBuildConfig.Optimized);
            }
            if (bc == BuildConfiguration.BuildType.Release)
            {
                return (LibBuildConfig != LibBuildConfig.Debug);
            }
            return false;
        }

        public List<string> GetFiles()
        {
            List<string> files = null;
            if (IsLibaryDll)
            {
                if (ISABS)
                {
                    files = FileUtils.GetFilePaths(Path, "*.dll", true, SearchOption.AllDirectories);
                }
                else
                {
                    files = FileUtils.GetFilePaths(ModuleDefManager.GetStaticLibPath() + Path, "*.dll", true, SearchOption.TopDirectoryOnly);
                }
            }
            else
            {
                if (ISABS)
                {
                    files = FileUtils.GetFilePaths(Path, "*.lib", true, SearchOption.AllDirectories);
                }
                else
                {
                    files = FileUtils.GetFilePaths(ModuleDefManager.GetStaticLibPath() + Path, "*.lib", true, SearchOption.TopDirectoryOnly);
                }

            }
            return files;
        }

    }

    public class TargetRules
    {
        public List<string> ModuleExcludeList = new List<string>();
        public TargetRules()
        {

        }
        public virtual ModuleDef GetCoreModule()
        {
            return null;
        }
        public List<LibSearchPath> LibSearchPaths = new List<LibSearchPath>();
        public List<string> GlobalDefines = new List<string>();
        //windows platform
        public string WindowTenVersionTarget = "1903";
        string WindowsApiVersion = "";
        public string GetWinSDKVer()
        {
            return WindowsApiVersion;
        }
        public void Resolve()
        {
            int VersionNum = 0;
            if (!int.TryParse(WindowTenVersionTarget, out VersionNum))
            {
                //default!
                WindowTenVersionTarget = "1903";
                VersionNum = 1903;
            }
            SettingCache.SetWinSdk(VersionNum);
            if (WindowsApiVersion.Length == 0)
            {
                WindowsApiVersion = PlatformData.GetWinSdkVersion(WindowTenVersionTarget);
            }
            if (Win_SupportsRT())
            {
                GlobalDefines.Add("PLATFORM_RT");
            }
        }
        public bool Win_SupportsRT()
        {
            int VersionNum = 0;
            int.TryParse(WindowTenVersionTarget, out VersionNum);
            return (VersionNum >= 1809);
        }
    }
}