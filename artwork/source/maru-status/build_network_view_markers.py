"""#32 editable native Network View markers."""
from pathlib import Path
import sys
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save,pack,cuts,report
OUT=ROOT/"artwork/exports/network-view-markers.png"
REVIEW=ROOT/"artwork/review/maru-status/32-network-view-markers.png"
C=dict(out="#39294e",fill="#57435f",rim="#8e7091",pale="#edd8b2",teal="#9eccb8",
       darkteal="#609687",orange="#e6a962",warm="#b57562",pink="#eab193",
       pinkdeep="#c77e76",blue="#a9b8c6",ochre="#be936c")
N=Image.Resampling.NEAREST
def marker(i):
    im=Image.new("RGBA",(9,9));d=ImageDraw.Draw(im)
    def r(a,b,c,e,f):d.rectangle((a,b,c,e),fill=C[f])
    def p(x,y,c):d.point((x,y),fill=C[c])
    d.polygon([(2,0),(6,0),(8,2),(8,6),(6,8),(2,8),(0,6),(0,2)],fill=C["out"])
    d.polygon([(2,1),(6,1),(7,2),(7,6),(6,7),(2,7),(1,6),(1,2)],fill=C["fill"])
    d.line([(1,3),(1,2),(2,1),(5,1)],fill=C["rim"])
    if i==0:
        for x,y in ((2,4),(3,5),(4,6),(5,5),(5,4),(6,3),(4,5),(6,2)):p(x,y,"teal")
        p(3,6,"darkteal");p(6,4,"darkteal")
    if i==1:
        r(3,3,5,5,"blue");p(3,2,"blue");p(5,2,"blue");p(4,6,"blue");p(4,7,"blue")
        for x,y in ((2,6),(3,5),(4,4),(5,3),(6,2)):p(x,y,"pink")
        p(5,4,"pinkdeep")
    if i==2:
        for x,y in ((5,2),(6,3),(5,3),(4,3),(4,4),(3,5),(2,6)):p(x,y,"orange")
        p(3,6,"warm");p(5,4,"warm");p(6,2,"fill")
    if i==3:
        r(2,3,6,6,"warm");r(2,3,6,3,"pink");r(3,4,5,6,"ochre")
        p(4,3,"out");p(4,4,"out");p(4,6,"out")
    if i==4:
        p(3,3,"pale");p(5,3,"pale");p(2,4,"orange");p(6,4,"orange")
        r(2,5,6,5,"orange");r(3,6,5,6,"orange");p(4,5,"fill");p(3,5,"ochre");p(5,5,"ochre")
    return im
def main():
    if (OUT.exists() or REVIEW.exists()) and "--replace" not in sys.argv:raise FileExistsError("Marker files exist")
    frames=[marker(i) for i in range(5)]
    s=pack(frames);q=report(s)
    assert q["size"]==[45,9] and q["alpha"]==[0,255] and not q["pure_black"] and q["min_value"]>=.30
    assert len({a.tobytes() for a in frames})==5 and len({a.getchannel("A").tobytes() for a in frames})==1
    assert [f.tobytes() for f in cuts(s,9,9,5,1)]==[a.tobytes() for a in frames]
    save(s,OUT)
    im=Image.new("RGBA",(740,273),"#f6e5c6");d=ImageDraw.Draw(im)
    def ink(x,y,t,k):d.text((x,y),t,fill="#4d354c",font=ImageFont.load_default(size=k))
    ink(20,15,"STARDEW ENERGISTICS   /   MARU TECH",14)
    ink(20,36,"32  Network View Markers",29)
    ink(20,76,"Five distinct silhouettes inside one badge shape",16)
    labels=("Connected","No power","Not set up","Full","Missing input")
    for i,(f,label) in enumerate(zip(frames,labels)):
        x=20+i*142
        d.rectangle((x,112,x+128,217),fill="#ddbb91")
        im.alpha_composite(f.resize((72,72),N),(x+18,123))
        d.rectangle((x+101,176,x+120,202),fill="#cba573")
        im.alpha_composite(f,(x+106,183));ink(x+105,194,"1x",9)
        ink(x+7,222,label,14)
    ink(20,249,"9 x 9 transparent frames  /  enlarged nearest-neighbor for inspection",14)
    save(im,REVIEW)
    print(q["size"],q["colors"],q["min_value"])
if __name__=="__main__":main()
