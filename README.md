# VR Lab — KMUTNB

VR experience of a laboratory (electronics/science), developed in Unity, aiming to recreate an interactive lab environment in virtual reality.

## Overview

This project recreates a laboratory (tables, instruments, equipment) in a 3D environment, with the goal of integrating VR interactions (grabbing objects, moving around, manipulating instruments).

## Tech stack

- **Engine**: Unity 6 (6000.5.0f1)
- **VR**: OpenXR + XR Interaction Toolkit
- **Rendering**: Universal Render Pipeline (URP)
- **Versioning**: Git + Git LFS
  
##  Project structure

```
Assets/
│── Scenes/
├── Scripts/
├── Prefabs/
├── Materials/
├── Models/
├── XR/
├── XRI/                # XR Interaction Toolkit (package/samples)
├── TextMesh Pro/        # External package
└── ...                  # Other imported package folders
```

## Setup / Installation

### Prerequisites

- [Unity Hub](https://unity.com/download) with Unity **6000.5.0f1** (or a compatible version) installed
- [Git](https://git-scm.com/) and [Git LFS](https://git-lfs.github.com/) installed
- A VR headset compatible with OpenXR (Meta Quest, Valve Index, HTC Vive, etc.) with its runtime installed
  
### Clone the project

```bash
git lfs install
git clone git@github.com:samysame/vr-lab-kmutnb.git
```
> Make sure Git LFS is installed **before** cloning, otherwise large files (3D models, textures) won't be retrieved correctly.

### Open in Unity

1. Open **Unity Hub**
2. Click **Add** > select the cloned project folder
3. Open the project with the Unity version indicated above
4. Let Unity reimport all assets (can take several minutes on first launch)
   
### Configure UnityYAMLMerge (once per machine)

This project uses `UnityYAMLMerge` to intelligently merge `.unity` (scene) and `.prefab` files in case of a Git conflict, rather than a classic text merge that can corrupt these files.

This configuration is not propagated automatically via `git pull`/`git clone` (it depends on Unity's install path on each machine). After cloning the repo, run the script matching your OS once:

**Windows**
```cmd
setup-git-merge.bat
```

**macOS / Linux / Git Bash**
```bash
chmod +x setup-git-merge.sh
./setup-git-merge.sh
```

Verify it's configured correctly:
```bash
git config --local --get merge.unityyamlmerge.driver
```

### (Optional) Aliases (`aliases.sh`)

The repo contains an `aliases.sh` file with a few handy aliases for this project.

#### Installation

```bash
source aliases.sh
```

To load them automatically on every new session, add this line to the end of your `~/.bashrc` (or `~/.zshrc`):
```bash
source /path/to/the/repo/aliases.sh
```

#### Available aliases

| Alias | Command |
|-------|---------|
| `gst` | `git status`   |
| `gco` | `git checkout`   |
| `gcob` | `git checkout -b`   |
| `gbr` | `git branch`   |
| `gcm` | `git commit -m`   |
| `gad` | `git add`   |
| `gp` | `git push`   |
| `gcp` | `git cherry-pick`   |

## Git workflow (multiple machines)

This project is worked on from multiple machines (on-site/remote). To avoid conflicts:
- **Before starting work**: `git pull`
- **Before switching machines**: commit + `git push`, even for unfinished work (`git commit -m "wip: ..."`)

### Branching strategy

Work is done on feature branches, not directly on `main`. `main` should always stay in a clean, working state.

```bash
git checkout -b feature/my-feature   # Create and switch to a new branch
# ... work, commit ...
git push -u origin feature/my-feature
```

Once the feature is ready, open a Pull Request on GitHub to merge it into `main`. This allows for review before integration and avoids conflicts piling up directly on the main branch. Delete the branch once it's merged to keep things tidy.

### Useful Git commands
```bash
git commit --amend             # Modify the most recent commit
git pull --rebase origin main  # Update your branch with remote changes, keeping a clean history
git restore <file>             # Discard changes in your working directory
git reset HEAD^                # Undo the last commit, keeping your files
git log                        # View the history of commits on the current branch
git diff                       # See unstaged changes
git stash                      # Stash modified tracked files out of the way
git stash pop                  # Restore your most recently stashed changes
```

### Important Unity settings (check on each machine)

- `Edit > Project Settings > Editor > Asset Serialization` → **Force Text**
- `Edit > Project Settings > Editor > Version Control` → **Visible Meta Files**
These settings ensure scenes/prefabs stay readable in Git diffs (text YAML format rather than binary).

## Notes

- Large files (`.fbx`, `.png`, `.psd`, `.wav`, `.mp4`, etc.) are handled via **Git LFS** — see `.gitattributes` for the full list of tracked extensions.
- `Library/`, `Logs/`, `Temp/`, `obj/`, `UserSettings/` are excluded from versioning (`.gitignore`) since Unity regenerates them automatically.

## Roadmap

- [x] Project cleanup and structuring
- [x] Git / Git LFS setup
- [ ] XR Interaction Toolkit integration (VR camera, controllers)
- [ ] Interactions with lab instruments
- [ ] VR teleportation / movement
- [ ] VR UI
