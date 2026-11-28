"""Maru #15/16. Fixed Core chassis with hand-posed single and twin crafting arms."""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont
from build_maru_machines import ROOT, EXPORT, P, N, core, pack, report, save

INK="#513768"
GLASS="#4a475e"
WALL="#55506c"
STEEL="#a38fb2"
STEEL_SHADOW="#765f8a"
VIOLET="#9f7db1"
VIOLET_EDGE="#735589"
VIOLET_FOCUS="#c6a8cc"
BRASS="#d3a168"
BRASS_DARK="#a97267"
PLATFORM="#716884"
RUBBER="#524a67"
COOL="#83aaa8"

def rack(advanced, state):
    im=core(0)
    d=ImageDraw.Draw(im)
    r=lambda a,b,c,e,f:d.rectangle((a,b,c,e),fill=f)
    dot=lambda x,y,c:d.point((x,y),fill=c)
    # Replace the CRT and control strip only. All pixels outside the face
    # stay byte-identical to the Core, including the outer case and plinth.
    r(3,8,11,24,P["case"])
    r(3,8,11,21,INK)
    r(4,9,10,20,GLASS)
    r(4,9,10,9,WALL)
    r(4,19,10,19,PLATFORM)
    r(4,20,10,20,RUBBER)
    # Glass corners, kept away from the readable tooling.
    dot(4,10,P["screenReflection"]);dot(10,18,WALL)
    if advanced:
        r(3,8,11,8,VIOLET)
        dot(3,9,VIOLET_EDGE);dot(11,9,VIOLET_EDGE)
        r(3,21,11,21,VIOLET_EDGE)
        if state:
            dot(4,9,VIOLET_FOCUS);dot(10,20,VIOLET)
    else:
        r(4,21,9,21,P["lit"])
    # Gold carrier tile travels across the bed during motion. The detector
    # on the apron shows activity without relying on color alone.
    px=(7,8,6)[state] if advanced else (7,8,9)[state]
    r(px-1,18,px+1,18,BRASS_DARK)
    dot(px,17,BRASS if state else "#987b6d")
    if advanced:
        path_a=(((5,10),(6,12),(6,14)),
                ((5,10),(5,13),(7,15)),
                ((5,10),(7,11),(8,14)))[state]
        path_b=(((10,12),(9,14),(9,16)),
                ((10,12),(10,15),(9,17)),
                ((10,12),(9,14),(7,16)))[state]
        # Independent twin tools: pale left shoulder, lavender right shoulder.
        arm(d,path_a,STEEL,STEEL_SHADOW,BRASS)
        arm(d,path_b,VIOLET,VIOLET_EDGE,VIOLET_FOCUS)
    else:
        path=(((5,10),(5,13),(7,14)),
              ((5,10),(7,12),(8,16)),
              ((5,10),(6,14),(9,16)))[state]
        arm(d,path,STEEL,STEEL_SHADOW,BRASS)
    r(4,22,7,23,INK)
    r(4,22,7,22,P["edge"])
    for x in ((9,10) if advanced else (10,)):
        dot(x,23,(COOL if advanced else P["teal"]) if state else P["dormant"])
    if state==2:dot(9 if advanced else 8,22,P["warning"])
    return im

def arm(d,path,beam,shade,tip):
    (a,b),(c,e),(f,g)=path
    # One shadow pixel below the links, bounded by the glass opening.
    d.line([(a,b+1),(c,e+1),(f,g+1)],fill=shade)
    d.line(path,fill=beam)
    d.line((a-1,b,a+1,b),fill=beam)
    d.point((c,e),fill=tip)
    d.point((f,g+1),fill=tip)

def card(frames,number,name,description):
    board=Image.new("RGBA",(720,362),"#f6e5c6")
    d=ImageDraw.Draw(board)
    def txt(x,y,s,size):
        d.text((x,y),s,font=ImageFont.load_default(size=size),fill="#4d354c")
    txt(20,15,"STARDEW ENERGISTICS   /   MARU TECH",14)
    txt(20,37,f"{number:02d}  {name}",27)
    txt(20,76,description,15)
    for i,label in enumerate(("Idle","Working 1","Working 2")):
        x=20+i*168
        d.rectangle((x,110,x+139,305),fill="#d8b78f")
        board.alpha_composite(frames[i].resize((96,192),N),(x+22,112))
        txt(x,316,label,15)
    x=548
    d.rectangle((x,110,x+100,171),fill="#caa37e")
    board.alpha_composite(core(0),(x+27,125))
    board.alpha_composite(frames[1],(x+43,125))
    txt(x,181,"Native 1x pair",14)
    txt(x,214,"Core + rack",14)
    txt(x,269,"16 x 32 each",14)
    txt(x,290,"3-state strip",14)
    return board

def main():
    source=core(0)
    options=((15,"crafting-rack","Crafting Rack","A compact single arm steps from parked to pickup to placement."),
             (16,"advanced-crafting-rack","Advanced Crafting Rack","Twin tooling, iridium trim, and a muted violet worklight."))
    targets=[EXPORT/f"{slug}.png" for _,slug,_,_ in options]
    previews=[ROOT/"artwork/review/maru-machines"/f"{num:02d}-{slug}.png" for num,slug,_,_ in options]
    if "--replace" not in sys.argv and any(p.exists() for p in targets+previews):
        raise FileExistsError("An existing rack export or review needs explicit --replace")
    for i,(num,slug,name,desc) in enumerate(options):
        frames=[rack(bool(i),state) for state in range(3)]
        for f in frames:
            assert f.size==(16,32) and f.getchannel("A").tobytes()==source.getchannel("A").tobytes()
            assert all(f.getpixel((x,y))==source.getpixel((x,y))
                       for y in range(32) for x in range(16) if not (3<=x<=11 and 8<=y<=24))
        assert len({f.tobytes() for f in frames})==3
        strip=pack(frames)
        rr=report(strip)
        assert rr["size"]==[48,32] and rr["alpha"]==[0,255] and not rr["pure_black"]
        save(strip,targets[i])
        save(card(frames,num,name,desc),previews[i])
        print(f"{slug}: {rr['size']}, palette {rr['colors']}, exact Core silhouette + oak feet")
if __name__=="__main__":
    main()