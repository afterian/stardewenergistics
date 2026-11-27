"""Native Maru device attachments; states ordered as in the art catalogue."""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/"tools/pixel"))
from pixel_pipeline import save,load,pack,cuts,report
P={'.':None,'o':'#49374f','p':'#775782','l':'#aa82a4','d':'#58445d',
   'c':'#b66c47','h':'#e3aa68','s':'#65434f','g':'#98734e',
   'a':'#d9a155','t':'#80bcb1','b':'#bfdfaf','r':'#cd6d62','G':'#f1ca79'}
SPECS=[
(24,'machine-link','Machine Link',(7,1),
 ('..oooo..','.olllpo.','ohpdddo.','ocpdXdo.','ocpdYdo.','opppppo.','.oocc o..'.replace(' ',''),'..oooo..'),
 ('Unconfigured','Idle','Working','Missing input'),('ad','td','bt','rd'),
 'Copper clip. Amber setup, teal activity, red missing input.'),
(25,'shipping-link','Shipping Link',(7,1),
 ('.oooo...','ohhc ooo.'.replace(' ',''),'ocdXXdo.','ohXYXXo.','ohXYXXo.','ocdXXdo.','oho ccho.'.replace(' ',''),'.ooooo..'),
 ('Idle','Shipping'),('gc','Gh'),'Copper clamp with a coin that glows during shipping.'),
(26,'power-feed','Power Feed',(5,7),
 ('..oooo...','.oddddoo.','olllYlppo','opdYYddpo','opddYYYpo','ocddYdXpo','ocpppccpo','.oooooooo'),
 ('Off','Charging'),('sg','tb'),'Base junction with a bolt and charging indicator.')]
for number,stem,title,at,rows,labels,states,note in SPECS:
    output=ROOT/'artwork/exports'/f'{stem}.png'
    preview=ROOT/'artwork/review/maru-machines'/f'{number}-{stem}.png'
    if output.exists() or preview.exists():raise FileExistsError(stem)
    assert len({len(row) for row in rows})==1,(stem,[len(row) for row in rows])
    frames=[]
    for colors in states:
        art=Image.new('RGBA',(16,16));d=ImageDraw.Draw(art)
        swatches=P|dict(zip('XY',(P[c] for c in colors)))
        for y,row in enumerate(rows):
            for x,c in enumerate(row):
                if c!='.':d.point((at[0]+x,at[1]+y),fill=swatches[c])
        frames.append(art)
    assert len({f.tobytes() for f in frames})==len(labels)
    assert len({f.getchannel('A').tobytes() for f in frames})==1
    strip=pack(frames);q=report(strip)
    assert strip.size==(len(labels)*16,16) and q['alpha']==[0,255] and not q['pure_black']
    assert all(a.tobytes()==b.tobytes() for a,b in zip(frames,cuts(strip,16,16,len(labels),1)))
    card=Image.new('RGBA',(760,345),'#f6e5c6');d=ImageDraw.Draw(card)
    def txt(x,y,s,k=14):d.text((x,y),s,font=ImageFont.load_default(size=k),fill='#4d354c')
    txt(20,14,'STARDEW ENERGISTICS   /   MARU TECH')
    txt(20,34,f'{number}  {title}',27);txt(20,73,note,15)
    for i,(art,label) in enumerate(zip(frames,labels)):
        x=20+i*150
        d.rectangle((x,111,x+128,239),fill='#d8b78f')
        card.alpha_composite(art.resize((128,128),Image.Resampling.NEAREST),(x,111))
        txt(x,254,label)
    d.rectangle((634,111,736,239),fill='#ddc19b')
    card.alpha_composite(frames[-1],(675,168));txt(655,253,'Native 1x')
    txt(20,301,'Native RGBA overlay  /  16 x 16 px per frame  /  game order left to right',15)
    save(strip,output);save(card,preview)
    assert load(output).tobytes()==strip.tobytes() and load(preview).size==(760,345)
    print(output.name,strip.size,preview.name)