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
    //private static SortedList myStreamNodes;
    public static SortedList myDepletions;
    public static SortedList myAcretions;
    public static SortedList myDiversionsN;
    public static int MF_TimeStep;
    //public static int TS_old = 0;
    public static bool Adv_TabF = false;
    public static Link m_releaseLnk;
    public static Node m_ResNode;
    public static double[] MF_LK_Vol = new double[1]; // this example has only one reservoir
    public static double[] MF_Segs = new double[600];
    public static double[] MF_ActDivs = new double[600];
    public static double[] MF_Acc_Dep_Identifier = new double[1000];
    public static double[] MF_Acc_Dep; //= new double[1000];
    public static object RAD_list;
    public static DataTable m_table;
    public static DataTable map_table;
    public static StreamWriter sw = new StreamWriter(@"Iter_Output.txt");
    public static double[] MF_Segs_Converge; //= new double[25];
    public static double[] MF_Segs_Converge_Prev; //= new double[25];
    public static List<int> Main_Ditches = new List<int>();

    //Fortran DLL interface
    
    [DllImport("GSFLOW_MODSIM.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void gsflow_prms(ref string process, ref bool AFR, ref int Numts, ref bool MODSIM_on);

    //[DllImport("MF_DLL_CV.dll", CallingConvention = CallingConvention.Cdecl)]
    //public static extern void MFNWT_INIT();

    //[DllImport("MF_DLL_CV.dll", CallingConvention = CallingConvention.Cdecl)]
    //public static extern void MFNWT_RDSTRESS(ref int a);

    //[DllImport("MF_DLL_CV.dll", CallingConvention = CallingConvention.Cdecl)]
    //public static extern void MFNWT_RUN(ref int a, ref int b, [In, Out] double[] Segs, [In, Out] double[] RealDiv_Amt, ref bool AdvFlwRdr);  

    //[DllImport("MF_DLL_CV.dll", CallingConvention = CallingConvention.Cdecl)]
    ////public static extern void COMPUTE_EXCHG([In, Out] double[] Exchng, [In, Out] double[] LKVol, ref int a);
    //public static extern void COMPUTE_EXCHG([In, Out] double[] Exchng_Ident, [In, Out] double[] Exchng_Amt, ref int a);

    //[DllImport("MF_DLL_CV.dll", CallingConvention = CallingConvention.Cdecl)]
    //public static extern void MFNWT_OCBUDGET(ref int a, ref int b, [In, Out] double[] Segs, [In, Out] double[] RealDiv_Amt);

    //[DllImport("MF_DLL_CV.dll", CallingConvention = CallingConvention.Cdecl)]
    //public static extern void MFNWT_WRITERAS(ref int a);

    //[DllImport("MF_DLL_CV.dll", CallingConvention = CallingConvention.Cdecl)]
    //public static extern void MFNWT_CLEAN();

    public static void Main(string[] CmdArgs)
    {
        string arg;
        arg = "setdims";
        bool afr = true;
        bool MODSIM_on = true;
        int Numts = 1;
        gsflow_prms(ref arg, ref afr, ref Numts, ref MODSIM_on);
        arg = "decl";
        gsflow_prms(ref arg, ref afr, ref Numts, ref MODSIM_on);
        arg = "init";
        gsflow_prms(ref arg, ref afr, ref Numts, ref MODSIM_on);
        if (MODSIM_on == false)
        {
            for (int i = 0; i < Numts; i++)
            {
                arg = "run";
                gsflow_prms(ref arg, ref afr, ref Numts, ref MODSIM_on);
            }
        }
        else
        { 
            string FileName = CmdArgs[0];
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
                XYFileReader.Read(myModel, FileName);
                PrepareMODSIMNetwork(Directory.GetCurrentDirectory() + "\\" + CmdArgs[1]);
                XYFileWriter.Write(myModel, FileName.Replace(".xy", "MSGSF.xy"));
                Modsim.RunSolver(myModel);
                //Copy output to the original file name - Custom Output carries the MF Dep/Acc
                File.Copy(FileName.Replace(".xy", "MSGSFOUTPUT.mdb"), FileName.Replace(".xy", "OUTPUT.mdb"), true);
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

    private static DataTable m_SyncTblSEG, m_SyncTblDIV;
    private static void PrepareMODSIMNetwork(string m_TblPath)
    {
        MWH.MWHUtils.GeneralUtils.MyDBUtils m_DBUtils = new MWH.MWHUtils.GeneralUtils.MyDBUtils(m_TblPath);
        //m_SyncTbl = new DataTable();
        //m_SyncTbl.Columns.Add("Feature", System.Type.GetType("System.String"));
        //m_SyncTbl.Columns.Add("MF Iseg", System.Type.GetType("System.Int32"));
        //m_SyncTbl.Columns.Add("MODSIM", System.Type.GetType("System.String"));
        string m_Sql = "SELECT Modsim_GSFlow_Sync.LnkName, Modsim_GSFlow_Sync.MF_iseg, Modsim_GSFlow_Sync.Diversion";
        m_Sql += " FROM Modsim_GSFlow_Sync";
        string m_Sql2 = m_Sql + " WHERE (((Modsim_GSFlow_Sync.MF_iseg) Is Not Null) AND ((Modsim_GSFlow_Sync.Diversion)=False))";
        m_Sql2 += " ORDER BY Modsim_GSFlow_Sync.MF_iseg";
        m_SyncTblSEG= m_DBUtils.GetTableFromDB(m_Sql2, "SegmentSync");//"SELECT Modsim_Streams.MF_iseg, Modsim_Streams.MOD_Name FROM Modsim_Streams WHERE (((Modsim_Streams.MF_iseg) Is Not Null)) GROUP BY Modsim_Streams.MF_iseg, Modsim_Streams.MOD_Name;", "Streams");
        //PopulateSyncInfo(m_TblStreams);
        m_Sql2 = m_Sql + " WHERE (((Modsim_GSFlow_Sync.MF_iseg) Is Not Null) AND ((Modsim_GSFlow_Sync.Diversion)=True))";
        m_Sql2 += " ORDER BY Modsim_GSFlow_Sync.MF_iseg";
        m_SyncTblDIV = m_DBUtils.GetTableFromDB(m_Sql2, "DiversionSync");//"SELECT Modsim_Streams.MF_iseg, Modsim_Streams.MOD_Name FROM Modsim_Streams WHERE (((Modsim_Streams.MF_iseg) Is Not Null)) GROUP BY Modsim_Streams.MF_iseg, Modsim_Streams.MOD_Name;", "Streams");
        //m_TblStreams = m_DBUtils.GetTableFromDB("SELECT Modsim_Canals.MF_iseg, Modsim_Canals.MOD_Name FROM Modsim_Canals WHERE (((Modsim_Canals.MF_iseg) Is Not Null)) GROUP BY Modsim_Canals.MF_iseg, Modsim_Canals.MOD_Name;", "Canals");
        //PopulateSyncInfo(m_TblStreams);
        m_SyncTblDIV.Columns.Add("Flow", System.Type.GetType("System.Int32"));
        m_SyncTblDIV.Columns.Add("PrevFlow", System.Type.GetType("System.Int32"));

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
                Link baseLink = myModel.FindLink((string)mrow["MODSIM"]);
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
    
    //private static void PopulateSyncInfo(DataTable m_Tbl)
    //{
    //    foreach (DataRow mrow in m_Tbl.Rows)
    //    {
    //        DataRow m_SyncRow = m_SyncTbl.NewRow();
    //        m_SyncRow["Feature"] = m_Tbl.TableName;
    //        m_SyncRow["MF Iseg"] = mrow["MF_iseg"];
    //        m_SyncRow["MODSIM"] = mrow["MOD_Name"];
    //        m_SyncTbl.Rows.Add(m_SyncRow);
    //    }
    //}

    private static void OnInitialize()
    {
        //MFNWT_INIT();

        //Link m_Link;
        //List<int> Divs_List = new List<int>();

        //// Read in a file that contains only the farm-level and main ditch-level diversions
        //using (StreamReader reader = File.OpenText("./various_debris/CV-wes-7915trans.divarr"))
        //{
        //    string input;
        //    int div;
        //    int i = 0;
        //    while ((input = reader.ReadLine()) != null)
        //    {
        //        while (input[0]=='#')  // ignore commented lines
        //        {
        //            input = reader.ReadLine();
        //        }

        //        div = Convert.ToInt32(input);
        //        Divs_List.Add(div);

        //        // Store in an array that is passable to MF.
        //        MF_ActDivs[i] = Convert.ToDouble(div);
        //        i++;
        //    }
        //}

        ////Read in a file that contains all the links (iseg's) that require GW-SW interactions
        //DataTable map_table = new DataTable();
        //map_table.Columns.Add("linkname");
        //map_table.Columns.Add("linknum");
        //map_table.Columns.Add("iseg");

        //Dimension arrays to the input table
        Array.Resize <double> (ref MF_Acc_Dep, m_SyncTblSEG.Rows.Count);
        Array.Resize<double>(ref MF_Acc_Dep_Identifier, m_SyncTblSEG.Rows.Count);

        // Also, initialize MF_Acc_Dep (accretion/depletion) variables
        // TODO: Is this needed - they should be zero
        int i = 0;
        foreach (DataRow m_Row in m_SyncTblSEG.Rows)// i = 0; i < MF_Acc_Dep.Length; i++)
        {
            MF_Acc_Dep_Identifier[i] = (double) m_Row["MF_Iseg"];
            MF_Acc_Dep[i] = 0;
            i += 1;
        }


        //List<string> Divs_List = new List<string>();
        //i = 0;
        //foreach (DataRow m_Row in m_SyncTblDIV.Rows)// i = 0; i < MF_Acc_Dep.Length; i++)
        //{
        //    Divs_List.Add((string) m_Row["LnkName"]);
        //    i += 1;
        //}

        //using (StreamReader reader = File.OpenText("./various_debris/CV-wes-7915trans.nlst"))
        //{
        //    string input;
        //    int div;
        //    int i = 0;
        //    while ((input = reader.ReadLine()) != null)
        //    {
        //        while (input[0] == '#')  // ignore commented lines
        //        {
        //            input = reader.ReadLine();
        //        }

        //        string[] m_arr = input.Split(new char[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);

        //        // Set up a new row
        //        DataRow mapping_info = map_table.NewRow();
        //        mapping_info["linkname"] = m_arr[0];
        //        mapping_info["linknum"] = m_arr[1];
        //        mapping_info["iseg"] = m_arr[2];

        //        // Need to store only those isegs that require GW-SW interaction because there are so many 'ghost'
        //        // links in the network...i.e., links that don't have corresponding isegs
        //        MF_Acc_Dep_Identifier[i] = Convert.ToDouble(m_arr[2]);
        //        i += 1;
        //    }
        //}

        //// Set up a list of the main ditch diversions for checking convergence
        //Main_Ditches.Add(161);
        //Main_Ditches.Add(191);
        //Main_Ditches.Add(227);
        //Main_Ditches.Add(251);
        //Main_Ditches.Add(282);
        //Main_Ditches.Add(283);
        //Main_Ditches.Add(290);
        //Main_Ditches.Add(308);
        //Main_Ditches.Add(332);
        //Main_Ditches.Add(337);
        //Main_Ditches.Add(348);
        //Main_Ditches.Add(366);
        //Main_Ditches.Add(392);
        //Main_Ditches.Add(396);
        //Main_Ditches.Add(397);
        //Main_Ditches.Add(418);
        //Main_Ditches.Add(438);
        //Main_Ditches.Add(442);
        //Main_Ditches.Add(443);
        //Main_Ditches.Add(445);
        //Main_Ditches.Add(454);
        //Main_Ditches.Add(465);
        //Main_Ditches.Add(525);
        //Main_Ditches.Add(553);
        //Main_Ditches.Add(601);     

        //Initialize Demands  //WftS
        //myDiversionsN = new SortedList();
        //foreach (int d in Divs_List)
        //{
        //    m_Link = myModel.FindLink(d.ToString()); // Use .FindLink() instead
        //    myDiversionsN.Add(m_Link.name, m_Link);
        //}

        //Dimension arrays to the input table
        Array.Resize<double>(ref MF_Segs_Converge_Prev, m_SyncTblDIV.Rows.Count);
        Array.Resize<double>(ref MF_Segs_Converge, m_SyncTblDIV.Rows.Count);

        Link m_Link;
        foreach (DataRow m_Row in m_SyncTblDIV.Rows)// i = 0; i < MF_Acc_Dep.Length; i++)
        {
            m_Link = myModel.FindLink((string)m_Row["LnkName"]); // Use .FindLink() instead
            myDiversionsN.Add(m_Link.name, m_Link);
        }

        //// Write a header row to the streamwriter for reading in later
        //sw.WriteLine("Flux TS Iter OldVal NewVal Node DivAmt");

        //Set initial values for testing of convergence among the 25 main diversions

        //for (int i=0; i < 25; i++)
        //{
        //    MF_Segs_Converge_Prev[i] = 0;
        //}


        //initialize custom output variables
        Csu.Modsim.NetworkUtils.ModelOutputSupport m_OutputSupport = (Csu.Modsim.NetworkUtils.ModelOutputSupport) myModel.OutputSupportClass;
        m_OutputSupport.AddUserDefinedOutputVariable(myModel, "MF_Depletion",true, false, "Flow");
        m_OutputSupport.AddUserDefinedOutputVariable(myModel, "MF_Accretion",true, false, "Flow");
        m_OutputSupport.AddCurrentUserLinkOutput += addLinkMFOutput;

    }

    private static void OnIterationTop()
    {
        //Before network gets primed for the solver
        Adv_TabF = false;
        if (myModel.mInfo.Iteration == 0)
        {
            MF_TimeStep = myModel.mInfo.CurrentModelTimeStepIndex;
            MF_TimeStep += 1;
            //MFNWT_RDSTRESS(ref MF_TimeStep);
            MFRunYet = false;
            //Set flag for GSFlow that models converge and need to advance time step
            Adv_TabF = true;
        }
    }

    private static void OnMessage(string message)
    {
    }

    private static void OnError(string message)
    {
    }

    private static void OnIterationBottom()
    {
        //Network primed, ready for solver, upper bounds and lower bounds set

        //Asign accretions and depletions to the MODSIM network.
        foreach (DataRow mrow in m_SyncTblSEG.Rows)
        {
            Link depLink = myModel.FindLink("MF_Dep_" + (string)mrow["LnkName"]);
            Link accLink = myModel.FindLink("MF_Acc_" + (string)mrow["LnkName"]);
            double m_value = MF_Acc_Dep[(int)mrow["MF_iseg"] - 1];  // This sets the MF returned GW-SW acc/dep
                                                                       // Need the -1 to account for 0-based indexing in C#

            //Value returned from MODFLOW in m3 and MODISM uses 1000m3, but using three decimal precision multiply by 1000 - No conversion needed
            if (depLink != null && accLink != null)
            {
                if (m_value > 0)
                {
                    //Acretions
                    depLink.mlInfo.hi = 0;
                    accLink.mlInfo.hi = Convert.ToInt32(m_value);
                }
                else
                {
                    //Depletions
                    //set Depletions to the stream network as upper bounds in the high priority links
                    depLink.mlInfo.hi = Convert.ToInt32(-m_value);
                    accLink.mlInfo.hi = 0;
                }
            }
            else
            {
                Console.WriteLine("Missing Link" + mrow["MODSIM"]);
            }
        }

    }

    //Add MF output to the original link
    private static void addLinkMFOutput(Link m_link , ref DataRow m_row )
    {
        Link m_MFLink = myModel.FindLink("MF_Dep_" + m_link.name);
        //TODO: check if the variable can replace the accuracy
        if (m_MFLink != null) { m_row["MF_Depletion"] = m_MFLink.mlInfo.flow/myModel.accuracy; }
        m_MFLink = myModel.FindLink("MF_Acc_" + m_link.name);
        if (m_MFLink != null) { m_row["MF_Accretion"] = m_MFLink.mlInfo.flow/1000; }
    }

    static bool DeltaVolNeg;
    static bool MFVolRecal;
    static long DeltaVol=0;
    static long volDiff=0;
    static double MODF_LAK;         // MF Lake Vol
    static bool MFRunYet = false;   // Needed in MODFLOWComputeReturns

    private static void OnIterationConverge()
    {
        bool MODFLOWConverge = false;
        int a = 0;

        //extract the MODSIM calculated diversion values for inserting into an array that is passed to MF
        SortedList myDiversions = new SortedList();
        foreach (DictionaryEntry de in myDiversionsN)
        {
            //Node m_DEMnode = (Node)de.Value;
            Link m_Link = (Link)de.Value; 
            myDiversions.Add(m_Link.name, m_Link.mlInfo.flow);  // Remember that "myDiversions" traces back to .divarr2 (all isegs requiring diversion overwrite)

            //// Set initial divertions for later check of MS-MF convergence.
            //if (Main_Ditches.Contains(Convert.ToInt32(m_Link.name)))
            //{
            MF_Segs_Converge[a] = m_Link.mlInfo.flow;
            a += 1;
            //}
        }

        // This is an extra step, but makes it easier to pass values to Fortran via a 
        // 2D array where the index locations are for known diversions on the MF side
        // these are all demand nodes, MF_Segs overwrites WR level diversions in SFR
        if (!MODFLOWConverge)
        {
            for (int i = 0; i < myDiversions.Count - 1; i++)
            {
                MF_Segs[i] = Convert.ToDouble(myDiversions.GetKey(i));
                MF_ActDivs[i] = Convert.ToDouble(myDiversions.GetByIndex(i));
            }
            // for example: MF_Segs[1] = Convert.ToDouble(myDiversions["1"]);
        }

        // Because specified releases equal to 0 from LAKs are a flag in MF, need to set a MODSIM reservoir release
        // of 0.0 to a slightly non-zero value to avoid this flag. Example to follow if necessary
        //if (MF_Segs[4] == 0)
        //{
        //    MF_Segs[4] = 0.01;
        //}

        MF_TimeStep = myModel.mInfo.CurrentModelTimeStepIndex;
        MF_TimeStep += 1;                             //remember that MODSIM is 0-based whereas MODFLOW is 1-based
        //if (MF_TimeStep >= 7987)
        //{
        //    //a debug breakpoint
        //    MF_TimeStep = myModel.mInfo.CurrentModelTimeStepIndex;
        //    MF_TimeStep += 1;
        //}
        //if (TS_old != MF_TimeStep) Adv_TabF = true;
        //MFNWT_RUN(ref MF_TimeStep, ref MF_TimeStep, MF_Segs, MF_ActDivs, ref Adv_TabF);  //For now, MODSIM-MODFLOW requires one time step per stress period
        MFRunYet = true;

        // reset the Adv_TabF flag
        //Adv_TabF = false;
        // store the current time step value for evaluating the status of the next MODSIM-MF iteration\
        //TS_old = MF_TimeStep;

        //bool MODFLOWConverge = SendDiversionToMODFLOW(myModel.mInfo.CurrentModelTimeStepIndex, myDiversions);

        //bool MODFLOWConverge = GetMODFLOW_Acc_Dep(myDiversions, m_ResNode.mnInfo.stend);

        // For this type of example problem, needed to add a new convergence criteria that is only
        // based on the diversions (and res release).  Accretions/depletions too unstable.
        // MODFLOWConverge = Get_Div_Chng(myDiversions, m_ResNode.mnInfo.stend);

        //MODF_LAK = MF_LK_Vol[0];
        //long resStorage = m_ResNode.mnInfo.stend;
        //m_ResNode.mnInfo.evapLink.mlInfo.hi = (long) (m_ResNode.mnInfo.stend -MODF_LAK);
        //volDiff = (long)(m_ResNode.mnInfo.stend - Math.Max(m_ResNode.m.min_volume, MF_LK_Vol[0]));
        MFVolRecal = true;


        //if (myModel.FindLink("MF_Dep_" + m_ResNode.name).mlInfo.hi == 0 && DeltaVol == 0)
        //{
        //    if (volDiff < 0)
        //    {
        //        DeltaVolNeg = false;
        //    }
        //    else
        //    {
        //        DeltaVolNeg = true;
        //    }
        //}
        //if (DeltaVolNeg)
        //{
        //    myModel.FindLink("MF_Dep_" + m_ResNode.name).mlInfo.hi += (long)(volDiff);
        ////}
        ////else
        ////{
        ////    myModel.FindNode("19_5").mnInfo.infLink.mlInfo.hi += (long)(-volDiff);
        //}

        MODFLOWConverge = Get_Div_Chng(myDiversions);  

        if (!MODFLOWConverge)
        {
            //MODFLOWConverge = CheckOscillating(MF_Segs);
        }

        //TODO: This could be controled by the setting in MODSIM
        if (myModel.mInfo.Iteration > 98)
        {
            MODFLOWConverge = true;
        }


        //if MODFLOWConverge == FALSE, MODSIM will loop again.
        myModel.mInfo.convg = MODFLOWConverge;

        // if between-code conversion achieved, run MF Budget
        if (MODFLOWConverge)
        {
            //Some debug code, can be removed
            //Console.WriteLine("MODSIM Res: " + String.Format("{0:#,###}", resStorage) + "   MODFLOW Res: " + String.Format("{0:#,###}", MODF_LAK));

            //MFNWT_OCBUDGET(ref MF_TimeStep, ref MF_TimeStep, MF_Segs, MF_ActDivs);

            //MFNWT_WRITERAS(ref MF_TimeStep);

            // reset oscillation indexer
            osc = -1;

            //some debug code
            if (myModel.mInfo.CurrentModelTimeStepIndex == 364)
            {
                string msg = "start debugging here";
            }
        }
        else
        {
            a = 0;
            foreach (DictionaryEntry de in myDiversionsN)
            {
                MF_Segs_Converge_Prev[a] = MF_Segs_Converge[a];
                a += 1;
            }
            //    for (int x = 0; x < 25; x++)
            //{
            //    MF_Segs_Converge_Prev[x] = MF_Segs_Converge[x];
            //}
           
        }
    }

    private static void OnFinished()
    {
        //MFNWT_CLEAN();
    }

   

    private static Boolean Get_Div_Chng(SortedList myDiversions)
    {
        bool converge = true;
        //double val;
        //double divAmt;
        //string name;
        double percent_diff = 0.005;
        int a = 0;
        foreach (DictionaryEntry de in myDiversionsN)
        {
            converge = converge && (Convert.ToDouble(MF_Segs_Converge[a]) <= MF_Segs_Converge_Prev[a] + (MF_Segs_Converge_Prev[a] * percent_diff));
            a += 1;
        }

        //if (((Convert.ToDouble(MF_Segs_Converge[0]) <= MF_Segs_Converge_Prev[0] + (MF_Segs_Converge_Prev[0] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[0]) >= MF_Segs_Converge_Prev[0] - (MF_Segs_Converge_Prev[0] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[1]) <= MF_Segs_Converge_Prev[1] + (MF_Segs_Converge_Prev[1] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[1]) >= MF_Segs_Converge_Prev[1] - (MF_Segs_Converge_Prev[1] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[2]) <= MF_Segs_Converge_Prev[2] + (MF_Segs_Converge_Prev[2] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[2]) >= MF_Segs_Converge_Prev[2] - (MF_Segs_Converge_Prev[2] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[3]) <= MF_Segs_Converge_Prev[3] + (MF_Segs_Converge_Prev[3] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[3]) >= MF_Segs_Converge_Prev[3] - (MF_Segs_Converge_Prev[3] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[4]) <= MF_Segs_Converge_Prev[4] + (MF_Segs_Converge_Prev[4] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[4]) >= MF_Segs_Converge_Prev[4] - (MF_Segs_Converge_Prev[4] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[5]) <= MF_Segs_Converge_Prev[5] + (MF_Segs_Converge_Prev[5] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[5]) >= MF_Segs_Converge_Prev[5] - (MF_Segs_Converge_Prev[5] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[6]) <= MF_Segs_Converge_Prev[6] + (MF_Segs_Converge_Prev[6] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[6]) >= MF_Segs_Converge_Prev[6] - (MF_Segs_Converge_Prev[6] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[7]) <= MF_Segs_Converge_Prev[7] + (MF_Segs_Converge_Prev[7] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[7]) >= MF_Segs_Converge_Prev[7] - (MF_Segs_Converge_Prev[7] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[8]) <= MF_Segs_Converge_Prev[8] + (MF_Segs_Converge_Prev[8] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[8]) >= MF_Segs_Converge_Prev[8] - (MF_Segs_Converge_Prev[8] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[9]) <= MF_Segs_Converge_Prev[9] + (MF_Segs_Converge_Prev[9] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[9]) >= MF_Segs_Converge_Prev[9] - (MF_Segs_Converge_Prev[9] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[10]) <= MF_Segs_Converge_Prev[10] + (MF_Segs_Converge_Prev[10] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[10]) >= MF_Segs_Converge_Prev[10] - (MF_Segs_Converge_Prev[10] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[11]) <= MF_Segs_Converge_Prev[11] + (MF_Segs_Converge_Prev[11] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[11]) >= MF_Segs_Converge_Prev[11] - (MF_Segs_Converge_Prev[11] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[12]) <= MF_Segs_Converge_Prev[12] + (MF_Segs_Converge_Prev[12] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[12]) >= MF_Segs_Converge_Prev[12] - (MF_Segs_Converge_Prev[12] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[13]) <= MF_Segs_Converge_Prev[13] + (MF_Segs_Converge_Prev[13] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[13]) >= MF_Segs_Converge_Prev[13] - (MF_Segs_Converge_Prev[13] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[14]) <= MF_Segs_Converge_Prev[14] + (MF_Segs_Converge_Prev[14] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[14]) >= MF_Segs_Converge_Prev[14] - (MF_Segs_Converge_Prev[14] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[15]) <= MF_Segs_Converge_Prev[15] + (MF_Segs_Converge_Prev[15] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[15]) >= MF_Segs_Converge_Prev[15] - (MF_Segs_Converge_Prev[15] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[16]) <= MF_Segs_Converge_Prev[16] + (MF_Segs_Converge_Prev[16] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[16]) >= MF_Segs_Converge_Prev[16] - (MF_Segs_Converge_Prev[16] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[17]) <= MF_Segs_Converge_Prev[17] + (MF_Segs_Converge_Prev[17] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[17]) >= MF_Segs_Converge_Prev[17] - (MF_Segs_Converge_Prev[17] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[18]) <= MF_Segs_Converge_Prev[18] + (MF_Segs_Converge_Prev[18] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[18]) >= MF_Segs_Converge_Prev[18] - (MF_Segs_Converge_Prev[18] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[19]) <= MF_Segs_Converge_Prev[19] + (MF_Segs_Converge_Prev[19] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[19]) >= MF_Segs_Converge_Prev[19] - (MF_Segs_Converge_Prev[19] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[20]) <= MF_Segs_Converge_Prev[20] + (MF_Segs_Converge_Prev[20] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[20]) >= MF_Segs_Converge_Prev[20] - (MF_Segs_Converge_Prev[20] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[21]) <= MF_Segs_Converge_Prev[21] + (MF_Segs_Converge_Prev[21] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[21]) >= MF_Segs_Converge_Prev[21] - (MF_Segs_Converge_Prev[21] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[22]) <= MF_Segs_Converge_Prev[22] + (MF_Segs_Converge_Prev[22] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[22]) >= MF_Segs_Converge_Prev[22] - (MF_Segs_Converge_Prev[22] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[23]) <= MF_Segs_Converge_Prev[23] + (MF_Segs_Converge_Prev[23] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[23]) >= MF_Segs_Converge_Prev[23] - (MF_Segs_Converge_Prev[23] * percent_diff)) &&
        //     (Convert.ToDouble(MF_Segs_Converge[24]) <= MF_Segs_Converge_Prev[24] + (MF_Segs_Converge_Prev[24] * percent_diff) && Convert.ToDouble(MF_Segs_Converge[24]) >= MF_Segs_Converge_Prev[24] - (MF_Segs_Converge_Prev[24] * percent_diff)))
        //     |
        //     ((Convert.ToDouble(MF_Segs_Converge[0]) == 0 && MF_Segs_Converge_Prev[0] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[1]) == 0 && MF_Segs_Converge_Prev[1] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[2]) == 0 && MF_Segs_Converge_Prev[2] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[3]) == 0 && MF_Segs_Converge_Prev[3] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[4]) == 0 && MF_Segs_Converge_Prev[4] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[5]) == 0 && MF_Segs_Converge_Prev[5] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[6]) == 0 && MF_Segs_Converge_Prev[6] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[7]) == 0 && MF_Segs_Converge_Prev[7] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[8]) == 0 && MF_Segs_Converge_Prev[8] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[9]) == 0 && MF_Segs_Converge_Prev[9] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[10]) == 0 && MF_Segs_Converge_Prev[10] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[11]) == 0 && MF_Segs_Converge_Prev[11] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[12]) == 0 && MF_Segs_Converge_Prev[12] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[13]) == 0 && MF_Segs_Converge_Prev[13] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[14]) == 0 && MF_Segs_Converge_Prev[14] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[15]) == 0 && MF_Segs_Converge_Prev[15] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[16]) == 0 && MF_Segs_Converge_Prev[16] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[17]) == 0 && MF_Segs_Converge_Prev[17] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[18]) == 0 && MF_Segs_Converge_Prev[18] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[19]) == 0 && MF_Segs_Converge_Prev[19] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[20]) == 0 && MF_Segs_Converge_Prev[20] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[21]) == 0 && MF_Segs_Converge_Prev[21] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[22]) == 0 && MF_Segs_Converge_Prev[22] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[23]) == 0 && MF_Segs_Converge_Prev[23] == 0) &&
        //      (Convert.ToDouble(MF_Segs_Converge[24]) == 0 && MF_Segs_Converge_Prev[24] == 0)))
   
        //{
        //    converge = true;
        //}

        //COMPUTE_EXCHG(MF_Acc_Dep_Identifier, MF_Acc_Dep, ref MF_TimeStep);

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Some Debug code that can be commented out later.  Copied and pasted from "GetMODFLOW_Acc_Dep" above
        //for (int i = 0; i < MF_Acc_Dep.GetLength(0); i++)
        //{
        //    name = Convert.ToString(m_table.Select("Modnum = " + MF_Acc_Dep[i, 0])[0][0]);
        //    val = Convert.ToDouble(myAcretions[m_table.Select("Modnum = " + MF_Acc_Dep[i, 0])[0][0]]);

        //    // retrieve the diverted amount to see if these values are changnig around with in the 
        //    // interation which would suggest that accretions/depletions are impacting diverted
        //    // amounts.  
        //    if (name == "CA1" || name == "CA2" || name == "CA3" || name == "CA4")
        //    {
        //        divAmt = Convert.ToDouble(myDiversions[m_table.Select("Modnum = " + MF_Acc_Dep[i, 0])[0][0]]);
        //    }
        //    else
        //    {
        //        if (name == "NonStorage1")
        //        {
        //            divAmt = Convert.ToDouble(myDiversions["13_1_19_4"]);
        //        }
        //        else divAmt = 0.0;
        //    }

        //    sw.WriteLine("Acc " + Convert.ToInt32(myModel.mInfo.CurrentModelTimeStepIndex) + " " + Convert.ToInt32(myModel.mInfo.Iteration) + " " + Convert.ToSingle(val) + " " + Convert.ToSingle(MF_Acc_Dep[i, 1]) + " " + name + " " + divAmt);

        //    val = Convert.ToDouble(myDepletions[m_table.Select("Modnum = " + MF_Acc_Dep[i, 0])[0][0]]);
        //    sw.WriteLine("Dep " + Convert.ToInt32(myModel.mInfo.CurrentModelTimeStepIndex) + " " + Convert.ToInt32(myModel.mInfo.Iteration) + " " + Convert.ToSingle(val) + " " + Convert.ToSingle(MF_Acc_Dep[i, 2]) + " " + name + " " + divAmt);
        //}

        //only one lake, so output the values held by MODSIM/MODFLOW for comparison in the output file. 
        //sw.WriteLine("LAKVol " + Convert.ToInt32(myModel.mInfo.CurrentModelTimeStepIndex) + " " + Convert.ToInt32(myModel.mInfo.Iteration) + " " + MF_LK_Vol[0] + " " + (long)(MODSIM_LK) + " " + "Reservoir " + Convert.ToDouble(myDiversions["13_1_19_4"]));

        //sw.Flush();
        ////////////////////////////////////////////////////////////////////////////////////////////////////////

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


    //private static int countRedo;
    //private static Boolean SendDiversionToMODFLOW(int timeStep, SortedList diversions)
    //{
    //    bool converge = false;
    //    //implement the MODISM diversions for the current time step in MODFLOW


    //    //need to make an array that stores the old diversions for comparison to the new


    //    //for testing assume three iterations where convergence in MODSIM is overwritten and set to false
    //    countRedo += 1;
    //    if (countRedo > 3)
    //    {
    //        countRedo = 0;
    //        converge = true;
    //        Console.WriteLine("TS:\t{0}", timeStep);
    //        Console.WriteLine("\t-KEY-\t-VALUE-");
    //        for (int i = 0; i < diversions.Count; i++)
    //        {
    //            Console.WriteLine("\t{0}:\t{1}", diversions.GetKey(i), diversions.GetByIndex(i));
    //        }
    //    }
    //    // return convergence flag
    //    return converge;
    //}


}

