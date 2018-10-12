using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using NDesk.Options; 
using System.Text;
using System.Threading.Tasks;

namespace TwinThreadChallenge
{
    
    class Program
    {
        private static string _searchterms = null;
        private static string _multitag = null;
        private static int verbosity;
        private static bool uniques = false;
        private static bool _hierarchy = false;
        static void Main(string[] args)
        {

            OptionSet options = new OptionSet()
                .Add("s=|search=", s => _searchterms = s)
                .Add("v", v => { if (v != null) ++verbosity; })
                .Add("c|criticals", c => { if (c != null) _searchterms = "status:3"; })
                .Add("u|uniques", u => { if (u != null) uniques = true; })
                .Add("mt=|multitag=", mt => _multitag = mt)
                .Add("hi|hierarchy", hi => { if (hi != null) _hierarchy = true; });

            options.Parse(args);


            string baseURL = "https://www.twinthread.com/code-challenge/assets.txt";
            RootObject rootObj = new RootObject();
            Console.WriteLine("Downloading assets...");
            try
            {
                using (var client = new WebClient())
                {
                    Stream stream = client.OpenRead(baseURL);
                    StreamReader reader = new StreamReader(stream);
                    string content = reader.ReadToEnd();
                    rootObj = JsonConvert.DeserializeObject<RootObject>(content);
                }
                Console.WriteLine("Download Complete.\n");
            }
            catch(Exception e)
            {
                Console.WriteLine("Error downloading assets: {0}", e);
            }

            if(_searchterms != null)
            {
                List<Asset> results = new List<Asset>();
                if(_multitag != null)
                {
                    string[] splitterms = _searchterms.Split(' ');
                    Console.WriteLine("Searching for Assets with search terms: {0}, multisearch tag: {1}...", splitterms[0], _multitag.ToUpper());
                    List<Asset> temp1 = SearchAssets(rootObj, splitterms[0]);
                    Console.WriteLine("Searching for Assets with search terms: {0}, multisearch tag: {1}...", splitterms[1], _multitag.ToUpper());
                    List<Asset> temp2 = SearchAssets(rootObj, splitterms[1]);
                    _multitag = _multitag.ToLower();
                    if (_multitag.Equals("and"))
                    {
                        results = temp1.Intersect(temp2).ToList<Asset>();
                    }
                    if (_multitag.Equals("or"))
                    {
                        results = temp1.Union(temp2).ToList<Asset>();
                    }
                }
                else
                {
                    Console.WriteLine("Searching for Assets with search terms: {0}...", _searchterms);
                    results = SearchAssets(rootObj, _searchterms);
                }
                
                bool isEmpty = !results.Any();
                if (isEmpty)
                {
                    Console.WriteLine("No asset found matching provided terms.");
                }
                else
                {
                    if(verbosity > 0)
                    {
                        var json = JsonConvert.SerializeObject(results, Formatting.Indented);
                        Console.WriteLine("Assets found:");
                        Console.WriteLine(json);
                    }
                    else
                    {
                        Console.WriteLine("Assets found:");
                        foreach(Asset a in results)
                        {
                            Console.WriteLine(a.name);
                        }
                    }
                    if(results.Count() == 1)
                    {
                        if(_hierarchy)
                        {
                            Console.WriteLine("\nAsset Hierarchy:\n");
                            Node rootNode = new Node();
                            rootNode.Name = results[0].name;
                            rootNode.AssetId = results[0].assetId;
                            rootNode.Children = rootNode.FindChildren(rootObj);

                            rootNode.PrintPretty("", true);
                        }

                    }
                }
            }

            if (uniques)
            {
                Console.WriteLine("\n Searching for unique classes...");
                Dictionary<string, List<string>> results = GetUniqueClasses(rootObj);
                Console.WriteLine("Number of Unique Classes: {0}", results.Count);
                Console.WriteLine("Display results? (y/n)");
                string input = Console.ReadLine();
                if (input.Equals("y"))
                {
                    Console.WriteLine("Results:");
                    foreach (KeyValuePair<string, List<String>> kvp in results)
                    {
                        Console.WriteLine("{0}:", kvp.Key);
                        kvp.Value.ForEach(i => Console.WriteLine("\t{0}", i));

                    }
                }
                else
                {
                    Console.WriteLine("Will not print results to console.");
                }
            }


            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        static Dictionary<string, List<string>> GetUniqueClasses(RootObject rootObj)
        {
            Dictionary<string, List<string>> results = new Dictionary<string, List<string>>();

            foreach (Asset a in rootObj.assets)
            {
                if(a.classList != null)
                {
                    foreach(ClassObject o in a.classList)
                    {
                        if (results.ContainsKey(o.name))
                        {
                            results[o.name].Add(a.name);
                        }
                        else
                        {
                            List<string> parentname = new List<string>();
                            parentname.Add(a.name);
                            results.Add(o.name, parentname);
                        }
                    }
                }
            }

            return results;
        }

        static List<Asset> SearchByID(RootObject rootObj, string value)
        {
            List<Asset> results = new List<Asset>();
            if (value.StartsWith("["))
            {
                value = value.Trim('[', ']');
                string[] limits = value.Split('-');
                int start = 0;
                int end = 0;
                if(!Int32.TryParse(limits[0], out start) || !Int32.TryParse(limits[1],out end))
                {
                    Console.WriteLine("Error parsing search limits. Make sure term is a valid integer.");
                    return results;
                }

                foreach(Asset a in rootObj.assets)
                {
                    if (a.assetId >= start && a.assetId <= end)
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.StartsWith("{"))
            {
                value = value.Trim('{', '}');
                string[] limits = value.Split('-');
                int start = 0;
                int end = 0;
                if (!Int32.TryParse(limits[0], out start) || !Int32.TryParse(limits[1], out end))
                {
                    Console.WriteLine("Error parsing search limits. Make sure term is a valid integer.");
                    return results;
                }

                foreach (Asset a in rootObj.assets)
                {
                    if (a.assetId > start && a.assetId < end)
                    {
                        results.Add(a);
                    }
                }
            }
            else
            {
                int i = 0;
                if (!Int32.TryParse(value, out i))
                {
                    i = -1;
                }


                foreach (Asset a in rootObj.assets)
                {
                    if (a.assetId == i)
                    {
                        results.Add(a);
                    }
                }
            }


            return results;
        }

        static List<Asset> SearchByName(RootObject rootObj, string value)
        {
            List<Asset> results = new List<Asset>();
            if (value.StartsWith("*"))
            {
                value = value.Trim('*');
                foreach(Asset a in rootObj.assets)
                {
                    if (a.name.EndsWith(value))
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.EndsWith("*"))
            {
                value = value.Trim('*');
                foreach(Asset a in rootObj.assets)
                {
                    if (a.name.StartsWith(value))
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.Contains('*'))
            {
                string[] limits = value.Split('*');
                foreach(Asset a in rootObj.assets)
                {
                    if (a.name.StartsWith(limits[0]) && a.name.EndsWith(limits[1]))
                    {
                        results.Add(a);
                    }
                }
                
            }
            else
            {
                foreach (Asset a in rootObj.assets)
                {
                    if (a.name.Equals(value))
                    {
                        results.Add(a);
                    }
                }
            }


            return results;
        }

        static List<Asset> SearchByDescription(RootObject rootObj, string value)
        {
            List<Asset> results = new List<Asset>();
            if (value.StartsWith("*"))
            {
                value = value.Trim('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.description.EndsWith(value))
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.EndsWith("*"))
            {
                value = value.Trim('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.description.StartsWith(value))
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.Contains('*'))
            {
                string[] limits = value.Split('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.description.StartsWith(limits[0]) && a.description.EndsWith(limits[1]))
                    {
                        results.Add(a);
                    }
                }

            }
            else
            {
                foreach (Asset a in rootObj.assets)
                {
                    if (a.description.Equals(value))
                    {
                        results.Add(a);
                    }
                }
            }


            return results;
        }

        static List<Asset> SearchByStatus(RootObject rootObj, string value)
        {
            List<Asset> results = new List<Asset>();
            if (value.StartsWith("["))
            {
                value = value.Trim('[', ']');
                string[] limits = value.Split('-');
                int start = 0;
                int end = 0;
                if (!Int32.TryParse(limits[0], out start) || !Int32.TryParse(limits[1], out end))
                {
                    Console.WriteLine("Error parsing search limits. Make sure term is a valid integer.");
                    return results;
                }

                foreach (Asset a in rootObj.assets)
                {
                    if (a.status > start && a.status < end)
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.StartsWith("{"))
            {
                value = value.Trim('{', '}');
                string[] limits = value.Split('-');
                int start = 0;
                int end = 0;
                if (!Int32.TryParse(limits[0], out start) || !Int32.TryParse(limits[1], out end))
                {
                    Console.WriteLine("Error parsing search limits. Make sure term is a valid integer.");
                    return results;
                }

                foreach (Asset a in rootObj.assets)
                {
                    if (a.status >= start && a.status <= end)
                    {
                        results.Add(a);
                    }
                }
            }
            else
            {
                int i = 0;
                if (!Int32.TryParse(value, out i))
                {
                    i = -1;
                }


                foreach (Asset a in rootObj.assets)
                {
                    if (a.status == i)
                    {
                        results.Add(a);
                    }
                }
            }


            return results;
        }

        static List<Asset> SearchByIcon(RootObject rootObj, string value)
        {
            List<Asset> results = new List<Asset>();
            if (value.StartsWith("*"))
            {
                value = value.Trim('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.icon.EndsWith(value))
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.EndsWith("*"))
            {
                value = value.Trim('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.icon.StartsWith(value))
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.Contains('*'))
            {
                string[] limits = value.Split('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.icon.StartsWith(limits[0]) && a.icon.EndsWith(limits[1]))
                    {
                        results.Add(a);
                    }
                }

            }
            else
            {
                foreach (Asset a in rootObj.assets)
                {
                    if (a.icon.Equals(value))
                    {
                        results.Add(a);
                    }
                }
            }


            return results;
        }

        static List<Asset> SearchByRunning(RootObject rootObj, string value)
        {
            List<Asset> results = new List<Asset>();
            if (value.StartsWith("*"))
            {
                value = value.Trim('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Running.value.EndsWith(value))
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.EndsWith("*"))
            {
                value = value.Trim('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Running.value.StartsWith(value))
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.Contains('*'))
            {
                string[] limits = value.Split('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Running.value.StartsWith(limits[0]) && a.Running.value.EndsWith(limits[1]))
                    {
                        results.Add(a);
                    }
                }

            }
            else
            {
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Running.value.Equals(value))
                    {
                        results.Add(a);
                    }
                }
            }


            return results;
        }

        static List<Asset> SearchByUtilization(RootObject rootObj, string value)
        {
            List<Asset> results = new List<Asset>();
            if (value.StartsWith("*"))
            {
                value = value.Trim('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Utilization.value.EndsWith(value))
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.EndsWith("*"))
            {
                value = value.Trim('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Utilization.value.StartsWith(value))
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.Contains('*'))
            {
                string[] limits = value.Split('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Utilization.value.StartsWith(limits[0]) && a.Utilization.value.EndsWith(limits[1]))
                    {
                        results.Add(a);
                    }
                }

            }
            else
            {
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Utilization.value.Equals(value))
                    {
                        results.Add(a);
                    }
                }
            }


            return results;
        }

        static List<Asset> SearchByPerformance(RootObject rootObj, string value)
        {
            List<Asset> results = new List<Asset>();
            if (value.StartsWith("*"))
            {
                value = value.Trim('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Performance.value.EndsWith(value))
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.EndsWith("*"))
            {
                value = value.Trim('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Performance.value.StartsWith(value))
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.Contains('*'))
            {
                string[] limits = value.Split('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Performance.value.StartsWith(limits[0]) && a.Performance.value.EndsWith(limits[1]))
                    {
                        results.Add(a);
                    }
                }

            }
            else
            {
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Performance.value.Equals(value))
                    {
                        results.Add(a);
                    }
                }
            }


            return results;
        }

        static List<Asset> SearchByLocation(RootObject rootObj, string value)
        {
            List<Asset> results = new List<Asset>();
            if (value.StartsWith("*"))
            {
                value = value.Trim('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Location.value.EndsWith(value))
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.EndsWith("*"))
            {
                value = value.Trim('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Location.value.StartsWith(value))
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.Contains('*'))
            {
                string[] limits = value.Split('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Location.value.StartsWith(limits[0]) && a.Location.value.EndsWith(limits[1]))
                    {
                        results.Add(a);
                    }
                }

            }
            else
            {
                foreach (Asset a in rootObj.assets)
                {
                    if (a.Location.value.Equals(value))
                    {
                        results.Add(a);
                    }
                }
            }


            return results;
        }

        //TODO
        static List<Asset> SearchByClassList(RootObject rootObj, string value)
        {
            List<Asset> results = new List<Asset>();
            if (value.StartsWith("*"))
            {
                value = value.Trim('*');
                foreach (Asset a in rootObj.assets)
                {
                    if (a.classList != null)
                    {
                        foreach(ClassObject c in a.classList)
                        {
                            if (c.name.EndsWith(value))
                            {
                                results.Add(a);
                                break;
                            }
                        }
                    }
                }
            }
            else if (value.EndsWith("*"))
            {
                value = value.Trim('*');
                foreach (Asset a in rootObj.assets)
                {
                    if(a.classList != null)
                    {
                        foreach(ClassObject c in a.classList)
                        {
                            if (c.name.StartsWith(value))
                            {
                                results.Add(a);
                                break;
                            }
                        }
                    }
                }
            }
            else if (value.Contains('*'))
            {
                string[] limits = value.Split('*');
                foreach (Asset a in rootObj.assets)
                {
                    if(a.classList != null)
                    {
                        foreach(ClassObject c in a.classList)
                        {
                            if (c.name.StartsWith(limits[0]) && c.name.EndsWith(limits[1]))
                            {
                                results.Add(a);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (Asset a in rootObj.assets)
                {
                    if(a.classList != null)
                    {
                        foreach (ClassObject c in a.classList)
                        {
                            if (c.name.Equals(value))
                            {
                                results.Add(a);
                                break;
                            }
                        }
                    }
                }
            }
            return results;
        }

        static List<Asset> SearchByParentID(RootObject rootObj, string value)
        {
            List<Asset> results = new List<Asset>();
            if (value.StartsWith("["))
            {
                value = value.Trim('[', ']');
                string[] limits = value.Split('-');
                int start = 0;
                int end = 0;
                if (!Int32.TryParse(limits[0], out start) || !Int32.TryParse(limits[1], out end))
                {
                    Console.WriteLine("Error parsing search limits. Make sure term is a valid integer.");
                    return results;
                }

                foreach (Asset a in rootObj.assets)
                {
                    if (a.parentId > start && a.parentId < end)
                    {
                        results.Add(a);
                    }
                }
            }
            else if (value.StartsWith("{"))
            {
                value = value.Trim('{', '}');
                string[] limits = value.Split('-');
                int start = 0;
                int end = 0;
                if (!Int32.TryParse(limits[0], out start) || !Int32.TryParse(limits[1], out end))
                {
                    Console.WriteLine("Error parsing search limits. Make sure term is a valid integer.");
                    return results;
                }

                foreach (Asset a in rootObj.assets)
                {
                    if (a.parentId >= start && a.parentId <= end)
                    {
                        results.Add(a);
                    }
                }
            }
            else
            {
                int i = 0;
                if (!Int32.TryParse(value, out i))
                {
                    i = -1;
                }


                foreach (Asset a in rootObj.assets)
                {
                    if (a.parentId == i)
                    {
                        results.Add(a);
                    }
                }
            }


            return results;
        }


        static List<Asset> SearchAssets(RootObject rootObj, string searchTerms)
        {
            List<Asset> results = new List<Asset>();
            
            string[] terms = searchTerms.Split(':');
            terms[0] = terms[0].ToLower();

            switch (terms[0])
            {
                case "assetid":
                    results = SearchByID(rootObj, terms[1]);
                    break;
                case "name":
                    results = SearchByName(rootObj, terms[1]);
                    break;
                case "description":
                    results = SearchByDescription(rootObj, terms[1]);
                    break;
                case "status":
                    results = SearchByStatus(rootObj, terms[1]);
                    break;
                case "icon":
                    results = SearchByIcon(rootObj, terms[1]);
                    break;
                case "running":
                    results = SearchByRunning(rootObj, terms[1]);
                    break;
                case "utilization":
                    results = SearchByUtilization(rootObj, terms[1]);
                    break;
                case "performance":
                    results = SearchByPerformance(rootObj, terms[1]);
                    break;
                case "location":
                    results = SearchByLocation(rootObj, terms[1]);
                    break;
                case "classlist":
                    results = SearchByClassList(rootObj, terms[1]);
                    break;
                case "parentid":
                    results = SearchByParentID(rootObj, terms[1]);
                    break;
                default:
                    Console.WriteLine("Error parsing search terms: Given Key is not a top level field of Asset.");
                    break;
            }
            return results;
        }
    }
    class Node
    {
        public string Name;
        public List<Node> Children;
        public int AssetId;

        public void PrintPretty(string indent, bool last)
        {
            Console.Write(indent);
            if (last)
            {
                Console.Write("\\-");
                indent += "  ";
            }
            else
            {
                Console.Write("|-");
                indent += "| ";
            }
            Console.WriteLine(Name);

            for (int i = 0; i < Children.Count; i++)
                Children[i].PrintPretty(indent, i == Children.Count - 1);
        }

        public List<Node> FindChildren(RootObject rootObj)
        {
            List<Node> results = new List<Node>();
            foreach (Asset a in rootObj.assets)
            {
                if (a.parentId == AssetId && a.assetId != AssetId)
                {
                    Node child = new Node();
                    child.Name = a.name;
                    child.AssetId = a.assetId;
                    child.Children = child.FindChildren(rootObj);
                    results.Add(child);
                }
            }

            return results;
        }


    }

    public class ClassObject
    {
        public int id { get; set; }
        public string name { get; set; }
        public string drill { get; set; }
    }

    public class AssetStatus
    {
        public int Normal { get; set; }
        public int Warning { get; set; }
        public int Critical { get; set; }
    }

    public class DataType
    {
        public int Float { get; set; }
        public int Integer { get; set; }
        public int String { get; set; }
        public int State { get; set; }
        public int Location { get; set; }
    }

    public class Running
    {
        public int propertyId { get; set; }
        public string value { get; set; }
        public string units { get; set; }
        public int dataType { get; set; }
        public int status { get; set; }
    }

    public class Utilization
    {
        public int propertyId { get; set; }
        public string value { get; set; }
        public string units { get; set; }
        public int dataType { get; set; }
        public int status { get; set; }
    }

    public class Performance
    {
        public int propertyId { get; set; }
        public string value { get; set; }
        public string units { get; set; }
        public int dataType { get; set; }
        public int status { get; set; }
    }

    public class Location
    {
        public int propertyId { get; set; }
        public string value { get; set; }
        public string units { get; set; }
        public int dataType { get; set; }
        public int status { get; set; }
    }

    public class Asset
    {
        public int assetId { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int status { get; set; }
        public string icon { get; set; }
        public Running Running { get; set; }
        public Utilization Utilization { get; set; }
        public Performance Performance { get; set; }
        public Location Location { get; set; }
        public List<ClassObject> classList { get; set; }
        public int? parentId { get; set; }
    }

    public class RootObject
    {
        public AssetStatus asset_status { get; set; }
        public DataType dataType { get; set; }
        public List<Asset> assets { get; set; }
    }
}