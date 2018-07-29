using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    public enum LibBuildConfig { Debug, Optimized, General }
    public class LibSearchPath
    {
        public LibSearchPath(string p, LibBuildConfig conf)
        {
            Path = p;
            LibBuildConfig = conf;
        }
        public string Path = "";
        public LibBuildConfig LibBuildConfig = LibBuildConfig.General;
    }
    public class TargetRules
    {
        public TargetRules()
        {

        }
        public virtual ModuleDef GetCoreModule()
        {
            return null;
        }
        public List<LibSearchPath> LibSearchPaths = new List<LibSearchPath>();
    }
}
