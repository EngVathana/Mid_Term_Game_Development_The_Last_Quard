# The Last Guard

A single-player, first-person castle defense game created for the Year 3, Term 1 Game Development midterm project.

Defend a medieval castle against increasingly difficult waves of Skeletons, Devils, and Golems. Use your rifle to protect the walls and keep the castle standing through all three waves.

## Objective

Eliminate every enemy in all three waves while protecting the castle. Complete the final wave with the castle still standing to win.

## Rules

- Start in daytime preparation mode. Hold **Z for 1.5 seconds** to begin the first wave.
- Combat takes place at night. Waves 2 and 3 begin automatically after the previous wave is cleared, following a short delay.
- Enemies approach the castle, attack blocking walls or gates, and can target the player when nearby.
- Walls and gates share a defense health pool. When it reaches zero, the defensive wall and gate objects are destroyed.
- The castle has its own health pool. If the castle reaches zero HP, the match ends in **Defeat**.
- If the player reaches zero HP, movement and shooting are disabled during a **10-second respawn countdown**. Respawning restores full player HP and a full magazine. Player death alone does not end the match; the castle must survive while the player is down.
- The rifle has a **40-round magazine**. Press **R** to reload, or let it reload automatically when empty. There is no reserve-ammunition limit in the current implementation.
- A wave is complete after spawning finishes and all enemies belonging to that wave are defeated.
- Use **PLAY AGAIN** after Victory or Defeat to restart, or **MAIN MENU** to return to the home screen.

### Wave configuration

The saved game scene currently uses these enemy counts:

| Wave | Skeletons | Devils | Golems | Total |
| 1 | 10 | 5 | 1 | 16 |
| 2 | 15 | 10 | 3 | 28 |
| 3 | 25 | 15 | 5 | 45 |

## Controls

| Input | Action |
| **W / A / S / D** or **arrow keys** | Move |
| **Mouse** | Look around and aim |
| **Hold left mouse button** | Shoot continuously |
| **R** | Reload |
| **Hold Z** | Start the first wave during preparation |
| **Left mouse button on menu buttons** | Start, restart, or return to the main menu |

The cursor locks during gameplay and becomes available on the home and end screens. The current game does not implement a dedicated pause-menu control.

## Unity version and requirements

- **Unity Editor:** `6000.3.8f1` (Unity 6.3), as recorded in `ProjectSettings/ProjectVersion.txt`.
- **Rendering:** Universal Render Pipeline (URP).
- **Input:** Keyboard and mouse.
- **Development tools:** Unity Hub and the specified Unity Editor version.
- **Internet connection:** Needed for the initial Unity package restore.

Package dependencies are declared in `Packages/manifest.json`. Use the committed project files and let Unity restore those dependencies.

## Setup

1. Install **Unity Hub** and **Unity Editor 6000.3.8f1**. Install the build-support module for your intended target platform if you plan to create a build.
2. Clone the repository, or download and extract its source archive:

   ```bash
   git clone https://github.com/EngVathana/Mid_Term_Game_Development_The_Last_Quard.git
   ```

3. In Unity Hub, choose **Add project from disk** and select the repository folder containing `Assets`, `Packages`, and `ProjectSettings`.
4. Open it with **6000.3.8f1** and wait for asset importing and package restoration to finish.
5. Open **`Assets/Scenes/SampleScene.unity`**.

Keep asset `.meta` files with their assets so Unity can preserve scene and prefab references.

## Play in the Unity Editor

1. Open `Assets/Scenes/SampleScene.unity`.
2. Press the Editor's **Play** button.
3. Click **START GAME** on the home screen.
4. Use the daytime preparation period to get into position.
5. Hold **Z** until the progress bar fills to start the first wave.
6. Defend the castle through all three waves. Monitor player HP, castle/wall health, ammunition, and the remaining-enemy counter.
7. After Victory or Defeat, select **PLAY AGAIN** or **MAIN MENU**.
8. Press the Editor's **Play** button again to stop the session.

## Create a playable build

1. Open **File > Build Profiles** in Unity.
2. Select the intended platform and install its build-support module if necessary.
3. Confirm that **`Assets/Scenes/SampleScene.unity`** is enabled in the scene list.
4. Choose **Build** and write the output to a separate build folder.
5. Test the resulting build before sharing it.
6. Upload the complete build archive and replace the download placeholder above.

For a Windows build, distribute the executable together with its accompanying data folder and other generated runtime files. Players should extract the complete archive before launching the `.exe`; distributing only the executable is insufficient.

## Play a downloaded build

1. Download the build using the link at the top of this README once it is available.
2. Extract the entire archive.
3. Launch the application for the build's stated platform.
4. Click **START GAME**, then follow the controls and wave instructions above.

Unity Editor is not required to play a standalone build.

## Team members and contributions

Team names were supplied by the team. Contribution summaries are based on repository commit history and should be confirmed before submission.

| Team member / repository identity | Contributions recorded in Git history |
| **Eng Vathana** — `EngVathana` | Game flow and GameManager; enemy behavior and spawn configuration; wall/castle health UI; sensitivity and balance adjustments; code cleanup and integration. |
| **Samnang Rithybot** — `j4meee` | Player health UI; ammo reset on respawn; respawn and crosshair fixes; castle collision setup; spawn-location and ammunition adjustments; integration. |
| **Kim Sambath** — `Sambath` | Automatic shooting; bullet tracers and muzzle flash; crosshair changes; related gameplay updates and script cleanup. |
| **Hem Eamsocheat** — `HEM EAMSOCHEAT` | Environment and level design; asset additions; tree clusters and straight tree borders; clearing the castle tile. |

## Asset references and credits

| Asset | Creator / source | Use in this project |
| [KayKit — Medieval Hexagon Pack](https://kaylousberg.itch.io/kaykit-medieval-hexagon) | Kay Lousberg | Castle, stone walls, gate, and medieval environment assets. The included pack license identifies these assets as CC0. |
| [Free Pack — Gun](https://assetstore.unity.com/packages/3d/props/guns/free-pack-gun-308387) | PolyOne Studio | AK-style rifle model and associated textures/materials. |
| [War FX](https://assetstore.unity.com/packages/vfx/particles/war-fx-5669) | Jean Moreno | Weapon muzzle flashes and supporting particle-effect assets/scripts. |
| TextMesh Pro | Unity package/resources included in the project | Menu text, HUD text, and health/ammunition labels. |
| Skeleton, DevilTall1, and Golem models/animations | **TODO: Add the original pack name, creator, source URL, and license.** | Enemy characters and animations in `Assets/Monster/`. |
| Terrain grass texture and crosshair artwork | **TODO: Confirm whether team-created or externally sourced; add creator/source as applicable.** | Terrain appearance and aiming UI. |

Local attribution files:

- [KayKit license](Assets/Treature/KayKit_Medieval_Hexagon_Pack_1.0_FREE/License.txt)
- [War FX readme](<Assets/JMO Assets/WarFX/!WarFX Readme.txt>)

Third-party assets retain their original licenses. Inclusion in this repository does not make every asset CC0 or grant unrestricted redistribution rights.

## Tutorial references

The team supplied the following YouTube references. Titles, authors, and exact feature attribution that could not be verified remain marked for confirmation.

| Tutorial title | Author / channel | URL | Feature learned or adapted |
| --- | --- | --- | --- |
| Tutorial reference 1 — title to confirm | TODO: Confirm channel | [Watch reference 1](https://youtu.be/acsV83gkxqQ?si=hkAXxgwMiE0oApoz) | TODO: Confirm the feature learned or adapted |
| Tutorial reference 2 — title to confirm | TODO: Confirm channel | [Watch reference 2](https://youtu.be/kAx5g9V5bcM?si=3znTIbJPmgWZGgeP) | TODO: Confirm the feature learned or adapted |
| FPS Full Game Tutorial · Unity · Part 1 — Basic Movement | TODO: Confirm channel | [Watch Basic Movement](https://www.youtube.com/watch?v=swOfmyJvb98) | First-person movement reference |

These links are listed as tutorial references. A separate gameplay recording and playable build download still need to be supplied.
