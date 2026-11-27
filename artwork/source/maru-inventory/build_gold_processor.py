"""Asset 02: original Maru Gold Processor, drawn at native pixel coordinates."""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save, report
art=Image.new("RGBA",(16,16));d=ImageDraw.Draw(art)
for c in (4,7,10):
    d.line((c,1,c,14),fill="#a77048")
    d.line((c,2,c,3),fill="#f1c776")
    d.line((c,12,c,13),fill="#dab064")
for c in (5,8,11):
    d.line((1,c,14,c),fill="#a77048")
    d.line((2,c,3,c),fill="#f1c776")
    d.line((12,c,13,c),fill="#dab064")
d.polygon([(4,3),(11,3),(12,4),(12,11),(11,12),(4,12),(3,11),(3,4)],fill="#664353")
d.rectangle((4,4,11,11),fill="#c29157")
d.line((4,4,10,4),fill="#efcb82")
d.line((4,5,4,10),fill="#e3b979")
d.rectangle((5,5,10,10),fill="#664d78")
d.line((5,5,10,5),fill="#957ca0")
d.line((5,6,5,9),fill="#896f9c")
d.line([(6,9),(6,7),(8,7),(8,6)],fill="#ac8b90")
d.point((9,9),fill="#b99894")
d.line((6,10,10,10),fill="#534361")
result=report(art);assert result["size"]==[16,16] and result["alpha"]==[0,255] and result["pure_black"]==0
out=ROOT/"artwork/exports/gold-processor.png"
if out.exists() and "--replace" not in sys.argv:raise FileExistsError(out)
save(art,out)
card=Image.new("RGBA",(560,278),"#f6e5c6");d=ImageDraw.Draw(card)
def txt(x,y,s,k):d.text((x,y),s,font=ImageFont.load_default(size=k),fill="#4d354c")
txt(20,15,"STARDEW ENERGISTICS   /   MARU TECH",14)
txt(20,37,"02  Gold Processor",27)
d.rectangle((20,87,181,248),fill="#d8b78f")
card.alpha_composite(art.resize((144,144),Image.Resampling.NEAREST),(29,96))
d.rectangle((228,100,279,151),fill="#caa37e")
card.alpha_composite(art,(246,118))
txt(229,163,"Native 1x",14)
txt(229,197,"Gold contacts. Etched ceramic.",17)
txt(229,222,"16 x 16  /  inventory",15)
save(card,ROOT/"artwork/review/maru-inventory/02-gold-processor.png")
print(result["size"],result["colors"])