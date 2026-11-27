"""Assets 13–14: matched Maru cabinets with five native occupancy states."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from build_maru_machines import ROOT, P, core, N, save, pack
from pixel_pipeline import cuts

REVIEW = ROOT/"artwork/review/maru-machines"
EXPORT = ROOT/"artwork/exports"

def rack(count, power):
    im=core(0)
    d=ImageDraw.Draw(im)
    def r(box,color): d.rectangle(box,fill=P[color])
    r((3,7,11,24),"case")
    r((12,23,12,23),"edge")
    if not power:
        for i in range(4):
            y=8+i*4
            r((3,y,11,y+2),"outline")
            r((4,y+1,10,y+1),"screenOff")
            r((4,y+3,10,y+3),"lit" if i==3 else "edge")
            r((10,y+1,10,y+1),"dormant")
            if i < count:
                r((4,y+1,8,y+1),"metal")
                r((5,y+1,6,y+1),"key")
                r((10,y+1,10,y+1),"teal")
        r((4,24,5,24),"glint")
        r((10,24,10,24),"blue")
    else:
        for i in range(4):
            x=3+4*(i%2)
            y=9+7*(i//2)
            r((x,y,x+3,y+5),"outline")
            r((x+1,y+1,x+2,y+4),"screenOff")
            if i < count:
                r((x+1,y+1,x+1,y+1),"key")
                r((x+1,y+2,x+2,y+4),"dimscan")
                r((x+1,y+2,x+1,y+3),"lime")
        r((11,9,11,21),"outline")
        for i in range(4):
            y=20-i*3
            r((11,y-1,11,y),"scan" if i<count else "dormant")
        r((5,23,8,23),"edge")
        r((10,23,11,23),"warning" if count==0 else "teal")
    return im

def label(draw,x,y,txt,font):draw.text((x,y),txt,font=ImageFont.load_default(size=font),fill="#4d354c")

def review_card(title, frames, desc, unit):
    im=Image.new("RGBA",(672,330),"#f6e5c6")
    d=ImageDraw.Draw(im)
    label(d,22,14,"STARDEW ENERGISTICS   /   MARU TECH",14)
    label(d,22,34,title,27)
    label(d,22,79,desc,15)
    for i,art in enumerate(frames):
        x=22+i*126
        d.rectangle((x,109,x+111,263),fill="#d8b78f" if i else "#dcbc9b")
        im.alpha_composite(art.resize((64,128),N),(x+24,120))
        label(d,x+11,272,("Empty" if i==0 else str(i)+" "+unit+("s" if i!=1 else "")),14)
    label(d,22,303,"4x nearest-neighbor  /  16 x 32 native frames  /  left-to-right game order",14)
    return im

def wall_card(storage,power):
    im=Image.new("RGBA",(472,298),"#f6e5c6")
    d=ImageDraw.Draw(im)
    label(d,22,14,"STARDEW ENERGISTICS   /   MARU TECH",14)
    label(d,22,34,"Core + storage + power",27)
    d.rectangle((22,80,449,243),fill="#d8b78f")
    for i,art in enumerate((storage,core(1),power)):
        x=140+i*64
        im.alpha_composite(art.resize((64,128),N),(x,107))
        label(d,x+3,245,("Storage","Core","Power")[i],14)
    label(d,22,276,"One tile apart  /  4x preview  /  matched cabinet and feet",14)
    return im

def main():
    batches=[("storage-rack","13-storage-rack.png","13  Storage Rack","Four disk bays with teal occupancy lights.","disk",False),
             ("power-rack","14-power-rack.png","14  Power Rack","Four removable battery bays and a vertical charge meter.","pack",True)]
    framesets=[]
    for stem,preview,title,desc,unit,is_power in batches:
        if any(path.exists() for path in (EXPORT/(stem+".png"), REVIEW/preview)):
            raise FileExistsError(stem)
        frames=[rack(i,is_power) for i in range(5)]
        assert len({item.tobytes() for item in frames})==5
        assert all(item.getchannel("A").tobytes()==core(0).getchannel("A").tobytes() for item in frames)
        sheet=pack(frames)
        assert sheet.size==(80,32) and sheet.mode=="RGBA"
        assert all(a.tobytes()==b.tobytes() for a,b in zip(frames,cuts(sheet,16,32,5,1)))
        framesets.append(frames)
    for (stem,preview,title,desc,unit,_),frames in zip(batches,framesets):
        save(pack(frames),EXPORT/(stem+".png"))
        save(review_card(title,frames,desc,unit),REVIEW/preview)
        with Image.open(EXPORT/(stem+".png")) as im:assert im.tobytes()==pack(frames).tobytes()
    save(wall_card(framesets[0][4],framesets[1][4]),REVIEW/"13-14-server-wall.png")
    print("Two 80x32 RGBA strips; five exact 16x32 states with shared Core alpha mask and baseline.")

if __name__=="__main__":main()