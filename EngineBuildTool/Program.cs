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
            if(args.Length > 0)
            {
                if (!args[0].Contains("-"))
                {
                    manager.TargetRulesName = args[0];
                }
            }
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
