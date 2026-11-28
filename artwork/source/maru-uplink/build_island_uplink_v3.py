"""Island Uplink v3: readable oval reflector with one exposed center antenna."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import sys
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save,pack,cuts,load,report
P=["#39294e","#533862","#765498","#8e70a6","#c4add4","#ad6a46","#d89d64","#68aaa0","#a4d6c1","#5b9494"]
N=Image.Resampling.NEAREST
def art(s):
    im=Image.new("RGBA",(16,32)); d=ImageDraw.Draw(im)
    def poly(p,i): d.polygon(p,fill=P[i])
    def line(p,i,w=1): d.line(p,fill=P[i],width=w)
    def rect(p,i): d.rectangle(p,fill=P[i])
    # Slim post and small oak shoe. Keep the stand visually secondary.
    line([(7,18),(9,22),(8,27)],0,4)
    line([(7,19),(9,22),(8,27)],2,2)
    rect((3,26,12,29),0);rect((4,26,11,27),5)
    line([(4,26),(11,26)],6);rect((4,29,5,30),0);rect((10,29,11,30),0)
    d.point((10,28),fill=P[7] if s else P[2])
    # A tilted ellipse, separated into back, face and fine lit rim.
    poly([(2,9),(5,10),(8,12),(11,15),(13,18),(13,20),(11,22),(8,22),
          (5,20),(2,17),(0,14),(0,11)],0)
    poly([(2,10),(4,10),(7,12),(10,15),(12,18),(12,20),(10,21),
          (8,20),(5,18),(2,15),(1,12)],2)
    poly([(2,11),(4,11),(6,12),(9,15),(11,18),(11,19),(9,19),
          (6,17),(3,14)],1)
    line([(2,10),(4,10),(7,12),(10,15),(12,18),(12,19)],4)
    line([(2,13),(4,16),(7,19),(9,20)],3)
    # One thin feed stalk visibly starts at the center and ends in empty space.
    line([(6,16),(7,15),(8,14),(9,13),(10,12),(11,11)],0,2)
    line([(6,15),(7,14),(8,13),(9,12),(10,11),(11,10)],6,1)
    rect((11,9,12,10),6)
    d.point((12,9),fill=P[8] if s else P[4])
    if s==1:
        line([(11,7),(12,6),(13,6),(14,7)],7)
    if s==2:
        line([(10,5),(11,4),(13,4),(15,6)],9)
        line([(12,4),(13,4)],8)
    return im
def main():
    frames=[art(i) for i in range(3)];strip=pack(frames);q=report(strip)
    assert strip.size==(48,32) and q["alpha"]==[0,255] and not q["pure_black"]
    assert len({t.tobytes() for t in frames})==3
    assert len({f.crop((0,9,16,32)).getchannel("A").tobytes() for f in frames})==1
    assert all(a.tobytes()==b.tobytes() for a,b in zip(frames,cuts(strip,16,32,3,1)))
    root=ROOT/"artwork/review/maru-uplink"
    save(strip,root/"island-uplink-v3-native.png")
    card=Image.new("RGBA",(686,343),"#f6e5c6");d=ImageDraw.Draw(card)
    def txt(x,y,t,k):d.text((x,y),t,fill="#4d354c",font=ImageFont.load_default(size=k))
    txt(22,16,"STARDEW ENERGISTICS   /   MARU TECH",14);txt(22,39,"20  Island Uplink / simple dish",29)
    txt(22,79,"One tilted reflector. One antenna from the center.",16)
    for i,(f,name) in enumerate(zip(frames,["Off","Pulse 1","Pulse 2"])):
        x=22+176*i
        d.rectangle((x,112,x+160,288),fill="#d8b78f")
        card.alpha_composite(f.resize((80,160),N),(x+40,119))
        txt(x+6,296,name,14)
    d.rectangle((558,112,664,288),fill="#d1ac83")
    core=load(ROOT/"artwork/exports/computer-core.png").crop((16,0,32,32))
    card.alpha_composite(core,(579,160));card.alpha_composite(frames[0],(624,160))
    txt(566,201,"Native 1x",14)
    txt(22,319,"Signal frames share the same hardware and tile anchor.",14)
    save(card,root/"20-island-uplink-v3.png")
    print(q["size"],q["colors"],q["min_value"])
if __name__=="__main__":main()
