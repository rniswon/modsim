using Csu.Modsim.ModsimModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RTI.CWR.MWC_MODSIMUtils
{
    public class RiparianRight
    {
        public Link riparianLink;
        public double shortPercent;
        public bool clusterLocked;

        public RiparianRight(Link link)
        {
            riparianLink = link;
            shortPercent = 1;
            clusterLocked = false;
        }

        /// <summary>
        /// Gets the flow in the DWS link that is not connected to a Demand Node
        /// </summary>
        /// <returns>Current flow in the link</returns>
        public long GetRiverDWSFlow()
        {
            long flow=0;
           
            LinkList ll = riparianLink.from.OutflowLinks;
            while(ll != null )
            {
                if (ll.link.to.nodeType != NodeType.Demand)
                {
                    if (flow > 0)
                        throw new Exception($"   ERROR: multiple DWS links found at {ll.link.to.name}");
                    flow = ll.link.mlInfo.flow;
                }
                ll = ll.next;
            }
            return flow;
        }
    }
}
