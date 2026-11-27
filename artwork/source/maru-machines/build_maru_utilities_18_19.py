"""Editable native-pixel masters for Maru Utility #18-19."""
from pathlib import Path
import sys
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save, load, pack, cuts, report
N=Image.Resampling.NEAREST
PAL=dict(ink="#453748",iron_shadow="#545163",iron="#746f7d",iron_lit="#928895",
    iron_edge="#aba0a1",plum="#513768",purp="#765498",lit="#9c7cc1",glint="#b4a0cf",
    metal="#857690",deep="#513943",woodshade="#753a40",wood="#b86638",oak="#d48846",
    oaklit="#edab60",coppershade="#885049",copper="#c67c55",copperlit="#e0a36a",
    ember="#ba5644",fire="#e67c49",glow="#f3b763",spark="#f8d58f",dormant="#67596e",
    teal="#73aeae",teal_hi="#a3d1ba")
def canvas():
    im=Image.new("RGBA",(16,32));d=ImageDraw.Draw(im)
    def r(x,y,x2,y2,c):d.rectangle((x,y,x2,y2),fill=PAL[c])
    def p(points,c):d.polygon(points,fill=PAL[c])
    return im,r,p

def generator(state):
    im,r,p=canvas()
    # Rectangular chimney with a common industrial overlap at the rear of the lid.
    r(9,4,13,13,"ink");r(8,3,14,5,"ink");r(9,3,13,3,"iron_lit")
    r(10,6,12,11,"iron");r(10,6,10,10,"iron_lit")
    p([(3,10),(11,10),(14,13),(14,28),(13,28),(13,30),(11,30),(11,29),
       (4,29),(4,30),(2,30),(2,28),(1,27),(1,13)],"ink")
    p([(3,11),(11,11),(13,13),(2,13)],"iron_lit")
    r(3,11,8,11,"iron_edge");r(2,14,13,26,"iron")
    r(2,14,2,26,"iron_lit");r(13,15,13,27,"iron_shadow");r(2,27,13,27,"iron_shadow")
    r(3,15,8,23,"ink");r(4,16,7,22,"deep")
    if state==1:
        r(4,20,7,22,"ember");r(5,17,6,21,"fire");r(5,19,5,21,"spark");r(6,20,7,21,"glow")
    if state==2:
        r(4,20,7,22,"ember");r(5,19,7,21,"fire");r(6,17,6,20,"glow");r(6,19,6,20,"spark");r(4,21,5,21,"glow")
    r(4,19,7,19,"ink");r(4,22,7,22,"ink")
    r(3,24,8,24,"iron_edge");r(4,25,7,25,"iron_shadow")
    # Bent copper tube turns three times; single-pixel breaks between strands.
    for start,end,y in [(10,12,16),(10,12,19),(10,12,22)]:
        r(start,y,end,y,"copperlit");r(start,y+1,end,y+1,"coppershade")
    r(12,17,12,19,"copper");r(10,20,10,22,"copper");r(12,23,12,24,"copper")
    r(10,15,10,16,"copper");r(4,28,5,28,"iron_shadow");r(11,29,12,29,"iron_shadow")
    return im

def port(state):
    im,r,p=canvas()
    # Oak mounting bracket, weather hood, small gasketed face and separate conduit socket.
    p([(4,7),(11,7),(12,8),(12,25),(11,26),(4,26),(3,25),(3,8)],"woodshade")
    r(4,8,11,25,"wood");r(4,8,10,8,"oaklit");r(4,9,4,24,"oak");r(11,9,11,24,"woodshade")
    p([(4,10),(11,10),(13,12),(13,21),(12,23),(3,23),(2,22),(2,12)],"plum")
    r(3,12,12,12,"lit");r(4,11,10,11,"glint");r(4,13,11,20,"purp")
    r(3,13,3,21,"metal");r(12,13,12,21,"ink");r(3,21,11,22,"iron_shadow")
    p([(5,14),(10,14),(11,15),(11,19),(10,20),(5,20),(4,19),(4,15)],"ink")
    r(5,15,10,18,"iron_shadow");r(5,15,9,15,"metal")
    r(6,17,7,18,"oak" if state==0 else "teal");r(6,17,6,17,"oaklit" if state==0 else "teal_hi")
    r(9,17,9,18,"dormant");r(6,24,9,28,"plum");r(7,24,8,25,"metal")
    r(7,26,8,27,"ink");r(7,27,7,27,"teal" if state else "iron_lit")
    r(5,9,5,9,"metal");r(10,24,10,24,"metal")
    return im

def render_card(number,name,frames,labels,detail,wall=False):
    c=Image.new("RGBA",(670,386),"#f6e5c6")
    d=ImageDraw.Draw(c)
    def txt(x,y,s,k):d.text((x,y),s,font=ImageFont.load_default(size=k),fill="#4d354c")
    txt(20,15,"STARDEW ENERGISTICS   /   MARU TECH",14)
    txt(20,37,f"{number:02}  {name}",28)
    txt(20,77,detail,15)
    for i,(f,label) in enumerate(zip(frames,labels)):
        x=20+156*i
        d.rectangle((x,112,x+143,321),fill="#d8b58b")
        if wall:
            for y in range(120,321,22):
                d.line((x+1,y,x+142,y),fill="#c69b70")
                d.line((x+1,y+1,x+142,y+1),fill="#e3c298")
        else:
            d.line((x+1,305,x+142,305),fill="#c69e76")
        c.alpha_composite(f.resize((96,192),N),(x+24,120))
        txt(x+8,327,label,16)
    d.rectangle((502,112,650,223),fill="#caa37e")
    for i,f in enumerate(frames):
        c.alpha_composite(f,(521+42*i,139))
    txt(511,181,"Native 1x",15)
    txt(510,245,"16 x 32 per frame",15)
    txt(510,271,f"{len(frames)} states, left to right",14)
    txt(20,360,"Native pixels. Schematic background only.",13)
    return c

def main():
    out=ROOT/"artwork/exports";review=ROOT/"artwork/review/maru-machines"
    jobs=[(18,"coal-generator","Coal Generator",[generator(i) for i in range(3)],
            ["Off","Burning 1","Burning 2"],"Cast iron, hand-bent copper and a banked coal fire.",False),
          (19,"network-port","Network Port",[port(i) for i in range(2)],
            ["Off / unplugged","Connected"],"Weather hood, oak side mount and downward-facing socket.",True)]
    for n,key,*_ in jobs:
        if ((out/f"{key}.png").exists() or (review/f"{n}-{key}.png").exists()) and "--replace" not in sys.argv:
            raise FileExistsError(key)
    for n,key,name,frames,labels,description,wall in jobs:
        assert len({f.getchannel("A").tobytes() for f in frames})==1
        assert len({f.tobytes() for f in frames})==len(frames)
        strip=pack(frames);q=report(strip)
        assert strip.size==(16*len(frames),32) and q["alpha"]==[0,255] and not q["pure_black"]
        for f in frames[1:]:
            delta=[(x,y) for y in range(32) for x in range(16) if f.getpixel((x,y))!=frames[0].getpixel((x,y))]
            assert all((4<=x<=7 and 16<=y<=22) if n==18 else ((6<=x<=7 and 17<=y<=18) or (x==7 and y==27)) for x,y in delta)
        exp=out/f"{key}.png";card=review/f"{n}-{key}.png"
        save(strip,exp);save(render_card(n,name,frames,labels,description,wall),card)
        assert load(exp).tobytes()==strip.tobytes()
        assert all(a.tobytes()==b.tobytes() for a,b in zip(cuts(load(exp),16,32,len(frames),1),frames))
        print(exp.name,strip.size,"states",len(frames), "binary alpha, no black, bounded state delta")

if __name__=="__main__":main()