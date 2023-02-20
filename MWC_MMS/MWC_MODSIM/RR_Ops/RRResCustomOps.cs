using System;
using System.Collections.Generic;
using System.Text;

using Csu.Modsim.ModsimIO;
using Csu.Modsim.ModsimModel;

using System.IO;
using System.Data;

using RTI.CWR.SQLiteUtils;

namespace RTI.CWR.MODSIMUtils.RRModelOps
{
    public delegate void ProcessMessage(string msg);  // delegate

    public class RRResCustomOps
    {

        public  Model m_Model = new Model();
        //private  string fileName;
        private  string dataDBPath;

        public event ProcessMessage messageOutRun;     //event

        public RRResCustomOps(ref Model model,string dataDBPath)
        {

            m_Model.Init += OnInitialize;
            m_Model.IterBottom += OnIterationBottom;
            m_Model.IterTop += OnIterationTop;
            m_Model.BackRoutIterTop += OnBRIterationTop;
            m_Model.Converged += OnIterationConverge;
            m_Model.End += OnFinished;
            //m_Model.OnMessage += OnMessage;
            //m_Model.OnModsimError += OnError;

            //fileName = args[0];
            this.dataDBPath = dataDBPath;// args[1];
            m_Model = model;
            //XYFileReader.Read(m_Model, fileName);

            //int run = Modsim.RunSolver(m_Model);

            //if (run == 0)
            //{
            //    Console.WriteLine("Finished MODSIM run!");
            //}
                    
            //Console.ReadKey();
        }

        private  void OnBRIterationTop(Model miBackRout, double[,] regRoutCoef)
        {
            throw new NotImplementedException();
        }

        private  int AccuracyConversion(long value)
        {
            int newValue = (int)(value/m_Model.ScaleFactor);
            return newValue;
        }

        private  Node LMendocino;
        private  Node LSonoma;
        private  Link pillsburyIndex;
        private  Link pillsburyStorage;
        private  Link westForkFlows;
        private  Link MendoContrlledRelease;
        private  Link hopelandGage;
        private  MinFlowCalculator LMendoMinFlow;
        private  MinFlowCalculator SonomaMinFlow;
        private  int storageState=0;
        private  ReleaseCapacity LMendoRelease;
        private  ReleaseCapacity LSonoRelease;

        private  void OnInitialize()
        {
            //min release clases
            LMendoMinFlow = new MinFlowCalculator(dataDBPath, "Mendo_MinFlow_1610QTUCP", ref m_Model);
            LMendocino = m_Model.FindNode("LMendocino");
            LSonoma = m_Model.FindNode("LSonoma");
            pillsburyIndex = m_Model.FindLink("PillsburyIndex");
            pillsburyStorage = m_Model.FindLink("PillsburyStorage");
            //  L.Sonoma min flow releases 
            SonomaMinFlow = new MinFlowCalculator(dataDBPath, "Sonoma_MinFlow_1610QTUCP", ref m_Model);

            //Mendocino Releases (controlled and uncontrolled)
            LMendoRelease = new ReleaseCapacity(dataDBPath, "MendocinoReleases", ref m_Model);

            //L.Sonoma Releases (controlled and uncontrolled)
            LSonoRelease = new ReleaseCapacity(dataDBPath, "SonomaReleases", ref m_Model);

            //West Fork Flow
            westForkFlows = m_Model.FindLink("WestForkFlow");
            MendoContrlledRelease = m_Model.FindLink("Mendo_ControlledRelease");
            hopelandGage = m_Model.FindLink("Hopland");
        }

       
        private  void OnIterationTop()
        {

            if (m_Model.mInfo.Iteration == 0)
            {
                DateTime thisDate = m_Model.TimeStepManager.Index2Date(m_Model.mInfo.CurrentModelTimeStepIndex, TypeIndexes.ModelIndex);
                
                //foreach (string key in modelFactors.Keys)
                //{
                //    Link l = m_Model.FindLink(key);
                //    if(l!=null)l.m.loss_coef = modelFactors[key][mon - 1];
                //}

                //Set operation flows -
                
                int YearTypeFlag = (int)(pillsburyIndex.mlInfo.flow / m_Model.ScaleFactor);
                if (YearTypeFlag == 1 || YearTypeFlag == 4)
                {
                    storageState = LMendoMinFlow.GetStorageState(m_Model, LMendocino.mnInfo.start, pillsburyStorage.mlInfo.flow);
                    LMendoMinFlow.AssignMinFlowsToNodes(m_Model.mInfo.CurrentModelTimeStepIndex, thisDate, storageState.ToString(),1);
                }
                else if (YearTypeFlag == 2 || YearTypeFlag == 3)
                    // YearTypeFlag: 2 & 3
                    // Min flow table uses columns 2 and 3 to store the values per node.
                    LMendoMinFlow.AssignMinFlowsToNodes(m_Model.mInfo.CurrentModelTimeStepIndex, YearTypeFlag.ToString());
                else
                    // ETS: Is this ever reached?
                    // Yes, the first iteration YearTypeFlag ==0 
                    LMendoMinFlow.AssignMinFlowsToNodes(m_Model.mInfo.CurrentModelTimeStepIndex, thisDate, "1",1);

                // L.Sonoma min flows are only a function of the YearTypeFlag.
                SonomaMinFlow.AssignMinFlowsToNodes(m_Model.mInfo.CurrentModelTimeStepIndex, thisDate, YearTypeFlag.ToString(),1);

            }

            if (m_Model.mInfo.Iteration > 1)
            {
                //Mendocino Releases (Controlled and Uncontrolled)
                LMendoRelease.SetReleaseCapacity(m_Model.mInfo.CurrentModelTimeStepIndex, LMendocino, interpolate: true);

                //West Fork flow - Release Check and overwrite Mendo controlled releases
                if ((westForkFlows != null))
                {
                    //reservoir releases cannot exceed 25 cfs if flow in west fork are > 2500 cfs and flows at Hopland are > 8000 cfs
                    double maxFlow_cfs = 2500 * 1.98347;
                    //If (westForkFlows.mlInfo.flow > maxFlow_cfs * accuracyFactor And hopelandGage.mlInfo.flow > 8000 * 1.98347 * accuracyFactor) And m_Model.mInfo.Iteration > 0 Then
                    if ((westForkFlows.mlInfo.flow > maxFlow_cfs * m_Model.ScaleFactor) && (m_Model.mInfo.Iteration > 0))
                    {
                        // 25 cfs in acre-feet per day
                        if (MendoContrlledRelease != null)
                            //MendoContrlledRelease.mlInfo.hi = (long)Math.Round((49.5867769 * m_Model.ScaleFactor), 0);
                            MendoContrlledRelease.mlInfo.hiVariable[m_Model.mInfo.CurrentModelTimeStepIndex, 0] = (long)Math.Round((49.5867769 * m_Model.ScaleFactor), 0);
                    }
                }

                //Lake Sonoma Releases (Controlled and Uncontrolled)
                LSonoRelease.SetReleaseCapacity(m_Model.mInfo.CurrentModelTimeStepIndex, LSonoma, interpolate: true);

            }
        }

        private  void OnMessage(string message)
        {
            Console.WriteLine(message);
        }

        private  void OnError(string message)
        {
            Console.WriteLine(message);
        }

        private  void OnIterationBottom()
        {
            //long a = MendoContrlledRelease.mlInfo.hi;
            //MendoContrlledRelease.mlInfo.hi = MendoContrlledRelease.mlInfo.hiVariable[m_Model.mInfo.CurrentModelTimeStepIndex, 0];
        }

        private  void OnIterationConverge()
        {
        }

        private  void OnFinished()
        {
        }
    }
}
