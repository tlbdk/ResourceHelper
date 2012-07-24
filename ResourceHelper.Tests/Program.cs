using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using NUnit.Framework;

namespace ResourceHelper.Tests
{
	class Program
	{
		[STAThread]
		static void Main (string[] args)
		{
			string[] my_args = { Assembly.GetExecutingAssembly ().Location };

			int returnCode = NUnit.ConsoleRunner.Runner.Main (my_args);

			if (returnCode != 0)
				Console.Beep ();
			Console.ReadKey ();
		}
	}
					
	[SetUpFixture]
	public class MySetUpClass
	{
		[SetUp]
		public void RunBeforeAnyTests ()
		{
			Environment.SetEnvironmentVariable ("RHWEBROOT", "/home/tlb/git/ResourceHelper/ResourceHelper.Sample/");
			//Environment.SetEnvironmentVariable ("RHWEBROOT", @"C:\Users\Troels Liebe Bentsen\Desktop\ResourceHelper\ResourceHelper.Sample\");
		}
			
		[TearDown]
		public void RunAfterAnyTests ()
		{
			Environment.SetEnvironmentVariable ("RHWEBROOT", null);
		}
	}


}
