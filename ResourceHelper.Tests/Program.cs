﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ResourceHelper.Tests
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Environment.SetEnvironmentVariable("RHWEBROOT", @"C:\Users\Troels Liebe Bentsen\Desktop\ResourceHelper\ResourceHelper.Sample\");

            string[] my_args = { Assembly.GetExecutingAssembly().Location };

            int returnCode = NUnit.ConsoleRunner.Runner.Main(my_args);

            if (returnCode != 0)
                Console.Beep();
            Console.ReadKey();
        }
    }
}