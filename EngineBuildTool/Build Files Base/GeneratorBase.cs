using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    public abstract class GeneratorBase
    {
        //for single platform generators
        public PlatformDefinition SingleTargetPlatform;
        public abstract void GenerateList(List<ModuleDef> Modules, ModuleDef CoreModule, List<BuildConfig> buildConfigs);
        public abstract void Execute();
        public abstract void RunPostStep(List<ModuleDef> Modules, ModuleDef CoreModule);
        public abstract void ClearCache();
        public virtual bool PushPlatformFilter(PlatformID Type)
        {
            return true;
        }
        public virtual bool PushPlatformFilter(PlatformID[] Type)
        {
            return true;
        }
        public virtual bool PushFilter(PlatformID Type, string BuildType)
        {
            return true;
        }
        public virtual void PopFilter() { }
    }
}
