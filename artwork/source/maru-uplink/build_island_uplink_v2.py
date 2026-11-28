"""Island Uplink v2: fixed hardware and a rising directional pulse, native pixel art."""
from pathlib import Path
import sys
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save,load,pack,cuts,report
P=dict(edge="#39294e",deep="#503661",body="#765498",mid="#9072ac",hi="#b49ccb",
       oakDark="#753a40",oakSide="#a45a3c",oak="#c17c48",oakHi="#e9af68",
       copper="#b86143",bronze="#d9985f",chrome="#9d879c",
       ledOff="#6c667b",led="#76b9ae",pulse="#add8bc",trail="#699c9a")
N=Image.Resampling.NEAREST
def uplink(state):
    im=Image.new("RGBA",(16,32));d=ImageDraw.Draw(im)
    def r(a,key):d.rectangle(a,fill=P[key])
    def q(a,key):d.polygon(a,fill=P[key])
    # Broad dish, not a flat oval: three stepped edges frame a plum reflector.
    q([(1,9),(3,9),(4,11),(6,13),(8,14),(10,14),(12,13),(13,13),(14,15),
       (13,18),(11,21),(8,23),(5,22),(3,20),(1,17),(0,14),(0,11)],"edge")
    q([(1,11),(3,11),(5,13),(7,14),(9,15),(12,14),(13,15),
       (12,18),(10,20),(7,21),(5,20),(3,18),(1,15)],"body")
    q([(2,12),(4,13),(7,15),(9,16),(12,15),(11,18),(9,19),(6,19),(4,17),(2,14)],"deep")
    d.line([(1,11),(2,13),(4,15),(7,16),(9,16)],fill=P["mid"])
    d.line([(1,10),(2,10),(4,12),(6,13),(8,14),(10,14),(12,13)],fill=P["hi"])
    d.line([(3,18),(5,20),(7,21),(9,20)],fill=P["mid"])
    # Feed arm comes up across the reflector and ends in a directional transmitter.
    d.line([(7,18),(9,16),(11,12),(13,10)],fill=P["edge"],width=3)
    d.line([(7,17),(9,15),(11,12),(13,10)],fill=P["copper"],width=2)
    d.line([(8,16),(10,14),(11,12),(13,10)],fill=P["bronze"])
    r((12,8,15,11),"edge");r((12,8,14,9),"bronze");r((13,10,14,10),"led" if state else "ledOff")
    # Mechanical gimbal with a counterweight and wide wooden cabinet on feet.
    r((7,20,10,25),"edge");r((8,21,9,24),"mid");r((10,21,12,22),"deep")
    d.line([(11,22),(12,24),(12,26)],fill=P["copper"],width=1)
    q([(3,23),(12,23),(14,25),(14,29),(13,30),(11,30),(11,29),(4,29),(4,30),(2,30),
       (1,29),(1,25)],"edge")
    q([(3,24),(12,24),(13,25),(2,25)],"oakHi")
    r((2,26,13,28),"oak");r((12,26,13,29),"oakSide");r((2,28,13,28),"oakDark")
    r((2,28,3,29),"oakSide");r((3,26,3,26),"chrome");r((11,26,11,26),"chrome")
    r((5,26,9,27),"deep");r((5,26,5,26),"led" if state else "ledOff")
    r((7,27,8,27),"chrome")
    if state==1:
        d.line([(11,5),(13,4),(15,5)],fill=P["led"])
        d.point((13,4),fill=P["pulse"])
    if state==2:
        d.line([(10,4),(12,2),(14,2),(15,3)],fill=P["trail"])
        d.line((12,2,14,2),fill=P["pulse"]);d.point((13,5),fill=P["led"])
    return im
def card(frames):
    im=Image.new("RGBA",(838,365),"#f6e5c6");d=ImageDraw.Draw(im)
    def txt(x,y,t,size):d.text((x,y),t,font=ImageFont.load_default(size=size),fill="#4d354c")
    txt(23,16,"STARDEW ENERGISTICS  /  MARU TECH",15);txt(23,39,"20  Island Uplink / revised",29)
    txt(23,79,"Recessed violet dish, copper feed arm, oak electronics cabinet",17)
    for i,(f,t) in enumerate(zip(frames,("Off","Pulse 1","Pulse 2"))):
        x=23+202*i
        d.rectangle((x,115,x+184,307),fill="#d5b68f")
        for y in range(120,298,20):d.line((x,y+4,x+184,y+4),fill="#d0ad83")
        im.alpha_composite(f.resize((80,160),N),(x+49,126));im.alpha_composite(f,(x+152,263))
        txt(x+5,313,t,16)
    x=634
    txt(x,113,"In the Maru kit",16);d.rectangle((x,147,813,285),fill="#d5b68f")
    core=load(ROOT/"artwork/exports/computer-core.png").crop((16,0,32,32))
    for j,f in enumerate((core,frames[1])):im.alpha_composite(f.resize((48,96),N),(x+27+j*72,171))
    txt(x,312,"3x size comparison",14)
    txt(23,340,"Native 16 x 32 frames. Signal moves; the hardware stays anchored.",14)
    return im
def main():
    frames=[uplink(i) for i in range(3)]
    strip=pack(frames)
    q=report(strip);assert q["size"]==[48,32] and q["alpha"]==[0,255] and not q["pure_black"]
    assert len({f.tobytes() for f in frames})==3
    assert all(f.crop((0,8,16,32)).getchannel("A").tobytes()==frames[0].crop((0,8,16,32)).getchannel("A").tobytes() for f in frames)
    assert all(f.crop((0,20,16,32)).getbbox()==(1,0,15,11) for f in frames)
    assert all(a.tobytes()==b.tobytes() for a,b in zip(frames,cuts(strip,16,32,3,1)))
    target=ROOT/"artwork/review/maru-uplink"
    save(strip,target/"island-uplink-v2-native.png")
    save(card(frames),target/"20-island-uplink-v2.png")
    print(q["size"],q["colors"],q["min_value"])
if __name__=="__main__":main()