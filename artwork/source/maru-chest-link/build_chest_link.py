"""#23 Chest Link: original native overlay; chest art is a preview fixture only."""
from pathlib import Path
import sys
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save,report
OUT=ROOT/"artwork/exports/chest-link.png"
REVIEW=ROOT/"artwork/review/maru-chest-link/23-chest-link.png"

def link():
    im=Image.new("RGBA",(16,16));d=ImageDraw.Draw(im)
    # Upper-right lid clamp, with the narrow tab hooked over the edge.
    d.polygon([(9,2),(13,2),(14,3),(14,7),(13,8),(11,8),(10,7),(9,7),(8,6),(8,3)],
              fill="#4a3351")
    d.rectangle((9,3,13,6),fill="#a85449")
    d.line((9,3,12,3),fill="#e2a366")
    d.line((9,4,9,6),fill="#ce7e57")
    d.line((12,5,12,7),fill="#ba6a4d")
    d.point((10,5),fill="#6a4050")
    d.point((11,4),fill="#6b4b5d")
    d.point((12,4),fill="#86bbae")
    d.point((13,4),fill="#aeceae")
    d.point((11,7),fill="#916057")
    return im

def mock_chest():
    im=Image.new("RGBA",(16,16));d=ImageDraw.Draw(im)
    d.polygon([(2,2),(13,2),(15,4),(15,13),(14,14),(1,14),(0,13),(0,4)],fill="#603a42")
    d.rectangle((1,4,14,13),fill="#b66e44")
    d.rectangle((2,3,13,6),fill="#daa264")
    d.rectangle((1,7,14,8),fill="#925344")
    d.rectangle((2,10,13,11),fill="#c47f50")
    d.line((7,3,7,6),fill="#b67850");d.line((4,3,4,5),fill="#bd8353")
    d.rectangle((6,7,9,10),fill="#735152");d.rectangle((7,7,8,8),fill="#d8ae74")
    return im

def main():
    if (OUT.exists() or REVIEW.exists()) and "--replace" not in sys.argv:raise FileExistsError("Export or preview exists")
    art=link();q=report(art)
    assert q["size"]==[16,16] and q["alpha"]==[0,255] and not q["pure_black"] and q["min_value"]>=.30
    assert art.getbbox()==(8,2,15,9)
    save(art,OUT)
    fixture=mock_chest();attached=fixture.copy();attached.alpha_composite(art)
    assert all(not art.getpixel((x,y))[3] or fixture.getpixel((x,y))[3] for y in range(16) for x in range(16))
    im=Image.new("RGBA",(600,310),"#f6e5c6");d=ImageDraw.Draw(im);near=Image.Resampling.NEAREST
    def ink(x,y,t,k):d.text((x,y),t,fill="#4d354c",font=ImageFont.load_default(size=k))
    ink(20,15,"STARDEW ENERGISTICS   /   MARU TECH",14)
    ink(20,36,"23  Chest Link",29)
    ink(20,76,"Copper lid clamp  /  teal linked indicator",16)
    for i,(f,label) in enumerate(((fixture,"Chest fixture"),(attached,"Linked chest"),(art,"Overlay only"))):
        x=20+i*188
        d.rectangle((x,112,x+170,235),fill="#ddbb91")
        im.alpha_composite(f.resize((96,96),near),(x+22,124))
        d.rectangle((x+126,196,x+157,226),fill="#caa37e")
        im.alpha_composite(f,(x+133,201))
        ink(x+8,245,label,16)
    ink(20,281,"16 x 16 transparent attachment  /  chest is a mockup, not part of the export",14)
    save(im,REVIEW)
    print(q["size"],q["colors"],q["min_value"],art.getbbox())
if __name__=="__main__":main()
