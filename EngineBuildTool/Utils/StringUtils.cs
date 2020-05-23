using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineBuildTool
{
    class StringUtils
    {
        public static string SanitizePath(string input)
        {
            input = input.Replace("\\", "/");
            input = input.Replace("//", "/");
            return input;
        }
        public static string SanitizePathToDoubleBack(string input)
        {
            return input.Replace("/", "\\");
        }
        public static string ConvertStringArrayToStringJoin(string[] array)
        {
            // Use string Join to concatenate the string elements.
            string result = string.Join(" ", array);
            return result;
        }
        public static string ArrayStringQuotes(string[] array)
        {
            // Use string Join to concatenate the string elements.
            string result = "";
            foreach (string s in array)
            {
                result += "\"" + s + "\"" + " ";
            }
            return result;
        }
        public static string ArrayStringQuotesComma(string[] array)
        {
            // Use string Join to concatenate the string elements.
            string result = "";
            for (int i = 0; i < array.Length; i++)
            {
                if (i > 0)
                {
                    result += ", ";
                }
                string s = array[i];
                result += "\"" + s+ "\"";
            }
            return result;
        }
        public static string ListStringDefines(List<string> array)
        {
            // Use string Join to concatenate the string elements.
            string result = "";
            foreach (string s in array)
            {
                result += "-D" + s + ";  ";
            }
            return result;
        }

        public static string ArrayStringQuotes(LibRef[] array)
        {
            // Use string Join to concatenate the string elements.
            string result = "";
            foreach (LibRef s in array)
            {
                if (s.Path.Length == 0)
                {
                    continue;
                }
                result += " " + s.BuildType + " \"" + s.Path + "\"" + " ";
            }
            return result;
        }
        public static string ArrayStringQuotesComma(LibRef[] array)
        {
            // Use string Join to concatenate the string elements.
            string result = "";
            foreach (LibRef s in array)
            {
                if (s.Path.Length == 0)
                {
                    continue;
                }
                result += " " + s.BuildType + " \"" + s.Path + "\"," + " ";
            }
            return result;
        }
        public static string RelativeToABS(List<string> Paths)
        {
            for (int i = 0; i < Paths.Count; i++)
            {
                Paths[i] = SanitizePath(ModuleDefManager.GetSourcePath()) + "/" + Paths[i];
            }
            return ArrayStringQuotes(Paths.ToArray());
        }
    }
}
