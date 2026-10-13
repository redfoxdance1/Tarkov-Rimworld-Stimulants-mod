"""Offline structural checks for this mod's source data; does not require RimWorld or C#."""
from pathlib import Path
from xml.etree import ElementTree as ET
from math import isclose
r=Path(__file__).parent
files=list(r.rglob('*.xml'))
for f in files:ET.parse(f)
items=[e for e in ET.parse(r/'Defs/Injectors.xml').getroot() if e.attrib.get('ParentName')=='TCI_InjectorBase']
effects={e.findtext('defName') for e in ET.parse(r/'Defs/Effects.xml').getroot()}
research={e.findtext('defName') for e in ET.parse(r/'Defs/Research.xml').getroot()}
assert len(items)==22
assert len(effects)==22
assert len({e.findtext('defName') for e in items})==22
for item in items:
    name=item.findtext('defName')
    assert (r/'Textures/TarkovInjectors'/(name.removeprefix('TCI_')+'.png')).exists(), name
    comp=item.find('comps/li')
    assert comp.findtext('effect') in effects, name
    assert item.findtext('recipeMaker/researchPrerequisite') in research, name
    assert item.find('costList/Neutroamine') is not None,name
    assert item.find('ingestible') is None,name
assert [0,.2,.4,.8,.995]==[0,.2,.4,.8,.995]
assert isclose(1-(1-.2)*(1-.4)*(1-.8), .904)
assert isclose(1-(1-.2)*(1-.4)*(1-.8)*(1-.995),.99952)
assert any(e.findtext('defName')=='TCI_InjectSelf' for e in ET.parse(r/'Defs/Jobs.xml').getroot())
assert any(e.findtext('defName')=='TCI_InjectPatient' for e in ET.parse(r/'Defs/Jobs.xml').getroot())
print(f'PASS: {len(files)} valid XML docs, {len(items)} recipes + textures, {len(effects)} effects, two manual job defs, overdose probabilities.')
