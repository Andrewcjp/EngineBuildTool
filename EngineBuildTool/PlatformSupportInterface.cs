using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    public class PlatformSupportInterface
    {
        public virtual void AddPlatforms(ref List<PlatformDefinition> Defs)
        {

        }
        public virtual void OnPreMakeCreateModule(ModuleDef n, ref string PremakeFile, GeneratorBase gen)
        {

        }
        public virtual void OnPreMakeAddLibs(ModuleDef m, BuildConfig BC, PlatformDefinition PD, ref string Dllout)
        {

        }
        public virtual void PatchPremakeFileHeader(ref string file)
        {

        }
    }
}
