"""Assemble real exported cable tiles like WireRenderer; schematic, not game capture."""
from pathlib import Path
from collections import deque
import sys
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save, load
DIRECTIONS=((1,0,-1),(2,1,0),(4,0,1),(8,-1,0))
TINT=(157,119,180)
PATH={(0,0):"cu",(1,0):"cu",(2,0):"heavy",(3,0):"heavy",
      (4,0):"heavy",(4,1):"heavy",(4,2):"heavy",(5,2):"cu",(6,2):"cu",
      (3,1):"heavy",(3,2):"heavy",(2,2):"cu"}
ASSETS=("wire-tiles.png","wire-tiles-glow.png","heavy-cable-tiles.png",
        "copper-data-signal.png","heavy-data-signal.png")

def distances():
    dist={(0,0):0};q=deque(dist)
    while q:
        x,y=q.popleft()
        for _,dx,dy in DIRECTIONS:
            p=(x+dx,y+dy)
            if p in PATH and p not in dist:
                dist[p]=dist[(x,y)]+1;q.append(p)
    assert set(dist)==set(PATH)
    return dist
DIST=distances()

def mask(x,y):
    return sum(b for b,dx,dy in DIRECTIONS if (x+dx,y+dy) in PATH)

def chop(im,i):
    return im.crop((i*16,0,i*16+16,16))

def tint(im):
    out=im.copy()
    out.putdata([tuple(c*t//255 for c,t in zip(p[:3],TINT))+(p[3],)
                 for p in im.get_flattened_data()])
    return out

def placed(atlases,p,power,inspect=False,phase=None):
    base,view,heavy,cusig,heavysig=atlases
    m=mask(*p)
    thick=PATH[p]=="heavy"
    f=tint(chop(heavy,m)) if thick else chop(view if power and inspect else base,m)
    if power:
        idx=(7 if phase is None else (phase-DIST[p])&7)*16+m
        f.alpha_composite(chop(heavysig if thick else cusig,idx))
    return f

def check(atlases):
    assert [im.size for im in atlases]==[(256,16)]*3+[(2048,16)]*2
    for im in atlases:
        assert set(im.getchannel("A").get_flattened_data())=={0,255}
    for phase,im in enumerate(atlases[3:]):
        shade=atlases[phase*2]
        for i in range(128):
            trial=chop(im,i).getchannel("A")
            dest=chop(shade,i%16).getchannel("A")
            assert len([a for a,d in zip(trial.get_flattened_data(),dest.get_flattened_data()) if a>d])==0
    assert len({placed(atlases,(3,0),True,phase=p).tobytes() for p in range(8)})>=4
    checked=0
    for (x,y),kind in PATH.items():
        for b,dx,dy in DIRECTIONS[1:3]:
            other=(x+dx,y+dy)
            if other not in PATH:continue
            a,k=placed(atlases,(x,y),False),placed(atlases,other,False)
            ap=[a.getpixel((15,j) if dx else (j,15))[3] for j in range(16)]
            bp=[k.getpixel((0,j) if dx else (j,0))[3] for j in range(16)]
            assert sum(bool(a*b) for a,b in zip(ap,bp))==(6 if kind==PATH[other]=="heavy" else 3)
            checked+=1
    return checked

def small_scene(atlases,power,inspect=False,phase=None):
    room=Image.new("RGBA",(120,56),"#d59b65")
    d=ImageDraw.Draw(room)
    for y in range(0,56,8):
        d.line((0,y,119,y),fill="#b37958")
        d.line((0,y+1,119,y+1),fill="#e2ad73")
        for x in range((y//8*11)%24,120,24):
            d.line((x,y+2,x,y+7),fill="#bd875e")
    for x,y in PATH: room.alpha_composite(placed(atlases,(x,y),power,inspect,phase),(4+x*16,4+y*16))
    return room

def main():
    atlases=[load(ROOT/"artwork/exports"/name) for name in ASSETS]
    seams=check(atlases)
    card=Image.new("RGBA",(1080,519),"#f6e5c6");d=ImageDraw.Draw(card)
    N=Image.Resampling.NEAREST
    def txt(x,y,s,k):d.text((x,y),s,font=ImageFont.load_default(size=k),fill="#4d354c")
    txt(22,16,"STARDEW ENERGISTICS   /   MARU TECH",15)
    txt(22,42,"21, 22, 35  Data bus in production",31)
    txt(22,84,"Actual copper, jacket and independent signal atlases, assembled at integer scale.",16)
    demos=[(24,125,"OFF  /  NO POWER",False,False,None),
           (552,125,"POWER ON  /  NORMAL VIEW",True,False,None),
           (24,319,"RUNNING  /  PHASE SNAPSHOT A",True,False,3),
           (552,319,"RUNNING  /  NETWORK VIEW, SNAPSHOT B",True,True,5)]
    for x,y,lab,power,inspect,phase in demos:
        txt(x,y,lab,15)
        card.alpha_composite(small_scene(atlases,power,inspect,phase).resize((360,168),N),(x,y+23))
        tile=placed(atlases,(3,0),power,inspect,phase)
        d.rectangle((x+371,y+45,x+481,y+155),fill="#caa37e")
        card.alpha_composite(tile.resize((96,96),N),(x+378,y+52))
        txt(x+378,y+161,"Junction 6x",13)
    # Final text only, no assertion of a runtime capture.
    out=ROOT/"artwork/review/maru-data-bus/21-22-35-data-bus-production.png"
    save(card,out)
    assert load(out).size==(1080,519)
    print(f"Read five exact exported atlases; checked {seams} assembled seams, 256 signal footprints, and sampled spread of animated phases.")
if __name__=="__main__":
    main()