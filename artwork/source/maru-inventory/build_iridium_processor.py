"""Asset 03: Maru Iridium Processor in native 16x16 RGBA."""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont
ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools/pixel"))
from pixel_pipeline import save, report

art = Image.new("RGBA", (16, 16))
d = ImageDraw.Draw(art)
for c in (5, 8, 11):
    d.line((c, 1, c, 14), fill="#644960")
    d.line((c, 2, c, 3), fill="#bf9abb")
    d.line((c, 12, c, 13), fill="#9a6e9c")
for c in (4, 7, 10):
    d.line((1, c, 14, c), fill="#644960")
    d.line((2, c, 3, c), fill="#bf9abb")
    d.line((12, c, 13, c), fill="#9a6e9c")
d.polygon([(4,3),(11,3),(12,4),(12,11),(11,12),(4,12),(3,11),(3,4)], fill="#453455")
d.rectangle((4,4,11,11), fill="#7c568a")
d.line((4,4,10,4), fill="#ba8eb5")
d.line((4,5,4,10), fill="#a879a5")
d.line((5,11,11,11), fill="#544168")
d.line((11,5,11,10), fill="#694779")
d.rectangle((5,5,10,10), fill="#514261")
d.line([(5,8),(5,6),(7,6)], fill="#6c597a")
d.line([(8,9),(10,9),(10,7)], fill="#664f74")
d.point((7,7), fill="#5b9294")
d.point((8,7), fill="#a4d7bf")
d.point((8,8), fill="#66aea7")
d.point((9,8), fill="#577c8a")
result = report(art)
assert result["size"] == [16,16] and result["alpha"] == [0,255] and result["pure_black"] == 0
out = ROOT / "artwork/exports/iridium-processor.png"
review = ROOT / "artwork/review/maru-inventory/03-iridium-processor.png"
if out.exists() or review.exists():
    raise FileExistsError("Refusing to overwrite existing Iridium Processor artwork.")
save(art, out)
card = Image.new("RGBA", (560,278), "#f6e5c6")
d = ImageDraw.Draw(card)
def txt(x,y,s,k):
    d.text((x,y),s,font=ImageFont.load_default(size=k),fill="#4d354c")
txt(20,15,"STARDEW ENERGISTICS   /   MARU TECH",14)
txt(20,37,"03  Iridium Processor",27)
d.rectangle((20,87,181,248),fill="#d8b78f")
card.alpha_composite(art.resize((144,144),Image.Resampling.NEAREST),(29,96))
d.rectangle((228,100,279,151),fill="#caa37e")
card.alpha_composite(art,(246,118))
txt(229,163,"Native 1x",14)
txt(229,197,"Violet alloy. Tiny teal core.",17)
txt(229,222,"16 x 16  /  inventory",15)
save(card, review)
with Image.open(out) as native, Image.open(review) as preview:
    assert native.mode == "RGBA" and native.tobytes() == art.tobytes()
    assert preview.size == (560,278)
    print("Export", native.size, native.mode, "palette", result["colors"], "preview", preview.size)