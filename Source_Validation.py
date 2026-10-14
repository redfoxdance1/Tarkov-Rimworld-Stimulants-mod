"""Static checks; compilation and gameplay require RimWorld 1.6."""
import re
from pathlib import Path
from xml.etree import ElementTree as ET
from math import isclose

r = Path(__file__).parent
files = list((r / "About").rglob("*.xml")) + list((r / "Defs").rglob("*.xml")) + list((r / "Patches").rglob("*.xml")) + list((r / "Compatibility").rglob("*.xml")) + [r / "LoadFolders.xml"]
for f in files:
    ET.parse(f)

items = [e for e in ET.parse(r / "Defs/Injectors.xml").getroot() if e.attrib.get("ParentName") == "TCI_InjectorBase"]
effects = {e.findtext("defName") for e in ET.parse(r / "Defs/Effects.xml").getroot()}
research = {e.findtext("defName") for e in ET.parse(r / "Defs/Research.xml").getroot()}
jobs = {e.findtext("defName") for e in ET.parse(r / "Defs/Jobs.xml").getroot()}
assert len(items) == len(effects) == 22
assert len({e.findtext("defName") for e in items}) == 22
skills = {"TCI_Basic": 4, "TCI_Combat": 6, "TCI_Advanced": 8, "TCI_Experimental": 10}
for item in items:
    name = item.findtext("defName")
    assert (r / "Textures/TarkovInjectors" / (name.removeprefix("TCI_") + ".png")).exists(), name
    assert item.findtext("comps/li/effect") in effects, name
    tier = item.findtext("recipeMaker/researchPrerequisite")
    assert tier in research and tier in skills, name
    assert int(item.findtext("recipeMaker/skillRequirements/Intellectual")) == skills[tier], name
    assert item.find("costList/Neutroamine") is not None, name
    assert item.find("ingestible") is None, name

core = (r / "Source/InjectorCore.cs").read_text(encoding="utf-8")
effects_code = (r / "Source/InjectorHediffs.cs").read_text(encoding="utf-8")
pickup = (r / "Source/FloatMenuOptionProvider_InjectorPickup.cs").read_text(encoding="utf-8")
match = re.search(r"float\[\] Risks\s*=\s*\{([^}]*)\}", core)
assert match, "Could not locate C# overdose table"
values = [float(v.strip().rstrip("f")) for v in match.group(1).split(",")]
assert values == [0, .2, .4, .8, .995], values
assert isclose(1 - (1-.2)*(1-.4)*(1-.8), .904)
assert "originalObdolbos" in core and "pawn.Kill(null)" in core
assert {"TCI_InjectSelf", "TCI_InjectPatient"} <= jobs
assert "public override void CompTickInterval(int delta)" in core
assert effects_code.count("public override void TickInterval(int delta)") == 2
assert "Severity += stabilized ? -0.005f : 0.01f" in effects_code
assert "FloatMenuOptionProvider_InjectorPickup : FloatMenuOptionProvider" in pickup
assert "clickedThing.TryGetComp<CompInjector>()" in pickup
assert "pawn.IsColonistPlayerControlled" in pickup
assert "JobDefOf.TakeInventory" in pickup
assert "job.checkEncumbrance = true" in pickup
assert "Dialog_Slider" in pickup
assert "requestedCount > maxCount" in pickup
# All three anti-bleeding effects must suppress bleeding without automatically tending injuries.
effect_nodes = {e.findtext("defName"): e for e in ET.parse(r / "Defs/Effects.xml").getroot()}
for name in ("Zagustin", "AHF1M", "Perfotoran"):
    node = effect_nodes["TCI_Effect_" + name]
    assert node.findtext("stages/li/totalBleedFactor") == "0", name
assert "<clotBleeding>" not in (r / "Defs/Effects.xml").read_text(encoding="utf-8")
assert "ClotWounds" not in effects_code and "injury.Tended(" not in effects_code

# Never complete interrupted injections or administer an item removed from inventory.
assert core.count("this.FailOn(() => !CanContinue())") == 2
assert core.count("pawn.inventory.innerContainer.Contains(Syringe)") == 2
assert "Patient.InBed()" in core
assert "WithProgressBarToilDelay" in core
assert (r / "LoadFolders.xml").read_text(encoding="utf-8").count('IfModActive="CETeam.CombatExtended"') == 1
ce = ET.parse(r / "Compatibility/CombatExtended/Patches/TCI_CE.xml").getroot()
assert len(ce.findall("Operation")) == 4

print(f"PASS: {len(files)} XML docs, {len(items)} injectors, research/skills, real overdose table, manual pickup, interval timers.")
