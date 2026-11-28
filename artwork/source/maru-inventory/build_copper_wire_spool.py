"""Asset 04: a turned wood spool with a loose copper tail, drawn at 1x."""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save, report
outline="#633c49"; wood_dark="#925a4d"; wood="#bd8661"; wood_light="#f0c697"
copper="#b56b58"; copper_dark="#884957"; copper_light="#e3a476"; copper_glint="#efd0a0"
art=Image.new("RGBA",(16,16)); d=ImageDraw.Draw(art)
# The tail is a single bent conductor with an end cap, not a second coil.
d.line([(9,10),(11,10),(11,11),(12,11),(12,13),(14,13),(14,11)],fill=copper_dark)
d.point((11,10),fill=copper_light)
d.point((12,11),fill=copper)
d.point((13,13),fill=copper)
d.point((14,11),fill=copper_glint)
# Turned lower flange, a honey-colored wood rim in front of the coil.
d.polygon([(3,10),(9,10),(11,12),(11,13),(9,14),(3,14),(1,13),(1,12)],fill=outline)
d.polygon([(3,11),(9,11),(10,12),(10,13),(3,13),(2,12)], fill=wood_dark)
d.line((3,12,8,12),fill=wood)
d.line((3,12,5,12),fill=wood_light)
# Copper windings: warm top edges with deep wine shadows between turns.
d.rectangle((2,5,10,11),fill=outline)
d.rectangle((3,5,9,10),fill=copper)
d.line((9,6,9,10),fill=copper_dark)
d.line((3,7,8,7),fill=copper_light)
d.line((3,9,8,9),fill=copper_light)
d.line((3,8,7,8),fill=copper_dark)
d.line((4,7,5,7),fill=copper_glint)
# Upper flange: separate lighter wood plate with a visible dark spindle hole.
d.polygon([(3,2),(9,2),(11,4),(11,5),(9,6),(3,6),(1,5),(1,4)],fill=outline)
d.polygon([(3,3),(9,3),(10,4),(9,5),(3,5),(2,4)],fill=wood)
d.line((3,3,8,3),fill=wood_light)
d.line((5,4,7,4),fill=outline)
d.point((5,5),fill=wood_dark)
d.point((9,4),fill=wood_dark)
result=report(art)
assert result["size"]==[16,16] and result["alpha"]==[0,255] and result["pure_black"]==0
out=ROOT/"artwork/exports/copper-wire-spool.png"
if out.exists() and "--replace" not in sys.argv:raise FileExistsError(out)
save(art,out)
card=Image.new("RGBA",(560,278),"#f6e5c6");d=ImageDraw.Draw(card)
def txt(x,y,s,k):d.text((x,y),s,font=ImageFont.load_default(size=k),fill="#4d354c")
txt(20,15,"STARDEW ENERGISTICS   /   MARU TECH",14)
txt(20,37,"04  Copper Wire Spool",27)
d.rectangle((20,87,181,248),fill="#d8b78f")
card.alpha_composite(art.resize((144,144),Image.Resampling.NEAREST),(29,96))
d.rectangle((228,100,279,151),fill="#caa37e")
card.alpha_composite(art,(246,118))
txt(229,163,"Native 1x",14)
txt(229,197,"Turned wood. Wound copper.",17)
txt(229,222,"16 x 16  /  inventory",15)
save(card,ROOT/"artwork/review/maru-inventory/04-copper-wire-spool.png")
print(result["size"],result["colors"])