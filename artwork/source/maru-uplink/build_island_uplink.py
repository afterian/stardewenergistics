"""#20 Island Uplink: editable native three-frame pixel master."""
from pathlib import Path
import sys
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save,pack,cuts,report
OUT=ROOT/"artwork/exports/island-uplink.png"
REVIEW=ROOT/"artwork/review/maru-uplink/20-island-uplink.png"
P=dict(out="#39294e",shadow="#513768",violet="#765498",light="#9c7cc1",glint="#b4a0cf",
       socket="#4b435d",steel="#857690",wooddeep="#753a40",wood="#b86638",oak="#d48846",
       sun="#edab60",mint="#a2cdb8",teal="#73aeae",fade="#6faaa1",dorm="#615278")
def frame(state):
    im=Image.new("RGBA",(16,32));d=ImageDraw.Draw(im)
    def p(vertices,c):d.polygon(vertices,fill=P[c])
    def r(x,y,z,w,c):d.rectangle((x,y,z,w),fill=P[c])
    def line(points,c):d.line(points,fill=P[c],width=1)
    # Anchored instrument plinth with honey-oak trim and a plum cabinet.
    p([(3,24),(12,24),(14,26),(14,29),(13,30),(2,30),(1,29),(1,26)],"out")
    r(2,26,13,28,"wood");r(2,26,13,26,"sun");r(3,27,12,28,"oak")
    r(3,29,12,29,"wooddeep");r(5,28,10,28,"wood")
    r(3,27,3,28,"steel");r(12,27,12,28,"steel")
    # Bolted swivel base and tilted arm.
    p([(5,23),(5,18),(7,17),(10,20),(10,23),(12,24),(12,26),(3,26),(3,24)],"out")
    r(4,24,11,25,"violet");r(4,24,7,24,"light");r(9,25,10,25,"shadow")
    r(6,21,8,23,"violet");r(6,21,6,23,"light");r(9,22,9,23,"shadow")
    r(7,25,7,25,"dorm" if state==0 else "teal")
    # Tilted elliptical dish: upper rim, concave band, and the shaded back.
    p([(3,12),(5,10),(8,9),(11,9),(12,10),(12,14),(10,17),(10,19),(8,21),
       (5,21),(3,20),(1,18),(1,15)],"out")
    p([(2,15),(4,12),(6,11),(9,10),(11,10),(11,14),(9,17),(7,19),(4,20),(2,18)],"violet")
    p([(3,15),(5,12),(9,11),(11,11),(10,14),(8,16),(5,18),(3,18)],"light")
    line([(2,16),(4,13),(6,11),(10,10)],"glint")
    line([(3,19),(6,19),(9,17),(11,14)],"shadow")
    line([(5,17),(7,16),(9,13),(11,11)],"socket")
    # Feedhorn projects northeast from the bowl.
    line([(8,15),(10,12),(12,10),(13,9)],"shadow")
    r(12,9,13,10,"steel");r(13,8,14,9,"shadow")
    r(13,8,13,8,"light");r(14,8,14,8,"dorm" if state==0 else "mint")
    # Only the detached signal pixels and status lights change frame-to-frame.
    pulses=() if state==0 else ((11,6,"fade"),(12,5,"teal"),(13,5,"mint"),(14,6,"teal")) if state==1 else (
        (10,5,"fade"),(11,4,"teal"),(12,3,"mint"),(13,3,"mint"),(14,4,"teal"),(15,5,"fade"))
    for x,y,c in pulses:r(x,y,x,y,c)
    return im

def main():
    if (OUT.exists() or REVIEW.exists()) and "--replace" not in sys.argv:raise FileExistsError("Export or preview exists")
    frames=[frame(q) for q in range(3)]
    alphas=[q.getchannel("A").crop((0,7,16,32)).tobytes() for q in frames]
    assert len(set(alphas))==1 and len({q.tobytes() for q in frames})==3
    assert len({q.crop((0,19,16,32)).getbbox() for q in frames})==1
    strip=pack(frames);q=report(strip)
    assert q["size"]==[48,32] and q["alpha"]==[0,255] and q["pure_black"]==0 and q["min_value"]>=.30
    assert [i.tobytes() for i in cuts(strip,16,32,3,1)]==[i.tobytes() for i in frames]
    save(strip,OUT)
    im=Image.new("RGBA",(650,342),"#f6e5c6");d=ImageDraw.Draw(im)
    near=Image.Resampling.NEAREST
    def ink(x,y,t,k):d.text((x,y),t,fill="#4d354c",font=ImageFont.load_default(size=k))
    ink(20,15,"STARDEW ENERGISTICS   /   MARU TECH",14)
    ink(20,36,"20  Island Uplink",29)
    ink(20,76,"Tilted dish  /  oak plinth  /  rising teal signal",16)
    for i,(f,label) in enumerate(zip(frames,("Off","Pulse 1","Pulse 2"))):
        x=20+i*208
        d.rectangle((x,112,x+188,269),fill="#d7ba93")
        for j in range(123,263,16):d.line((x+1,j,x+187,j),fill="#cfad83")
        im.alpha_composite(f.resize((64,128),near),(x+42,124))
        d.rectangle((x+132,213,x+170,254),fill="#cda879")
        im.alpha_composite(f,(x+143,216));ink(x+141,247,"1x",10)
        ink(x+9,278,label,18)
    ink(20,313,"4x game-scale mockup  /  16 x 32 frames  /  mounting stays fixed",14)
    save(im,REVIEW)
    print(q["size"],q["colors"],q["min_value"],[f.getbbox() for f in frames])
if __name__=="__main__":main()
