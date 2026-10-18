"""Offline asset/trajectory checks. This is NOT a Unity play-test or build check.

Run from any directory: python3 Tools/Validation/validate_prism_challenge.py
Optional C# syntax parsing: add tree-sitter + tree-sitter-c-sharp to PYTHONPATH.
No project dependencies are installed or changed by this script.
"""
from pathlib import Path
import math
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
FEATURE = ROOT / "Assets/PrismChallenge"
results = []


def check(condition, message):
    if not condition:
        raise AssertionError(message)
    results.append("PASS " + message)


def number(source, key):
    return float(re.search(r"^  " + re.escape(key) + r": ([\d.]+)$", source, re.M)[1])


def simulate(start, target, speed, jump, gravity, fall_multiplier, acceleration, dash=False):
    """Conservative point-foot approximation of the shipped controller.

    Includes a double jump, optional real dash deceleration/frozen Y, and air braking.
    It does not implement Unity capsule contacts, input timing, or dimension overlap.
    """
    x, y = start
    vx = 0.0
    vy = jump
    dt = .01
    target_left, target_right, target_top = target
    old_y = y
    for step in range(350):
        t = step * dt
        if dash and .23 <= t < .68:
            dash_time = t - .23
            vx = 16 * (1 - .65 * dash_time / .45)
            vy = 0
        else:
            if dash and step == 68:
                vx = vy = 0
            desired = speed if x < target_left + 1.1 else 0
            rate = acceleration if desired else 5
            vx = desired if not acceleration else vx + max(-rate * dt, min(rate * dt, desired - vx))
            if step == 33 and not dash:
                vy = jump
            vy -= gravity * (fall_multiplier if vy < 0 else 1) * dt
        x += vx * dt
        y += vy * dt
        if vy < 0 and old_y >= target_top >= y and target_left + .05 <= x <= target_right - .05:
            return x, t
        old_y = y
        if y < -5:
            break
    return None


def main():
    visuals = (FEATURE / "Runtime/PrismChallengeVisuals.cs").read_text()
    room = (FEATURE / "Runtime/PrismChallengeRoom.cs").read_text()
    specs = re.findall(r'Island\(root\.transform, ([\d.f]+), ([\d.f]+), ([\d.f]+), (-?\d+), "([^"]+)"\)', visuals)
    islands = [(float(a.rstrip("f")), float(b.rstrip("f")), float(c.rstrip("f")), int(w), name) for a, b, c, w, name in specs]
    check(len(islands) == 8, "Eight authored islands are present")
    check(sum(i[3] >= 0 for i in islands) == 4, "Four phase-specific islands use existing dimension logic")
    check(all(right > left and right - left >= 4.5 for left, right, _, _, _ in islands), "All landing islands have generous usable widths")
    check(all(islands[i + 1][2] - islands[i][2] <= 1.5 for i in range(7)), "No ascent exceeds 1.5 world units per jump")
    for scene_name in ["PaintedDimension", "SampleScene"]:
        scene = (ROOT / f"Assets/Scenes/{scene_name}.unity").read_text()
        speed, jump, fall = (number(scene, key) for key in ["moveSpeed", "jumpForce", "fallGravityMultiplier"])
        gravity = 9.81 * 4
        for acceleration in [0, number(scene, "acceleration")]:
            for i in [0, 1, 2, 4, 5, 6]:
                source = islands[i]
                destination = islands[i + 1]
                landed = simulate((source[1] - .8, source[2] + .04), destination[:3], speed, jump, gravity, fall, acceleration)
                check(landed is not None, f"Trajectory approximation: {scene_name}, island {i + 1} → {i + 2}, acceleration={acceleration:g}")
            landed = simulate((31.2, 3.04), islands[4][:3], speed, jump, gravity, fall, acceleration, dash=True)
            check(landed is not None, f"Jump/dash trajectory approximation: {scene_name}, acceleration={acceleration:g}")
    guids = {}
    for meta in ROOT.joinpath("Assets").rglob("*.meta"):
        match = re.search(r"^guid: (\w+)", meta.read_text(errors="replace"), re.M)
        if match:
            guids.setdefault(match[1], []).append(meta)
    for path in [FEATURE] + list(FEATURE.rglob("*")):
        if path.suffix == ".meta":
            continue
        meta = path.with_name(path.name + ".meta")
        check(meta.is_file(), f"Stable Unity metadata: {path.relative_to(ROOT)}")
        guid = re.search(r"^guid: (\w+)", meta.read_text(), re.M)[1]
        check(len(guids[guid]) == 1, f"Unique Unity GUID: {path.name}")
    material = (FEATURE / "Resources/PrismChallenge/PrismUnlit.mat").read_text()
    shader_guid = re.search(r"^guid: (\w+)", (FEATURE / "Resources/PrismChallenge/PrismUnlit.shader.meta").read_text(), re.M)[1]
    check(shader_guid in material, "Trail material references the packaged shader")
    sprite_material = (FEATURE / "Resources/PrismChallenge/PrismSprites.mat").read_text()
    existing_material = (ROOT / "Assets/Art/Painted/PaintedSprites.mat").read_text()
    shader = re.search(r"m_Shader: (.+)", sprite_material)[1]
    check(shader in existing_material, "Sprites use the project's established URP sprite shader")
    check("SetCheckpoint(" not in room and "RestoreHealth(" not in room and "TryTakeDamage(" not in room, "Challenge never replaces main checkpoints, heals or damages the player")
    check("UnityEngine.Random" not in visuals and "Random.Range" not in visuals, "Challenge art does not perturb shared game randomness")
    check("follow.minX = originalMinX" in room and "follow.maxX = originalMaxX" in room, "Explicit camera restoration exists")
    check("Run.Finished) ReturnToSafePad" in room, "Falling after completion returns to the podium")
    try:
        from tree_sitter import Language, Parser
        import tree_sitter_c_sharp
    except ImportError:
        results.append("SKIP C# syntax parser unavailable; install it outside the project if desired")
    else:
        parser = Parser(Language(tree_sitter_c_sharp.language()))
        for path in sorted(FEATURE.rglob("*.cs")) + [ROOT / "Tools/Validation/Editor/PrismChallengeChecks.cs"]:
            tree = parser.parse(path.read_bytes())
            check(not tree.root_node.has_error, f"C# syntax: {path.relative_to(ROOT)}")
    print("\n".join(results))
    print(f"\n{sum(line.startswith('PASS') for line in results)} offline checks passed. Unity execution is still required.")


if __name__ == "__main__":
    try:
        main()
    except (AssertionError, ValueError, KeyError, TypeError) as error:
        print("\n".join(results))
        print("FAIL", error, file=sys.stderr)
        sys.exit(1)
