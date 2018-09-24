using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Runtime.Serialization;

namespace EngineBuildTool
{
    class Program
    {
        static void Main(string[] args)
        {
            ModuleDefManager manager = new ModuleDefManager();
            if (args.Length > 0)
            {
                if (!args[0].Contains("-"))
                {
                    manager.TargetRulesName = args[0];
                }
            }
            if (args.Contains("-VS15"))
            {
                CmakeGenerator.UseVs17 = false;
            }
            else if (args.Contains("-VS17"))
            {
                CmakeGenerator.UseVs17 = true;
            }
            else
            {
                CmakeGenerator.UseVs17 = FileUtils.FindVSVersion();
            }
            Console.WriteLine("Using Visual Studio " + (CmakeGenerator.UseVs17 ? "2017" : "2015"));
            if (args.Contains("-Clean"))
            {
                manager.Clean();
            }
            if (!args.Contains("-NoGen"))
            {
                manager.Run();
            }

            Console.WriteLine("Press any key....");
            Console.ReadKey();
        }

    }
}
