using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EngineBuildTool
{
    [System.Serializable]
    public struct SettingData
    {
        [XmlAttribute]
        public int WindowsSDKVer;
    }
    class SettingCache
    {
        public SettingData Dataset;
        static SettingCache Instance;
        bool IsCmakeCacheValid = true;
        public static void Save()
        {
            XmlSerializer ser = new XmlSerializer(typeof(SettingData));

            TextWriter writer = new StreamWriter(GetSettingFilePath());
            ser.Serialize(writer, Instance.Dataset);
            writer.Close();
        }
        public static void InvalidateCache()
        {
            Instance.IsCmakeCacheValid = false;
        }
        public static void Load()
        {
            Instance = new SettingCache();
            if (File.Exists(GetSettingFilePath()))
            {
                
                // Construct an instance of the XmlSerializer with the type  
                // of object that is being deserialized.  
                XmlSerializer mySerializer =
                new XmlSerializer(typeof(SettingData));
                // To read the file, create a FileStream.  
                FileStream myFileStream = new FileStream(GetSettingFilePath(), FileMode.Open);
                // Call the Deserialize method and cast to the object type.  
                Instance.Dataset = (SettingData)
                mySerializer.Deserialize(myFileStream);
                Console.WriteLine("Settings Cache Loaded");
            }
            else
            {
                Instance.Dataset = new SettingData();
            }
        }
        public static bool IsCacheValid()
        {
            return Instance.IsCmakeCacheValid;
        }
        public static SettingCache Get()
        {
            return Instance;
        }
        static string GetSettingFilePath()
        {
            return ModuleDefManager.GetIntermediateDir() + "\\SettingCache.xml";
        }
        public static void SetWinSdk(int newver)
        {
            if(newver != Instance.Dataset.WindowsSDKVer)
            {
                Console.WriteLine("Windows SDK version changed invalidating cache");
                InvalidateCache();
            }
            Instance.Dataset.WindowsSDKVer = newver;
        }
    }
}
