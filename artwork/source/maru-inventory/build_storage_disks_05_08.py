"""Maru storage disks #05-08: editable palette and shared native-pixel cartridge."""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save, load, report
# Outer edge, bevel shadow, face, bevel light, recessed face, inset light, polished tab.
SPECS=[
    (5,"copper","Cream label. Copper casing.",
     ("#583541","#885047","#bc714d","#e3a16a","#714453","#a86561","#d38a59")),
    (6,"iron","Cool iron. Two raised rivets.",
     ("#41364c","#575366","#858494","#b5b4bd","#575268","#80758a","#aba9b6")),
    (7,"gold","Warm gold. One teal status light.",
     ("#5f3f46","#96704c","#c89952","#efd084","#80594f","#b48764","#e0b766")),
    (8,"iridium","Iridium purple. Two teal status lights.",
     ("#422e54","#5f4577","#825fa3","#b293ce","#56416c","#775a93","#a383bb"))
]
BASE=(
    "................",
    "................",
    "...OOOOOOOO.....",
    "..OLLLLLLLLO....",
    "..OLFFFFFHFFO...",
    "..OLFFFFFHFFDO..",
    "..OLFFFFFHFFDO..",
    "..OLFFFFFHFFDO..",
    "..OLDDDDDDFFDO..",
    "..OLIIIIIDFFDO..",
    "..OLIIIIIDFFDO..",
    "..OLHIIIIDFFDO..",
    "..OFFFFFFFTFDO..",
    "..ODDDDDDDDDDO..",
    "...OOOOOOOOOO...",
    "................",
)
assert all(len(row)==16 for row in BASE)
NEAR=Image.Resampling.NEAREST

def disk(kind,palette):
    colors=dict(zip("ODFLIHT",palette))
    im=Image.new("RGBA",(16,16))
    d=ImageDraw.Draw(im)
    for y,row in enumerate(BASE):
        for x,key in enumerate(row):
            if key!=".":d.point((x,y),fill=colors[key])
    # Recessed top label and edge details distinguish the tiers without changing the silhouette.
    if kind=="copper":
        d.rectangle((4,5,8,7),fill="#cdaa91")
        d.line((4,5,8,5),fill="#f3e6cd")
        d.line((4,6,8,6),fill="#e4cfb5")
        d.point((5,6),fill="#a98382")
        d.point((7,6),fill="#b38a83")
    else:
        d.rectangle((4,5,8,7),fill=colors["I"])
        d.line((4,5,8,5),fill=colors["H"])
        d.line((5,6,7,6),fill=colors["D"])
    if kind=="iron":
        for x,y in ((10,5),(11,11)):
            d.point((x,y),fill="#d0c3cf")
            d.point((x+1,y+1),fill=colors["D"])
    if kind in ("gold","iridium"):
        for x in ((10,) if kind=="gold" else (10,12)):
            d.line((x,6,x,7),fill="#568c8f")
            d.point((x,6),fill="#98d5bd")
    return im

def card(im,number,kind,desc):
    c=Image.new("RGBA",(560,278),"#f6e5c6")
    d=ImageDraw.Draw(c)
    def txt(x,y,s,k):
        d.text((x,y),s,font=ImageFont.load_default(size=k),fill="#4d354c")
    txt(20,15,"STARDEW ENERGISTICS   /   MARU TECH",14)
    txt(20,37,f"{number:02}  {kind.title()} Disk",27)
    d.rectangle((20,87,181,248),fill="#d8b78f")
    c.alpha_composite(im.resize((144,144),NEAR),(29,96))
    d.rectangle((228,100,279,151),fill="#caa37e")
    c.alpha_composite(im,(246,118))
    txt(229,163,"Native 1x",14)
    txt(229,197,desc,16)
    txt(229,222,"16 x 16  /  inventory",15)
    return c

def main():
    review=ROOT/"artwork/review/maru-inventory"
    exports=ROOT/"artwork/exports"
    frames=[(v,disk(v[1],v[3])) for v in SPECS]
    assert len({im.getchannel("A").tobytes() for _,im in frames})==1
    assert len({im.tobytes() for _,im in frames})==4
    targets=[]
    for (number,kind,desc,_),im in frames:
        out=exports/f"{kind}-disk.png"
        proof=review/f"{number:02}-{kind}-disk.png"
        if (out.exists() or proof.exists()) and "--replace" not in sys.argv:
            raise FileExistsError(f"{out} or {proof}")
        q=report(im)
        assert q["size"]==[16,16] and q["alpha"]==[0,255] and q["pure_black"]==0
        targets.append((out,proof,im,card(im,number,kind,desc),q))
    for out,proof,im,review_card,q in targets:
        save(im,out)
        save(review_card,proof)
        assert load(out).tobytes()==im.tobytes()
        assert load(proof).size==(560,278)
        print(out.name,q["size"],q["colors"],im.getbbox())

if __name__=="__main__":
    main()