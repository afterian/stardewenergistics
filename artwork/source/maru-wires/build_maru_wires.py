"""Build the three native Maru wire sheets and an exact-pixel review card."""
from pathlib import Path
import json, sys
from PIL import Image, ImageDraw, ImageFont, ImageChops
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save, load, pack, cuts, report
HERE=Path(__file__).parent
REVIEW=ROOT/"artwork/review/maru-wires"
OUT=ROOT/"artwork/exports"
N=Image.Resampling.NEAREST
BITS=(1,2,4,8)
DELTA=((0,-1),(1,0),(0,1),(-1,0))
PROFILES={
 "wire-tiles.png":dict(ramp=["#663945","#cf824c","#874744"], staple=["#826679","#d0ad93","#896e76"]),
 "wire-tiles-glow.png":dict(ramp=["#3e605d","#9ad1bc","#5e9990"], staple=["#496b65","#c5e4bc","#649486"]),
 "heavy-cable-tiles.png":dict(ramp=["#686868","#bdbdbd","#d5d5d5","#b0b0b0","#999999","#686868"], staple=["#818181","#d1d1d1","#e2e2e2","#c7c7c7","#a7a7a7","#787878"]),
}

def wire(mask,ramp,staple):
    n=len(ramp)
    a,b=(6,8) if n==3 else (5,10)
    im=Image.new("RGBA",(16,16));d=ImageDraw.Draw(im)
    if not mask:
        q=1 if n==3 else 0
        d.rounded_rectangle((a-1, a-q, b+1, b+q),radius=1,fill=ramp[0])
        d.line((a,7,b,7),fill=staple[1])
        d.line((a+1,8,b-1,8),fill=ramp[1])
        return im
    # Arms join the same center square on all 16 masks. Edges are never decorated.
    for bit,box in zip(BITS,[(a,0,b,b),(a,a,15,b),(a,a,b,15),(0,a,b,b)]):
        if mask&bit:d.rectangle(box,fill=ramp[0])
    for bit in BITS:
        if not mask&bit: continue
        for i,c in enumerate(ramp):
            j=a+i
            if bit==1:d.line((j,0,j,a-1),fill=c)
            if bit==2:d.line((b+1,j,15,j),fill=c)
            if bit==4:d.line((j,b+1,j,15),fill=c)
            if bit==8:d.line((0,j,a-1,j),fill=c)
    for y in range(a,b+1):
        for x in range(a,b+1):
            if (x==a and not mask&8) or (x==b and not mask&2) or (y==a and not mask&1) or (y==b and not mask&4):
                continue
            idx=(y-a) if mask&10 else (x-a)
            d.point((x,y),fill=ramp[idx])
    # Thin staples and heavier ferrules stay away from the tile boundary.
    if mask & 2:
        start=12; d.line((start,a,start,b),fill=staple[0])
        for i,c in enumerate(staple):d.point((start+1,a+i),fill=c)
    elif mask & 4:
        start=12; d.line((a,start,b,start),fill=staple[0])
        for i,c in enumerate(staple):d.point((a+i,start+1),fill=c)
    elif mask & 8:
        start=3; d.line((start+1,a,start+1,b),fill=staple[0])
        for i,c in enumerate(staple):d.point((start,a+i),fill=c)
    else:
        start=3; d.line((a,start+1,b,start+1),fill=staple[0])
        for i,c in enumerate(staple):d.point((a+i,start),fill=c)
    return im

def edge(im,i):
    points=[[(x,0) for x in range(16)],[(15,y) for y in range(16)],
            [(x,15) for x in range(16)],[(0,y) for y in range(16)]]
    return tuple(im.getpixel(p) for p in points[i])

def verify(atlases):
    total=0
    for key,frames in atlases.items():
        stroke=6 if key.startswith("heavy") else 3
        assert len(set(f.tobytes() for f in frames))==16
        for mask,f in enumerate(frames):
            coords={(x,y) for y in range(16) for x in range(16) if f.getpixel((x,y))[3]}
            assert coords
            stack=[next(iter(coords))]; seen=set(stack)
            while stack:
                x,y=stack.pop()
                for dx,dy in DELTA:
                    q=(x+dx,y+dy)
                    if q in coords and q not in seen: seen.add(q);stack.append(q)
            assert seen==coords,(key,mask,"disconnected pixels")
            for i,bit in enumerate(BITS):
                e=edge(f,i)
                assert tuple(j for j,p in enumerate(e) if p[3])==(tuple(range(5,11) if stroke==6 else range(6,9)) if mask&bit else ())
                for m,other in enumerate(frames):
                    if mask&bit and m&BITS[(i+2)%4]:
                        assert e==edge(other,(i+2)%4),(key,mask,m,i,"seam")
                        total+=1
            if stroke==6:
                assert all(p[0]==p[1]==p[2] for p in f.get_flattened_data() if p[3])
    for tile,glow in zip(atlases["wire-tiles.png"],atlases["wire-tiles-glow.png"]):
        assert tile.getchannel("A").tobytes()==glow.getchannel("A").tobytes()
    for c in atlases["wire-tiles.png"]:
        for h in atlases["heavy-cable-tiles.png"]:
            for i in range(4):
                ca={j for j,p in enumerate(edge(c,i)) if p[3]}
                ha={j for j,p in enumerate(edge(h,(i+2)%4)) if p[3]}
                if ca and ha: assert ca < ha
    return total

def tint(im,color):
    alpha=im.getchannel("A")
    out=ImageChops.multiply(im.convert("RGB"),Image.new("RGB",im.size,color)).convert("RGBA")
    out.putalpha(alpha)
    return out

def review(atlases, report_count):
    im=Image.new("RGBA",(1120,716),"#f7e6c7")
    dr=ImageDraw.Draw(im)
    def txt(x,y,s,k=17):dr.text((x,y),s,font=ImageFont.load_default(size=k),fill="#4b3444")
    txt(26,18,"STARDEW ENERGISTICS   /   MARU TECH",16)
    txt(26,43,"Floor wiring",31)
    txt(26,89,"All 16 connection masks in each native strip",17)
    for i in range(16):txt(193+i*56,127,str(i),13)
    labels=["Copper Wire","Network View","Heavy Cable"]
    for row,(name,frames) in enumerate(atlases.items()):
        y=151+row*68
        txt(26,y+13,labels[row],16)
        for i,f in enumerate(frames):
            x=176+i*56
            dr.rectangle((x,y,x+49,y+49),fill=["#dec29b","#d8bd91","#d6bf9b"][row])
            im.alpha_composite(f.resize((48,48),N),(x+1,y+1))
    geo={(0,2),(1,2),(2,2),(3,2),(4,2),(5,2),(6,2),(2,1),(2,0),(2,3),(5,1),(6,1)}
    # Test fixture: a tee and a cross on copper, elbow and paint on cable,
    # and one narrow-to-wide joint at x=3/4.
    farm=load(ROOT/"artwork/benchmark/farm-reference.jpg")
    for n,x in enumerate((26,589)):
        txt(x,369,["FARM DIRT  /  NORMAL VIEW","OAK FLOOR  /  NETWORK VIEW"][n],17)
        if n==0:
            surf=farm.crop((1000,270,1512,526))
        else:
            # A schematic oak floor sampled from the room's warm material ramp.
            oak=Image.new("RGBA",(128,64),"#c68a50")
            od=ImageDraw.Draw(oak)
            for row in range(8):
                y=row*8
                od.line((0,y,127,y),fill="#a9653a")
                od.line((0,y+1,127,y+1),fill="#dfa663")
                for j in range(5):
                    xx=(j*31+row*11)%128
                    od.line((xx,y+1,xx,y+7),fill="#ac713f")
                    if xx+9<128: od.line((xx+4,y+4,xx+9,y+4),fill="#d79a56")
            surf=oak.resize((512,256),N)
        for tx,ty in sorted(geo):
            mask=sum(bit for bit,(dx,dy) in zip(BITS,DELTA) if (tx+dx,ty+dy) in geo)
            if tx>=4:
                f=atlases["heavy-cable-tiles.png"][mask]
                if n:f=tint(f,"#97bbdf")
            else:f=atlases[("wire-tiles-glow.png" if n else "wire-tiles.png")][mask]
            surf.alpha_composite(f.resize((64,64),N),((tx*64)+32,ty*64))
        im.alpha_composite(surf,(x,404))
    txt(26,675,f"Exact exported pixels at 3x / 4x. {report_count:,} same-family seam checks. Mockup, not a game capture.",16)
    return im

def main():
    atlases={k:[wire(i,**v) for i in range(16)] for k,v in PROFILES.items()}
    n=verify(atlases)
    for k,frames in atlases.items():
        im=pack(frames)
        q=report(im)
        assert im.size==(256,16) and q["alpha"]==[0,255] and not q["pure_black"]
        assert all(a.tobytes()==b.tobytes() for a,b in zip(cuts(im,16,16,16,1),frames))
        if (OUT/k).exists() and "--replace" not in sys.argv:raise FileExistsError(OUT/k)
    for k,frames in atlases.items():save(pack(frames),OUT/k)
    save(review(atlases,n),REVIEW/"maru-wires-review.png")
    (REVIEW/"wire-checks.json").write_text(json.dumps({"same_family_rgba_seam_checks":n,"edge_registration":True,"mixed_width_alignment":True,"glow_alpha_matches":True,"heavy_cable_is_neutral_grayscale":True,"frames_per_strip":16,"width":256,"height":16},indent=2)+"\n",encoding="utf-8")
    print(f"{n} exact same-family seam checks passed. Three 256x16 RGBA sheets written.")
if __name__=="__main__":main()
