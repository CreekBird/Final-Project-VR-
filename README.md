# Tower Siege — AR Tower Defense

**Tower Siege** is a mobile **augmented-reality tower-defense game** built in **Unity 6** with **AR Foundation**. Scan a real-world surface, anchor a fantasy battlefield in AR, build towers on a grid, and defend against waves of monsters.

> Repository: https://github.com/CreekBird/Final-Project-VR-

---

## Features

- **AR plane detection & tap-to-place** battlefield, with a fallback "Place battlefield here" button that appears after 7 seconds if a surface is hard to find.
- **Three tower types** — Archer, Wizard, and Cannon — each with its own range, fire rate, and damage.
- **Wave-based combat** — animated Orc enemies follow a path toward your base; clear every wave to win.
- **Economy** — earn gold by defeating enemies, spend it on towers; lose lives when enemies reach the base.
- **Fantasy presentation** — low-poly 3D towers and monsters, a "blue stone & silver" UI theme, and a dusk-sky main menu.

## Requirements

- **Unity 6000.3.11f1** (Unity 6.3 LTS) or a compatible Unity 6 release.
- **AR Foundation** with **ARCore** (Android) or **ARKit** (iOS).
- An **ARCore-capable Android device** for deployment.

## Build & Run

```bash
git clone https://github.com/CreekBird/Final-Project-VR-.git
```

1. Open the project in **Unity Hub** with Unity 6000.3.11f1.
2. Switch the platform to **Android** (`File → Build Profiles → Android`).
3. Connect an ARCore-capable device and choose **Build And Run**.

## How to Play

1. From the main menu, tap **PLAY**.
2. Point the camera at a **well-lit, textured** flat surface until a plane is highlighted.
3. **Tap the plane** (or wait 7 seconds and tap **"Place battlefield here"**) to spawn the board.
4. Select a tower from the shop, then **tap a grid cell** to build it.
5. Tap **Send Wave** to begin each wave. Survive all waves to win.

## Project Structure

| Path | Contents |
|------|----------|
| `Assets/Scripts/` | Core game logic (GameManager, BattlefieldPlacer, TowerPlacer, WaveManager, UIManager, etc.) |
| `Assets/Art/` | Imported fantasy models, textures, skybox, and UI sprites |
| `Assets/Prefabs/` | Towers, enemies, and the battlefield prefab |
| `Assets/Scenes/` | `MainMenu` and `Game` scenes |
| `docs/` | LaTeX project report (`TowerSiege_Report.tex`) and compiled PDF |

## Asset Credits (all CC0 / free-to-use)

- **Enemies:** Quaternius — *Ultimate Monsters* (CC0)
- **Towers:** CraftPix — *Free Defence Tower 3D Low Poly Models*
- **UI:** Kenney — *UI Pack RPG Expansion* & *Game Icons* (CC0)
- **Skybox:** Poly Haven HDRI (CC0)

## Report

A short project report is included in [`docs/TowerSiege_Report.pdf`](docs/TowerSiege_Report.pdf) (source: `docs/TowerSiege_Report.tex`).
