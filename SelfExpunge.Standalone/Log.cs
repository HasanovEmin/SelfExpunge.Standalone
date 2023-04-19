using System;
using System.IO;

namespace SelfExpunge.Standalone
{
    internal class Log
    {
        private readonly string _folderPath;

        private static bool _exists = false;

        public static bool Exists 
        {
            get { return _exists; } 
        }

        public static string FileName { get; set; }

        public Log(string folderPath)
        {
            _folderPath = folderPath;
            if (_folderPath == null) 
            {
                Console.WriteLine("Logging path isn't set");
            }
            else
            {
                
                if (!Directory.Exists(_folderPath))
                {
                    Console.WriteLine("Path for Logging doesn't exists");
                }
                else
                {
                    DateTime date = DateTime.Now;
                    var day = DateTime.Today.Day;
                    var month = DateTime.Today.Month;
                    var year = DateTime.Today.Year;

                    var file = $"{day}_{month}_{year}SelfExpungeLog.txt";

                    FileName = Path.Combine(_folderPath, file);

                    File.AppendAllText(FileName, date.ToString());
                    File.AppendAllText(FileName, "\n");
                    _exists = true;
                }
            }
            
        }

        public static void Write(string info)
        {
            if (!Exists)
            {
                return;
            }

            info += "\n";

            File.AppendAllText(FileName, info);


        }

        internal static void StatusFailed(string message)
        {
            Console.WriteLine($"Error : {message}");
            Log.Write(message);
            Console.WriteLine(message);
            Log.Write("\t\tTicket Status = Failed");
            Console.WriteLine("\t\tTicket Status = Failed");
            Log.Write("-----------------------------------------------");
        }

        internal static void Status()
        {
            Log.Write("\t\tTicket Status = Done");
            Console.WriteLine("\t\tTicket Status = Done");
            Log.Write("-----------------------------------------------");
        }
    }
}
