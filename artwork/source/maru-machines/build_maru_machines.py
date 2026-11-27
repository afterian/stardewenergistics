"""Original pixel masters for the Maru placeables, with reproducible PNG export."""
import json, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import rgb, save, snap, grade, cuts, pack, report

HERE=Path(__file__).parent
REVIEW=ROOT/"artwork/review/maru-machines"
EXPORT=ROOT/"artwork/exports"
P={k:(*rgb(v),255) for k,v in json.loads((HERE/"maru-palette.json").read_text())["colors"].items()}
N=Image.Resampling.NEAREST

def paint(build):
    im=Image.new("RGBA",(16,32))
    d=ImageDraw.Draw(im)
    def r(x1,y1,x2,y2,c):d.rectangle((x1,y1,x2,y2),fill=P[c])
    def p(points,c):d.polygon(points,fill=P[c])
    build(r,p)
    return im

def core(state):
    def build(r,p):
        p([(4,3),(11,3),(13,5),(14,7),(14,29),(13,30),(10,30),(10,29),(5,29),(5,30),(2,30),(1,29),(1,7),(2,5)],"outline")
        p([(4,4),(11,4),(13,6),(13,25),(12,26),(2,26),(2,7)],"case")
        p([(4,4),(11,4),(13,6),(3,6)],"lit")
        r(4,4,8,4,"glint");r(2,8,2,24,"lit")
        r(12,7,13,25,"edge");r(2,25,12,26,"edge")
        # CRT surround and glass
        p([(4,8),(10,8),(11,9),(11,16),(10,17),(4,17),(3,16),(3,9)],"outline")
        p([(4,9),(10,9),(10,16),(4,16)],["screenOff","screenOn","screenOn","low"][state])
        r(4,9,8,9,"screenReflection" if state==0 else "dimscan")
        r(5,17,10,17,"lit")
        if state:
            scan="dimscan" if state==3 else "scan"
            r(4,13,5,13,scan);r(6,12,6,13,scan);r(7,14,7,14,scan);r(8,13,10,13,scan)
            if state in (1,2):r(9,10,9,10,"lime")
        else:
            r(4,10,5,10,"screenReflection");r(4,11,4,11,"screenReflection")
        # Vents, status and a square power button
        r(4,20,8,22,"edge");r(4,20,8,20,"outline");r(4,22,8,22,"outline")
        r(4,23,8,23,"lit")
        r(10,20,11,21,"outline");r(10,20,10,20,"blue")
        r(10,23,10,23,["dormant","teal","lime","warning"][state])
        r(12,23,12,23,["dormant","teal","dormant","dormant"][state])
        # Thin oak plinth, two feet, one central wire socket
        r(2,27,13,28,"darkWood");r(2,27,12,27,"oak");r(2,27,6,27,"oakLit")
        r(2,28,4,29,"amberWood");r(11,28,13,29,"amberWood");r(2,28,3,28,"oak")
        r(7,27,8,28,"outline");r(7,28,7,28,"metal")
    return paint(build)

def terminal(state):
    def build(r,p):
        # The console silhouette is shorter and wider than the tower.
        p([(4,8),(11,8),(13,10),(13,19),(15,22),(15,25),(14,26),(14,29),(13,30),(11,30),(11,29),(4,29),(4,30),(2,30),(1,29),(1,26),(0,25),(0,22),(2,19),(2,10)],"outline")
        p([(4,9),(11,9),(12,10),(12,19),(3,19),(3,10)],"case")
        r(4,9,10,9,"lit");r(4,9,7,9,"glint");r(12,11,12,19,"edge")
        r(4,11,11,17,"outline");r(5,12,10,16,"screenOn" if state else "screenOff")
        if state:
            for x,y,c in [(5,12,"lime"),(7,12,"scan"),(9,12,"teal"),(5,14,"scan"),(7,14,"teal"),(9,14,"lime")]:
                r(x,y,x,y,c)
            r(5,16,9,16,"dimscan")
        else:
            r(5,12,7,12,"screenReflection");r(5,13,5,13,"screenReflection")
        r(5,18,10,18,"edge");r(11,18,11,18,"teal" if state else "dormant")
        # Sloping keyboard deck, visible keys, oak console apron.
        p([(3,20),(12,20),(14,22),(14,23),(1,23),(1,22)],"lit")
        r(3,20,11,20,"glint");r(3,21,11,22,"edge")
        for x in (3,5,7,9,11):r(x,21,x,21,"key")
        r(5,22,9,22,"metal");r(12,22,12,22,"teal" if state else "blue")
        r(1,24,14,24,"oakLit");r(1,25,14,26,"amberWood")
        r(2,25,12,25,"oak");r(3,26,12,26,"darkWood")
        r(2,26,3,29,"oak");r(12,26,13,29,"amberWood")
        r(7,26,8,27,"outline");r(7,26,7,26,"metal")
    return paint(build)

def compose_review(groups):
    out=Image.new("RGBA",(940,690),"#f6e5c6")
    d=ImageDraw.Draw(out)
    font=lambda s:ImageFont.load_default(size=s)
    def title(x,y,txt,size=18): d.text((x,y),txt,fill="#4c3247",font=font(size))
    title(26,20,"STARDEW ENERGISTICS",17);title(26,43,"Maru Tech  /  Computer Core + Network Terminal",29)
    crop=Image.open(ROOT/"artwork/benchmark/farm-reference.jpg").convert("RGBA").crop((996,152,1536,458))
    origin=(26,96);out.alpha_composite(crop,origin)
    for sprite,x,y in [(groups[0][1],125,165),(groups[1][1],208,165)]:
        out.alpha_composite(sprite.resize((64,128),N),(origin[0]+x,origin[1]+y))
    title(589,114,"FARM MOCKUP",17)
    title(589,147,"Native sprites, 4x nearest-neighbor.",15)
    title(589,172,"Placed over the reference image.",15)
    title(589,197,"Not a live game capture.",15)
    title(589,244,"Palette: amethyst, oak, CRT green.",15)
    title(589,270,"Plum outlines replace pure black.",15)
    for i,img in enumerate([groups[0][1],groups[1][1]]):
        x=587+i*109;d.rectangle((x,303,x+91,374),fill="#cfb18f")
        out.alpha_composite(img,(x+38,317));title(x+9,351,("Core" if i==0 else "Terminal")+"  @1x",13)
    title(26,422,"COMPUTER CORE",17);title(617,422,"NETWORK TERMINAL",17)
    for i,(sprite,t) in enumerate(zip(groups[0]+groups[1],["Off","On","LED blink","Low power","Off","On"])):
        x=26+i*148;d.rectangle((x,454,x+134,637),fill="#d7bb98" if i in (1,5) else "#e4cba8")
        tile=sprite.resize((80,160),N);out.alpha_composite(tile,(x+27,462))
        title(x+9,648,t,17)
    return out

def main():
    groups=[[core(i) for i in range(4)],[terminal(i) for i in range(2)]]
    sheet=pack(groups[0]+groups[1],3)
    # A registered integer-grid master is the safe equivalent of an oversized batch sheet.
    save(sheet.resize((sheet.width*6,sheet.height*6),N),HERE/"maru-machines-master-6x.png")
    back=snap(sheet.resize((sheet.width*6,sheet.height*6),N),48,64,step=6)
    assert back.tobytes()==sheet.tobytes()
    frames=cuts(back,16,32,3,2)
    names=["computer-core","network-terminal"]
    assert all((not (EXPORT/(name+".png")).exists()) or (len(sys.argv)>1 and sys.argv[1]=="--replace") for name in names), "Existing exports. Use --replace only for intentional rebuild."
    data={}
    for name,source_frames in zip(names,[frames[:4],frames[4:]]):
        final=[grade(im,[p[:3] for p in P.values()],floor=.30) for im in source_frames]
        assert len({im.getchannel("A").tobytes() for im in final})==1,"State silhouette moved"
        assert len({im.tobytes() for im in final})==len(final),"Duplicate state"
        strip=pack(final)
        info=report(strip)
        assert strip.size==(16*len(final),32) and info["alpha"]==[0,255] and info["pure_black"]==0 and info["min_value"]>=.30
        save(strip,EXPORT/(name+".png"))
        data[name]=info
        groups[names.index(name)]=final
    save(compose_review(groups),REVIEW/"maru-core-and-terminal.png")
    (REVIEW/"asset-checks.json").write_text(json.dumps(data,indent=2)+"\n",encoding="utf-8")
    print({name:(r["size"],r["colors"],r["min_value"]) for name,r in data.items()})
if __name__=="__main__":main()
