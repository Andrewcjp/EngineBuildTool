using System;
using System.Linq;

namespace EngineBuildTool
{
    class Program
    {
        static void Main(string[] args)
        {
            ModuleDefManager.USEPREMAKE = true;
            System.Diagnostics.Stopwatch Time = new System.Diagnostics.Stopwatch();
            Time.Start();
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
                if (args.Contains("-vs19"))
                {
                    PreMakeGenerator.Use2019 = true;
                }
                else
                {
                    PreMakeGenerator.Use2019 = false;// FileUtils.FindVSVersion();     
                }
                if (args.Contains("-nounity"))
                {
                    VisualStudioProjectEditor.AllowUnityBuild = false;
                }
                if (args.Contains("-premake"))
                {
                    ModuleDefManager.USEPREMAKE = true;
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
            Time.Stop();
            Console.WriteLine("Build tool finished in " + Time.ElapsedMilliseconds + "ms ");
            if (Console.IsOutputRedirected)
            {
                return;
            }
            Console.WriteLine("Closing In 5s Press any key to abort");
            Time.Restart();
            float Starttime = 5; 
            while (Time.ElapsedMilliseconds < (Starttime * 1000))
            {                
                if (Console.KeyAvailable) 
                {
                    Console.WriteLine("\nAborted! Press Any Key To exit");
                    Console.ReadKey(true);
                    Console.ReadKey(true);
                    break;
                }
                Console.Write("\rClosing In " + (Starttime - Time.Elapsed.Seconds) + "s ");
            }
            Time.Stop();
        }

    }
}
