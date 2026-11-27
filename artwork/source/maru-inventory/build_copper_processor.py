"""Asset 01: Maru copper processor. Exact 16x16 source map."""
from pathlib import Path
import sys
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save, report
P={".":"#00000000","o":"#663748","c":"#b96a45","h":"#e2a366","s":"#453455","m":"#6b507e","l":"#9b7db0","d":"#514061","q":"#59446f"}
GRID=(
"................",
"................",
".....o.o.o......",
".....h.h.h......",
"....sssssss.....",
"..ohsllllmsc o...".replace(" ",""),
"....slqqqdss....",
"..ohslqqqdsch...",
"....slqqqdss....",
"..ohslddddsch...",
"....smmmmmds....",
".....ssssss.....",
".....c.c.c......",
".....o.o.o......",
"................",
"................",
)
assert len(GRID)==16 and all(len(row)==16 for row in GRID)
im=Image.new("RGBA",(16,16))
d=ImageDraw.Draw(im)
for y,row in enumerate(GRID):
    for x,c in enumerate(row):
        if c!=".":d.point((x,y),fill=P[c])
q=report(im)
assert q["alpha"]==[0,255] and not q["pure_black"]
out=ROOT/"artwork/exports/copper-processor.png"
if out.exists() and "--replace" not in sys.argv:raise FileExistsError(out)
save(im,out)
card=Image.new("RGBA",(560,278),"#f6e5c6")
d=ImageDraw.Draw(card)
def txt(x,y,s,k):
    d.text((x,y),s,font=ImageFont.load_default(size=k),fill="#4d354c")
txt(20,15,"STARDEW ENERGISTICS   /   MARU TECH",14)
txt(20,37,"01  Copper Processor",27)
d.rectangle((20,87,181,248),fill="#d8b78f")
card.alpha_composite(im.resize((144,144),Image.Resampling.NEAREST),(29,96))
d.rectangle((228,100,279,151),fill="#caa37e")
card.alpha_composite(im,(246,118))
txt(229,163,"Native 1x",14)
txt(229,197,"Copper pins. Dark ceramic top.",17)
txt(229,222,"16 x 16  /  inventory",15)
save(card,ROOT/"artwork/review/maru-inventory/01-copper-processor.png")
print(q["size"],q["colors"])