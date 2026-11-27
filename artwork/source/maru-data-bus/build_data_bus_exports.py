"""Render native Maru data-cable strips: 16 mask columns, 8 signal phases."""
from pathlib import Path
import sys
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save, pack, report
OUT=ROOT/"artwork/exports"
BITS=(1,2,4,8)
DIR=((0,-1),(1,0),(0,1),(-1,0))

def base(mask,heavy,inspect=False):
    im=Image.new("RGBA",(16,16));d=ImageDraw.Draw(im)
    a,b=(5,10) if heavy else (6,8)
    outside,inside,trace=("#777777","#b9b9b9","#929292") if heavy else (("#493657","#83668a","#719a90") if inspect else ("#694151","#8b534f","#a7755f"))
    if mask==0:
        d.rounded_rectangle((a-1,a,b+1,b),1,fill=outside)
        d.line((a,7,b,7),fill=trace)
        return im
    for bit,outer,inner in zip(BITS,
        [(a,0,b,8),(7,a,15,b),(a,7,b,15),(0,a,8,b)],
        [(a+1,0,b-1,8),(7,a+1,15,b-1),(a+1,7,b-1,15),(0,a+1,8,b-1)]):
        if mask&bit:d.rectangle(outer,fill=outside)
    for bit,inner in zip(BITS,[(a+1,0,b-1,8),(7,a+1,15,b-1),(a+1,7,b-1,15),(0,a+1,8,b-1)]):
        if mask&bit:d.rectangle(inner,fill=inside)
    for bit,(dx,dy) in zip(BITS,DIR):
        if not mask&bit:continue
        for k in range(9):
            x,y=7+k*dx,7+k*dy
            if not(0<=x<16 and 0<=y<16):continue
            if heavy and k in (2,3,5,6):
                d.point((x,y),fill=trace);d.point((x+(dy!=0),y+(dx!=0)),fill=trace)
            elif not heavy:d.point((x,y),fill=trace)
    if heavy and mask.bit_count()>=3:
        d.rectangle((5,5,10,10),fill=outside)
        d.rectangle((6,6,9,9),fill=inside)
        d.rectangle((6,7,9,8),fill=trace)
    return im

def signal(mask,heavy,phase):
    im=Image.new("RGBA",(16,16));d=ImageDraw.Draw(im)
    if mask==0:
        d.point((7,7),fill="#6c938d" if heavy else "#d49865")
        return im
    for bit,(dx,dy) in zip(BITS,DIR):
        if not mask&bit:continue
        for k in ((2,3,5,6) if heavy else tuple(range(1,8))):
            x,y=7+dx*k,7+dy*k
            dt=(phase-k//2)%7
            lit=phase!=7 and dt==0
            trail=phase!=7 and dt==1
            c=(("#b4e5b6" if lit else "#80c7ae" if trail else "#597783") if heavy
                else ("#f3d091" if lit else "#de9b65" if trail else "#bb825f"))
            d.point((x,y),fill=c)
            if heavy:d.point((x+(dy!=0),y+(dx!=0)),fill="#689b97" if lit or trail else "#4b6373")
    if heavy and mask.bit_count()>=3:
        d.rectangle((6,7,9,8),fill="#433954")
        d.point((7,7),fill="#d0e6a8" if phase==0 else "#7ba39a")
        d.point((8,8),fill="#80c7ae" if phase==0 else "#5e7986")
    return im

def edges(im,d):
    axis=((lambda k:(k,0)),(lambda k:(15,k)),(lambda k:(k,15)),(lambda k:(0,k)))[d]
    return [im.getpixel(axis(k)) for k in range(16)]

def main():
    for name,h,i in [("wire-tiles.png",False,False),("wire-tiles-glow.png",False,True),("heavy-cable-tiles.png",True,False)]:
        frames=[base(m,h,i) for m in range(16)]
        for m,t in enumerate(frames):
            for d,bit in enumerate(BITS):
                e=edges(t,d)
                if m&bit:
                    assert sum(c[3]>0 for c in e)==(6 if h else 3)
                    for n,q in enumerate(frames):
                        if n&BITS[(d+2)%4]:assert e==edges(q,(d+2)%4)
                else:assert all(c[3]==0 for c in e)
        save(pack(frames),OUT/name)
    for h,n in [(False,"copper-data-signal.png"),(True,"heavy-data-signal.png")]:
        frames=[signal(m,h,p) for p in range(8) for m in range(16)]
        for idx,t in enumerate(frames):
            mask=idx%16
            b=base(mask,h)
            assert all(not t.getpixel((x,y))[3] or b.getpixel((x,y))[3] for x in range(16) for y in range(16))
        s=pack(frames);assert s.size==(2048,16) and report(s)["alpha"]==[0,255]
        save(s,OUT/n)
    print("Base RGBA seams, signal containment and native dimensions verified.")
if __name__=="__main__":main()
