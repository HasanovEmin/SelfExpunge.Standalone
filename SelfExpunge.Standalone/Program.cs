using Aveva.Core.Database;
using Utilities = Aveva.Core.Utilities;
using Aveva.Core.Utilities.Messaging;
using System;
using System.Configuration;
using System.IO;
using Stand = Aveva.Core3D.Standalone;

namespace SelfExpunge.Standalone
{
    internal class Program
    {
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
                            ($"ProjectName: {ProjectName} \n"
                            + $"MdbName: {MdbName} \n"
                            + $"UniqeId: {UniqeId} \n" +
                            $"Status: {Status} \n");


                        if (Status == false)
                        {
                            if (ConnectToProgram(ProjectName, User, Password, MdbName))
                            {
                                Log.Write("---------------------Done------------------------");
                                Console.WriteLine("---------------------Done------------------------");
                                File.Delete(file);
                            }
                        }
                    }

                }
            }
        }

        private static void Init()
        {
            var logDirectory = ConfigurationManager.AppSettings["LOGPATH"];
            Log log = new Log(logDirectory);
            TaskFileDirectory = ConfigurationManager.AppSettings["TASKFILEPATH"];
            User = ConfigurationManager.AppSettings["USER"];
            Password = ConfigurationManager.AppSettings["PASSWORD"];
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
                    Log.Write($"Error: {message}");
                    Log.Write("-----------------------------------------------");
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
                        Log.Write(ex.Message);
                        Console.WriteLine(ex.Message);
                        Log.Write("-----------------------------------------------");
                    }
                    
  
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error : {ex.Message}");
                Log.Write(ex.Message);
                Log.Write("-----------------------------------------------");
            }
            finally
            {
                MDB.CurrentMDB.CloseMDB();
                Project.CurrentProject.Close();
            }
            
            Log.Write("-----------------------------------------------");
            return true;
        }
    }
}
