using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Csu.Modsim.ModsimIO;
using Csu.Modsim.ModsimModel;
using MODSIM_GSFLOW_C;
using RTI.CWR.MWC_MODSIMUtils;

namespace MODSIM_GSFLOW
{
	class Program
	{
		public static Model myModel = new Model();
		private static RiparianAllocation allocationTool;

		static void Main(string[] CmdArgs)
		{
			try
			{
				//Process variables for riparian 'plug-in'
				bool riparianON = false;
				int _riparianCost = -999;
				if (CmdArgs.Contains("-RiparianON"))
				{
					riparianON = true;
					for (int i = 0; i<CmdArgs.Length;i++)
                    {
						if(CmdArgs[i]== "-RiparianON")
                        {
							CmdArgs[i] = "Delete";
							_riparianCost = int.Parse(CmdArgs[i + 1]);
							CmdArgs[i+1] = "Delete";
						}
                    }
					CmdArgs = CmdArgs.Where(w => w != "Delete").ToArray();
				}
				//Initialize 'plug-ins'
				if (CmdArgs.Contains(".contol"))
				{
					// This plugin read information from the Control file. This is done at initialize
					//		MODSIM initialization uses the model read with the file name provided by the plugin
					Console.WriteLine($"GSFLOW Args: { String.Join(" ", CmdArgs)}");

					SurfGWModule sSurfGWModule = new SurfGWModule(CmdArgs);
					sSurfGWModule.messageOut += OnMessage;

					if ((sSurfGWModule.Model_mode >= 10 && sSurfGWModule.Model_mode <= 13) || sSurfGWModule.Model_mode == 3) // modes with MODSIM
					{
						XYFileReader.Read(myModel, sSurfGWModule.xyFileName);
						myModel.OnMessage += OnMessage;
						myModel.OnModsimError += OnError;


						//Adding 'plug-ins'
						if (riparianON)
						{

							Console.WriteLine("\tActivating riparian logic allocation...");
							allocationTool = new RiparianAllocation(ref myModel, _riparianCost);
							allocationTool.messageOutRun += OnMessage;
						}

						sSurfGWModule.InitializeRUN(ref myModel);

						int run = Modsim.RunSolver(myModel);

						sSurfGWModule.FinalizeRUN();

						if (run == 0)
						{
							Console.WriteLine("Successful completion of the MODSIM run!");
						}
					}
					else
					{
						sSurfGWModule.InitializeRUN(ref myModel);
					}
					Console.WriteLine("Simulation finished.");
                }
                else
                {
					//MODSIM Only run
					Console.WriteLine($"Reading MODSIM file: {CmdArgs[0]}");
					XYFileReader.Read(myModel, CmdArgs[0]);

					//Adding 'plug-ins'
					if (riparianON)
					{
						Console.WriteLine("\tActivating riparian logic allocation...");
						allocationTool = new RiparianAllocation(ref myModel, _riparianCost);
						allocationTool.messageOutRun += OnMessage;
					}
					else
					{
						myModel.OnMessage += OnMessage;
						myModel.OnModsimError += OnMessage;
					}
					Console.WriteLine("Executing MODSIM model...");
					
					int run = Modsim.RunSolver(myModel);

					if (run == 0)
					{
						Console.WriteLine($"Sucessful completion of the MODSIM run!");
					}
				}
			}
			catch (Exception ex)
			{
				Console.Write(ex.Message + Environment.NewLine + ex.StackTrace.ToString());
			}
			//Console.ReadKey();
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
