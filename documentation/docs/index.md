# Welcome to MODSIM-GSFLOW Coupling Documentation

For full mkdocs documentation visit [mkdocs.org](https://www.mkdocs.org).

## Introduction
This tool is a C# code that couples a MODSIM network with a corresponding GSFLOW model at iteration level.  The coupling provides access to GSFLOW variables and MODSIM variables using the corresponding dll files.  The coupling is implemented using the customization structure in MODSIM, which include calls during the MODSIM simulation OnInitialize, OnIterationTop, OnIterationBottom and OnConverge.  

## Commands

* `MODSIM_GSFLOW_C gsflow.control` - Executes a simulation with the preferences specified in the gsflow control file. The working directory should be the location of the GSFLOW control file. 

## Requirements 
### GSFLOW Control file
The GSFLOW control file requires the following entries:

* Synchronization Database

        DatabaseXXX    
    This argument includes the name of the synchronization database.
* MODSIM base file

        MODSIM FILE XXXX

    The file should include the objects, cost and logic to allocation water in the surface water system according to the required priorities, physical and administrative constraints.
### Synchronization Database
The synchronization table is an SQLite database with tables that allow building a relationship between the MODSIM objects and the GSFLOW objects. The database requires the following tables

* `MS-GSF_mapping_info` - This table contains the relationship between the MODSIM link name and the GSFLOW segment.  Additionally, it has field to flag the segments that are diversions and reservoir outlets.
* `MS-GSF_Lake_Mapping_Info` This table contains the relationship between the MODSIM reservoirs and the GSFLOW Lakes.
* `Settings` This table has general preferences used for the coupling code.  

## Run Modes
    MODSIM-only mode = 13

## Coupling Initialization
* Initialize the coupling arrays
    | Array         | Type      | Description/Use   |
    |----           |---        |---                |
    | Diversions    |Double     |
    | IDivert       |int        |                   |
    | IRelease      |int
    | EXCHANGE      |double
    | EXCHANGEPREV  |double
    | DELTAVOL      |double
    | DELTAVOLPREV  |double
    | LAKEVOL       |double
    | MXLKVOL       |double
    | STARTLAKEVOL  |double
    | DPOOL         |double
    | LAKEVAP       |double
    | LinkHi        |long
    | LinkHi_Sv     |long
#### Special Behavior
|Mode       |Description        |
|---        |---
|Mode 13    | 

## MODSIM Interaction 
In preparation for the coupled simulation 
* MODSIM reads the specified base MODSIM file, which should include time series, cost and logic for surface water allocation.
* The code creates a copy of the base file with the suffix _**_MSGMF**_ . 
This is the network that is modified for the coupled simulation and executed through the code. 
*   Reads the required tables from the synchronization database.
* Creates a sink node (_**MF_SINK**_) and a source node (_**MF_SOURCE**_) to simulate the stream accretions and depletions calculated by GSFLOW. A link (_**MF_SINK_TO_SOURCE**_) is created between the source and the sink to dispose the water that is not use in the simulation. 
    * The link is set up with a -1 cost.
    * The source capacity is set to 99000.0 [1000m3/day] 
* For each synchronized segment/link accretion and depletion links are created. Depletion and accretion links are created:
    * With the name of the link and a prefix "_**MF_Dep_**_" for depletion links and a prefix "_**MF_Acc_**_" for accretion links.
    * A cost of -500,000 plus a counter of links synchronized. 
    * links are created from the _TO_ node of the link (i.e., downstream end).
* For each reservoir node, depletion and accretion links are created to simulate runoff, evaporation and seepage calculated by GSFLOW.
* Initialize the accuracy variable. this allows to convert decimal values to the the MODSIM integer values, based on the user defined preferences for the number of decimals.      
```
        accuracy = Math.Pow(10.0, (double)myModel.accuracy);
```        

### On Initialize
* Initialize arrays used for the coupling data transfer. The array size is set to the segments and reservoirs synchronization tables (rows).
    | Array         | Type      | Description/Use   |
    |----           |---        |---                |
    | MS_Flows      |Double     |
    | MS_FlowsPREV  |Double     |
    | MS_FlowsLIMITED|Double    |
    | MS_Links      |Link       |
    | MS_Reservoirs |Node       |
* Populate the MS_Links and MS_Reservoir arrays with a pointer to the corresponding MODSIM link and node objects.
* Initialize custom MODSIM output to store the accretions and depletions for each link. The name of the custom variables are:
    * MF_Depletion
    * MF_Accretion
* Initialize the conversion factors
    | Variable  | Metric     | English   |
    |---        |---            |---    |
    | uConvToMODFLOW    | 1000      | 43560.0001|
    |uConvRateToMODSIM  | 0.0254    | 1/12|
* Initialize the _**LinksHi**_ array with the MODSIM upper bound.
* Clone the _**LinksHi**_ into **_LinkHi_Sv_**
#### Special Behavior
|Mode       |Description        |
|---        |---
|Mode 11    | Rezise the MODSIM evaporation array to use the values from |

### On Iteration Top
* Set the accretions and depletions in the MODSIM links.  Uses the _**EXCHANGE**_ array with the corresponding conversion factors (_**accuracy / uConvToMODFLOW**_).
* Positive values in _**EXCHANGE**_ are placed in the accretion link and negative values are multiplied by -1 and placed as positive in the depletion links.
* Values are placed in the _**mlInfo.hi**_ MODSIM variable.
* `[Condition: MFRunYet]` 
    * Resets the MODSIM reservoir start volume using the _**STARTLAKEVOL**_ variable.
    * Deals with reservoir storage releases when the reservoir is 90-100 percent full and the _**LAKE**_ values are greater than the maximum reservoir capacity. In these cases the release of the reservoir is calculated.  

### On Converge
* Store the current _**MS_Flows**_ into _**MS_FlowsPREV**_.
* Store diversions from the MODSIM flow in _**MS_Flows**_.
* Store the current _**EXCHANGE**_ in _**EXCHANGEPREV**_, if different when returning from GSFLOW.
* Store the current _**MS_Flows**_ in _**MS_FlowsLIMITED**_.
* Process the steady state results. This is performed the first time that the code reaches this point.  It uses a variable _**breakout**_ to process the reservoir results and restart the MODSIM solution. 
* Executes the current state of GSFLOW-PRMS and populate arrays that are returned to the c# code. The variables include:
    | Variable      | Contains      | Returns       |
    |---            |---            |---            |
    | MS_Flows      | Flows from MODSIM simulated in each link |
    | IDivert|
    | EXCHANGE|
    | DELTAVOL|
    | LAKEVOL|
    | LAKEVAP|
* Check for MODSIM-GSFLOW convergence
    | Check     | Criteria      |
    |---        |---            |
    |Flows      | Checks that all the differences between the current and previous flows _**MS_FLOWS**_ are within the   volume convergence tolerance (_**EXCHNGVol_Tolerance**_).|
    | Accretions/Depletions| Checks that all the differences between the current and previous _**EXCHANGE**_ valued are within the   volume convergence tolerance. (_**EXCHNGVol_Tolerance**_).|

    `if (Model_mode != 11)`
    | Check     | Criteria      |
    |---        |---            |
    |Lake Volume      | Checks that all the differences between the current and previous flows _**DELTAVOL**_ are within the   volume convergence tolerance (_**LAKEVol_Tolerance**_).|
    | MODSIM Storage    | Checks that the simulated MODSIM end storage is within the _**LAKEVol_Tolerance**_ with the GSFLOW simulated _**LAKEVOL**_.  

* `[If Converged]` For all reservoirs set the _**STARTLAKEVOL**_ = _**LAKEVOL**_. This will be used to set the MODSIM start volume in the next iteration. 

* `[If Converged and MODSIM-GSFLOW iterated more than twice]`   
    * Run GSFLOW once more to advance to the next time.
    * For rows with adjusted hi values it uses _**LinkHi_Sv**_ to reset the maximum value in the hi variable.  

#### Special Behavior
|Mode       |Description        |
|---        |---
|Mode 11    | Set the reservoirs evaporation rate based on the PRMS calculated rate. It doesn't run GSFLOW again when MODSIM-GSFLOW converge.|

## Questions/Issues
1. The array _**MS_FlowsLIMITED**_ doesn't seem to be populated in GSFLOW as indicated in the code comments. It's set in the C# to the MS_Flows and used in the reservoir outflow to get an average?
    
    
    It seems to just keep the previous value and used for tolerance check 
2. Why we need to deal with the releases when the reservoir is approaching to be full or greater than full.  It seems like we should not be constraining releases in those cases. Specially when the reservoir is full releases should represent uncontrolled spillway releases.
    ```
    if (!(((double)myModel.FindNode(m_row["AssocRes"].ToString()).mnInfo.stend > (0.9 * (double)myModel.FindNode(m_row["AssocRes"].ToString()).m.max_volume)) || ((LAKEVOL[int.Parse(m_SyncTblRES.Select("MODSIM_Name Like '" + m_row["AssocRes"].ToString() + "'")[0][0].ToString()) - 1] / uConvToMODFLOW * accuracy) > (0.9 * (double)myModel.FindNode(m_row["AssocRes"].ToString()).m.max_volume))))
    ```
    It seems like it's the opposite - meaning when is not 90% or above.

3. How is the table field _**adjted**_ used?

4. What is the fucntion of _**MFRunYet**_?
    
    This is set to true OnConverged, when GSFLOW has run and the solution has not converged.  It's set to false when MODSIM-GSFLOW converged and finalize the time step (advance).
    It's mainly used to set the reservoir starting volume to the GSFLOW starting volume after GSFLOW has been run and the 


5. Why we don't allow convergence of MODSIM-GSFLOW before 7 iterations?

    _**localMODSIMIter**_ is local counter of MODSIM iterations to make sure that there are enough MODSIM iterations after the GSFLOW solution has been set in MODSIM.  
    [TODO] need to figure out how to reset the MODSIM internal iteration variable. 

6. Does the last GSFLOW run once MODSIM-GSFLOW converge update any of the arrays?

7. The use of **_LinkHi_Sv_** array seems to be wrong. It uses index 0 to reset all the hi variables. 

## Ideas
* The _**LAKE**_ array could be set up as a dictionary to use the name of the node and simplify the dynamic search for the index.
* Separate the core code from other customization like "WES_ON" and other fixes that might be needed for specific applications.  We can have a class with the core functionality and a command line application that references the core class. 
* for max variable time series the setting of the variable might need to be done at the beginning of every time step.