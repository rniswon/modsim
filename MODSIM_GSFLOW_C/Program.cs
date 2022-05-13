using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Csu.Modsim.ModsimIO;
using Csu.Modsim.ModsimModel;
using MODSIM_GSFLOW_C;
//using SurfGWModule;

namespace MODSIM_GSFLOW_C
{
	class Program
	{
		public static Model myModel = new Model();
		
		static void Main(string[] CmdArgs)
		{
			string FileName = CmdArgs[0];
			myModel.OnMessage += OnMessage;
			myModel.OnModsimError += OnError;

			XYFileReader.Read(myModel, FileName);

			//Adding 'plug-ins'
			SurfGWModule sSurfGWModule = new SurfGWModule(CmdArgs,ref myModel);

			int run = Modsim.RunSolver(myModel);

			if (run == 0)
			{
				Console.WriteLine("Successful completion of the MODSIM run!");
			}

			Console.ReadKey();
		}

		private static void OnMessage(string message)
		{
			Console.WriteLine(message);
		}

		private static void OnError(string message)
		{
			Console.WriteLine(message);
		}
	}
}
