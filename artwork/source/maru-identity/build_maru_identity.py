"""Original Stardew Energistics circuit-leaf and copper network wrench."""
from pathlib import Path
import sys
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save,report

def draw_icon():
    im=Image.new("RGBA",(64,64));d=ImageDraw.Draw(im)
    # Two nested bevels and a separate inset board: edges are integer pixels.
    d.rounded_rectangle((2,2,61,61),radius=12,fill="#39294e")
    d.rounded_rectangle((4,4,59,59),radius=10,fill="#765498")
    d.rounded_rectangle((6,6,57,58),radius=9,fill="#513768")
    d.rounded_rectangle((8,8,55,55),radius=8,fill="#483455")
    d.line((15,5,46,5),fill="#aa85b4",width=2)
    d.line((11,8,14,6),fill="#9573a7",width=2)
    d.line((10,11,10,19),fill="#654b79",width=2)
    d.line((17,56,47,56),fill="#322843",width=2)
    # Quiet status accents, deliberately secondary to the single mark.
    d.rectangle((12,14,14,17),fill="#9d779a")
    d.point((13,14),fill="#bd96ae")
    d.rectangle((49,47,51,49),fill="#6e566f")
    d.point((49,47),fill="#8b7195")
    # Original asymmetric leaf silhouette. Lower-left stem doubles as a signal line.
    d.polygon([(15,45),(19,40),(20,31),(23,25),(30,20),(41,17),(48,13),(49,19),
               (48,29),(45,35),(40,40),(34,43),(26,44),(22,43),(18,49),(14,50)],fill="#613c4c")
    d.polygon([(17,45),(21,40),(22,31),(25,26),(31,22),(42,19),(47,16),(47,25),
               (45,32),(41,37),(35,41),(27,42),(23,41),(19,47),(16,48)],fill="#b97153")
    d.polygon([(22,40),(25,30),(30,25),(37,23),(43,20),(46,18),(46,26),(43,31),
               (38,34),(32,39),(26,40)],fill="#d89765")
    d.line([(22,31),(25,26),(31,22),(42,19),(47,16)],fill="#f0bc83",width=2)
    d.line([(24,42),(29,43),(37,41),(43,37),(46,32)],fill="#955248",width=2)
    # Leaf veins are intentionally orthogonal like traces; plum etch with round contacts.
    trace="#654157"
    d.line([(20,45),(27,38),(30,38),(36,32),(36,28),(41,23)],fill=trace,width=3)
    d.line([(27,38),(27,33),(30,30)],fill=trace,width=2)
    d.line([(36,32),(40,32),(43,29)],fill=trace,width=2)
    for x,y in [(30,30),(41,23),(43,29)]:
        d.ellipse((x-2,y-2,x+2,y+2),fill="#7b4a55")
        d.rectangle((x-1,y-1,x+1,y+1),fill="#efc183")
    d.line((16,47,18,45),fill="#e4a673",width=2)
    return im

def draw_wrench():
    im=Image.new("RGBA",(16,16));d=ImageDraw.Draw(im)
    # Diagonal adjustable head with a clear notch and broad copper handle.
    d.polygon([(2,11),(7,6),(7,4),(9,2),(12,2),(11,4),(10,5),(11,6),(12,6),
               (14,4),(14,7),(12,9),(10,9),(5,14),(3,14),(2,13)],fill="#693c4a")
    d.polygon([(3,11),(8,6),(8,4),(9,3),(11,3),(9,5),(11,7),(12,7),(13,6),
               (13,7),(11,8),(10,8),(5,13),(3,13)],fill="#bc735c")
    d.line([(3,11),(8,6),(8,4),(9,3),(10,3)],fill="#e8a777")
    d.line([(6,12),(9,9),(10,8),(11,8)],fill="#a75955")
    # One highlighted worm screw and one small indicator set INTO the handle.
    d.line((8,6,9,7),fill="#efd0a0")
    d.point((9,7),fill="#d29779")
    d.point((4,12),fill="#6f6c82")
    d.point((5,11),fill="#78b2ab")
    return im

def card(art,num,title,detail):
    b=Image.new("RGBA",(560,278),"#f6e5c6");d=ImageDraw.Draw(b)
    def t(x,y,s,k):d.text((x,y),s,font=ImageFont.load_default(size=k),fill="#4d354c")
    t(20,15,"STARDEW ENERGISTICS   /   MARU TECH",14)
    t(20,37,f"{num}  {title}",27)
    d.rectangle((20,87,181,248),fill="#d8b78f")
    n=2 if art.width==64 else 9
    p=art.resize((art.width*n,art.height*n),Image.Resampling.NEAREST)
    b.alpha_composite(p,(101-p.width//2,168-p.height//2))
    d.rectangle((228,88,308,168),fill="#caa37e")
    b.alpha_composite(art,(268-art.width//2,128-art.height//2))
    t(228,177,"Native 1x",14)
    t(228,206,detail,15)
    t(228,231,f"{art.width} x {art.height}  /  transparent",14)
    return b

def main():
    for num,title,slug,art,detail in [
        (33,"Mod Icon","mod-icon",draw_icon(),"Copper leaf. Etched circuitry."),
        (36,"Network Wrench","network-wrench",draw_wrench(),"Adjustable jaw. Teal status LED."),
    ]:
        out=ROOT/"artwork/exports"/f"{slug}.png"
        rev=ROOT/"artwork/review/maru-identity"/f"{num}-{slug}.png"
        if "--replace" not in sys.argv and (out.exists() or rev.exists()):raise FileExistsError(slug)
        q=report(art)
        size=64 if num==33 else 16
        assert art.mode=="RGBA" and q["size"]==[size,size] and q["alpha"]==[0,255] and not q["pure_black"]
        save(art,out);save(card(art,num,title,detail),rev)
        print(slug,q["size"],q["colors"], art.getbbox())
if __name__=="__main__":main()