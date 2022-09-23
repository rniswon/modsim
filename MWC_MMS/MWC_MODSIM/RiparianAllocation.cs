using System;
using System.Collections.Generic;
using System.Text;

using Csu.Modsim.ModsimIO;
using Csu.Modsim.ModsimModel;

using System.IO;
using System.Data;
using RTI.CWR.MMS_Support;

namespace RTI.CWR.MWC_MODSIMUtils
{
    public delegate void ProcessMessage(string msg);  // delegate
    public class RiparianAllocation
    {
        public Model m_Model = new Model();
        
        private Dictionary<string, RiparianRight> riparianLinks;
        private double UBRed;
        private double UBPerc;
        private double MaxAlloc;
        private int RipAllocLoopCount;
        private int countLocked;
        private int _riparianCost;

        public event ProcessMessage messageOut;     //event

        public RiparianAllocation (ref Model model, int riparianCost = -999)
        {

            model.Init += OnInitialize;
            model.IterBottom += OnIterationBottom;
            model.IterTop += OnIterationTop;
            model.Converged += OnIterationConverge;
            model.End += OnFinished;

            m_Model = model;

            _riparianCost = riparianCost;
            
        }
      
        private long AccuracyConversionToReal(long value)
        {
            long newValue = (long)Math.Round(value/m_Model.ScaleFactor,0);
            return newValue;
        }

        private void OnInitialize()
        {
            InitilizeVariables();
            RipAllocLoopCount = 0;
            countLocked = 0;

        }

        private  void InitilizeVariables()
        {
            riparianLinks = new Dictionary<string, RiparianRight>(); // List of links from inflow -> Riprian demand node

            Link l = m_Model.firstLink;
            while (l != null)
            {
                if (l.m.cost == _riparianCost && int.Parse(l.m.maxVariable.dataTable.Rows[0][1].ToString())!=0)
                {
                    // The dictionary kwy corresponds to the link name in the MODSIM Network. Need to use 'to' for linking with demand
                    RiparianRight rr = new RiparianRight(l);
                    riparianLinks.Add(l.name, rr);
                }
                l = l.next;
            }
            messageOut($"Found {riparianLinks.Count} ripian right lins");
        }

        private  void OnIterationTop()
        {
            if (m_Model.mInfo.Iteration == 0)
            {
            
                if (RipAllocLoopCount == 0)
                {
                    UBPerc = 0;
                    UBRed = 0.1;
                    UBPerc += UBRed;
                

                    //Initialize all riparian rights as unlocked
                    foreach (string name in riparianLinks.Keys)
                    {
                        riparianLinks[name].clusterLocked = false;
                        riparianLinks[name].riparianLink.mlInfo.cost = _riparianCost;
                    }
                }
                else
                {
                    UBPerc += UBRed;
                }
                //UBPerc = Math.Min(1, UBPerc);
            }
            foreach (string name in riparianLinks.Keys)
            {
                //max flow (hi) updated based on UBPerc and stepsize. hi is integer, so we need to round and convert. hiVariable = orginal upper limit
                if (!riparianLinks[name].clusterLocked)
                    riparianLinks[name].riparianLink.mlInfo.hi = (long) Math.Round(riparianLinks[name].riparianLink.mlInfo.hiVariable[m_Model.mInfo.CurrentModelTimeStepIndex,0] * (UBPerc),0); 
            }
        }

        private  void OnIterationBottom()
        {
        }

        private  void OnIterationConverge()
        {
            long currentAlloc = 0;
            double minSP = 1;

           //messageOut("    UBRed:" + UBRed + " UBPerc:" + UBPerc);

            foreach (string name in riparianLinks.Keys)
            {
                currentAlloc += riparianLinks[name].riparianLink.mlInfo.flow;
                riparianLinks[name].shortPercent = Math.Round((double)riparianLinks[name].riparianLink.mlInfo.flow / riparianLinks[name].riparianLink.mlInfo.hi, m_Model.accuracy);
                //riparianLinks[name].shortPercent = (double)riparianLinks[name].riparianLink.mlInfo.flow / riparianLinks[name].riparianLink.mlInfo.hi;


                //only calculate minSP for unlocked users
                if (riparianLinks[name].clusterLocked == false && riparianLinks[name].shortPercent < minSP)
                    minSP = riparianLinks[name].shortPercent;

            }

            if (RipAllocLoopCount == 0)
            {
                MaxAlloc = currentAlloc;
                UBRed = 0.1;
            }

            m_Model.mInfo.convg = false;
            RipAllocLoopCount = RipAllocLoopCount + 1;

            if ((minSP == 1 && UBRed >= 0) || (minSP < 1 && UBRed < 0)) //If True -- we want to KEEP moving in the direction of UBRed
            {
                
                //If we have met precision for P, implement LOCK for users where SP = MinSP AND 
                if (Math.Abs(UBRed) < 1 / (m_Model.ScaleFactor * 100) && (minSP < 0.9999 && UBRed < 0))
                {

                    messageOut($"   ---");
                    foreach (string name in riparianLinks.Keys)
                    {
                        if (riparianLinks[name].shortPercent < 1 && riparianLinks[name].clusterLocked == false)
                        {

                            //if(riparianLinks[name].GetRiverDWSFlow()==0) 

                            //Implement upstream navigation + locking logic here

                            //flag upstream 
                            //flagUpstream(riparianLinks[name].riparianLink.from);
                            riparianLinks[name].clusterLocked = true;
                            riparianLinks[name].riparianLink.mlInfo.cost = -50999;
                            countLocked += 1;
                            messageOut($"   right {name} locked in riparian processing. Set to: {UBPerc * 100}%");
                            messageOut($"      right:{Math.Round((double)riparianLinks[name].riparianLink.mlInfo.hiVariable[m_Model.mInfo.CurrentModelTimeStepIndex, 0])},flow:{riparianLinks[name].riparianLink.mlInfo.flow }, hi:{ riparianLinks[name].riparianLink.mlInfo.hi }");
                        }

                    }
                    if (countLocked == riparianLinks.Count)
                    {
                        messageOut($"   riparian rights set to: {UBPerc * 100}%");
                        //Accept convergence and move to the next time step
                        m_Model.mInfo.convg = true;
                        RipAllocLoopCount = 0;
                        countLocked = 0;

                        
                    }

                    //Reset UBRed        
                    UBRed = 0.1;
                }
            }
            else
            {
                //allocation is lower than expected.
                //  diversion restriction it too high

                UBRed = UBRed * -0.5;
            }
            
         m_Model.mInfo.Iteration = 0;


        }

        //private  void flagUpstream(Node fromNode, Link currentDSLink)
        //{
            
        //    while (inflowLinks.next != null)
        //    {
        //        Link fromNode = fromNode.link;
                

        //        //check if riparian
        //        if (riparianLinks.ContainsKey(currentNode.name))
        //            {
        //            //if riparian and unlocked, LOCK
        //            if (riparianLinks[currentNode.name].clusterLocked == false)
        //            {
        //                riparianLinks[currentNode.name].clusterLocked = true;
        //            }
        //            //if NOT a riparian link, keep moving upwards

        //        }


        //    }
        //    throw new NotImplementedException();
        //}

        private  void OnFinished()
        {
        }
    }
}
