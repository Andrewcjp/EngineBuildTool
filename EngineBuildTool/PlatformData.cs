using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    class PlatformData
    {
        public static string GetWinSdkVersion(string ver)
        {
            switch (ver)
            {
                case "1903":
                    return "10.0.18362.0";
                case "1809":
                    return "10.0.17763.0";
                case "1803": 
                    return "10.0.17134.0";
            }
            return "10.0.18362.0";
        }
    }
}
