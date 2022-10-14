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
        public ControlHelper(string filePath)
        {
            _filePath = filePath;
        }

        public void ReplaceKeyValue (string key, string[] newValue)
        {
            string[] settings = File.ReadAllLines(_filePath);
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
            File.WriteAllLines(_filePath, settings);
        }

        public void ReplaceKeyRelativePath(string key, string[] newValue)
        {
            string[] settings = File.ReadAllLines(_filePath);
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
            File.WriteAllLines(_filePath, settings);
        }
        public void ReplaceString(string basestr, string newstr)
        {
            string[] settings = File.ReadAllLines(_filePath);
            for (int i = 0; i < settings.Length; i++)
            {
                settings[i] = settings[i].Replace(basestr, newstr);
            }
            File.WriteAllLines(_filePath, settings);
        }
    }
}
