"""Original 480x120 Nexus banner. Run from any directory with Pillow."""
from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import load, report, save
NEAR=Image.Resampling.NEAREST
C=Image.new("RGBA",(480,120),"#4e354a")
d=ImageDraw.Draw(C)
def rect(xy,c): d.rectangle(xy,fill=c)
def line(xy,c,w=1): d.line(xy,fill=c,width=w)
def dot(xy,c): d.point(xy,fill=c)

# Original room art: beams, staggered boards, side wall, evening window.
rect((211,4,475,75),"#774953")
for y in range(8,77,9):
    line((212,y,475,y),"#8d5858")
    line((212,y+1,475,y+1),"#69404c")
    for k in range(6):
        x=217+((k*57+y*11)%254)
        line((x,y+2,x,y+7),"#6f4450")
        line((x+4,y+4,min(x+12,474),y+4),"#805052")
rect((211,73,475,117),"#9c6651")
for x in range(218,480,18):
    line((x,75,x+8,116),"#87564f")
    line((x+1,75,x+9,116),"#a97357")
for y in (82,94,112): line((213,y,474,y),"#905e50")
rect((213,70,474,76),"#64404c")
line((215,71,474,71),"#ac7256")
rect((216,4,222,77),"#59394b")
line((217,5,217,72),"#a66c58")
rect((456,4,461,76),"#59394b")
line((457,5,457,73),"#9e6853")
rect((219,10,460,14),"#523649")
line((221,10,459,10),"#bc845b")
# Little three-pane twilight window: band of violet hills below a blue dusk sky.
rect((321,21,374,52),"#53384c")
rect((323,22,372,50),"#b1805b")
rect((326,25,369,47),"#5a6087")
rect((326,25,369,28),"#4c5079")
rect((326,35,369,46),"#776e97")
line((326,39,369,39),"#94799b")
d.polygon([(326,46),(334,41),(343,43),(348,40),(358,44),(369,41),(369,47),(326,47)],fill="#594e73")
for p in ((330,31),(345,28),(362,32),(361,36)):dot(p,"#e2bd99")
rect((346,24,348,48),"#a26c58")
rect((323,48,372,51),"#c59562")
line((324,50,373,50),"#d4a36d")
# Punched corner studs, shelves and amber task lamps.
for x in (236,306,388,447):
    rect((x,28,x+7,31),"#58374b")
    rect((x+2,28,x+5,29),"#b67a58")
    rect((x+1,30,x+6,31),"#e0a97b")
    dot((x+4,31),"#ecc391")
for x in (244,260,396):
    rect((x,41,x+6,45),"#64434f")
    line((x,40,x+6,40),"#ae775d")
# Cabled server wall. Load exact native assets, not enlarged visual approximations.
for x in (229,246,263,280,297):
    rect((x-1,80,x+15,83),"#735155")
for fname,x,frame in (("storage-rack.png",229,4),("computer-core.png",246,1),
                      ("power-rack.png",263,4),("crafting-rack.png",280,1),
                      ("advanced-crafting-rack.png",297,1)):
    src=load(ROOT/"artwork/exports"/fname)
    assert src.height==32 and (frame+1)*16<=src.width,(fname,src.size)
    C.alpha_composite(src.crop((frame*16,0,(frame+1)*16,32)),(x,49))
# Original keg sprites with wooden slats, metal bands, and a copper tap.
def keg(x,y):
    rect((x,y+19,x+19,y+23),"#76534e")
    d.polygon([(x+2,y+3),(x+5,y+1),(x+15,y+1),(x+18,y+3),(x+19,y+7),
               (x+18,y+19),(x+15,y+22),(x+5,y+22),(x+2,y+19),(x+1,y+7)],fill="#4e3749")
    d.polygon([(x+3,y+4),(x+6,y+3),(x+14,y+3),(x+17,y+4),(x+17,y+17),
               (x+15,y+20),(x+5,y+20),(x+3,y+18)],fill="#b47757")
    rect((x+4,y+6,x+16,y+16),"#a36954")
    for dx,c in ((5,"#ce9362"),(7,"#80544e"),(9,"#d49e6a"),(12,"#8e5b50"),(15,"#8f5c51")):
        line((x+dx,y+6,x+dx,y+18),c)
    line((x+4,y+4,x+15,y+4),"#d9a56d")
    line((x+4,y+7,x+16,y+7),"#675367")
    line((x+3,y+8,x+17,y+8),"#928093")
    line((x+3,y+16,x+17,y+16),"#59485e")
    line((x+4,y+17,x+16,y+17),"#8e7e90")
    rect((x+9,y+11,x+12,y+13),"#694550")
    line((x+10,y+11,x+12,y+11),"#e1a871")
    dot((x+12,y+13),"#b09871")
for x,y in ((340,64),(365,64),(390,64),(416,64),(441,64)): keg(x,y)
# Flat animated-data-bus language, shown at one representative phase.
line([(253,80),(253,97),(448,97),(448,86)],"#51394f",5)
line([(253,80),(253,97),(448,97),(448,86)],"#74546b",3)
for x in (347,372,397,423):
    line((x,87,x,97),"#53394f",3)
    line((x,88,x,96),"#c59065")
    rect((x-2,95,x+2,99),"#735d7d")
    dot((x,96),"#a3c4af")
for x in range(261,440,6):
    line((x,97,x+1,97),"#a7c9ae" if x%14<4 else "#678d89")
dot((448,96),"#d2d7a9")
# A frame panel, very small decorative traces and hand-drawn 5x7 pixel title.
rect((3,4,209,115),"#5b3e54")
rect((6,6,206,113),"#63465f")
rect((10,11,202,109),"#51394f")
line((11,11,201,11),"#9e745e")
line((11,109,201,109),"#8b645e")
for x in (12,199):
    for y in (13,107):dot((x,y),"#dbaf79")
G={
"S":("01111","10000","10000","01110","00001","00001","11110"),
"T":("11111","00100","00100","00100","00100","00100","00100"),
"A":("01110","10001","10001","11111","10001","10001","10001"),
"R":("11110","10001","10001","11110","10100","10010","10001"),
"D":("11110","10001","10001","10001","10001","10001","11110"),
"E":("11111","10000","10000","11110","10000","10000","11111"),
"W":("10001","10001","10001","10101","10101","11011","10001"),
"N":("10001","11001","10101","10011","10001","10001","10001"),
"G":("01110","10001","10000","10111","10001","10001","01110"),
"I":("111","010","010","010","010","010","111"),
"C":("01111","10000","10000","10000","10000","10000","01111"),
}
def draw_word(word,baseline,scale,fill,sh):
    length=sum((len(G[ch][0])+1)*scale for ch in word)-scale
    start=(212-length)//2
    for ch in word:
        for y,row in enumerate(G[ch]):
            for x,v in enumerate(row):
                if v=="1":
                    a=start+x*scale;b=baseline+y*scale
                    rect((a+1,b+2,a+scale,b+scale+1),sh)
        start+=(len(G[ch][0])+1)*scale
    start=(212-length)//2
    for ch in word:
        for y,row in enumerate(G[ch]):
            for x,v in enumerate(row):
                if v=="1":
                    a=start+x*scale;b=baseline+y*scale
                    rect((a,b,a+scale-1,b+scale-1),fill)
        start+=(len(G[ch][0])+1)*scale
draw_word("STARDEW",28,3,"#f1c791","#794f57")
draw_word("ENERGISTICS",57,3,"#f4dbc0","#8a666d")
line((22,90,87,90),"#a37872")
line((124,90,189,90),"#a37872")
# Original circuit leaf emblem.
d.polygon([(104,94),(98,90),(97,85),(102,86),(106,90),(111,85),(116,85),
           (113,91),(108,93),(106,97)],fill="#b68c7e")
line([(102,88),(106,92),(112,87)],"#dfbd94")
line((106,92,106,95),"#86ad93")
# Banner perimeter with decorative corner seams.
line((1,1,478,1),"#c68f6d");line((1,118,478,118),"#b78063")
line((1,1,1,118),"#c68f6d");line((478,1,478,118),"#b78063")
for corner in ((0,0),(479,0),(0,119),(479,119)):dot(corner,(0,0,0,0))
q=report(C)
assert C.size==(480,120) and C.mode=="RGBA" and q["pure_black"]==0
out=ROOT/"artwork/exports/nexus-banner.png"
proof=ROOT/"artwork/review/maru-promo/34-nexus-banner.png"
if (out.exists() or proof.exists()) and "--replace" not in sys.argv:
    raise FileExistsError("Existing Nexus banner output")
save(C,out)
review=Image.new("RGBA",(1024,362),"#f6e5c6");rd=ImageDraw.Draw(review)
for x,y,s,k in [(32,15,"STARDEW ENERGISTICS   /   MARU TECH",18),
                (32,39,"34  Nexus banner",28),
                (32,326,"480 x 120 native   /   Enlarged 2x for review",17)]:
    rd.text((x,y),s,font=ImageFont.load_default(size=k),fill="#4d354c")
review.alpha_composite(C.resize((960,240),NEAR),(32,81));save(review,proof)
assert load(out).tobytes()==C.tobytes()
print(C.size,C.mode,q["colors"],q["min_value"],q["alpha"])
