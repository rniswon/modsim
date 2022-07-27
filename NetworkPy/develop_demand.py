# example for russian river (loading entire model)
import gsflow

xll = 465900
yll = 4238400
# gsf = gsflow.GsflowModel.load_from_file("rr_tr.control")

# # set grid offsets and epsg code for shapefile
# gsf.mf.modelgrid.set_coord_info(xoff=xll, yoff=yll, epsg=26910)

# # create the modsim object and write to shapefile
# modsim = gsflow.modsim.Modsim(gsf)
# modsim.write_modsim_shapefile("develop.shp", flag_spillway="elev", flag_ag_diversion=True)


# example for sagehen (using only necessary files):
from gsflow.modsim import Modsim
from gsflow.modflow import Modflow, ModflowAg
import flopy as fp
import os

# set path:
ws = os.path.abspath(os.path.dirname(__file__))
model_ws = os.path.join(ws,"..","..","gsflow_examples","sagehen_3lay_modsim_ag","input","modflow")

xll = 214270.0
yll = 4366610.0

# create empty modflow object and load individual package files
ml = Modflow()
dis = fp.modflow.ModflowDis.load(os.path.join(model_ws, "sagehen.dis"), ml)
sfr = fp.modflow.ModflowSfr2.load(os.path.join(model_ws, "sagehen.sfr"), ml)
lak = fp.modflow.ModflowLak.load(os.path.join(model_ws, "sagehen.lak"), ml)
ag = ModflowAg.load(os.path.join(model_ws, "sagehen.ag"), ml)

# set grid offsets and epsg code
ml.modelgrid.set_coord_info(xoff=xll, yoff=yll, epsg=26911)

# create modsim object and write shapefile
modsim = Modsim(ml)
modsim.write_modsim_shapefile("develop.shp", nearest=False, flag_ag_diversion=True)



# https://github.com/pygsflow/pygsflow/blob/develop/examples/pygsflow_MODSIM_stream_vectors.ipynb