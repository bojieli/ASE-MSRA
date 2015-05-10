/*
 *author:v-guil
 *email:v-guil@microsoft.com
 *description:
 *      this file defines some useful tool used for coding;
 *      the macro define SHOW_DETAILS_IN_DEBUG is to show details in the console while debug
 **/
#define SHOW_DETAILS_IN_DEBUG   //disable the define if you don't want see too many outputs while debug
#define SHOW_WARNING_IN_DEBUG   //disable the define if you don't want to see too many warnins while debug

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NewElevatorFramework
{
    static class Utility
    {
        const int initialLogCounts = 10000;
        private static List<String> logRecordList = new List<string>(initialLogCounts);

        //methods
        //global 
            //exit the console application
        public static void stopProgram(){
            Console.Write("Press any key to exit application...");
            Console.ReadKey();
            Environment.Exit(0);
        }
            //-------log record methods-------
        /// <summary>
        /// record normal log
        /// </summary>
        /// <param name="message"></param>
        public static void log(string message){
            logRecordList.Add(message);
#if (DEBUG && SHOW_DETAILS_IN_DEBUG)
            printInConsoleWithColor(message,ConsoleColor.Green);
#endif
        }

        public static void logWarning(string message) {
            logRecordList.Add(message); 
#if (DEBUG && SHOW_DETAILS_IN_DEBUG && SHOW_WARNING_IN_DEBUG)
            printInConsoleWithColor(message,ConsoleColor.Yellow);
#endif
        }

        public static void logError(string message) {
            logRecordList.Add(message);
            printInConsoleWithColor(message, ConsoleColor.Red);
            saveLogRecord("DefaulDebugLog.txt");
            stopProgram();
        }

        private static void printInConsoleWithColor(string message,ConsoleColor color) {
            var tempColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = tempColor;
        }

        /// <summary>
        /// save the reocord message while debug model
        /// </summary>
        /// <param name="fileName"></param>
        public static Thread saveLogRecord(string fileName) {
            Thread saveFile = null;
#if DEBUG
            ThreadStart fileWrite = delegate
            {
                StreamWriter fileStream = new StreamWriter("./" + fileName);
                foreach (var log in logRecordList)
                {
                    fileStream.WriteLine(log);
                }
                fileStream.Close();
            };
            saveFile = new Thread(fileWrite);
            saveFile.Start();
#endif
             return saveFile;
        }

        //simulator
        public static double outputAnalysisResult(SimulateProgram simulator) {
            int totalTime = 0;
            int sigleTime = 0;
            double averageTime = 0;
            string msg;
            Passenger[] passengers = simulator.Passengers;
            Console.WriteLine();
            Console.WriteLine("----------------------------Time Analysis----------------------------");
            foreach(var passenger in passengers){
                sigleTime = passenger.ArrivedTime - passenger.ComingTime;
                totalTime += sigleTime;
                msg = "passenger "+passenger.Name+"       coming at "+passenger.ComingTime+" tick, arrived at "+passenger.ArrivedTime+" tick, cost "+sigleTime+" Ticks.";
                logRecordList.Add(msg);
                Console.WriteLine(msg);
            }
            averageTime = (totalTime * 1.0) / passengers.Length;

            msg = "Average Time Cost : " + averageTime + " ;";
            logRecordList.Add(msg);
            Console.WriteLine(msg);
            return averageTime;
        }

        //passengers
        public static int howManyPassengerInTotal(SimulateProgram simulator) {
            Passenger[] passengers = simulator.Passengers;
            log(""+passengers.Length);
            return passengers.Length;
        }

        public static int howManyPassengerArrived(SimulateProgram simulator) { 
            Passenger[] passengers = simulator.Passengers;
            int total = 0;
            foreach (var passenger in passengers) {
                if (passenger.IsArrived) {
                    total++; 
                } 
            }
            log("" + total);
            return total;
        }

        public static int howManyPassengerNonarrived(SimulateProgram simulator) { 
            int totalPassenger = howManyPassengerInTotal(simulator);
            int totalArrivedPassenger = howManyPassengerArrived(simulator);
            return (totalPassenger - totalArrivedPassenger);
        }
    }
}
