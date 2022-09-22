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
			try
			{
				//Initialize 'plug-ins'
				// This plugin read information from the Control file. This is done at initialize
				//		MODSIM initialization uses the model read with the file name provided by the plugin
				SurfGWModule sSurfGWModule = new SurfGWModule(CmdArgs);
				sSurfGWModule.messageOut += OnMessage;

				XYFileReader.Read(myModel, sSurfGWModule.xyFileName);
				myModel.OnMessage += OnMessage;
				myModel.OnModsimError += OnError;

				sSurfGWModule.InitializeRUN(ref myModel);

				int run = Modsim.RunSolver(myModel);

				sSurfGWModule.FinalizeMODSIM();

				if (run == 0)
				{
					Console.WriteLine("Successful completion of the MODSIM run!");
				}
			}
			catch (Exception ex)
			{
				Console.Write(ex.Message + Environment.NewLine + ex.StackTrace.ToString());
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
