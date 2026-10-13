#!/usr/bin/env python3
"""Expand the source snapshot and regenerate original placeholder textures."""
from pathlib import Path
import base64
import io
import json
import re
import tarfile

ROOT = Path(__file__).resolve().parents[1]
encoded = (ROOT / "import/source_text.tar.gz.b64").read_text(encoding="ascii")
with tarfile.open(fileobj=io.BytesIO(base64.b64decode(encoded)), mode="r:gz") as archive:
    for entry in archive:
        rel = Path(entry.name)
        if not entry.isfile() or rel.is_absolute() or ".." in rel.parts:
            raise RuntimeError(f"Unsafe source archive member: {entry.name}")
        dest = (ROOT / rel).resolve()
        if not dest.is_relative_to(ROOT.resolve()):
            raise RuntimeError("Path traversal")
        dest.parent.mkdir(parents=True, exist_ok=True)
        with archive.extractfile(entry) as source, dest.open("wb") as out:
            out.write(source.read())

# PNGs are generated independently so no extracted Tarkov artwork is needed.
from PIL import Image, ImageDraw, ImageFont
DATA = json.loads((ROOT / "DesignData.json").read_text(encoding="utf-8"))
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
try:
    font = ImageFont.truetype(FONT, 19)
except OSError:
    font = ImageFont.load_default()
outdir = ROOT / "Textures/TarkovInjectors"
outdir.mkdir(parents=True, exist_ok=True)
palette = [(78,174,210),(140,208,97),(233,177,75),
           (203,119,164),(190,195,206),(118,158,215)]
for index, stim in enumerate(DATA):
    key = stim["key"]
    label = stim["label"]
    img = Image.new("RGBA", (128,128), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    tint = palette[index % len(palette)]
    draw.rounded_rectangle((32,30,96,99), radius=12, fill=(25,33,42,242),
                           outline=(216,224,230,255), width=3)
    draw.rounded_rectangle((43,47,85,85), radius=6, fill=tint+(240,))
    draw.rounded_rectangle((56,12,72,34), radius=3,
                           fill=(198,209,219,255), outline=(55,65,71), width=2)
    draw.rectangle((60,3,68,15), fill=(203,213,217,255))
    draw.rectangle((53,99,75,113), fill=(182,192,201,255))
    draw.rectangle((47,112,81,120), fill=(174,189,197,255))
    abbr = re.sub(r"[^A-Z0-9]", "", label.upper())[:4]
    bounds = draw.textbbox((0,0), abbr, font=font)
    draw.text(((128-(bounds[2]-bounds[0]))//2,56), abbr,
              font=font, fill=(17,29,41,255))
    img.save(outdir / f"{key}.png")
(outdir / "InjectButton.png").write_bytes((outdir / "SJ6.png").read_bytes())
print(f"Imported source and generated {len(DATA)+1} original placeholder icons.")
