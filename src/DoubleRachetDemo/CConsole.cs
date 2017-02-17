using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoubleRachetDemo
{
    public static class CConsole
    {
        private static void PrintWithColor(ConsoleColor col, string message, params object[] formatting)
        {
            var oColor = Console.ForegroundColor;
            Console.ForegroundColor = col;
            Console.WriteLine(message, formatting);
            Console.ForegroundColor = oColor;
        }
        public static void Red(string message, params object[] formatting)
        {
            PrintWithColor(ConsoleColor.Red, message, formatting);
        }
        public static void Green(string message, params object[] formatting)
        {
            PrintWithColor(ConsoleColor.Green, message, formatting);
        }
        public static void Blue(string message, params object[] formatting)
        {
            PrintWithColor(ConsoleColor.Blue, message, formatting);
        }
        public static void Yellow(string message, params object[] formatting)
        {
            PrintWithColor(ConsoleColor.Yellow, message, formatting);
        }
        public static void Magenta(string message, params object[] formatting)
        {
            PrintWithColor(ConsoleColor.Magenta, message, formatting);
        }
        public static void White(string message, params object[] formatting)
        {
            PrintWithColor(ConsoleColor.White, message, formatting);
        }
        public static void Gray(string message, params object[] formatting)
        {
            PrintWithColor(ConsoleColor.Gray, message, formatting);
        }
        public static void Cyan(string message, params object[] formatting)
        {
            PrintWithColor(ConsoleColor.Cyan, message, formatting);
        }
        public static void DarkGray(string message, params object[] formatting)
        {
            PrintWithColor(ConsoleColor.DarkGray, message, formatting);
        }
        public static void DarkGreen(string message, params object[] formatting)
        {
            PrintWithColor(ConsoleColor.DarkGreen, message, formatting);
        }
        public static void DarkCyan(string message, params object[] formatting)
        {
            PrintWithColor(ConsoleColor.DarkCyan, message, formatting);
        }
    }    
}
