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
			string[] my_args = { Assembly.GetExecutingAssembly().Location };

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

		}
			
		[TearDown]
		public void RunAfterAnyTests ()
		{

		}
	}


}
