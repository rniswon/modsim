using Csu.Modsim.ModsimIO;
using Csu.Modsim.ModsimModel;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Data;
using System.Collections.Generic;
using MWH.MWHUtils.GeneralUtils;



public static class SurfGWModule
{
    public static Model myModel = new Model();
    public static SortedList myDepletions;
    public static SortedList myAcretions;
    public static Link[] MS_Links;
    public static Node[] MS_Reservoirs;
    public static bool Adv_TabF = false;
    public static Link m_releaseLnk;
    public static Node m_ResNode;
    public static double[] MF_LK_Vol = new double[1]; // this example has only one reservoir
    public static double[] MF_ActDivs = new double[1];
    public static double[] MF_Acc_Dep_Identifier = new double[1000];
    public static double[] MS_Flows; //= new double[1000];
    public static double[] MS_FlowsPREV;
    public static double[] Diversions = new double[1];
    public static int[] IDivert = new int[1];
    public static double[] EXCHANGE = new double[1];
    public static double[] EXCHANGEPREV = new double[1];
    public static double[] DELTAVOL = new double[1];
    public static double[] DELTAVOLPREV = new double[1];
    public static double[] LAKEVOL = new double[1];
    public static object RAD_list;
    public static DataTable m_table;
    public static DataTable map_table;
    public static StreamWriter sw = new StreamWriter(@"Iter_Output.txt");
    public static List<int> Main_Ditches = new List<int>();
    public static bool afr, MS_GSF_converge;
    public static int Model_mode, Nsegshold, Nlakeshold;
    public static int[] startTime = new int[6];
    public static int Process_mode;
    public static string mappingFileName;
    public static string xyFileName;
    private static int accuracy;

    //Fortran DLL interface

    [DllImport("GSFLOW_MODSIM.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void gsflow_prms(ref int Process_mode, ref bool afr, ref bool MS_GSF_converge, ref int Nsegshold, ref int nlakeshold, [In, Out] double[] Diversions, [In, Out] int[] IDivert, [In, Out] double[] EXCHANGE, [In, Out] double[] DELTAVOL, [In, Out] double[] LAKEVOL);

    [DllImport("GSFLOW_MODSIM.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void gsflow_prmsSettings([In, Out] ref int Numts, ref int Model_mode, ref int startTime, ref int File1_length, [In, Out] char[] FileName1, ref int File2_length, [In, Out] char[] FileName2);

    [DllImport("GSFLOW_MODSIM.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void LAK2MODSIM_InitLakes([In, Out] double[] DELTAVOL, [In, Out] double[] LAKEVOL);

    public static void Main(string[] CmdArgs)
    {
        
        int Numts = 1;
        int len_xyname, len_mapname;

        xyFileName = new String(' ', 80);
        mappingFileName = new String(' ', 80);
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
        gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, Diversions, IDivert, EXCHANGE, DELTAVOL, LAKEVOL);

        /* need file name of mapping file, read from GSFLOW Control File
           file has link Name, iseg, diversion, ResRelease */

        // Convert string to Fortran array of characters.
        char[] xyPathChars = xyFileName.ToCharacterArrayFortran(len_xyname);
        char[] mapPathChars = mappingFileName.ToCharacterArrayFortran(len_mapname);

        gsflow_prmsSettings(ref Numts, ref Model_mode, ref startTime[0], ref len_xyname, xyPathChars, ref len_mapname, mapPathChars);
        xyFileName = new string(xyPathChars);
        string map_FileName = new string(mapPathChars);
        map_FileName = GetFullPath(map_FileName);
        xyFileName = GetFullPath(xyFileName);
        
        // 0=GSFLOW; 1=PRMS; 2=MODFLOW; 10=MODSIM-GSFLOW; 11=MODSIM-PRMS; 12=MODSIM-MODFLOW; 13=MODSIM
        if (Model_mode < 12 | Model_mode > 20 ) // > 20 means a special PRMS-only mode
        {
            Process_mode = 1; // declare
            gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, Diversions,  IDivert, EXCHANGE,DELTAVOL, LAKEVOL);

            Process_mode = 2; // initialize
            gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, Diversions,  IDivert, EXCHANGE,DELTAVOL, LAKEVOL);
        }

        Process_mode = 0; // run

        // Redimension arrays to equal number of segments and lakes
        Diversions = (double[])ResizeArray(Diversions, new int[] { Nsegshold });
        IDivert = (int[])ResizeArray(IDivert, new int[] { Nsegshold });
        EXCHANGE = (double[])ResizeArray(EXCHANGE, new int[] { Nsegshold });
        EXCHANGEPREV = (double[])ResizeArray(EXCHANGEPREV, new int[] { Nsegshold });
        DELTAVOL = (double[])ResizeArray(DELTAVOL, new int[] { Nlakeshold });
        DELTAVOLPREV = (double[])ResizeArray(DELTAVOLPREV, new int[] { Nlakeshold });
        LAKEVOL = (double[])ResizeArray(LAKEVOL, new int[] { Nlakeshold });

        if (Model_mode < 10) // GSFLOW and PRMS-only
        {
            for (int i = 0; i < Numts; i++)
            {
                gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, Diversions,  IDivert, EXCHANGE,DELTAVOL, LAKEVOL);
            }

            Process_mode = 3; // clean
            gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, Diversions,  IDivert, EXCHANGE,DELTAVOL, LAKEVOL);
        }

        else
        // do something different if MODSIM-MODFLOW **** CAUTION ****
        {
            myModel = new Model();
            myModel.Init += OnInitialize;
            myModel.IterBottom += OnIterationBottom;
            myModel.IterTop += OnIterationTop;
            myModel.Converged += OnIterationConverge;
            myModel.End += OnFinished;
            myModel.OnMessage += OnMessage;
            myModel.OnModsimError += OnError; 
            try
            {
            if (Model_mode == 11) // MODSIM-PRMS
              {
                  gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, Diversions, IDivert, EXCHANGE,DELTAVOL, LAKEVOL);
              }
                XYFileReader.Read(myModel,xyFileName);
                accuracy = (int)Math.Pow(10.0, (double) myModel.accuracy);
                PrepareMODSIMNetwork(map_FileName);
                XYFileWriter.Write(myModel, xyFileName.Replace(".xy", "MSGSF.xy"));
                Modsim.RunSolver(myModel);
                //Copy output to the original file name - Custom Output carries the MF Dep/Acc
                File.Copy(xyFileName.Replace(".xy", "MSGSFOUTPUT.mdb"), xyFileName.Replace(".xy", "OUTPUT.mdb"), true);
                Console.WriteLine(" MF_MS Simulation Finished Succesfully");

            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);

            }
            finally
            {
                sw.Close();

            }
        }
    }

    private static string GetFullPath(string FileName)
    {
        FileName = FileName.Trim();
        string fileDir= Directory.GetCurrentDirectory();
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

    private static DataTable m_SyncTblSEG, m_SyncTblRES;//, m_SyncTblDIV;
    private static void PrepareMODSIMNetwork(string m_TblPath)
    {
        MWH.MWHUtils.GeneralUtils.MyDBUtils m_DBUtils = new MWH.MWHUtils.GeneralUtils.MyDBUtils(m_TblPath);
        string m_Sql = "SELECT [MS-GSF_mapping_info].[Link Name], [MS-GSF_mapping_info].[iseg], [MS-GSF_mapping_info].[Diversion], [MS-GSF_mapping_info].ResRelease FROM [MS-GSF_mapping_info] ORDER BY [MS-GSF_mapping_info].iseg;";
        m_SyncTblSEG= m_DBUtils.GetTableFromDB(m_Sql, "SegmentSync");//"SELECT Modsim_Streams.MF_iseg, Modsim_Streams.MOD_Name FROM Modsim_Streams WHERE (((Modsim_Streams.MF_iseg) Is Not Null)) GROUP BY Modsim_Streams.MF_iseg, Modsim_Streams.MOD_Name;", "Streams");
        //Get Reservoir mapping talbe 
        m_Sql = "SELECT * FROM [MS-GSF_Lake_Mapping_Info] ORDER BY [MS-GSF_Lake_Mapping_Info].GSF_LAK_ID;";
        m_SyncTblRES = m_DBUtils.GetTableFromDB(m_Sql, "ReservoirSync");
        
        //Create Sink Node
        Node m_Sink = myModel.AddNewNode(true);
        m_Sink.nodeType = NodeType.Sink;
        //m_Sink.graphics.point.X = -1150;
        //m_Sink.graphics.point.Y = 5000;
        m_Sink.name = "MF_SINK";

        //Create Source Node
        Node m_Source = myModel.AddNewNode(true);
        m_Source.nodeType = NodeType.NonStorage;
        //m_Source.graphics.point.X = 7200;
        //m_Source.graphics.point.Y = 180;
        m_Source.name = "MF_SOURCE";
        DataTable m_TSTbl = m_Source.m.adaInflowsM.dataTable;
        SetDefaultTableValue(ref m_TSTbl, 1000000000);

        //Connect Source Node
        Link m_Link = myModel.AddNewLink(true);
        Utils.ConnectFromNode(m_Link, m_Source);
        Utils.ConnectToNode(m_Link, m_Sink);
        m_Link.name = "MF_SINK_TO_SOURCE";
        m_Link.m.cost = -1;

        //Create Depletion Links
        count = 0;
        foreach (DataRow mrow in m_SyncTblSEG.Rows)
        {
            try
            {
                Link baseLink = myModel.FindLink((string)mrow["Link Name"]);
                if (baseLink != null)
                {
                    //Depletions to a link simulated at the downstream node of the MODSIM link.
                    CreateDepAccLinks(m_Sink, m_Source, baseLink.to, baseLink.name);
                }
            }
            finally { }
        }
        for (Node m_res = myModel.firstNode; m_res != null; m_res = m_res.next)
        {
            //TODO: Check if the reservoir modeling approach is consistent here
            if (m_res.nodeType == NodeType.Reservoir)
            {
                //TODO: if water is be made avaialble to the owners, this code might need to be modified.
                //      Is possible the balancing routine take care of the additions/losses.
                CreateDepAccLinks(m_Sink, m_Source, m_res, m_res.name);
            }
        }
    }
    private static int count;
    private static void CreateDepAccLinks(Node m_Sink, Node m_Source, Node m_Node, String baseName)
    {
        //Create Depletion Link
        Link m_DepLink = myModel.AddNewLink(true);
        Utils.ConnectFromNode(m_DepLink, m_Node);
        Utils.ConnectToNode(m_DepLink, m_Sink);
        m_DepLink.name = "MF_Dep_" + baseName;
        m_DepLink.m.cost = -300000 - count;
        DataTable m_TSTbl = m_DepLink.m.maxVariable.dataTable;
        SetDefaultTableValue(ref m_TSTbl, 0);
        //Create Accretion Link
        Link m_AccLink = myModel.AddNewLink(true);
        Utils.ConnectFromNode(m_AccLink, m_Source);
        Utils.ConnectToNode(m_AccLink, m_Node);
        m_AccLink.name = "MF_Acc_" + baseName;
        m_AccLink.m.cost = -5 - count;
        m_TSTbl = m_AccLink.m.maxVariable.dataTable;
        SetDefaultTableValue(ref m_TSTbl, 0);
        count++;
    }

    private static void SetDefaultTableValue(ref DataTable m_Tbl, int value)
    {
        DataRow tsRow = m_Tbl.NewRow();
        tsRow[0] = myModel.TimeStepManager.dataStartDate;
        tsRow[1] = value;
        m_Tbl.Rows.Add(tsRow);
    }

    private static Array ResizeArray(Array arr, int[] newSizes)
    {
        if (newSizes.Length != arr.Rank)
            throw new ArgumentException("arr must have the same number of dimensions " +
                                        "as there are elements in newSizes", "newSizes");

        var temp = Array.CreateInstance(arr.GetType().GetElementType(), newSizes);
        int length = arr.Length <= temp.Length ? arr.Length : temp.Length;
        Array.ConstrainedCopy(arr, 0, temp, 0, length);
        return temp;
    }

    private static double uConvToMODFLOW;
    private static void OnInitialize()
    {
        //Dimension arrays to the input table
        Array.Resize <double> (ref MS_Flows, m_SyncTblSEG.Rows.Count);
        Array.Resize<double>(ref MS_FlowsPREV, m_SyncTblSEG.Rows.Count);
        Array.Resize<Link>(ref MS_Links, m_SyncTblSEG.Rows.Count);
        Array.Resize<Node>(ref MS_Reservoirs, m_SyncTblRES.Rows.Count);
       
        // Also, initialize MF_Acc_Dep (accretion/depletion) variables
        // TODO: Is this needed - they should be zero
        int i = 0;
        Link m_Link;
        foreach (DataRow m_Row in m_SyncTblSEG.Rows)// i = 0; i < MF_Acc_Dep.Length; i++)
        {
            //MF_Acc_Dep_Identifier[i] = (double) m_Row["iseg"];
            if (i != (int)((double)m_Row["iseg"] - 1)) throw new Exception("Iseg doesn't match the index of the array");
            MS_Flows[i] = 0;
            IDivert[i] = (int) (double) m_Row["Diversion"];
            m_Link = myModel.FindLink((string)m_Row["Link Name"]); // Use .FindLink() instead
            MS_Links[i] = m_Link;
            i += 1;
        }

        //Initialize Reservoir arrays
        Node m_Res;
        i = 0;
        foreach (DataRow m_Row in m_SyncTblRES.Rows)// i = 0; i < MF_Acc_Dep.Length; i++)
        {
            if (i != ((short)m_Row["GSF_LAK_ID"]) - 1) throw new Exception("Iseg doesn't match the index of the array");
            m_Res = myModel.FindNode((string)m_Row["MODSIM_Name"]); 
            MS_Reservoirs[i] = m_Res;
            i += 1;
        }


        //initialize custom output variables
        Csu.Modsim.NetworkUtils.ModelOutputSupport m_OutputSupport = (Csu.Modsim.NetworkUtils.ModelOutputSupport) myModel.OutputSupportClass;
        m_OutputSupport.AddUserDefinedOutputVariable(myModel, "MF_Depletion",true, false, "Flow");
        m_OutputSupport.AddUserDefinedOutputVariable(myModel, "MF_Accretion",true, false, "Flow");
        m_OutputSupport.AddCurrentUserLinkOutput += addLinkMFOutput;

        //Setting units conversion factor
        if (myModel.UseMetricUnits)
        {
            //1000m3 is the default units for MODSIM in metric mode
            //MODFLOW assumed to run in m3.
            uConvToMODFLOW = 1000;
        }
        else
        {
            //the default units for MODSIM in english mode at run time is acre-ft
            //MODFLOW assumed to run in ft3.
            uConvToMODFLOW = 43559.9;
        }

        // Write a header row to the streamwriter for evaluating convergence with R
        sw.WriteLine("TS iseg Exchange_Prev Exchange");
    }

    private static void OnIterationTop()
    {
        if (myModel.mInfo.Iteration == 0) MFRunYet = false;  
    }

    private static void OnMessage(string message)
    {
        Console.Write(message + "\n");
    }

    private static void OnError(string message)
    {
    }

    private static void OnIterationBottom()
    {
        //Asign accretions and depletions to the MODSIM network.
        for (int i = 0; i < MS_Links.Length; i++)
        {
            double m_value = EXCHANGE[i] * accuracy / uConvToMODFLOW;  // This sets the MF returned GW-SW acc/dep
                                                      // units conversion to MODSIM is required
                                                      // Need the -1 to account for 0-based indexing in C#
            assignDepAcc(MS_Links[i].name, m_value);
        }
        
        //Implement Reservoir accretions/depletions
        for (int i = 0;i<MS_Reservoirs.Length;i++)
        {
            double m_value = DELTAVOL[i] * accuracy / uConvToMODFLOW;  // This sets the MF returned GW-SW acc/dep
                                           // Need the -1 to account for 0-based indexing in C#
            assignDepAcc(MS_Reservoirs[i].name, m_value);
        }
    }

    private static void assignDepAcc (String m_Name, double m_Value)
    {
        //Value returned from MODFLOW in m3 and MODISM uses 1000m3, but using three decimal precision multiply by 1000 - No conversion needed
        Link depLink = myModel.FindLink("MF_Dep_" + m_Name );
        Link accLink = myModel.FindLink("MF_Acc_" + m_Name);
        
        if (depLink != null && accLink != null)
        {
            if (m_Value > 0)
            {
                //Acretions
                depLink.mlInfo.hi = 0;
                accLink.mlInfo.hi = Convert.ToInt32(m_Value);
            }
            else
            {
                //Depletions
                //set Depletions to the stream network as upper bounds in the high priority links
                depLink.mlInfo.hi = Convert.ToInt32(-m_Value);
                accLink.mlInfo.hi = 0;
            }
        }
        else
        {
            Console.WriteLine("ERROR !!!  Missing Acc/Dep Links for " + m_Name);
        }
    }
    //Add MF output to the original link
    private static void addLinkMFOutput(Link m_link ,  DataRow m_row )
    {
        Link m_MFLink = myModel.FindLink("MF_Dep_" + m_link.name);
        //TODO: check if the variable can replace the accuracy
        if (m_MFLink != null) { m_row["MF_Depletion"] = m_MFLink.mlInfo.flow/accuracy; }
        m_MFLink = myModel.FindLink("MF_Acc_" + m_link.name);
        if (m_MFLink != null) { m_row["MF_Accretion"] = m_MFLink.mlInfo.flow/ accuracy; }
    }

    static bool MFRunYet = false;   // Needed in MODFLOWComputeReturns
    
    private static void OnIterationConverge()
    {
        bool MS_GSF_converge = false;
        
        //extract the MODSIM calculated diversion values for inserting into an array that is passed to MF
        for (int i = 0; i < m_SyncTblSEG.Rows.Count; i++)
        {
            MS_FlowsPREV[i] = MS_Flows[i];
            //Only add flows for diversion links.
            if (IDivert[i] > 0 ) MS_Flows[i] = MS_Links[i].mlInfo.flow / accuracy * uConvToMODFLOW; //flow values converted to MODFLOW units
            EXCHANGEPREV[i] = EXCHANGE[i];            
        }
        //Implement Reservoir accretions/depletions
        for (int i = 0; i < MS_Reservoirs.Length; i++) DELTAVOLPREV[i] = DELTAVOL[i];

        //Need to know the value of LAKEVOL for the first (SS)
        // If first iteration of first time step, overide MODSIM Lake volumes
        if (!MFRunYet && (myModel.mInfo.CurrentModelTimeStepIndex == 0))
        {
            // Easiest way forward might be to expose LAK2MODSIM in the DLL so it is callable both by GSFLOW and by MODSIM (this may have implications for MODSIM-PRMS mode)
            LAK2MODSIM_InitLakes(DELTAVOL, LAKEVOL);
            for (int i = 0; i < LAKEVOL.Length; i++)
            {
                MS_Reservoirs[i].m.starting_volume = (long)(LAKEVOL[i] * accuracy / uConvToMODFLOW);
                MS_Reservoirs[i].mnInfo.start = (long)(LAKEVOL[i] * accuracy / uConvToMODFLOW);
            } 
        }

        if (Model_mode < 12) // not sure what to do with MODSIM-MODFLOW (13), maybe call MFNWT_RUN
        {
            gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, MS_Flows, IDivert, EXCHANGE,DELTAVOL, LAKEVOL); // run mode
        }
             

        //Check for convergence between MODSIM and MODFLOW
        MS_GSF_converge = Get_Div_Chng();
        MS_GSF_converge = MS_GSF_converge && MFRunYet;

        if (!MS_GSF_converge)
        {
            afr = false;
            MFRunYet = true;
            //MODFLOWConverge = CheckOscillating(MF_Segs);
        } else
        {
            gsflow_prms(ref Process_mode, ref afr, ref MS_GSF_converge, ref Nsegshold, ref Nlakeshold, MS_Flows, IDivert, EXCHANGE, DELTAVOL, LAKEVOL); // converged mode
            afr = true;
        }

        if (myModel.mInfo.Iteration > myModel.maxit)
        {
            Console.WriteLine("Ran into maximum number of iterations - Warning !!! models have not converged.");
            MS_GSF_converge = true;
        }
        myModel.mInfo.convg = MS_GSF_converge;
    }

    private static void OnFinished()
    {
        //TO DO: Do we need to do something here?
        //MFNWT_CLEAN();
    }
 
    private static Boolean Get_Div_Chng()//SortedList myDiversions)
    {
        bool converge = true;
        double percent_diff = 0.005;
        for (int i = 0; i < MS_Flows.Length; i++)
        {
            // Check for changes in the MODSIM flows in the diversion links.
            // Convergence checked in MODFLOW units.
            converge = converge && ((double)Math.Abs(MS_Flows[i]- MS_FlowsPREV[i]) <= (double)(Math.Abs(MS_FlowsPREV[i]) * percent_diff));
            converge = converge && ((double)Math.Abs(EXCHANGE[i] - EXCHANGEPREV[i]) <= (double)(Math.Abs(EXCHANGEPREV[i]) * percent_diff));
            //if (Math.Abs(MS_Flows[i] - MS_FlowsPREV[i])>0) Console.WriteLine("Diver:" + i + ":" + Math.Abs(MS_Flows[i] - MS_FlowsPREV[i]));
            //if (Math.Abs(EXCHANGE[i] - EXCHANGEPREV[i]) > 0) Console.WriteLine("Exch:" + i + ":" + Math.Abs(EXCHANGE[i] - EXCHANGEPREV[i]));
            // myModel.mInfo.CurrentModelTimeStepIndex

            //Here is what the header looks like: sw.WriteLine("TS iseg Exchange_Prev Exchange");
            sw.WriteLine(Convert.ToInt32(myModel.mInfo.CurrentModelTimeStepIndex + 1) + " " + Convert.ToInt32(i + 1) + " " + Convert.ToSingle(EXCHANGEPREV[i]) + " " + Convert.ToSingle(EXCHANGE[i]));
            sw.Flush();
        }

        for (int i = 0; i < DELTAVOL.Length; i++)
        {
            //TO DO: Add reservoir volume convergence.
            // Needs to compare MODSIM end storage with MODFLOW LAKEVOL
            // Convergence checked in MODFLOW units.
            converge = converge && ((double)Math.Abs(DELTAVOL[i] - DELTAVOLPREV[i]) <= (double)(Math.Abs(DELTAVOLPREV[i]) * percent_diff));
            //if (Math.Abs(DELTAVOL[i] - DELTAVOLPREV[i]) > 0) Console.WriteLine("Res:" + i + ":" + Math.Abs(DELTAVOL[i] - DELTAVOLPREV[i]));
            //Check for convergence on the Reservoir Volumes
            converge = converge && ((double)Math.Abs(MS_Reservoirs[i].mnInfo.stend - LAKEVOL[i]) <= (double)(Math.Abs(MS_Reservoirs[i].mnInfo.stend) * percent_diff));
            Console.WriteLine("Res. Converge: MS:" + MS_Reservoirs[i].mnInfo.stend + " MF: " + LAKEVOL[i]);
        }
        return converge;
    }

    public static double[,] Div_Osc = new double[5, 4];  //4 cols: last 4 diversions by MODSIM
    private static Int32 osc = -1;
    private static Boolean CheckOscillating(double[] MF_Segs)
    {
        bool converge = true;

        //initialize array if i = -1
        if (osc == -1)
        {
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    Div_Osc[x, y] = 0;
                }
            }
        }

        // First, enter the current values into the storage array, may need to shift values
        if (osc < 3)
        {
            // If code lands here, then not enough iterations yet to determine if oscillating
            osc += 1;
            for (int j = 0; j < 5; j++)
            {
                Div_Osc[j, osc] = MF_Segs[j];
            }
            converge = false;
        }
        else
        {
            // If code lands here, check for oscialltion and then bump values if not
            osc += 1;
            for (int y = 0; y < 4; y++)
            {
                if (Math.Abs(Div_Osc[0, y] - MF_Segs[0]) < 20 &&
                    Math.Abs(Div_Osc[1, y] - MF_Segs[1]) < 20 &&
                    Math.Abs(Div_Osc[2, y] - MF_Segs[2]) < 20 &&
                    Math.Abs(Div_Osc[3, y] - MF_Segs[3]) < 20 &&
                    Math.Abs(Div_Osc[4, y] - MF_Segs[4]) < 20)
                {
                    // If code lands here then oscillation has occurred
                    converge = true;
                    break;
                }
                else
                {
                    converge = false;
                }
            }
            if (!converge)
            {
                // If the code didn't settle on "converge = true" above, then shift values
                // and store the latest entry
                for (int x = 0; x < 5; x++)
                {
                    for (int y = 1; y < 4; y++)
                    {
                        Div_Osc[x, y - 1] = Div_Osc[x, y];
                    }
                }
                for (int x = 0; x < 5; x++)
                {
                    Div_Osc[x, 3] = MF_Segs[x];
                }
                osc -= 1;
            }
        }


        return converge;
    }

    public static char[] ToCharacterArrayFortran(this string source, int length)
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

