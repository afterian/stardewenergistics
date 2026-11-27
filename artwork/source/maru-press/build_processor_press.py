"""Original native Maru Processor Press; rebuild from editable pixel primitives."""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import load, save, pack, cuts, report
OUT=ROOT/"artwork/exports/processor-press.png"
REVIEW=ROOT/"artwork/review/maru-press/11-processor-press.png"
N=Image.Resampling.NEAREST
P={"outline":"#39294e","deep":"#513768","violet":"#765498","light":"#9c7cc1","glint":"#b4a0cf",
"darkwood":"#753a40","wood":"#b86638","oak":"#d48846","honey":"#edab60",
"copper":"#c8784c","copperhot":"#e99e60","darkmetal":"#665771","pewter":"#b8a8ad",
"glass":"#54445e","reflect":"#6e5976","amber":"#dba356","yellow":"#f2cd85","teal":"#73aeae","low":"#745d72"}

def frame(state):
    im=Image.new("RGBA",(16,32));d=ImageDraw.Draw(im)
    def r(x,y,z,w,c):d.rectangle((x,y,z,w),fill=P[c])
    def q(x,y,c):d.point((x,y),fill=P[c])
    # Original honey-oak drawer cabinet. Keep this registration in all three states.
    d.polygon([(2,18),(13,18),(15,20),(15,29),(14,30),(1,30),(0,29),(0,20)],fill=P["outline"])
    r(1,20,14,28,"wood");r(1,20,14,21,"oak");r(2,19,13,20,"honey")
    r(1,24,14,24,"darkwood");r(2,25,13,26,"oak");r(1,28,14,28,"darkwood")
    r(2,29,13,29,"wood");r(3,21,10,21,"honey");r(3,25,7,25,"honey")
    r(1,22,1,23,"darkmetal");r(14,22,14,23,"darkmetal");r(1,27,1,28,"darkmetal");r(14,27,14,28,"darkmetal")
    q(1,22,"pewter");q(14,27,"pewter")
    r(5,26,9,27,"darkwood");r(6,26,8,26,"pewter");q(5,27,"darkmetal");q(9,27,"darkmetal")
    q(11,22,"low" if state==0 else "teal");q(12,22,"teal" if state==2 else "low")
    # Fixed supports and window (the glass and moving punch fit inside the frame).
    r(4,12,11,19,"outline");r(5,14,10,18,"glass");r(5,15,10,15,"reflect")
    r(6,18,9,18,"copper")
    # Two wide purple jaws with subtle upper-left light.
    for x in (2,11):
        d.polygon([(x,11),(x+1,10),(x+3,10),(x+3,18),(x+2,20),(x,20),(x-1,19),(x-1,12)],fill=P["outline"])
        r(x,12,x+2,18,"violet");r(x,12,x+2,12,"light");q(x+1,11,"light");q(x+1,12,"glint")
        r(x+2,13,x+2,18,"deep");r(x,18,x+1,19,"deep")
    # Copper lever starts raised and clamps down as the window sparks.
    y=13 if state==0 else 15
    r(5,y,10,y,"copperhot");r(6,y+1,9,y+1,"copper")
    if state==1:
        q(6,17,"yellow");q(8,17,"amber");q(9,18,"copperhot")
    if state==2:
        q(6,18,"amber");q(7,17,"copperhot");q(9,17,"yellow");q(10,18,"amber")
    return im

def card(frames):
    im=Image.new("RGBA",(650,342),"#f6e5c6");d=ImageDraw.Draw(im)
    def ink(x,y,t,k):d.text((x,y),t,fill="#4d354c",font=ImageFont.load_default(size=k))
    ink(20,15,"STARDEW ENERGISTICS   /   MARU TECH",14)
    ink(20,36,"11  Processor Press",29)
    ink(20,76,"Honey-oak drawer box  /  violet jaws  /  copper punch",16)
    names=("Idle","Working 1","Working 2")
    for i,(f,label) in enumerate(zip(frames,names)):
        x=20+i*208
        d.rectangle((x,112,x+188,269),fill="#dfbd92")
        # Two blocks of oak planks keep the outline readable at the actual game scale.
        for j in range(123,263,16): d.line((x+1,j,x+187,j),fill="#d7af83")
        im.alpha_composite(f.resize((64,128),N),(x+42,124))
        d.rectangle((x+132,213,x+170,254),fill="#cda879")
        im.alpha_composite(f,(x+143,216));ink(x+141,247,"1x",10)
        ink(x+9,278,label,18)
    ink(20,313,"4x game-scale mockup  /  16 x 32 frames  /  inventory uses the idle sprite",14)
    return im

def main():
    if (OUT.exists() or REVIEW.exists()) and "--replace" not in sys.argv:raise FileExistsError("Review or export already exists")
    frames=[frame(i) for i in range(3)]
    for a in frames:
        assert a.size==(16,32) and a.getbbox()==frames[0].getbbox()
        assert a.getchannel("A").tobytes()==frames[0].getchannel("A").tobytes()
    assert len({a.tobytes() for a in frames})==3
    for a in frames[1:]:
        delta=[(x,y) for y in range(32) for x in range(16) if a.getpixel((x,y))!=frames[0].getpixel((x,y))]
        assert delta and all((5<=x<=10 and 13<=y<=18) or (11<=x<=12 and y==22) for x,y in delta),delta
    strip=pack(frames)
    q=report(strip)
    assert q["size"]==[48,32] and q["alpha"]==[0,255] and q["pure_black"]==0 and q["min_value"]>=.30
    assert [i.tobytes() for i in cuts(strip,16,32,3,1)]==[i.tobytes() for i in frames]
    save(strip,OUT);save(card(frames),REVIEW)
    assert load(OUT).tobytes()==strip.tobytes()
    print(q["size"],q["colors"],q["min_value"],frames[0].getbbox())
if __name__=="__main__":main()
