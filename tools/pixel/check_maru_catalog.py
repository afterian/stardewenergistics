"""Verify every native art sheet against the mod catalog, then make an overview and ZIP."""
from pathlib import Path
import argparse, hashlib, json, re, sys, zipfile
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(Path(__file__).resolve().parent))
from pixel_pipeline import cuts, load, report, save
OUT=ROOT/"artwork"/"exports"
PRE=ROOT/"artwork"/"review"/"maru-catalog"
T=lambda k:ImageFont.load_default(size=k)
N=Image.Resampling.NEAREST
def catalogue(source):
    out={}
    for line in source.splitlines():
        m=re.search(r'(Item|Placeable)\((\d+), "([^"]+)", (\d+)(?:, (\d+))?\)',line)
        if m:
            typ,num,name,count,_=m.groups()
            out[int(num)]=(name,16,16 if typ=="Item" else 32,int(count) if typ=="Placeable" else 1)
        m=re.search(r'new AssetSpec\((\d+), "([^"]+)", AssetKind\.\w+, (\d+), (\d+), (\d+),',line)
        if m:
            num,name,w,h,frames=m.groups()
            out[int(num)]=(name,int(w),int(h),int(frames))
    if not out or sorted(out)!=list(range(1,max(out)+1)):
        raise ValueError("Catalog IDs are incomplete")
    return out
def make_catalog_card(records):
    side=[(num,r) for num,r in records.items() if num!=34]
    w,h=906,950
    card=Image.new("RGBA",(w,h),"#f6e5c6");d=ImageDraw.Draw(card)
    def txt(pos,s,k=16):d.text(pos,s,fill="#4d354d",font=T(k))
    txt((22,13),"STARDEW ENERGISTICS  /  MARU TECH",15)
    txt((22,36),"The complete art set",29)
    txt((22,76),f"{len(records)} native exports  |  Representative frames shown at integer scale",15)
    for i,(num,r) in enumerate(side):
        x=22+(i%6)*146;y=110+(i//6)*91
        d.rounded_rectangle((x,y,x+133,y+81),radius=4,fill="#ebcfaa")
        txt((x+6,y+5),f"{num:02d}  "+r['name'].removesuffix(".png").replace("-"," ").title().replace("Network View Markers","Network Markers").replace("Advanced Crafting Rack","Adv. Crafting Rack"),11)
        frames=cuts(load(OUT/r["name"]),r["w"],r["h"],r["frames"],1)
        if num in (21,22,35,37,38):
            ii=[3,7,10,15]
            if num==35:
                for j,idx in enumerate(ii):
                    f=frames[idx].copy()
                    f.putdata([((a*157)//255,(b*119)//255,(c*180)//255,q) for a,b,c,q in f.get_flattened_data()])
                    card.alpha_composite(f.resize((32,32),N),(x+1+33*j,y+29))
            else:
                for j,idx in enumerate(ii):card.alpha_composite(frames[idx].resize((32,32),N),(x+1+33*j,y+29))
        else:
            selected=([frames[0],frames[-1]] if len(frames)>1 else frames)
            if num in (28,29,32):selected=frames[:4]
            for j,f in enumerate(selected):
                z=2 if max(f.size)<=32 and len(selected)<=2 else 1
                if f.width>100:continue
                goal=(f.width*z,f.height*z)
                xx=x+(j*134//len(selected))+(134//len(selected)-goal[0])//2
                yy=y+17+(64-goal[1])//2
                card.alpha_composite(f.resize(goal,N),(xx,yy))
    banner=load(OUT/records[34]["name"])
    txt((22,763),"34  Nexus Banner",16)
    card.alpha_composite(banner,(22,792))
    txt((22,924),"Artwork review only. Runtime hookup and Deck delivery are separate.",14)
    return card
def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument("catalog",type=Path);a=p.parse_args()
    expected=catalogue(a.catalog.read_text(encoding="utf-8-sig"))
    r={}
    for n,(f,w,h,k) in sorted(expected.items()):
        path=OUT/f
        with Image.open(path) as raw:
            q=report(raw.convert("RGBA"))
            assert raw.mode=="RGBA" and "icc_profile" not in raw.info,(f,"format")
        assert q["size"]==[w*k,h] and q["alpha"]==[0,255] and q["pure_black"]==0,(f,q["size"],q["alpha"],q["pure_black"])
        frames=cuts(load(path),w,h,k,1)
        assert all(t.getbbox() is not None for t in frames),(f,"empty frame")
        if n not in (37,38):assert len({t.tobytes() for t in frames})==k,(f,"duplicate frame")
        r[n]=dict(name=f,w=w,h=h,frames=k,sha256=hashlib.sha256(path.read_bytes()).hexdigest())
    png={f.name for f in OUT.glob("*.png")}
    assert png=={e["name"] for e in r.values()}, png ^ {e["name"] for e in r.values()}
    PRE.mkdir(parents=True,exist_ok=True)
    (PRE/"maru-art-qa.json").write_text(json.dumps(r,indent=2)+"\n",encoding="utf-8")
    save(make_catalog_card(r),PRE/"maru-complete-catalog.png")
    target=PRE/"maru-native-art-pack.zip"
    with zipfile.ZipFile(target,"w",zipfile.ZIP_DEFLATED) as z:
        for e in r.values():z.write(OUT/e["name"],e["name"])
    with zipfile.ZipFile(target) as z:
        assert z.testzip() is None and len(z.namelist())==len(r)
        assert all(hashlib.sha256(z.read(e["name"])).hexdigest()==e["sha256"] for e in r.values())
    print(f"Validated and packaged {len(r)} native assets.")
if __name__=="__main__":main()
