"""Original pixel mockup of Maru's animated data bus. Does not alter mod assets."""
from collections import deque
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import load, save
OUT=ROOT/"artwork/review/maru-data-bus"
N=Image.Resampling.NEAREST
DIR=((0,-1),(1,0),(0,1),(-1,0))
G={(i,3) for i in range(1,11)} | {(1,2),(1,1),(10,2),(10,1),(5,4),(5,5),(7,2),(7,1)}
HEAVY={(i,3) for i in range(2,10)}|{(5,4)}
ORIGIN=(1,1)
DIST={ORIGIN:0}
Q=deque([ORIGIN])
while Q:
    x,y=Q.popleft()
    for dx,dy in DIR:
        p=(x+dx,y+dy)
        if p in G and p not in DIST:
            DIST[p]=DIST[x,y]+16
            Q.append(p)
assert set(DIST)==G

def floor_bg():
    im=Image.new("RGBA",(192,112),"#cc9050")
    d=ImageDraw.Draw(im)
    for yy in range(0,112,8):
        d.line((0,yy,191,yy),fill="#aa6c43")
        d.line((0,yy+1,191,yy+1),fill="#dfa565")
        for xx in range((yy*3)%32,192,32):
            d.line((xx,yy+1,xx,yy+7),fill="#b87943")
            if xx+14<192:d.line((xx+5,yy+4,xx+14,yy+4),fill="#d29853")
    return im

def tile(p, t, powered=True):
    x,y=p
    big=p in HEAVY
    lo,hi=(5,10) if big else (6,8)
    shape=Image.new("RGBA",(16,16));d=ImageDraw.Draw(shape)
    connected=[(x+dx,y+dy) for dx,dy in DIR]
    active=[i for i,q in enumerate(connected) if q in G]
    arms=[(lo,0,hi,8),(7,lo,15,hi),(lo,7,hi,15),(0,lo,8,hi)]
    for i in active:d.rectangle(arms[i],fill="#493557" if big else "#694151")
    # Cushion edges and small raised bus hub; no silver rings or cylindrical highlight.
    if big:
        for i,(xx,yy) in enumerate(((7,0),(15,7),(7,15),(0,7))):
            if i not in active:continue
            if i==0:d.rectangle((lo+1,0,hi-1,7),fill="#6e507b")
            elif i==1:d.rectangle((8,lo+1,15,hi-1),fill="#6e507b")
            elif i==2:d.rectangle((lo+1,8,hi-1,15),fill="#6e507b")
            else:d.rectangle((0,lo+1,7,hi-1),fill="#6e507b")
    # A dark inset center trace, with a traveling dot on narrow spans and paired windows on trunk.
    for i in active:
        dx,dy=DIR[i]
        q=connected[i]
        sign=1 if DIST[q]>DIST[p] else -1
        for k in range(1,9):
            xx,yy=7+dx*k,7+dy*k
            if not (0<=xx<16 and 0<=yy<16):continue
            dist=DIST[p]+k*sign
            phase=(t-dist)%32
            window=(k in (2,3,5,6)) if big else True
            if big:
                color=("#b4e5b6" if phase<3 else "#74b5ac" if phase<8 else "#597783") if powered and window else ("#463a59" if window else "#6e507b")
                if window:
                    second=("#83c3ad" if phase<4 else "#5b8887" if phase<8 else "#4b6373") if powered else "#423653"
                    d.point((xx,yy),fill=color)
                    d.point((xx+(dy!=0),yy+(dx!=0)),fill=second)
            else:
                color=("#f2c67d" if phase<3 else "#e0a363" if phase<6 else "#bd805d") if powered else "#94654f"
                d.point((xx,yy),fill=color)
    if big and len(active)>=3:
        d.rectangle((5,5,10,10),fill="#493557")
        d.rectangle((6,6,9,9),fill="#80658c")
        d.rectangle((6,7,9,8),fill="#433954")
        lit=powered and (t-DIST[p])%32<8
        d.point((7,7),fill="#d0e6a8" if lit else "#7ba39a" if powered else "#78717e")
        d.point((8,8),fill="#80c7ae" if lit else "#5e7986")
    else:
        d.point((7,7),fill="#a5d4a9" if big and powered else "#a77766" if not big else "#695477")
    return shape

def panel(t,powered):
    scene=floor_bg()
    for p in sorted(G):
        scene.alpha_composite(tile(p,t,powered),(16*p[0],16*p[1]))
    core=load(ROOT/"artwork/exports/computer-core.png")
    terminal=load(ROOT/"artwork/exports/network-terminal.png")
    scene.alpha_composite(core.crop((16 if powered else 0,0,32 if powered else 16,32)),(16,0))
    scene.alpha_composite(terminal.crop((16 if powered else 0,0,32 if powered else 16,32)),(160,0))
    d=ImageDraw.Draw(scene)
    # Demo node caps: keyhole silhouettes, not inventory items.
    for x,y in [(119,23),(87,87)]:
        d.rounded_rectangle((x-4,y-5,x+4,y+4),radius=1,fill="#493557")
        d.rectangle((x-3,y-4,x+3,y-1),fill="#90659b")
        d.point((x,y),fill="#b4e5b6" if powered else "#685b6b")
    return scene

def card(t):
    im=Image.new("RGBA",(1025,421),"#f5e4ca")
    d=ImageDraw.Draw(im)
    def txt(pos,s,k=16,c="#493349"):
        d.text(pos,s,font=ImageFont.load_default(size=k),fill=c)
    txt((23,17),"STARDEW ENERGISTICS  /  MARU TECH",14)
    txt((23,38),"Data bus",30)
    txt((23,79),"POWERED  /  PACKET LOOP",15)
    txt((617,79),"UNPOWERED  /  NO SIGNAL",15)
    im.alpha_composite(panel(t,True).resize((576,336),N).crop((0,0,576,281)),(23,105))
    im.alpha_composite(panel(t,False).resize((384,224),N),(617,105))
    # Integer-scale closeup of a heavy junction, staying on the original pixels.
    detail=tile((5,3),t,True).resize((64,64),N)
    d.rectangle((618,329,681,392),fill="#d1a474")
    im.alpha_composite(detail,(618,329))
    txt((703,340),"Three-way bus node",16)
    txt((703,365),"Paired teal data windows",14)
    txt((23,394),"Prototype. Pulse is a visual indicator, not a channel count. Not in-game.",14)
    return im

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    for p in G:
        assert tile(p,0).getchannel("A").tobytes()==tile(p,0,False).getchannel("A").tobytes()
    frames=[card(t) for t in range(0,32,2)]
    assert len({f.tobytes() for f in frames})==16
    save(card(12),OUT/"maru-data-bus-preview.png")
    dst=OUT/"maru-data-bus-loop.gif"
    frames[0].convert("RGB").save(dst,save_all=True,append_images=[f.convert("RGB") for f in frames[1:]],duration=100,loop=0,disposal=2,optimize=False)
    with Image.open(dst) as check:
        assert check.n_frames==16 and check.info["loop"]==0
        for i in range(check.n_frames):check.seek(i);assert check.info["duration"]==100
    print(f"{dst.name}: 16 frames, 1.6 seconds, loops indefinitely")
if __name__=="__main__":main()
