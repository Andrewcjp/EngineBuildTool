using System;
using System.Linq;

namespace EngineBuildTool
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = args[i].ToLower();
            }
            ModuleDefManager manager = new ModuleDefManager();
            if (args.Length > 0)
            {
                if (!args[0].Contains("-"))
                {
                    manager.TargetRulesName = args[0];
                }

                if (args.Contains("-vs15"))
                {
                    CmakeGenerator.UseVs17 = false;
                }
                else if (args.Contains("-vs17"))
                {
                    CmakeGenerator.UseVs17 = true;
                }
                else
                {
                    CmakeGenerator.UseVs17 = FileUtils.FindVSVersion();
                }
                if (args.Contains("-nounity"))
                {
                    CmakeGenerator.AllowUnityBuild = false;
                }
            }
            else
            {
                CmakeGenerator.UseVs17 = FileUtils.FindVSVersion();
            }
            Console.WriteLine("Using Visual Studio " + (CmakeGenerator.UseVs17 ? "2017" : "2015"));
            if (args.Contains("-clean"))
            {
                manager.Clean();
            }
            if (!args.Contains("-nogen"))
            {
                manager.Run();
            }

            Console.WriteLine("Press any key....");
            Console.ReadKey();
        }

    }
}
