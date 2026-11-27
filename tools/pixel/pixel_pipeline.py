"""Native pixel art processing. Pillow only. Run with python -B."""
import argparse, colorsys, json, math
from collections import Counter
from pathlib import Path
from PIL import Image

def load(path):
    with Image.open(path) as im:
        return im.convert("RGBA")

def save(im, dest):
    dest=Path(dest)
    dest.parent.mkdir(parents=True, exist_ok=True)
    # Rebuild without source metadata such as ICC profile.
    clean=Image.frombytes("RGBA", im.size, im.convert("RGBA").tobytes())
    clean.save(dest)

def rgb(value):
    return tuple(bytes.fromhex(value.removeprefix("#")))

def hsv(p):
    return colorsys.rgb_to_hsv(*(c/255 for c in p[:3]))

def snap(im,w,h,ox=0,oy=0,step=1):
    """Sample the center of each registered native pixel block."""
    if min(w,h,step)<1 or min(ox,oy)<0 or ox+w*step>im.width or oy+h*step>im.height:
        raise ValueError("Grid exceeds source bounds")
    out=Image.new("RGBA",(w,h))
    out.putdata([im.getpixel((ox+x*step+step//2,oy+y*step+step//2)) for y in range(h) for x in range(w)])
    return out

def grade(im,palette,floor=0.22,sat=1.0,val=1.0,alpha=128):
    """Clean alpha. Lift low values, then match a hand-selected HSV palette."""
    pp=[(p,hsv(p)) for p in palette]
    if not pp or not 0<=floor<=1 or min(sat,val)<0 or not 1<=alpha<=255:
        raise ValueError("Invalid grading settings")
    memo={}
    out=[]
    for p in im.get_flattened_data():
        if p[3]<alpha:
            out.append((0,0,0,0)); continue
        if p not in memo:
            h,s,v=hsv(p)
            s=min(1,s*sat)
            v=min(1,max(floor,v*val))
            def dist(pair):
                _,(h2,s2,v2)=pair
                dh=min(abs(h-h2),1-abs(h-h2))
                return (2*dh)**2*min(s,s2)**2 + .30*(s-s2)**2 + 2*(v-v2)**2
            eligible=[q for q in pp if q[1][2]+1e-9>=floor]
            if not eligible: raise ValueError("No palette colors at or above shadow floor")
            memo[p]=(*min(eligible,key=dist)[0],255)
        out.append(memo[p])
    result=Image.new("RGBA",im.size);result.putdata(out)
    return result

def pack(frames,cols=None):
    if not frames or len(set(i.size for i in frames))!=1:
        raise ValueError("All frames must be present and have equal size")
    cols=cols or len(frames)
    if cols<1: raise ValueError("Column count must be positive")
    w,h=frames[0].size
    out=Image.new("RGBA",(w*cols,h*math.ceil(len(frames)/cols)))
    for i,frame in enumerate(frames):out.paste(frame,((i%cols)*w,(i//cols)*h))
    return out

def cuts(im,w,h,cols,rows,ox=0,oy=0,gap=0):
    if min(w,h,cols,rows)<1 or min(ox,oy,gap)<0 or ox+cols*w+(cols-1)*gap>im.width or oy+rows*h+(rows-1)*gap>im.height:
        raise ValueError("Slice grid exceeds image bounds")
    return [im.crop((x,y,x+w,y+h)) for j in range(rows) for i in range(cols)
            for x,y in [(ox+i*(w+gap),oy+j*(h+gap))]]

def report(im):
    c=Counter(p[:3] for p in im.get_flattened_data() if p[3])
    return dict(size=list(im.size), colors=len(c), alpha=sorted(set(im.getchannel("A").get_flattened_data())),
        min_value=round(min((hsv(p)[2] for p in c),default=0),3),
        pure_black=c[(0,0,0)], top=[["#%02X%02X%02X"%p,n] for p,n in c.most_common(12)])

def main():
    p=argparse.ArgumentParser(description=__doc__);subs=p.add_subparsers(dest="op",required=True)
    a=subs.add_parser("sample");a.add_argument("src");a.add_argument("--box",nargs=4,type=int)
    a=subs.add_parser("grade");a.add_argument("src");a.add_argument("dst");a.add_argument("palette")
    for name,d in (("floor",.22),("sat",1.),("val",1.)):a.add_argument("--"+name,type=float,default=d)
    a.add_argument("--alpha",type=int,default=128)
    a=subs.add_parser("snap");a.add_argument("src");a.add_argument("dst")
    a.add_argument("width",type=int);a.add_argument("height",type=int);a.add_argument("--step",type=int,required=True)
    a.add_argument("--origin",type=int,nargs=2,default=(0,0))
    a=subs.add_parser("slice");a.add_argument("src");a.add_argument("out_dir")
    for name in ("width","height","cols","rows"):a.add_argument(name,type=int)
    a.add_argument("--gap",type=int,default=0);a.add_argument("--origin",type=int,nargs=2,default=(0,0))
    a=subs.add_parser("pack");a.add_argument("dst");a.add_argument("sources",nargs="+");a.add_argument("--cols",type=int)
    a=subs.add_parser("check");a.add_argument("src");a.add_argument("width",type=int);a.add_argument("height",type=int)
    a.add_argument("--floor",type=float,default=.22)
    x=p.parse_args()
    if x.op=="sample":print(json.dumps(report(load(x.src).crop(x.box) if x.box else load(x.src)),indent=2))
    if x.op=="grade":
        colors=json.loads(Path(x.palette).read_text(encoding="utf-8"))["colors"]
        save(grade(load(x.src),[rgb(c) for c in colors.values()],x.floor,x.sat,x.val,x.alpha),x.dst)
    if x.op=="snap":save(snap(load(x.src),x.width,x.height,*x.origin,x.step),x.dst)
    if x.op=="slice":
        for i,im in enumerate(cuts(load(x.src),x.width,x.height,x.cols,x.rows,*x.origin,x.gap)):save(im,Path(x.out_dir)/f"{i:02d}.png")
    if x.op=="pack":save(pack([load(f) for f in x.sources],x.cols),x.dst)
    if x.op=="check":
        r=report(load(x.src));print(json.dumps(r,indent=2))
        if r["size"]!=[x.width,x.height] or r["alpha"]!=[0,255] or r["pure_black"] or r["min_value"]<x.floor:
            p.exit(1,"Asset failed dimension, alpha, or shadow checks.\n")
if __name__=="__main__":main()
