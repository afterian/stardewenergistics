"""Editable native meter fills with exact TerminalMenu compositing."""
from pathlib import Path
import sys
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save,pack,cuts,report
REVIEW=ROOT/"artwork/review/maru-status"
OUT=ROOT/"artwork/exports"
PURPLE="#513768";LIGHT="#9c7cc1";DEEP="#39294e";INDIGO="#665077"
COPPER="#c8784c";SUB="#463754";INSET="#674f6c"

def draw_meter(kind):
    empty=Image.new("RGBA",(48,8));a=ImageDraw.Draw(empty)
    a.polygon([(1,0),(46,0),(47,1),(47,6),(46,7),(1,7),(0,6),(0,1)],fill=DEEP)
    a.rectangle((1,1,46,6),fill=PURPLE)
    a.line((2,1,45,1),fill=LIGHT)
    a.line((2,6,45,6),fill=INDIGO)
    a.rectangle((2,2,45,5),fill=SUB)
    a.line((2,5,45,5),fill=INSET)
    a.point((1,4),fill=COPPER);a.point((46,4),fill=COPPER)
    for x in (12,24,36):a.point((x,6),fill="#ad8cb2")
    active=Image.new("RGBA",(48,8));d=ImageDraw.Draw(active)
    colors=("#aac77c","#79aa6c","#649764","#4f825f") if kind=="power" else (
        "#a0ccc3","#79b1aa","#649b96","#507e83")
    for y,c in enumerate(colors,2):d.line((2,y,45,y),fill=c)
    for x in (12,24,36):
        d.line((x,3,x,4),fill=colors[2])
        d.point((x,5),fill=colors[3])
    return empty,active

def fill_preview(base,active,amount):
    result=base.copy();w=round(48*min(1,max(0,amount)))
    if w:result.alpha_composite(active.crop((0,0,w,8)))
    return result

def make_asset(kind,number):
    name=f"{kind}-meter";out=OUT/(name+".png");preview=REVIEW/(f"{number}-{name}.png")
    if (out.exists() or preview.exists()) and "--replace" not in sys.argv:raise FileExistsError(name)
    empty,fill=draw_meter(kind)
    strip=pack([empty,fill]);q=report(strip)
    assert q["size"]==[96,8] and q["alpha"]==[0,255] and not q["pure_black"] and q["min_value"]>=.30
    assert fill.getbbox()==(2,2,46,6)
    assert [f.tobytes() for f in cuts(strip,48,8,2,1)]==[empty.tobytes(),fill.tobytes()]
    for i in range(49):
        composed=fill_preview(empty,fill,i/48)
        # Exterior boundary is unchanged, including for one-pixel crop and full-scale fill.
        assert empty.getchannel("A").tobytes()==composed.getchannel("A").tobytes()
        for y in range(8):
            for x in range(48):
                if x>=i or not(2<=x<=45 and 2<=y<=5):assert composed.getpixel((x,y))==empty.getpixel((x,y))
    save(strip,out)
    img=Image.new("RGBA",(690,304),"#f6e5c6");d=ImageDraw.Draw(img);near=Image.Resampling.NEAREST
    def ink(x,y,t,k):d.text((x,y),t,fill="#4d354c",font=ImageFont.load_default(size=k))
    ink(20,15,"STARDEW ENERGISTICS   /   MARU TECH",14)
    ink(20,36,f"{number}  {kind.title()} Meter",29)
    ink(20,76,("Green power" if kind=="power" else "Teal capacity")+"  /  violet track and copper contacts",16)
    for i,(fill_amt,label) in enumerate(((0,"Empty"),(.21,"Low  /  21%"),(.57,"Mid  /  57%"),(1,"Full"))):
        x=20+(i%2)*333;y=110+(i//2)*76
        d.rectangle((x,y,x+299,y+65),fill="#dfbd92")
        bar=fill_preview(empty,fill,fill_amt)
        img.alpha_composite(bar.resize((192,32),near),(x+10,y+8))
        img.alpha_composite(bar,(x+226,y+19))
        ink(x+10,y+42,label,13)
    ink(20,273,"Actual left-to-right fill crop  /  each frame 48 x 8  /  4x and 1x",14)
    save(img,preview)
    print(name,q["size"],q["colors"],q["min_value"],"49 crop registrations")
