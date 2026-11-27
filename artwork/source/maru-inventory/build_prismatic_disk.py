"""Asset 09, shared Maru native cartridge."""
from pathlib import Path
import sys
from PIL import ImageDraw
from build_storage_disks_05_08 import BASE,card,disk
from pixel_pipeline import load,report,save
ROOT=Path(__file__).resolve().parents[3]
im=disk("prismatic",("#574362","#745a81","#947f9f","#c9b5c1","#594e6d","#96829d","#bba0b0"))
d=ImageDraw.Draw(im)
bands=("#bd8799","#d8a986","#cad0a0","#96bba8","#8eb4bf","#a293c1","#a98cb1")
for y,row in enumerate(BASE):
    for x,key in enumerate(row):
        if key=="F" and (y not in (5,6,7) or x>8):
            d.point((x,y),fill=bands[min(6,max(0,(2*x+y-12)//4))])
for x,c in enumerate(("#e5b6b3","#e6c69d","#dadcaf","#b8d5b0","#a4cbbf","#a5c7d4","#bcaed7","#d1b4d5"),3):
    d.point((x,3),fill=c)
d.line((3,4,3,7),fill="#cf9bab")
d.line((3,8,3,11),fill="#c29bac")
for x,y,c in ((10,6,"#d4e4bb"),(10,7,"#7fa4a0"),(12,6,"#b6d4dc"),(12,7,"#748eaa")):
    d.point((x,y),fill=c)
q=report(im)
assert q["size"]==[16,16] and q["alpha"]==[0,255] and q["pure_black"]==0
exports=ROOT/"artwork/exports"
for kind in ("copper","iron","gold","iridium"):
    assert im.getchannel("A").tobytes()==load(exports/f"{kind}-disk.png").getchannel("A").tobytes()
out=exports/"prismatic-disk.png"
review=ROOT/"artwork/review/maru-inventory/09-prismatic-disk.png"
if (out.exists() or review.exists()) and "--replace" not in sys.argv:
    raise FileExistsError("Existing Prismatic Disk output")
save(im,out)
save(card(im,9,"prismatic","Rainbow sheen. Soft lit rim."),review)
assert load(out).tobytes()==im.tobytes()
print(q)
