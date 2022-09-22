using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace RRModelingSystem
{
    //public delegate void ProcessMessage(string msg);  // delegate
    class TextUtils
    {
        public static event ProcessMessage MessageOut; // event

        public static DataTable CSVToDataTable(string fileFullPath, char seperator, bool ColHeader = false, string removeHeading = "")
        {
            DataTable myTable = new DataTable("MyTable");
            int i;
            DataRow myRow;
            string[] fieldValues;
            StreamReader myReader = null;
            try
            {
                //Open file and read first line to determine how many fields there are.
                //myReader = f.OpenText(fileFullPath)
                myReader = File.OpenText(fileFullPath);
                fieldValues = myReader.ReadLine().Split(new[] { seperator }, StringSplitOptions.RemoveEmptyEntries);
                //Create data columns accordingly
                MessageOut($"Found {fieldValues.Length} columns. Importing values...");
                if (ColHeader == false)
                {
                    for (i = 0; i < fieldValues.Length; i++)
                    {
                        myTable.Columns.Add(new DataColumn("Field" + i));
                    }


                    //Adding the first line of data to data table
                    myRow = myTable.NewRow();
                    for (i = 0; i < fieldValues.Length; i++)
                    {
                        myRow[i] = fieldValues[i].ToString();
                    }
                    myTable.Rows.Add(myRow);
                }
                else
                {
                    for (i = 0; i < fieldValues.Length; i++)
                    {
                        if (removeHeading != "")
                            myTable.Columns.Add(new DataColumn(fieldValues[i].ToString().Replace(removeHeading, "")));
                        else
                            myTable.Columns.Add(new DataColumn(fieldValues[i].ToString()));
                        if(fieldValues[i].ToString().ToLower().Contains("date"))
                            myTable.Columns[fieldValues[i].ToString()].DataType = typeof(DateTime);
                    }

                }


                //Now reading the rest of the data to data table
                int rows = 0;
                while (myReader.Peek() != -1)
                {
                    fieldValues = myReader.ReadLine().Split(new[] { seperator }, StringSplitOptions.RemoveEmptyEntries);
                    if (fieldValues.Length > myTable.Columns.Count)
                    {
                        //myTable.Columns.Add("");
                        throw new Exception($"A greater number of columns detected in a row staring with {myTable.Columns[0].ColumnName} - {fieldValues[0]}");
                    }

                    myRow = myTable.NewRow();
                    for (i = 0; i < fieldValues.Length; i++)
                    {
                        myRow[i] = fieldValues[i].ToString();
                    }
                    myTable.Rows.Add(myRow);
                    rows += 1;
                }
                MessageOut($"Completed importing {rows} rows.");
            }
            catch (Exception ex)
            {
                MessageOut("Error building datatable: " + ex.Message);
                return new DataTable("Empty");
            }
            finally
            {
                if (myReader!=null)
                    myReader.Close();
            }
            return myTable;
        }

        public static List<string> ReadControlProperties(string settingsfile, string[] keyword, int items=-1)
        {
            string[] settings = null;
            List<string> properties = null;
            try
            {
                settings = File.ReadAllLines(settingsfile);
                if (settings.Length > 0)
                {
                    bool foundKey = false;
                    properties = new List<string>();
                    foreach (string v in settings)
                    {
                        if (foundKey)
                        {
                            if (properties.Count == 1 && items<0)
                            {
                                items = int.Parse(v)+1;
                            }
                            else
                            {
                                if (properties.Count <= items)
                                    properties.Add(v);
                                else
                                    break;
                            }
                        }
                        else if (ContainAll(v,keyword))
                        {
                            string val=v;
                            foreach (string key in keyword)
                            {
                                val=val.Replace(key, "").Trim();
                            }
                            properties.Add(val);
                            foundKey = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageOut($"ERROR [READING SETTINGS FILE]:" + ex.Message);
            }

            if(properties != null && properties.Count>1)
                properties.Remove(properties[1]); //remove the second parameter that is the type of data
            return properties;
        }

        private static bool ContainAll(string v, string[] keyword)
        {
            foreach(string s in keyword)
            {
                if (!v.Contains(s))
                    return false;
            }
            return true;
        }
    }
}
