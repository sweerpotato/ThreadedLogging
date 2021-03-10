using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ThreadedLogging
{
    public class Program
    {
        private static readonly Random _RNG = new Random(2);

        private static void Main()
        {
            string logFilePath = Path.Combine(Environment.CurrentDirectory, $"TestLogFile{DateTime.Now:yyyyMMdd_hh_mm_ss}.log");

            Logger.Initialize(logFilePath);

            Logger.Log("Starting threads", TraceLevel.Info);

            new Thread(new ThreadStart(WriteToLog)) { Name = "Logging thread one" }.Start();
            new Thread(new ThreadStart(WriteToLog)) { Name = "Logging thread two" }.Start();
            new Thread(new ThreadStart(WriteToLog)) { Name = "Logging thread three" }.Start();
            
            Logger.Log("Finished starting threads", TraceLevel.Info);

            Console.ReadKey();
        }

        private static void WriteToLog()
        {
            while (true)
            {
                Logger.Log($"This is a log message from {Thread.CurrentThread.Name} at {DateTime.Now:HH:mm:ss}", TraceLevel.Info);

                Thread.Sleep(_RNG.Next(2000));
            }
        }
    }
}
