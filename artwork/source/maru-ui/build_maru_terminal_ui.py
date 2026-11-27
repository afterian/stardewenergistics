"""Native Maru UI #27-29 and individual review cards."""
from pathlib import Path
import sys
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import pack,report,save,cuts
OUT=ROOT/"artwork/exports"
PRE=ROOT/"artwork/review/maru-ui"
N=Image.Resampling.NEAREST
PLUM="#4f3754"; PURPLE="#775487"; LIGHT="#a688b6"
WOOD_D="#865044"; WOOD="#b8784f"; HONEY="#e6ae70"; BRASS="#d69a5e"
SEA="#85b9a6"; SEA_D="#49766e"; CREAM="#efce9d"
def canvas(w,h):
    im=Image.new("RGBA",(w,h));return im,ImageDraw.Draw(im)
def panel():
    im,d=canvas(48,48)
    d.polygon([(3,0),(44,0),(47,3),(47,44),(44,47),(3,47),(0,44),(0,3)],fill=PLUM)
    d.rectangle((2,3,45,44),fill=WOOD_D);d.rectangle((3,2,44,45),fill=WOOD)
    d.rectangle((4,4,43,43),outline=BRASS,width=2)
    d.line((4,4,43,4),fill=HONEY);d.line((4,4,4,43),fill=HONEY)
    d.rectangle((7,7,40,40),fill="#4f394e")
    d.rectangle((9,9,38,38),fill="#3e574e")
    d.line((9,9,38,9),fill="#5c7661");d.line((9,9,9,38),fill="#57755f")
    d.line((10,38,38,38),fill="#394d48")
    for x,y in [(4,4),(42,4),(4,42),(42,42)]:
        d.point((x,y),fill=PLUM);d.point((x+1,y+1),fill=CREAM)
    return im
def stretch48(im,w,h):
    dst=Image.new("RGBA",(w,h))
    X=(0,16,w-16,w);Y=(0,16,h-16,h)
    for j in range(3):
        for i in range(3):
            dst.paste(im.crop((i*16,j*16,(i+1)*16,(j+1)*16)).resize((X[i+1]-X[i],Y[j+1]-Y[j]),N),(X[i],Y[j]))
    return dst
def tabs(i):
    im,d=canvas(16,16)
    if i==0:
        d.polygon([(2,5),(5,3),(12,3),(14,5),(14,12),(13,13),(2,13),(1,12),(1,6)],fill=PLUM)
        d.polygon([(2,6),(5,4),(12,4),(13,5),(10,7),(2,7)],fill=HONEY)
        d.rectangle((2,8,12,12),fill=WOOD);d.rectangle((11,7,13,12),fill=WOOD_D)
        for y in (9,11):d.line((3,y,10,y),fill=BRASS)
        d.line((5,8,5,12),fill=WOOD_D);d.point((9,6),fill=CREAM)
    elif i==1:
        d.line((4,13,10,6),fill=PLUM,width=4)
        d.line((4,13,10,6),fill=WOOD,width=2)
        d.polygon([(5,5),(8,2),(10,2),(14,6),(14,8),(12,10),(10,10),(5,6)],fill=PLUM)
        d.polygon([(6,5),(9,3),(13,7),(12,9),(10,9)],fill=LIGHT)
        d.line((7,5,9,4),fill=CREAM)
        d.point((4,12),fill=HONEY)
    else:
        d.rectangle((6,1,9,14),fill=PLUM);d.rectangle((1,6,14,9),fill=PLUM)
        d.polygon([(4,3),(11,3),(12,4),(12,11),(11,12),(4,12),(3,11),(3,4)],fill=PLUM)
        d.ellipse((3,3,12,12),fill=PURPLE)
        d.line((6,2,8,2),fill=LIGHT);d.line((4,5,6,3),fill=LIGHT)
        d.rectangle((6,6,9,9),fill=PLUM);d.rectangle((7,7,8,8),fill=SEA)
    return im
def settings(k):
    im,d=canvas(9,9)
    d.polygon([(1,0),(7,0),(8,1),(8,7),(7,8),(1,8),(0,7),(0,1)],fill=PLUM)
    d.rectangle((1,1,7,7),fill=PURPLE)
    d.line((1,1,6,1),fill=LIGHT)
    if k in (0,1):
        p=[(2,5),(4,3),(6,5)] if k==0 else [(2,3),(4,5),(6,3)]
        d.line(p,fill=CREAM,width=1);d.point((4,4),fill=CREAM)
    elif k in (2,3):
        d.line((2,4,6,4),fill=SEA if k==2 else "#e6a079")
        if k==2:d.line((4,2,4,6),fill=SEA)
    elif k==4:
        d.rectangle((2,2,5,5),outline=CREAM)
        d.line([(6,3),(6,6),(3,6)],fill=SEA)
    elif k==5:
        d.rectangle((2,3,6,6),outline=CREAM)
        d.line((3,2,5,2),fill=SEA)
        d.point((4,4),fill=SEA)
    else:
        d.ellipse((2,2,6,6),outline=CREAM);d.line([(4,3),(4,4),(5,4)],fill=SEA)
    return im
def card(no,name,frames,labels,desc):
    im,d=canvas(674,268);d.rectangle((0,0,673,267),fill="#f6e5c6")
    def txt(x,y,t,k):d.text((x,y),t,font=ImageFont.load_default(size=k),fill=PLUM)
    txt(20,13,"STARDEW ENERGISTICS   /   MARU TECH",14);txt(20,34,f"{no}  {name}",27);txt(20,70,desc,15)
    if no==27:
        for art,x,y in [(frames[0].resize((96,96),N),20,108),(stretch48(frames[0],144,60).resize((288,120),N),152,108)]:
            d.rectangle((x,y,x+art.width-1,y+art.height-1),fill="#d8b78f")
            im.alpha_composite(art,(x,y))
        txt(20,214,"48 x 48 source",14);txt(152,236,"Stretched using fixed 16 px corners",14)
    else:
        for i,(f,label) in enumerate(zip(frames,labels)):
            x=20+93*i
            d.rectangle((x,108,x+78,195),fill="#d8b78f")
            size=(f.width*4 if no==28 else f.width*6,f.height*4 if no==28 else f.height*6)
            im.alpha_composite(f.resize(size,N),(x+39-size[0]//2,112+(64-size[1])//2))
            im.alpha_composite(f,(x+39-f.width//2,176-f.height//2))
            txt(x+3,205,label,13)
        txt(20,242,"Close-up and native detail",14)
    return im
def main():
    sets=[
        (27,"Terminal Frame","terminal-frame", [panel()],[], "Honey-oak frame, copper inlay and muted green screen inset."),
        (28,"Tab Icons","tab-icons",[tabs(i) for i in range(3)],["Storage","Craft","Jobs"],"Crate, hammer and gear at a glance."),
        (29,"Settings Icons","settings-icons",[settings(i) for i in range(7)],["Up","Down","Add","Remove","Copy","Paste","Clock"],"Seven 9 x 9 controls for the Machine Link settings.")]
    for no,name,key,fs,labels,desc in sets:
        out=OUT/(key+".png")
        if out.exists() and "--replace" not in sys.argv:raise FileExistsError(out)
        strip=pack(fs);check=report(strip);assert check["alpha"]==[0,255] and not check["pure_black"] and check["size"]==[len(fs)*fs[0].width,fs[0].height]
        assert all(a.tobytes()==b.tobytes() for a,b in zip(fs,cuts(strip,fs[0].width,fs[0].height,len(fs),1)))
        assert len({f.tobytes() for f in fs})==len(fs)
        save(strip,out)
        save(card(no,name,fs,labels,desc),PRE/(f"{no}-{key}.png"))
        print(key,check["size"])
if __name__=="__main__":main()
