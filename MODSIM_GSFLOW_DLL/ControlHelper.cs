using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MODSIM_GSFLOW_C
{
    public class ControlHelper
    {
        private string _filePath;
        private string[] settings;
        public ControlHelper(string filePath)
        {
            _filePath = filePath;
            settings = File.ReadAllLines(_filePath);
        }

        public string[] ReadKeyValue(string key)
        {
            List<string> value = new List<string>();
            //string[] settings = File.ReadAllLines(_filePath);
            for (int i = 0; i < settings.Length; i++)
            {
                if (settings[i].StartsWith("###"))
                {
                    if (settings[i + 1] == key)
                    {
                        for (int j = 0; j < int.Parse(settings[i + 2]); j++)
                        {
                            value.Add(settings[i + j + 4]);
                        }
                        break;
                    }
                }
            }
            return value.ToArray();
        }

        public string[] ReadLineWithKeyValue(string key)
        {
            List<string> value = new List<string>();
            //string[] settings = File.ReadAllLines(_filePath);
            for (int i = 0; i < settings.Length; i++)
            {
                if (settings[i].StartsWith(key))
                {
                    string[] lineValues = settings[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int j = 0; j < lineValues.Length; j++)
                        {
                            value.Add(lineValues[j]);
                        }
                        break;
                }
            }
            return value.ToArray();
        }

        public void ReplaceKeyValue (string key, string[] newValue)
        {
            //string[] settings = File.ReadAllLines(_filePath);
            for(int i=0;i<settings.Length;i++)
            {
                if(settings[i].StartsWith("###"))
                {
                    if(settings[i+1]==key)
                    {
                        if(int.Parse(settings[i+2])!=newValue.Length)
                        {
                            throw new Exception($"Unexpected number of arguments provided for key {key} - needs {newValue.Length}.");
                        }
                        for(int j=0;j<newValue.Length;j++)
                        {
                            settings[i + j + 4] = newValue[j];
                        }
                        break;  
                    }
                }
            }
           // File.WriteAllLines(_filePath, settings);
        }

        public void ReplaceKeyRelativePath(string key, string[] newValue)
        {
            //string[] settings = File.ReadAllLines(_filePath);
            for (int i = 0; i < settings.Length; i++)
            {
                if (settings[i].StartsWith("###"))
                {
                    if (settings[i + 1] == key)
                    {
                        if (int.Parse(settings[i + 2]) != newValue.Length)
                        {
                            throw new Exception($"Unexpected number of arguments provided for key {key} - needs {newValue.Length}.");
                        }
                        for (int j = 0; j < newValue.Length; j++)
                        {
                            Uri control = new Uri(_filePath);
                            Uri relPath = control.MakeRelativeUri(new Uri(newValue[j]));
                            settings[i + j + 4] = Uri.UnescapeDataString(relPath.ToString());
                        }
                        break;
                    }
                }
            }
           // File.WriteAllLines(_filePath, settings);
        }
        public void ReplaceString(string basestr, string newstr)
        {
            for (int i = 0; i < settings.Length; i++)
            {
                settings[i] = settings[i].Replace(basestr, newstr);
            }
        }
        public void SaveChangesToFile(string newFile = "")
        {
            string locFileName = _filePath;
            if (newFile != "")
                locFileName = newFile;
            File.WriteAllLines(locFileName, settings);
            //onMessage($"Changes saved to {locFileName}");
        }

        public void CreatePaths(string v)
        {
            string workspace = Path.GetDirectoryName(_filePath);
            for (int i = 0; i < settings.Length; i++)
            {
                if(settings[i].Contains(v))
                {
                    try
                    {
                        string relPath = settings[i];
                        if(Path.GetExtension(_filePath)==".nam")
                        {
                            string[] lineValues = settings[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            relPath = lineValues[2];
                        }
                        string dirPath = Path.GetDirectoryName(Path.GetFullPath(Path.Combine(workspace, relPath)));
                        if (!Directory.Exists(dirPath))
                            Directory.CreateDirectory(dirPath);
                    }
                    catch (Exception)
                    {
                    }
                }
                
            }
        }
    }
}
