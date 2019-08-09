using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Qs1DirFinder
{
    //this class is READONLY, it cannot make any changes to any ini files
    public class Ini
    {
        public readonly Dictionary<string, Dictionary<string, string>> IniList;
        private Regex _headerFormat;
        private Regex _propertyFormat;

        public Ini(string path)
        {
            _headerFormat = new Regex(@"^\[.+]$"); //checks if a line is a header by being [Header Name]
            _propertyFormat = new Regex(@"^\w+=.*$"); //checks if a property line by being Property=Contents
            IniList = CreateIniList(path);
        }

        private Dictionary<string, Dictionary<string, string>> CreateIniList(string path)
        {
            string parentKey = string.Empty; //used as a reference of which header a property belongs to
            Dictionary<string, Dictionary<string, string>> tempDict = new Dictionary<string, Dictionary<string, string>>();

            StreamReader infile = new StreamReader(path);
            string line = infile.ReadLine();
            while(line != null)
            {
                if(_headerFormat.IsMatch(line) == true)
                {
                    parentKey = line.Substring(1, line.Length-2);
                    tempDict.Add(parentKey, new Dictionary<string, string>()); //new header section added with empty list for properties
                }
                else if(string.IsNullOrEmpty(parentKey) == false && _propertyFormat.IsMatch(line) == true)
                {
                    tempDict[parentKey].Add(
                        line.Substring(0, line.IndexOf('=')),
                        line.Substring(line.IndexOf('=')+1));
                }
                line = infile.ReadLine();
            }
            infile.Close();

            return tempDict;
        }


        //returns a copy of the single dictionary section
        public Dictionary<string, string> GetSection(string headerName)
        {
            return IniList[headerName];
        }

        //returns string representation of property given header and property name
        public string GetProperty(string headerName, string propertyName)
        {
            return IniList[headerName][propertyName];
        }

        public bool SectionExists(string headerName)
        {
            return IniList.ContainsKey(headerName);
        }

        //automatically checks if section exists; so no exception will be thrown if a bad headername is given
        public bool PropertyExists(string headerName, string propertyName)
        {
            if(IniList.ContainsKey(headerName) == true && IniList[headerName].ContainsKey(propertyName) == true)
            {
                return true;
            }
            return false;
        }
        

    }
}
