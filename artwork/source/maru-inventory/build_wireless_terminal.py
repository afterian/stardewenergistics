"""Asset 10: original 16×16 Maru Wireless Terminal."""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save,report
im=Image.new("RGBA",(16,16))
d=ImageDraw.Draw(im)
# Stub antenna and plum protective shell.
d.line((11,1,11,5),fill="#5b426f")
d.point((11,1),fill="#977ba9")
d.polygon([(4,3),(11,3),(13,5),(13,12),(11,14),(4,14),(2,12),(2,5)],fill="#493553")
d.polygon([(4,4),(11,4),(12,5),(12,12),(11,13),(4,13),(3,12),(3,5)],fill="#725589")
d.line((4,4,10,4),fill="#aa86bd")
d.line((3,5,3,9),fill="#8d6da5")
d.line((12,6,12,11),fill="#5c436f")
# Honey-oak overmold grip, on the lower heel only.
d.rectangle((4,11,11,12),fill="#ab744e")
d.line((4,11,11,11),fill="#dca56a")
d.line((4,12,11,12),fill="#97624d")
# CRT bezel with blocky mint scan marks (no antialiasing).
d.rectangle((4,5,10,9),fill="#48504f")
d.rectangle((5,6,9,8),fill="#42745e")
d.line((5,6,9,6),fill="#62957a")
d.line((6,7,8,7),fill="#9bc68c")
d.point((6,8),fill="#7ab38b")
d.point((8,8),fill="#8abca8")
# Offset talk button and two compact keys.
d.rectangle((11,8,11,9),fill="#997781")
d.rectangle((5,10,7,12),fill="#77516e")
d.point((6,11),fill="#b28cb2")
d.point((6,12),fill="#9b7caa")
d.line((9,11,10,11),fill="#d8b58e")
d.point((10,12),fill="#675276")
q=report(im);assert q["size"]==[16,16] and q["alpha"]==[0,255] and q["pure_black"]==0
out=ROOT/"artwork/exports/wireless-terminal.png"
card_path=ROOT/"artwork/review/maru-inventory/10-wireless-terminal.png"
if any(p.exists() for p in (out,card_path)) and "--replace" not in sys.argv:
    raise FileExistsError("Existing Wireless Terminal output")
save(im,out)
card=Image.new("RGBA",(560,278),"#f6e5c6");d=ImageDraw.Draw(card)
def txt(x,y,s,k):d.text((x,y),s,font=ImageFont.load_default(size=k),fill="#4d354c")
txt(20,15,"STARDEW ENERGISTICS   /   MARU TECH",14)
txt(20,37,"10  Wireless Terminal",27)
d.rectangle((20,87,181,248),fill="#d8b78f")
card.alpha_composite(im.resize((144,144),Image.Resampling.NEAREST),(29,96))
d.rectangle((228,100,279,151),fill="#caa37e")
card.alpha_composite(im,(246,118))
txt(229,163,"Native 1x",14)
txt(229,197,"Green CRT. Oak heel grip.",17)
txt(229,222,"16 x 16  /  inventory",15)
save(card,card_path)
print(q)
