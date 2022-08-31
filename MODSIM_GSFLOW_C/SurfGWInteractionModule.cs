
using Csu.Modsim.ModsimModel;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Data;
using System.Collections.Generic;
//using MWH.MWHUtils.GeneralUtils;
using Csu.Modsim.ModsimIO;
using System.Data.OleDb;
using System.Data.SQLite;

namespace MODSIM_GSFLOW_C
{
    public delegate void ProcessMessage(string msg);  // delegate

    public class SurfGWModule
    {
        private Model myModel;//= new Model();
        public SortedList myDepletions;
        public SortedList myAcretions;
        public Link[] MS_Links;
        public Node[] MS_Reservoirs;
        public bool Adv_TabF = false;
        public bool breakout = false;
        public Link m_releaseLnk;
        public Node m_ResNode;
        public double[] MF_LK_Vol = new double[1]; // this example has only one reservoir
        public double[] MF_ActDivs = new double[1];
        public double[] MF_Acc_Dep_Identifier = new double[1000];
        public double[] MS_Flows; //= new double[1000];
        public double[] MS_FlowsPREV;
        public double[] MS_FlowsLIMITED;
        public double[] MS_FlowsPOTENTIAL;
        public double[] Diversions = new double[1];
        public double[] agDemand = new double[1];
        public int[] IDivert = new int[1];
        public int[] IRelease = new int[1];
        public double[] EXCHANGE = new double[1];
        public double[] EXCHANGEPREV = new double[1];
        public double[] DELTAVOL = new double[1];
        public double[] DELTAVOLPREV = new double[1];
        public double[] LAKEVOL = new double[1];
        public double[] LAKEVAP = new double[1];
        public double[] MXLKVOL = new double[1];
        public double[] DPOOL = new double[1];
        public double[] STARTLAKEVOL = new double[1];
        public object RAD_list;
        public DataTable m_table;
        public DataTable map_table;
        //public  StreamWriter sw = new StreamWriter(@"Iter_Output.txt");
        //public  StreamWriter in_out_sw5 = new StreamWriter(@"Lake5_Ins_Outs.txt");   // for output to debugging file
        //public  StreamWriter in_out_sw6 = new StreamWriter(@"Lake6_Ins_Outs.txt");
        //public  StreamWriter all_links = new StreamWriter(@"All_Links_Q.txt");     // Another debug file
        public List<int> Main_Ditches = new List<int>();
        public bool afr, MS_GSF_converge;
        public int Model_mode, Nsegshold, Nlakeshold;
        public int[] startTime = new int[6];
        public int Process_mode;
        public int txtiter = 1;
        public long[] LinkHi = new long[1];
        public long[] LinkHi_Sv = new long[1];
        public string mappingFileName;
        public string xyFileName;
        private double accuracy;
        private int localMODSIMIter;
        private SWGW_MODSIMUtils swgwUtils;
        private int Numts;
        private string map_FileName;


        //Flags for custom project codes
        private bool WES_ON = false;

        public event ProcessMessage messageOut;


        //Fortran DLL interface

        [DllImport("GSFLOW_MODSIM.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void gsflow_prms(ref int Process_mode, ref bool afr, ref bool MS_GSF_converge, ref int Nsegshold, ref int nlakeshold, [In, Out] double[] Diversions, [In, Out] int[] IDivert, [In, Out] double[] EXCHANGE, [In, Out] double[] DELTAVOL, [In, Out] double[] LAKEVOL, [In, Out] double[] LAKEVAP, [In, Out] double[] agDemand);

        [DllImport("GSFLOW_MODSIM.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void put_prms_control_file([In] ref char[] command_line_args);

        [DllImport("GSFLOW_MODSIM.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void gsflow_prmsSettings([In, Out] ref int Numts, ref int Model_mode, ref int startTime, ref int File1_length, [In, Out] char[] FileName1, ref int File2_length, [In, Out] char[] FileName2);

        [DllImport("GSFLOW_MODSIM.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void LAK2MODSIM_InitLakes([In, Out] double[] DELTAVOL, [In, Out] double[] LAKEVOL, [In, Out] double[] MXLKVOL);

        public Model GetModel()
        {
            return myModel;
        }

        public SurfGWModule(string[] CmdArgs)
        {
            try
            {

                Numts = 1;
                int len_xyname, len_mapname;

                xyFileName = new String(' ', 80);
                mappingFileName = new String(' ', 80);
                char[] command_line_args = String.Join(" ", CmdArgs).PadRight(256).ToCharArray();
                len_xyname = xyFileName.Length;
                len_mapname = mappingFileName.Length;

                // Process_mode: 0 = run, 1 = delcare; 2 = initialize; 3 = clean; 4 = setdims
                Process_mode = 4;  // setdims
                afr = true;
                MS_GSF_converge = false;
                /* pass 2 arrays with NSS values, first has Diversion flag, second has ResRelease flag */
                /* need to pass DIVS */
                Nsegshold = 1;  //initialize temporarily
                Nlakeshold = 1;  //initialize temporarily
                try
                {
                    put_prms_control_file(ref command_line_args);
                    gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, Diversions, IDivert, EXCHANGE, DELTAVOL, LAKEVOL, LAKEVAP, agDemand);
                }
                catch (Exception ex)
                {

                }
                /* need file name of mapping file, read from GSFLOW Control File
                   file has link Name, iseg, diversion, ResRelease */

                // Convert string to Fortran array of characters.
                char[] xyPathChars = ToCharacterArrayFortran(xyFileName, len_xyname);
                char[] mapPathChars = ToCharacterArrayFortran(mappingFileName, len_mapname);

                gsflow_prmsSettings(ref Numts, ref Model_mode, ref startTime[0], ref len_xyname, xyPathChars, ref len_mapname, mapPathChars);
                //Start time is [0]=year [1]=month [2]=day
                //End simulation using Numts
                xyFileName = new string(xyPathChars);
                map_FileName = new string(mapPathChars);

                map_FileName = GetFullPath(map_FileName);
                xyFileName = GetFullPath(xyFileName);
                
                //These are the options to add in the .control file to run different versions
                // 0=GSFLOW; 1=PRMS; 2=MODFLOW; 10=MODSIM-GSFLOW; 11=MODSIM-PRMS; 12=MODSIM-MODFLOW; 13=MODSIM
                //  Option: MODSIM-GSFLOW is the fully integrated mode.
                if (Model_mode < 13 | Model_mode > 20) // > 20 means a special PRMS-only mode
                {
                    Process_mode = 1; // declare
                    gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, Diversions, IDivert, EXCHANGE, DELTAVOL, LAKEVOL, LAKEVAP, agDemand);
                }

                // Redimension arrays to equal number of segments and lakes
                Diversions = (double[])ResizeArray(Diversions, new int[] { Nsegshold });
                agDemand = (double[])ResizeArray(agDemand, new int[] { Nsegshold });
                IDivert = (int[])ResizeArray(IDivert, new int[] { Nsegshold });
                IRelease = (int[])ResizeArray(IRelease, new int[] { Nsegshold });
                EXCHANGE = (double[])ResizeArray(EXCHANGE, new int[] { Nsegshold });
                EXCHANGEPREV = (double[])ResizeArray(EXCHANGEPREV, new int[] { Nsegshold });
                DELTAVOL = (double[])ResizeArray(DELTAVOL, new int[] { Nlakeshold });
                DELTAVOLPREV = (double[])ResizeArray(DELTAVOLPREV, new int[] { Nlakeshold });
                LAKEVOL = (double[])ResizeArray(LAKEVOL, new int[] { Nlakeshold });
                MXLKVOL = (double[])ResizeArray(MXLKVOL, new int[] { Nlakeshold });
                STARTLAKEVOL = (double[])ResizeArray(STARTLAKEVOL, new int[] { Nlakeshold });
                DPOOL = (double[])ResizeArray(DPOOL, new int[] { Nlakeshold });
                LAKEVAP = (double[])ResizeArray(LAKEVAP, new int[] { Nlakeshold });
                LinkHi = (long[])ResizeArray(LinkHi, new int[] { Nsegshold });
                LinkHi_Sv = (long[])ResizeArray(LinkHi, new int[] { Nsegshold });

                if (Model_mode < 12 | Model_mode > 20) // > 20 means a special PRMS-only mode
                {
                    Process_mode = 2; // initialize
                    gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, Diversions, IDivert, EXCHANGE, DELTAVOL, LAKEVOL, LAKEVAP, agDemand);
                }

                Process_mode = 0; // run



            }
            catch (Exception ex)
            {
                messageOut(ex.Message);
                //Console.ReadLine();
            }
        }


        public void InitializeRUN(ref Model m_Model)
        {
            messageOut($"\tUsing DB:{map_FileName}");
            messageOut($"\tUsing xy File:{xyFileName}");
            if (Model_mode < 10) // GSFLOW and PRMS-only
            {
                for (int i = 0; i < Numts; i++)
                {
                    gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, Diversions, IDivert, EXCHANGE, DELTAVOL, LAKEVOL, LAKEVAP, agDemand);
                }

                Process_mode = 3; // clean
                gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, Diversions, IDivert, EXCHANGE, DELTAVOL, LAKEVOL, LAKEVAP, agDemand);
            }

            else
            // do something different if MODSIM-MODFLOW **** CAUTION ****
            {
                //myModel = new Model();
                m_Model.Init += OnInitialize;
                m_Model.IterBottom += OnIterationBottom;
                m_Model.IterTop += OnIterationTop;
                m_Model.Converged += OnIterationConverge;
                m_Model.End += OnFinished;
                //myModel.OnMessage += OnMessage;
                //myModel.OnModsimError += OnError;
                myModel = m_Model;


                //if (Model_mode == 11) // MODSIM-PRMS
                //{
                //  gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, Diversions, IDivert, EXCHANGE,DELTAVOL, LAKEVOL, LAKEVAP);
                //}
                //XYFileReader.Read(myModel, xyFileName);
                accuracy = myModel.ScaleFactor;// Math.Pow(10.0, (double)myModel.accuracy);

                swgwUtils = new SWGW_MODSIMUtils(ref myModel);
                if (Model_mode != 13)  // MODSIM-only mode
                {
                    swgwUtils.PrepareMODSIMNetwork(map_FileName, out EXCHNGVol_Tolerance, out LAKEVol_Tolerance);
                }

                XYFileWriter.Write(myModel, xyFileName.Replace(".xy", "MSGSF.xy"));
                //Delete the existing output file -- useful for debugging.
                string outputFile = xyFileName.Replace(".xy", "MSGSFOUTPUT.sqlite");
                if (File.Exists(outputFile))
                    File.Delete(outputFile);
                //Modsim.RunSolver(myModel);

            }
        }

        public void FinalizeMODSIM()
        {
            try
            {
                //Copy output to the original file name - Custom Output carries the MF Dep/Acc
                File.Copy(xyFileName.Replace(".xy", "MSGSFOUTPUT.sqlite"), xyFileName.Replace(".xy", "OUTPUT.sqlite"), true);
                messageOut(" MF_MS Simulation Finished Succesfully");

            }
            catch (Exception ex)
            {
                messageOut(ex.Message + Environment.NewLine + ex.StackTrace.ToString());
                Console.ReadLine();
            }
}

        private string GetFullPath(string FileName)
        {
            FileName = FileName.Trim();
            string fileDir = Directory.GetCurrentDirectory();
            bool addPath = false;
            if (FileName != Path.GetFullPath(FileName)) addPath = true;
            while (FileName.StartsWith("..\\"))
            {
                fileDir = Directory.GetParent(fileDir).FullName;
                FileName = FileName.Substring(3);
            }
            if (addPath) FileName = fileDir + "\\" + FileName;
            return FileName;
        }

        private Array ResizeArray(Array arr, int[] newSizes)
        {
            if (newSizes.Length != arr.Rank)
                throw new ArgumentException("arr must have the same number of dimensions " +
                                            "as there are elements in newSizes", "newSizes");

            var temp = Array.CreateInstance(arr.GetType().GetElementType(), newSizes);
            int length = arr.Length <= temp.Length ? arr.Length : temp.Length;
            Array.ConstrainedCopy(arr, 0, temp, 0, length);
            return temp;
        }

        private double uConvToMODFLOW, uConvRateToMODSIM;
        private void OnInitialize()
        {
            if (Model_mode != 13)  // Model_mode = 13: MODSIM-only
            {
                //Dimension arrays to the input table
                Array.Resize<double>(ref MS_Flows, swgwUtils.m_SyncTblSEG.Rows.Count);
                Array.Resize<double>(ref MS_FlowsPREV, swgwUtils.m_SyncTblSEG.Rows.Count);
                Array.Resize<double>(ref MS_FlowsLIMITED, swgwUtils.m_SyncTblSEG.Rows.Count);
                Array.Resize<Link>(ref MS_Links, swgwUtils.m_SyncTblSEG.Rows.Count);
                Array.Resize<Node>(ref MS_Reservoirs, swgwUtils.m_SyncTblRES.Rows.Count);

                // Also, initialize MF_Acc_Dep (accretion/depletion) variables
                // TODO: Is this needed - they should be zero
                int i = 0;
                Link m_Link;
                foreach (DataRow m_Row in swgwUtils.m_SyncTblSEG.Rows)// i = 0; i < MF_Acc_Dep.Length; i++)
                {
                    //MF_Acc_Dep_Identifier[i] = (double) m_Row["iseg"];
                    if (i != (int)(double.Parse(m_Row["iseg"].ToString()) - 1)) throw new Exception("Iseg doesn't match the index of the array");
                    MS_Flows[i] = 0;
                    IDivert[i] = (int)double.Parse(m_Row["Diversion"].ToString());
                    IRelease[i] = (int)double.Parse(m_Row["ResRelease"].ToString());
                    if (IRelease[i] >= 1)
                    {
                        IDivert[i] = 2;  // Fortran needs to distinguish between diversion and reservoir release
                    }                    // A code of 2 will signify a reservoir release
                    if (m_Row["Link Name"].ToString() != "")
                    {
                        m_Link = myModel.FindLink(m_Row["Link Name"].ToString()); // Use .FindLink() instead
                    }
                    else
                    {
                        if (m_Row["Link Name"].ToString() == "" && (Convert.ToInt32(m_Row["Diversion"]) != 0 || Convert.ToInt32(m_Row["ResRelease"]) != 0))
                        {
                            messageOut("Diversion or reservoir release specified for missing MODSIM link in Mapping_Info table");
                            System.Environment.Exit(1);
                        }
                        //m_Link = myModel.AddNewLink(true);
                        //m_Link.name = "dummy_" + i.ToString();
                        m_Link = null;
                    }

                    MS_Links[i] = m_Link;
                    i += 1;
                }

                //Initialize Reservoir arrays
                Node m_Res;
                i = 0;
                foreach (DataRow m_Row in swgwUtils.m_SyncTblRES.Rows)// i = 0; i < MF_Acc_Dep.Length; i++)
                {
                    if (i != (short.Parse(m_Row["GSF_LAK_ID"].ToString())) - 1) throw new Exception("Iseg doesn't match the index of the array");
                    m_Res = myModel.FindNode((string)m_Row["MODSIM_Name"]);
                    MS_Reservoirs[i] = m_Res;

                    //If PRMS-MODSIM initialize the reservoir evaporation arrays
                    if (Model_mode == 11) //PRMS-MODSIM mode
                    {
                        //PRMS calculates potential evaporation with is used in MODSIM to compute reservoir evaporation
                        //  MODSIM user input will be overwritten 
                        MS_Reservoirs[i].mnInfo.evaporationrate = (double[,])ResizeArray(MS_Reservoirs[i].mnInfo.evaporationrate, new int[] { MS_Reservoirs[i].mnInfo.start_storage.Length, 1 });
                    }
                    i += 1;
                }

                // Initialize Diversions array to 0
                //Diversions = 0.0;

                //initialize custom output variables
                Csu.Modsim.NetworkUtils.ModelOutputSupport m_OutputSupport = (Csu.Modsim.NetworkUtils.ModelOutputSupport)myModel.OutputSupportClass;
                m_OutputSupport.AddUserDefinedOutputVariable(myModel, "MF_Depletion", true, false, "Flow");
                m_OutputSupport.AddUserDefinedOutputVariable(myModel, "MF_Accretion", true, false, "Flow");
                m_OutputSupport.AddCurrentUserLinkOutput += addLinkMFOutput;

                //Setting units conversion factor
                //NOTE: All MODSIM internal variables are strictly volumes (per time step)
                //      There are NO rates anywhere in the custom variables
                if (myModel.UseMetricUnits)
                {
                    // 1000m3 is the default units for MODSIM in metric mode
                    // MODFLOW assumed to run in m3.
                    uConvToMODFLOW = 1000;
                    //  PRMS will always send evap in inches.  We need to apply the conversion meters in metric.
                    uConvRateToMODSIM = 0.0254;
                }
                else
                {
                    // the default units for MODSIM in english mode at run time is 
                    // total acre-ft for the entirety of the time step
                    // MODFLOW assumed to run in ft3.
                    uConvToMODFLOW = 43560.0001;
                    //temporary fix for RR PRMS
                    //                uConvToMODFLOW = 1233.48;
                    //  PRMS will always send evap in inches.  We need to apply the conversion feet in english.
                    uConvRateToMODSIM = 1 / 12;
                }

                // Write a header row to the streamwriter for evaluating convergence with R
                //sw.WriteLine("TS iseg Exchange_Prev Exchange");

                // Store max link capacity for restoration of MODFLOW-adjusted maximum amounts, 
                // arbitrarily choosing the first link in the synchronization table
                i = 0;
                foreach (DataRow m_Row in swgwUtils.m_SyncTblSEG.Rows)// i = 0; i < MF_Acc_Dep.Length; i++)
                {
                    if (swgwUtils.m_SyncTblSEG.Rows[i]["Link Name"].ToString() != "")
                    {
                        Link resRelLink = myModel.FindLink(swgwUtils.m_SyncTblSEG.Rows[i]["Link Name"].ToString());
                        if (resRelLink.m.maxVariable.dataTable.Rows.Count > 0)
                        {
                            LinkHi[i] = (long)resRelLink.m.maxVariable.dataTable.Rows[0][1];
                        }
                        else
                        {
                            LinkHi[i] = resRelLink.m.maxConstant;
                        }
                    }
                    i += 1;
                }
                LinkHi_Sv = (long[])LinkHi.Clone();

                // Initialize variable to iterate between MODSIM and GSFLOW
                MFRunYet = false;

                // Initialize local MODSIM iteration count
                localMODSIMIter = 0;
            }
        }

        private void OnIterationTop()
        {
            if (Model_mode != 13)  // not MODSIM-only mode
            {
                //Asign accretions and depletions to the MODSIM network.
                for (int i = 0; i < MS_Links.Length; i++)
                {
                    double m_value = EXCHANGE[i] * accuracy / uConvToMODFLOW;  // This sets the MF returned GW-SW acc/dep
                                                                               // units conversion to MODSIM is required
                                                                               // Need the -1 to account for 0-based indexing in C#
                    try
                    {
                        if (MS_Links[i] != null)
                        {
                            assignDepAcc(MS_Links[i].name, m_value);
                        }

                    }
                    catch (NullReferenceException ex)
                    {
                        continue;
                    }
                }

                // Implement Reservoir accretions/depletions
                for (int i = 0; i < MS_Reservoirs.Length; i++)
                {
                    double m_value = DELTAVOL[i] * accuracy / uConvToMODFLOW;  // This sets the MF returned GW-SW acc/dep
                                                                               // Need the -1 to account for 0-based indexing in C#
                                                                               // m_value /= 7; //Only for Carson (weekly)                                                                       
                    try
                    {
                        assignDepAcc(MS_Reservoirs[i].name, m_value);
                    }
                    catch (NullReferenceException ex)
                    {
                        continue;
                    }

                    // reset starting volume to the last converged MODFLOW reservoir volumes
                    // but don't do this following the steady-state initialization step
                    if (!MFRunYet && localMODSIMIter == 0 && myModel.mInfo.CurrentModelTimeStepIndex != 0)
                    {
                        MS_Reservoirs[i].mnInfo.start = (long)(STARTLAKEVOL[i] * accuracy / uConvToMODFLOW);
                        MS_Reservoirs[i].mnInfo.stend = MS_Reservoirs[i].mnInfo.start;
                    }
                }

                // Curtail reservoir release by setting upper bound where appropriate
                if (MFRunYet)
                {
                    for (int i = 0; i < swgwUtils.m_SyncTblSEG.Rows.Count; i++)
                    {
                        if (Math.Abs(MS_FlowsLIMITED[i] - MS_Flows[i]) > EXCHNGVol_Tolerance)  // The array MS_Flows returned with altered values if MODFLOW determines not enough flow available
                                                                                               // Fix the 0.01 to instead be a global tolerance variable
                        {
                            // Set upper limit on link so as not to allow more through than physically available  // int j = Convert.ToInt16(m_SyncTblSEG.Rows[i]["iseg"].ToString());  // DataRow[] m_row = m_SyncTblSEG.Select("iseg = " + j.ToString());
                            DataRow m_row = swgwUtils.m_SyncTblSEG.Rows[i];

                            //Check to ensure the current link is a reservoir release link

                            // 10-13-2018: What I'm hoping is a bug fix for Wes's model:
                            // In the IF() statement that follows, it was originally written to check that the volume in the reservoir currently being checked
                            // wasn't nearing the deadpool.  However, I found that sometimes MODSIM would get into this region while MODFLOW was not.  Therefore
                            // amended code to check both MODSIM and MODFLOW for the reservoir being checked.  To do this, I used the m_syncRES table to lookup
                            // the MODFLOW Lake # for the current reservoir node name, assuming that the "AssocRes" value for the current row contains the 
                            // reservoir feeding the current link.  For example, there are three ways to lake get 5 (And remember everything is 0 based):
                            // So instead of writing "LAKE[4]", it could be written as either of the following:
                            // LAKEVOL[Int32.Parse(m_row["AssocRes"].ToString().Substring(m_row["AssocRes"].ToString().LastIndexOf('_') + 1)) - 1]  // Assumes all models use a "Lake_5" type format, or in other words that there is an "_" (underscore) followed by the lake number
                            // LAKEVOL[Int32.Parse(m_SyncTblRES.Select("MODSIM_Name Like '" + m_row["AssocRes"].ToString() + "'")[0][0].ToString()) - 1]
                            //
                            // For now, I'm going to use the latter to amend the if statement.

                            // TODO: m_row["Link Name"] in the 3rd line below won't work if it isn't specified in the mapping info datatable
                            if (Convert.ToInt16(m_row["ResRelease"]) > 0)
                            {
                                Link resRelLink = myModel.FindLink(m_row["Link Name"].ToString());

                                //This if avoids crash trying to find node with empty values in the database.
                                //  not sure how it was running without this
                                //  Need to check that it doesn't create an issue with not setting flag 
                                //     m_row["adjted"] = 1;
                                if (m_row["AssocRes"].ToString() != "")
                                {
                                    //if (!(((double)myModel.FindNode(m_row["AssocRes"].ToString()).mnInfo.stend > (0.9 * (double)myModel.FindNode(m_row["AssocRes"].ToString()).m.max_volume)) 
                                    //    || ((LAKEVOL[int.Parse(m_SyncTblRES.Select("MODSIM_Name Like '" + m_row["AssocRes"].ToString() + "'")[0][0].ToString()) - 1] / uConvToMODFLOW * accuracy) > (0.9 * (double)myModel.FindNode(m_row["AssocRes"].ToString()).m.max_volume))))
                                    //{
                                    //    // Recall that MS_Flows is in GSFLOW/MODFLOW units and therefore needs to be converted back to MODSIM units before being stuffed back into a MODSIM-used parameter
                                    //    if (Convert.ToInt32(MS_Flows[i] / uConvToMODFLOW * accuracy) == 0)
                                    //    {
                                    //        resRelLink.mlInfo.hi = Convert.ToInt32(0.0001 / uConvToMODFLOW * accuracy);
                                    //    }
                                    //    // The next else if statement added in response to the bug affecting Wes's model
                                    //    //else if (MS_FlowsLIMITED[i] > MS_Flows[i] && (!(((double)myModel.FindNode(m_row["AssocRes"].ToString()).mnInfo.stend > (0.9 * (double)myModel.FindNode(m_row["AssocRes"].ToString()).m.max_volume)) || ((LAKEVOL[Int32.Parse(m_SyncTblRES.Select("MODSIM_Name Like '" + m_row["AssocRes"].ToString() + "'")[0][0].ToString()) - 1] / uConvToMODFLOW * accuracy) > (0.9 * (double)myModel.FindNode(m_row["AssocRes"].ToString()).m.max_volume)))))
                                    //    //{
                                    //    //    resRelLink.mlInfo.hi = Convert.ToInt32(MS_FlowsLIMITED[i] / uConvToMODFLOW * accuracy);
                                    //    //}
                                    //    else
                                    //    {
                                    //        resRelLink.mlInfo.hi = Convert.ToInt32((MS_Flows[i] + MS_FlowsLIMITED[i]) / 2 / uConvToMODFLOW * accuracy);
                                    //    }

                                    //    // Flag row as having been adjusted for restoring later
                                    //    m_row["adjted"] = 1;
                                    //    // messageOut("|" + resRelLink.mlInfo.hi + "|");
                                    //}
                                    //else if ((LAKEVOL[Int32.Parse(m_SyncTblRES.Select("MODSIM_Name Like '" + m_row["AssocRes"].ToString() + "'")[0][0].ToString()) - 1] / uConvToMODFLOW * accuracy) > (double)myModel.FindNode(m_row["AssocRes"].ToString()).m.max_volume)
                                    //{
                                    //    resRelLink.mlInfo.hi = Math.Max(resRelLink.mlInfo.hi, Convert.ToInt32((LAKEVOL[Int32.Parse(m_SyncTblRES.Select("MODSIM_Name Like '" + m_row["AssocRes"].ToString() + "'")[0][0].ToString()) - 1] / uConvToMODFLOW * accuracy) - (double)myModel.FindNode(m_row["AssocRes"].ToString()).m.max_volume));
                                    //}

                                    //// Flag row as having been adjusted for restoring later
                                    //m_row["adjted"] = 1;
                                    //// messageOut("|" + resRelLink.mlInfo.hi + "|");
                                }
                            }
                        }
                    }
                }
            }
            localMODSIMIter++;
        }

        //private  void OnMessage(string message)
        //{
        //    messageOut(message + "\n");
        //}

        //private  void OnError(string message)
        //{
        //    messageOut(message + "\n");
        //}

        private void OnIterationBottom()
        {
            if (WES_ON)
            {
                double gvflow;
                Link gv_gage;
                Link al_link;
                int month;
                DateTime currentDate = myModel.TimeStepManager.Index2Date(myModel.mInfo.CurrentModelTimeStepIndex, TypeIndexes.ModelIndex);

                month = currentDate.Month;
                gv_gage = myModel.FindLink("1");
                al_link = myModel.FindLink("divtabsCV-8015trans-diversions-c82-19790702-20150928.txt");

                // check flow at gv < 200 cfs, convert to ac-ft/mo, multiply by accuracy
                gvflow = Convert.ToDouble(gv_gage.mlInfo.flow);

                if (month >= 4 & month < 10)  // 'irrigation season
                {
                    if (gvflow <= ((200 * 86400 * 7) / uConvToMODFLOW) * accuracy)
                    {
                        // convert 200 cfs to acre-ft per stress period
                        // set 1/3-2/3 split through capacities and inflows
                        al_link.mlInfo.hi = (long)(0.34 * gvflow);
                        al_link.mlInfo.lo = (long)(0.33 * gvflow);
                    }
                    else
                    {
                        // set max capacity to 100 cfs, current max capacity ~80 but was/could be higher
                        al_link.mlInfo.hi = (long)((100.0 * 86400 * 7) / uConvToMODFLOW * accuracy);
                    }
                }
            }

        }

        private void assignDepAcc(String m_Name, double m_Value)
        {
            //Value returned from MODFLOW in m3 and MODISM uses 1000m3, but using three decimal precision multiply by 1000 - No conversion needed
            Link depLink = myModel.FindLink("MF_Dep_" + m_Name);
            Link accLink = myModel.FindLink("MF_Acc_" + m_Name);

            if (depLink != null && accLink != null)
            {
                if (m_Value > 0)
                {
                    //Acretions
                    depLink.mlInfo.hi = 0;
                    accLink.mlInfo.hi = Convert.ToInt32(Math.Round(m_Value, 0));
                }
                else
                {
                    //Depletions
                    //set Depletions to the stream network as upper bounds in the high priority links
                    depLink.mlInfo.hi = Convert.ToInt32(Math.Round(-m_Value, 0));
                    accLink.mlInfo.hi = 0;
                }
            }
            else
            {
                messageOut("ERROR !!!  Missing Acc/Dep Links for " + m_Name);
            }
        }
        //Add MF output to the original link
        private void addLinkMFOutput(Link m_link, DataRow m_row)
        {
            try
            {
                if (myModel.LinkNameExists("MF_Dep_" + m_link.name, true))
                {
                    Link m_MFLink = myModel.FindLink("MF_Dep_" + m_link.name);
                    //TODO: check if the variable can replace the accuracy
                    if (m_MFLink != null) { m_row["MF_Depletion"] = (double)m_MFLink.mlInfo.flow / accuracy; }
                    m_MFLink = myModel.FindLink("MF_Acc_" + m_link.name);
                    if (m_MFLink != null) { m_row["MF_Accretion"] = (double)m_MFLink.mlInfo.flow / accuracy; }
                }
            }
            catch { }

        }

        bool MFRunYet = false;   // Needed in MODFLOWComputeReturns

        private void OnIterationConverge()
        {
            // Some debug code
            DateTime currentDate = myModel.TimeStepManager.Index2Date(myModel.mInfo.CurrentModelTimeStepIndex, TypeIndexes.ModelIndex);
            DateTime chkDate = new DateTime(1980, 10, 19);
            int equiv = DateTime.Compare(currentDate, chkDate);
            if (equiv == 0)
            {
                string debugbreakpt = "stop here";
                debugbreakpt += "do something more";
            }

            if (Model_mode != 13)
            {
                bool MS_GSF_converge = false;

                //Check for a minimum number of iteration after MS-GSF has not converged
                if (localMODSIMIter >= 7)
                {
                    // extract the MODSIM calculated diversion values for inserting into an array that is passed to MF
                    for (int i = 0; i < swgwUtils.m_SyncTblSEG.Rows.Count; i++)
                    {
                        MS_FlowsPREV[i] = MS_Flows[i];
                        // Only add flows for diversion links.
                        if (IDivert[i] > 0)
                        {
                            MS_Flows[i] = (double)MS_Links[i].mlInfo.flow / accuracy * uConvToMODFLOW; //flow values converted to MODFLOW 
                        }
                        // MODFLOW interprets a specified release from a lake of 0.0 as a flag, specifically a flag
                        // telling MODFLOW to calculate the natural outflow from the based on the outlet's bed elevation
                        // this prevents that flag from being tripped.
                        if (IRelease[i] > 0 && MS_Flows[i] == 0)
                        {
                            MS_Flows[i] = 0.0001;
                        }
                        EXCHANGEPREV[i] = EXCHANGE[i];

                        // Synchronize MS_FlowsLIMITED because if different when returning from GSFLOW, 
                        // need to do something
                        MS_FlowsLIMITED[i] = MS_Flows[i];
                    }

                    // The following function also used in OnInitialize()
                    Store_Net_Res_AccDepl();

                    if (!breakout)
                    {
                        ProcessGSFLOW_SSResults();
                        //These won't be necesary if executed OnInitialize
                        myModel.mInfo.convg = false;
                        myModel.mInfo.Iteration = 0;
                        localMODSIMIter = 0;
                        return;

                    }


                    //ETS - 08/22/21 this seems to be for debugging hardcoded from some specific case - commented.
                    //if (myModel.mInfo.CurrentModelTimeStepIndex >= 2504 | myModel.mInfo.Iteration >= (maxNoIterations - 500))
                    //{
                    //    MS_Flows[21] = MS_Flows[21];
                    //}

                    if (Model_mode <= 12)
                    {
                        gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, MS_Flows, IDivert, EXCHANGE, DELTAVOL, LAKEVOL, LAKEVAP, agDemand); // run mode
                    }

                    //// Check for MODFLOW-determined limitations in release and/or diversion amounts
                    //// This code necessary because of 
                    //for (int i = 0; i < m_SyncTblSEG.Rows.Count; i++)
                    //{
                    //    if (MS_FlowsLIMITED[i] > MS_Flows[i])  // The array MS_Flows returned with altered values if MODFLOW determines not enough flow available
                    //    {
                    //        // Set upper limit on link so as not to allow more through than physically available

                    //    }
                    //}

                    //
                    // Lake 1 (inline lake)
                    double LK5_in_val;
                    //double LK6_in_val;
                    //double up1_val;
                    //double up2_val;
                    //double up3_val;
                    //double up4_val;
                    //double up5_val;
                    //double up6_val;
                    //double rdm_val1;
                    //double rdm_val2;
                    //double l542_in;
                    //double l542_out;
                    //double LK5_out_val1;
                    //double LK7_out2;
                    //double LK6_out_val1;
                    //double LK6_out2;
                    ////double LK9_out_val2;
                    ////double LK9_out_val3;
                    //Link LK5_in = myModel.FindLink("219");
                    //Link LK5_out1 = myModel.FindLink("505");
                    //Link LK7_out_2 = myModel.FindLink("523");

                    //Link up1_lnk = myModel.FindLink("543");
                    //Link up2_lnk = myModel.FindLink("217");

                    //Link LK6_in = myModel.FindLink("198");
                    //Link LK6_out1 = myModel.FindLink("lake_6_out_1");
                    //Link LK6_out_2 = myModel.FindLink("517");

                    //LK5_in_val = (double)LK5_in.mlInfo.flow / accuracy * uConvToMODFLOW;
                    //LK7_out2 = (double)LK7_out_2.mlInfo.flow / accuracy * uConvToMODFLOW;
                    //LK5_out_val1 = (double)LK5_out1.mlInfo.flow / accuracy * uConvToMODFLOW;

                    //up1_val = (double)up1_lnk.mlInfo.flow / accuracy * uConvToMODFLOW;
                    //up2_val = (double)up2_lnk.mlInfo.flow / accuracy * uConvToMODFLOW;

                    //LK6_in_val = (double)LK6_in.mlInfo.flow / accuracy * uConvToMODFLOW;
                    //LK6_out2 = (double)LK6_out_2.mlInfo.flow / accuracy * uConvToMODFLOW;
                    //LK6_out_val1 = (double)LK6_out1.mlInfo.flow / accuracy * uConvToMODFLOW;

                    //double LK5_oldvol;
                    ////double LK2_oldvol;
                    //double LK5_newvol;
                    ////double LK2_newvol;

                    //LK5_oldvol = MS_Reservoirs[4].mnInfo.start / accuracy * uConvToMODFLOW;
                    ////LK2_oldvol = MS_Reservoirs[1].mnInfo.start / accuracy * uConvToMODFLOW;

                    //LK5_newvol = MS_Reservoirs[4].mnInfo.stend / accuracy * uConvToMODFLOW;
                    ////LK2_newvol = MS_Reservoirs[1].mnInfo.stend / accuracy * uConvToMODFLOW;

                    //////messageOutLine(LK1_in.ToString() + " " + LK1_out.ToString() + " " + LK2_tot_in.ToString() + " " + LK2_out_val.ToString());
                    ////in_out_sw.WriteLine(LK1_in_val + " " + LK1_out_val + " " + LK1_oldvol + " " + LK1_newvol + " " + DELTAVOL[0].ToString() + " " + LK2_tot_in + " " + LK2_out_val + " " + LK2_oldvol + " " + LK2_newvol + " " + DELTAVOL[1].ToString());
                    //in_out_sw5.WriteLine(Convert.ToInt32(myModel.mInfo.CurrentModelTimeStepIndex + 1) + " " + iterCount + " " + LK5_in_val + " " + (LK5_out_val1) + " " + LK5_oldvol + " " + LK5_newvol + " " + DELTAVOL[6].ToString());
                    //in_out_sw5.Flush();

                    //double LK6_oldvol;
                    //double LK6_newvol;
                    //LK6_oldvol = MS_Reservoirs[5].mnInfo.start / accuracy * uConvToMODFLOW;
                    //LK6_newvol = MS_Reservoirs[5].mnInfo.stend / accuracy * uConvToMODFLOW;
                    //in_out_sw6.WriteLine(iterCount + " " + LK6_in_val + " " + LK6_out_val1 + " " + LK6_oldvol + " " + LK6_newvol + " " + DELTAVOL[5].ToString());
                    //in_out_sw6.Flush();

                    // Attempting another way to view the link flows.  Print out all links to a file to hone in on where oscillations are occurring
                    for (int i = 0; i < MS_Links.Length; i++)
                    {
                        if (MS_Links[i] != null)
                        {
                            Link expLink = myModel.FindLink(MS_Links[i].name);
                            //all_links.WriteLine(Convert.ToInt32(myModel.mInfo.CurrentModelTimeStepIndex + 1) + " " + MS_Links[i].name + " " + "iter_" + txtiter.ToString() + " " + (double)expLink.mlInfo.flow / accuracy * uConvToMODFLOW);
                            //all_links.Flush();
                        }
                        else
                        {
                            //all_links.WriteLine(Convert.ToInt32(myModel.mInfo.CurrentModelTimeStepIndex + 1) + " " + (i +1).ToString() + " " + "iter_" + txtiter.ToString() + " 0.0");
                            //all_links.Flush();
                        }
                    }
                    txtiter += 1;
                    // to here

                    //Check for convergence between MODSIM and MODFLOW
                    MS_GSF_converge = Get_Div_Chng();
                    MS_GSF_converge = MS_GSF_converge && MFRunYet;
                    if (Model_mode != 12)   //Different flow of console output in MODSIM-MODFLOW mode, don't want the '.' in this case 
                    {
                        messageOut(".");
                    }

                    swgwUtils.iterCount += 1;

                    if (swgwUtils.iterCount >= swgwUtils.maxNoIterations)//(myModel.mInfo.Iteration > myModel.maxit)
                    {
                        messageOut("\r\n MODSIM & GSFLOW Ran into maximum number of iterations - Warning !!! models have not converged.");
                        MS_GSF_converge = true;
                    }
                    //ETS - This check make sense if the iteration is reset every time that MODSIM restart. 
                    if (myModel.mInfo.Iteration > myModel.maxit)
                    {
                        messageOut("\r\n MODSIM ran into maximum number of iterations - Warning !!! models have not converged.");
                        MS_GSF_converge = true;

                        update_lake_synchronization();
                    }

                    if (Model_mode == 0 || Model_mode == 1 || Model_mode == 2 || Model_mode == 3)
                    {
                        afr = true;
                        swgwUtils.iterCount = 0;
                    }
                    else
                    {
                        //For modes MODSIM-GSFLOW, MODSIM-MODFLOW, MODSIM_PRMS(AG)
                        if (!MS_GSF_converge || swgwUtils.iterCount < 2)
                        {
                            afr = false;
                            MFRunYet = true;
                            //MODFLOWConverge = CheckOscillating(MF_Segs);
                            //MODSIM converged but we are sending it back to iterate with MODFLOW values.

                            MS_GSF_converge = false;

                            //Processing Deamnds from Ag.Package
                            for (int i = 0; i < swgwUtils.m_SyncTblSEG.Rows.Count; i++)
                            {
                                //Setting the MODSIM demand to the value set from GSFLOW
                                //   Using the diversions array 
                                if (Convert.ToInt16(swgwUtils.m_SyncTblSEG.Rows[i]["Diversion"]) > 0 && agDemand[i] >= 0)
                                {
                                    //Assumes that the demand is connected to the link mapped to the segment.
                                    Node demNode = MS_Links[i].from.InflowLinks.link.from;
                                    if (demNode.nodeType == NodeType.Demand)
                                    {
                                        int hydState = demNode.mnInfo.hydStateIndex;
                                        demNode.mnInfo.nodedemand[myModel.mInfo.CurrentModelTimeStepIndex, hydState] = (long)Math.Round(agDemand[i] * accuracy / uConvToMODFLOW, 0);
                                        messageOut($"                    MS_GSF Setting Demands for {demNode.name} to {agDemand[i]}");
                                    }
                                }

                            }

                        }
                        else
                        {
                            gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, MS_Flows, IDivert, EXCHANGE, DELTAVOL, LAKEVOL, LAKEVAP, agDemand); // converged mode
                            afr = true;
                            messageOut("           MS_GSF Last Iteration: " + swgwUtils.iterCount + " Stress Period: " + myModel.mInfo.CurrentModelTimeStepIndex);
                            swgwUtils.iterCount = 0;
                            MFRunYet = false;

                            // Reset adjusted link.hi's
                            // Restore original link capacities
                            for (int i = 0; i < swgwUtils.m_SyncTblSEG.Rows.Count; i++)
                            {
                                if (Convert.ToInt32(swgwUtils.m_SyncTblSEG.Rows[i]["adjted"]) > 0)
                                {
                                    Link resRelLink = myModel.FindLink(swgwUtils.m_SyncTblSEG.Rows[i]["Link Name"].ToString());
                                    //ETS - [TODO] it seems like this array should use i index not 0
                                    //      [TODO] This may need to account for time series of "hi's" 
                                    //             (See Enrique's Notes from 8/22/2021 for more information)
                                    resRelLink.mlInfo.hi = LinkHi_Sv[i];

                                    // Flag row's "adjusted" column back to not adjusted
                                    swgwUtils.m_SyncTblSEG.Rows[i]["adjted"] = 0;
                                }
                            }

                            //Set local MODSIM iteration count
                            localMODSIMIter = 0;
                            myModel.mInfo.Iteration = 0;
                        }
                    }
                }
                //Set local MODSIM iteration count
                myModel.mInfo.convg = MS_GSF_converge;
            }
        }

        private void ProcessGSFLOW_SSResults()
        {
            //Need to know the value of LAKEVOL for the first (SS)
            // If first iteration of first time step, overide MODSIM Lake volumes
            if (!MFRunYet && (myModel.mInfo.CurrentModelTimeStepIndex == 0) && !breakout)
            {
                for (int i = 0; i < MS_Reservoirs.Length; i++)
                {
                    if (MS_Reservoirs[i] != null)
                    {
                        MXLKVOL[i] = MS_Reservoirs[i].m.max_volume / accuracy * uConvToMODFLOW;
                    }
                    else
                    {
                        MXLKVOL[i] = -1.0;
                    }
                }
                // Easiest way forward might be to expose LAK2MODSIM in the DLL so it is callable both by GSFLOW and by MODSIM (this may have implications for MODSIM-PRMS mode)
                if (Model_mode != 11)  //Model_mode 11: PRMS-MODSIM mode
                {
                    LAK2MODSIM_InitLakes(DELTAVOL, LAKEVOL, MXLKVOL);
                    for (int i = 0; i < LAKEVOL.Length; i++)
                    {
                        if (MS_Reservoirs[i] != null)
                        {
                            MS_Reservoirs[i].m.starting_volume = (long)(LAKEVOL[i] * accuracy / uConvToMODFLOW);

                            // From Enrique:   I looked at the MODSIM code and it seems like there is a reservoir 
                            //                 initialization happening before the custom onInitialize happens.  
                            //                 This initialization sets the end volume of the t-1 to the start volume. 
                            //                 I believe that's why the initial storage is kept at the values we are setting.  
                            MS_Reservoirs[i].mnInfo.stend = MS_Reservoirs[i].m.starting_volume;

                            // The following is a work-around until 
                            // Inline reservoir: 0.46%  Offline reservoir: 
                            MS_Reservoirs[i].m.resBalance.targetPercentages[0] = (double)(DELTAVOL[i] * accuracy / uConvToMODFLOW) / MS_Reservoirs[i].m.max_volume * 100;
                            DPOOL[i] = (long)DELTAVOL[i];  // Store DPOOL in MODFLOW units, not MODSIM units.  
                            MS_Reservoirs[i].mnInfo.start = (long)(LAKEVOL[i] * accuracy / uConvToMODFLOW);
                            MS_Reservoirs[i].mnInfo.start_storage[0] = MS_Reservoirs[i].mnInfo.start;

                            STARTLAKEVOL[i] = LAKEVOL[i];

                            // Because the code needs to cycle back to redo the MODSIM solution after running this bit of code,
                            // reset the DELTAVOL values back to 0 since this variable is used in OnIterationTop()
                            DELTAVOL[i] = 0;
                        }
                    }
                    breakout = true;
                }
            }
        }

        private void update_lake_synchronization()
        {
            messageOut("");
            //Trying to correct the end Volume convergence
            for (int i = 0; i < MS_Reservoirs.Length; i++)
            {
                //DELTAVOL[i] += -((MS_Reservoirs[i].mnInfo.stend / accuracy * uConvToMODFLOW) - LAKEVOL[i]);
                //VOLSync = true;
                //converge = false;
                if (MS_Reservoirs[i] != null)
                {
                    messageOut("Res. Converge" + i + ": MS:" + MS_Reservoirs[i].mnInfo.stend / accuracy * uConvToMODFLOW + " MF: " + LAKEVOL[i] + " DPOOL: " + string.Format("{0:N1}", DPOOL[i]));
                    STARTLAKEVOL[i] = LAKEVOL[i];
                }
            }
        }

        private void Store_Net_Res_AccDepl()
        {
            //Implement Reservoir accretions/depletions
            for (int i = 0; i < MS_Reservoirs.Length; i++)
            {
                DELTAVOLPREV[i] = DELTAVOL[i];
                if (Model_mode == 11) //PRMS-MODSIM mode
                {
                    //PRMS calculates potential evaporation with is used in MODSIM to compute reservoir evaporation
                    //  MODSIM user input will be overwritten
                    //  PRMS will always send evap in inches.  We need to apply the conversion metric/english.
                    //  LAKEEVAP is going to include only evaporation, precipitation is comming in the DELTAVOL variable.
                    if (MS_Reservoirs[i] != null) MS_Reservoirs[i].mnInfo.evaporationrate[myModel.mInfo.CurrentModelTimeStepIndex, 0] = -LAKEVAP[i] * uConvRateToMODSIM;
                }
            }

        }

        private void OnFinished()
        {
            //TO DO: Do we need to do something here?
            //MFNWT_CLEAN();
        }

        private double EXCHNGVol_Tolerance; //in m3
        private double LAKEVol_Tolerance;//in m3

        private Boolean Get_Div_Chng()//SortedList myDiversions)
        {
            //for (int i = 0; i < MS_Links.Length; i++)
            //{
            //    Link depLink = myModel.FindLink("MF_Dep_" + MS_Links[i].name);
            //    //if (depLink.mlInfo.hi > depLink.mlInfo.flow)
            //    messageOut(depLink.name + ": \t" + depLink.mlInfo.cost + "\t" + depLink.mlInfo.hi + "\t" + depLink.mlInfo.flow);
            //    messageOut( "\t Flow: " + MS_Links[i].mlInfo.flow );
            //}
            //for (int i = 0; i < DELTAVOL.Length; i++)
            //{
            //    Link depLink = myModel.FindLink("MF_Dep_" + MS_Reservoirs[i].name);
            //    //if (depLink.mlInfo.hi > depLink.mlInfo.flow)
            //    messageOutLine(depLink.name + ": \t" + depLink.mlInfo.cost + "\t" + depLink.mlInfo.hi + "\t" + depLink.mlInfo.flow);
            //}


            bool converge = true;
            // double percent_diff = 0.005;

            for (int i = 0; i < MS_Flows.Length; i++)
            {
                // Check for changes in the MODSIM flows in the diversion links.
                // Convergence checked in MODFLOW units.
                converge = converge && ((double)Math.Abs(MS_Flows[i] - MS_FlowsPREV[i]) <= EXCHNGVol_Tolerance);  // (double)(Math.Abs(MS_FlowsPREV[i]) * percent_diff));
                converge = converge && ((double)Math.Abs(EXCHANGE[i] - EXCHANGEPREV[i]) <= EXCHNGVol_Tolerance); // (double)(Math.Abs(EXCHANGEPREV[i]) * percent_diff));
                if ((double)Math.Abs(MS_Flows[i] - MS_FlowsPREV[i]) > EXCHNGVol_Tolerance) 
                    messageOut("For iseg: " + (i + 1).ToString() + " difference between MODSIM & MF is: " + Math.Abs(MS_Flows[i] - MS_FlowsPREV[i]));
                //if ((i == 18 || i == 19) && myModel.mInfo.CurrentModelTimeStepIndex >= 364) messageOut("Diver:" + i + ":" + MS_Flows[i] + "Exch: " + EXCHANGE[i]);
                // if ((double)Math.Abs(EXCHANGE[i] - EXCHANGEPREV[i]) > EXCHNGVol_Tolerance) messageOut("GW-SW Exch:" + i + ":" + Math.Abs(EXCHANGE[i] - EXCHANGEPREV[i]));
                // myModel.mInfo.CurrentModelTimeStepIndex

                //Here is what the header looks like: sw.WriteLine("TS iseg Exchange_Prev Exchange");
                //sw.WriteLine(Convert.ToInt32(myModel.mInfo.CurrentModelTimeStepIndex + 1) + " " + Convert.ToInt32(i + 1) + " " + Convert.ToSingle(EXCHANGEPREV[i]) + " " + Convert.ToSingle(EXCHANGE[i]));
                //sw.Flush();
            }

            if (Model_mode != 11)
            {

                for (int i = 0; i < DELTAVOL.Length; i++)
                {
                    //TO DO: Add reservoir volume convergence.
                    // Needs to compare MODSIM end storage with MODFLOW LAKEVOL
                    // Convergence checked in MODFLOW units.
                    converge = converge && ((double)Math.Abs(DELTAVOL[i] - DELTAVOLPREV[i]) <= LAKEVol_Tolerance);
                    //if ((double)Math.Abs(DELTAVOL[i] - DELTAVOLPREV[i]) > (double)(Math.Abs(DELTAVOLPREV[i]) * percent_diff)) messageOut("Res:" + i + ":" + Math.Abs(DELTAVOL[i] - DELTAVOLPREV[i]));
                    //Check for convergence on the Reservoir Volumes
                    converge = converge && ((double)Math.Abs(MS_Reservoirs[i].mnInfo.stend / accuracy * uConvToMODFLOW - LAKEVOL[i]) <= LAKEVol_Tolerance);
                    //if (i == 2 && myModel.mInfo.CurrentModelTimeStepIndex >= 364) messageOut("Res. Converge" + i + ": MS:" + MS_Reservoirs[i].mnInfo.stend / accuracy * uConvToMODFLOW + " MF: " + LAKEVOL[i]);
                    if (MS_Reservoirs[i] != null)
                    {
                        if ((double)Math.Abs(MS_Reservoirs[i].mnInfo.stend / accuracy * uConvToMODFLOW - LAKEVOL[i]) > LAKEVol_Tolerance) 
                            messageOut("Res. Converge" + i + ": MS:" + MS_Reservoirs[i].mnInfo.stend / accuracy * uConvToMODFLOW + " MF: " + LAKEVOL[i]);
                    }
                }
                if (converge)
                {
                    update_lake_synchronization();
                }
            }
            return converge;
        }

        public char[] ToCharacterArrayFortran(string source, int length)
        {
            var chars = new char[length];
            int sourceLength = source.Length;
            for (int i = 0; i < length; i++)
            {
                if (i < sourceLength)
                    chars[i] = source[i];
                else
                    chars[i] = ' '; // Important that these are blank for Fortran compatibility.
            }
            return chars;
        }

    }

}