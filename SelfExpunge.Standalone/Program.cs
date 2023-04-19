using Aveva.Core.Database;
using Utilities = Aveva.Core.Utilities;
using Aveva.Core.Utilities.Messaging;
using System;
using System.IO;
using Stand = Aveva.Core3D.Standalone;
using System.Collections;
using System.Xml;

namespace SelfExpunge.Standalone
{
    internal class Program
    {
        public static string SettingsPath { get; set; } = @"xxx\SelfExpungeApp.config";

        public static Hashtable Settings { get; set; } = new Hashtable(); 
        public static string ProjectName { get; set; }
        public static string User { get; set; } = "SYSTEM";
        public static string Password { get; set; } = "XXXXXX";
        public static string MdbName { get; set; }
        public static string UniqeId { get; set; }
        public static bool Status { get; set; } = false;

        public static string TaskFileDirectory { get; set; }

        [STAThread]
        static void Main(string[] args)
        {
            Init();
            Run();

        }

        private static void Run()
        {
            if (!Directory.Exists(TaskFileDirectory))
            {
                Log.Write($"{TaskFileDirectory} doesn't exists");
                return;
            }
            else
            {
                var files = Directory.GetFiles(TaskFileDirectory, "*.txt", SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    if (file.Contains("UserExpungeInfo"))
                    {
                        var lines = File.ReadAllLines(file);
                        foreach (var line in lines)
                        {
                            string option = line.Substring(0, line.IndexOf("=")).ToLower();
                            if (option == "projectname")
                            {
                                ProjectName = line.Substring(line.IndexOf("=") + 1);
                            }
                            if (option == "mdb")
                            {
                                MdbName = line.Substring(line.IndexOf("=") + 1);
                            }
                            if (option == "uniqeid")
                            {
                                UniqeId = line.Substring(line.IndexOf("=") + 1);
                            }
                            if (option == "status")
                            {
                                var status = line.Substring(line.IndexOf("=") + 1);
                                Status = status == "NO" ? false : true;
                            }


                        }

                        Log.Write(file.ToString() + "\n" + "Options:");
                        Log.Write
                            ($"\tProjectName: {ProjectName} \n"
                            + $"\tMdbName: {MdbName} \n"
                            + $"\tUniqeId: {UniqeId} \n" +
                            $"\tStatus: {Status} \n");


                        if (Status == false)
                        {
                            if (ConnectToProgram(ProjectName, User, Password, MdbName))
                            {
                                
                                File.Delete(file);
                            }
                        }
                    }

                }
            }
        }

        private static void Init()
        {
            if (GetSettings(SettingsPath) == false)
            {
                Console.WriteLine("Please control your SelfExpungeApp.config");
            }
            var logDirectory = Settings["LOGPATH"].ToString();
            Log log = new Log(logDirectory);
            TaskFileDirectory = Settings["TASKFILEPATH"].ToString();
            User = Settings["USER"].ToString();
            Password = Settings["PASSWORD"].ToString();
        }

        private static bool GetSettings(string settingsPath)
        {
            
            string xml = string.Empty;
            StreamReader reader = null;
            try
            {
                reader = new StreamReader(new FileStream(
                                                   settingsPath,
                                                   FileMode.Open,
                                                   FileAccess.Read,
                                                   FileShare.Read));

                xml = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                reader.Close();
                
            }
            
            
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);
            foreach (XmlNode child in xmlDocument.ChildNodes)
            {
                var name = child.Name;
                if (child.Name.ToLower().Contains("config"))
                {
                    foreach (XmlNode config in child.ChildNodes)
                    {
                        if (config.Name.ToLower().Contains("setting"))
                        {
                            foreach (XmlNode setting in config.ChildNodes)
                            {
                                if (setting.Name.ToLower().Contains("add"))
                                {
                                    Settings.Add(
                                        setting.Attributes["key"].Value,
                                        setting.Attributes["value"].Value
                                        );
                                }
                            }
                        }
                    }

                }

            }
            return true;
        }

        static bool ConnectToProgram(string projectName, string user, string password, string mdbName)
        {
            try
            {
                Stand.Standalone.Start(1);
                PdmsMessage error;
                if (!Stand.Standalone.Open(projectName, user, password, mdbName, out error))
                {
                    var message = error.MessageText();
                    Log.StatusFailed(message);
                    return false;
                }
                else
                {
                    Log.Write("Module opened");

                    try
                    {
                        Utilities.CommandLine.Command.CreateCommand($@"!session = object session('{UniqeId}')").RunInCurrentScopeInPdms();
                        Utilities.CommandLine.Command.CreateCommand($@"expunge '$!session'").RunInCurrentScopeInPdms();
                    }
                    catch (Exception ex)
                    {
                        Log.StatusFailed(ex.Message);
                        return false;
                    }
                    
  
                }


            }
            catch (Exception ex)
            {
                Log.StatusFailed(ex.Message);
                
            }
            finally
            {
                MDB.CurrentMDB.CloseMDB();
                Project.CurrentProject.Close();
            }
            Log.Status();
            
            return true;
        }
    }
}
