"""Extract downloaded Higgsfield sheets locally; no generation or network calls."""
from pathlib import Path
import json, re
from PIL import Image

ROOT = Path(__file__).resolve().parent
names = re.findall(r"\['([^']+)',", (ROOT.parents[1] / 'game.js').read_text(encoding='utf-8'))[:30]
maps = ['초록 사잇길','쌍둥이 연못','돌담 미로','억새 평원','메마른 밭','비밀 정원','물길 교차로','바위 협곡','나선 농원','네잎 분지','달빛 습지','왕의 요새']
effects = ['seed','honey','ribbon','fire','vine','frost','lightning','star','petals','spores','mint','rainbow']
manifest = {'generator':'Higgsfield MCP / nano_banana_2','characters':[], 'backgrounds':[], 'effects':[]}

def key(image):
    image = image.convert('RGBA')
    image.putdata([(r,g,b,0 if r>180 and b>180 and g<100 else a) for r,g,b,a in image.getdata()])
    return image

def cell(image, col, row, cols, rows):
    w,h=image.size
    return image.crop((round(col*w/cols),round(row*h/rows),round((col+1)*w/cols),round((row+1)*h/rows)))

for category in ['characters','backgrounds','effects']:
    (ROOT/category).mkdir(exist_ok=True)

chars=key(Image.open(ROOT/'source/characters.png'))
for i,name in enumerate(names):
    part=cell(chars,i%6,i//6,6,5)
    # Preserve the shared cell instead of rescaling each character independently.
    canvas=Image.new('RGBA',(384,384))
    canvas.alpha_composite(part,((384-part.width)//2,384-part.height))
    filename=f'characters/{i:02d}.png'
    canvas.save(ROOT/filename)
    manifest['characters'].append({'id':i,'name':name,'file':filename,'width':384,'height':384})

background=Image.open(ROOT/'source/backgrounds.png')
for i,name in enumerate(maps):
    part=cell(background,i%4,i//4,4,3)
    filename=f'backgrounds/{i:02d}.png'
    part.save(ROOT/filename)
    manifest['backgrounds'].append({'id':i,'name':name,'file':filename,'width':part.width,'height':part.height})

fx=key(Image.open(ROOT/'source/effects.png'))
# The model supplied a dark green backdrop as well as magenta dividers.
fx.putdata([(r,g,b,0 if r<40 and g<60 and b<40 else a) for r,g,b,a in fx.getdata()])
for i,name in enumerate(effects):
    frames=[]
    for j in range(4):
        part=cell(fx,j,i,4,12)
        # Trim the generated grid dividers, including their antialiased edges.
        part=part.crop((6,6,part.width-6,part.height-6))
        canvas=Image.new('RGBA',(384,384))
        canvas.alpha_composite(part,((384-part.width)//2,(384-part.height)//2))
        filename=f'effects/{name}-{j}.png'
        canvas.save(ROOT/filename)
        frames.append(canvas)
    strip=Image.new('RGBA',(1536,384))
    for j,frame in enumerate(frames): strip.alpha_composite(frame,(j*384,0))
    strip.save(ROOT/f'effects/{name}-strip.png')
    manifest['effects'].append({'id':name,'file':f'effects/{name}-strip.png','frames':4,'frameWidth':384,'frameHeight':384,'previewFps':8})

(ROOT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
html='''<!doctype html><meta charset="utf-8"><title>고구마 8bit 에셋</title><style>body{background:#202d26;color:#fff4c7;font:16px system-ui;margin:32px}section{display:flex;flex-wrap:wrap;gap:16px}figure{margin:0;padding:12px;background:#364d3d;text-align:center;border:2px solid #63864d}img{width:144px;height:144px;object-fit:contain;image-rendering:pixelated}.map{width:240px;height:240px}.fx{width:144px;height:144px;background-size:576px 144px;image-rendering:pixelated;animation:play .5s steps(4) infinite}@keyframes play{to{background-position:-576px 0}}h1{font-size:24px}</style><h1>작은 고구마밭 · Higgsfield 에셋</h1><p>캐릭터 30종 · 배경 12종 · 스킬 이펙트 12종 / 4프레임. 배경의 지형 그림은 충돌 판정 데이터와 별개입니다.</p>'''
for category,title in [('characters','캐릭터'),('backgrounds','배경'),('effects','스킬 이펙트')]:
    html+=f'<h2>{title}</h2><section>'
    for item in manifest[category]:
        media=f'<div class="fx" style="background-image:url({item["file"]})"></div>' if category=='effects' else f'<img class="{"map" if category=="backgrounds" else ""}" src="{item["file"]}">'
        html+=f'<figure>{media}<figcaption>{item.get("name",item["id"])}</figcaption></figure>'
    html+='</section>'
(ROOT/'preview.html').write_text(html,encoding='utf-8')
print('Exported 30 characters, 12 backgrounds, 48 effect frames and 12 strips.')
