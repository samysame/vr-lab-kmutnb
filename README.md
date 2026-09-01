# VR Lab — KMUTNB

VR experience of a laboratory (electronics/science), developed in Unity, aiming to recreate an interactive lab environment in virtual reality.

## Overview

This project recreates a laboratory (tables, instruments, equipment) in a 3D environment, with the goal of integrating VR interactions (grabbing objects, moving around, manipulating instruments).

## Tech stack

- **Engine**: Unity 6 (6000.5.0f1)
- **VR**: OpenXR + XR Interaction Toolkit
- **Rendering**: Universal Render Pipeline (URP)
- **Versioning**: Git + Git LFS
- **Simulation backend**: Python · FastAPI · ngspice (see `Backend/README.md`)
  
##  Project structure

```
Assets/
│── Backend/     # FastAPI + ngspice simulation service (see Backend/README.md)
│── Scenes/
├── Scripts/
├── Prefabs/
├── Materials/
├── Models/
├── XR/
├── XRI/                 # XR Interaction Toolkit (package/samples)
├── TextMesh Pro/        # External package
└── ...                  # Other imported package folders
```

## Setup / Installation

### Prerequisites

- [Unity Hub](https://unity.com/download) with Unity **6000.5.0f1** (or a compatible version) installed
- [Git](https://git-scm.com/) and [Git LFS](https://git-lfs.github.com/) installed
- A VR headset compatible with OpenXR (Meta Quest, Valve Index, HTC Vive, etc.) with its runtime installed
- Python 3.11+ and ngspice (for the simulation backend — see `Backend/README.md` for full setup)

Follow the [Meta Horizon tutorial](https://developers.meta.com/horizon/documentation/unity/unity-tutorial-hello-vr/) for a thorough setup of your environment.  
  
### Clone the project

```bash
git lfs install
git clone git@github.com:samysame/vr-lab-kmutnb.git
```
> Make sure Git LFS is installed **before** cloning, otherwise large files (3D models, textures) won't be retrieved correctly.
> 
> ```bash
> git lfs version
> git lfs status
> git lfs ls-files
> ```

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

### Important Unity settings (check on each machine)

- `Edit > Project Settings > Editor > Asset Serialization` → **Force Text**
- `Edit > Project Settings > Editor > Version Control` → **Visible Meta Files**
These settings ensure scenes/prefabs stay readable in Git diffs (text YAML format rather than binary).

### Set up the simulation backend (once per machine)

The circuit simulation logic runs as a separate local service, not inside Unity. See `Backend/README.md` for full setup instructions (Python venv, ngspice install, running the server).

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
git checkout -b feat/my-feature   # Create and switch to a new branch
# ... work, commit ...
git push -u origin feat/my-feature
```

Once the feature is ready, open a Pull Request on GitHub to merge it into `main`. This allows for review before integration and avoids conflicts piling up directly on the main branch. Delete the branch once it's merged to keep things tidy.

### Conventional branch naming

Purpose Prefixes — describe the intent of the work:

- `feature/` (or `feat/`): For new features (e.g., feature/add-login-page, feat/add-login-page)
- `bugfix/` (or `fix/`): For bug fixes (e.g., bugfix/fix-header-bug, fix/header-bug)
- `hotfix/`: For urgent fixes (e.g., hotfix/security-patch)
- `release/`: For branches preparing a release (e.g., release/v1.2.0)
- `chore/`: For non-code tasks like dependency, docs updates (e.g., chore/update-dependencies)

### Useful Git commands
```bash
git commit --amend             # Modify the most recent commit
git push --force-with-lease    # Force-push safely after a rebase/amend (fails if the remote has changes you don't have locally)
git pull --rebase origin main  # Update your branch with remote changes, keeping a clean history
git restore <file>             # Discard changes in your working directory
git reset HEAD^                # Undo the last commit, keeping your files
git log                        # View the history of commits on the current branch
git diff                       # See unstaged changes
git stash                      # Stash modified tracked files out of the way
git stash pop                  # Restore your most recently stashed changes
```

## Notes

- Large files (`.fbx`, `.png`, `.psd`, `.wav`, `.mp4`, etc.) are handled via **Git LFS** — see `.gitattributes` for the full list of tracked extensions.
- `Library/`, `Logs/`, `Temp/`, `obj/`, `UserSettings/` are excluded from versioning (`.gitignore`) since Unity regenerates them automatically.

### Git LFS — Large Files

On 2026-08-28, several large files (`dg822 pro.fbx` ~65MB, `3DModel.jpg` ~50MB, `Lab.skp` ~5.8MB) had been committed as regular Git blobs instead of LFS pointers — most likely because `git lfs install` hadn't been run on the machine that originally pushed them, or because `.gitattributes` wasn't yet configured for these extensions at the time.

**Fix applied:**
```bash
git lfs track "*.fbx" "*.jpg" "*.png" "*.skp"
git lfs migrate import --include="*.fbx,*.jpg,*.png,*.skp" --include-ref=refs/heads/main
git push --force origin main
```

This operation **rewrote the repo's history** (new commit hashes) to convert all affected blobs into LFS pointers, across the entire history — not just the latest commit.

#### If you had already cloned this repo before 2026-08-28

Your local history no longer matches `origin/main`. To resync:

```bash
git fetch origin
git reset --hard origin/main
```

This discards any uncommitted local changes — commit or stash your work first. Alternatively, the simplest fix is to re-clone the repo from scratch.

#### Checking that a file is properly tracked by LFS

```bash
git lfs ls-files
```

#### Detecting untracked large binaries (run periodically)

```bash
git rev-list --objects --all | \
  git cat-file --batch-check='%(objecttype) %(objectname) %(objectsize) %(rest)' | \
  awk '$1=="blob" && $3 > 1000000 {print}'
```

If this command returns any results, a large file was committed without going through LFS — check `.gitattributes` and run `git lfs migrate import` again if needed.


#### Git LFS Pull

After a `git clone/pull` or `git lfs migrate`, large files (`.fbx`, `.jpg`, `.png`, `.skp`) may not downloaded as their actual binary content — Unity will only see small LFS pointer files instead of the real assets, which breaks imports and can cause "Missing Prefab" errors on objects like the Lab and DG822PRO models.
Run `git lfs pull`.


## Roadmap

- [x] Project cleanup and structuring
- [x] Git / Git LFS setup
- [ ] XR Interaction Toolkit integration (VR camera, controllers)
- [ ] Interactions with lab instruments
- [ ] VR teleportation / movement
- [ ] VR UI
- [ ] Simulation backend integration (Python/FastAPI/ngspice)
