# VR Lab — KMUTNB

Expérience VR d'un laboratoire (électronique/sciences), développée sous Unity, avec pour objectif de reproduire un environnement de labo interactif en réalité virtuelle.

## 🎮 Aperçu

Ce projet recrée un laboratoire (tables, instruments, équipements) dans un environnement 3D, avec pour objectif d'y intégrer des interactions VR (préhension d'objets, déplacement, manipulation d'instruments).

## 🛠️ Stack technique

- **Moteur** : Unity 6 (6000.5.0f1)
- **VR** : OpenXR + XR Interaction Toolkit
- **Rendu** : Universal Render Pipeline (URP)
- **Versioning** : Git + Git LFS

## 📁 Structure du projet

```
Assets/
│── Scenes/
├── Scripts/
├── Prefabs/
├── Materials/
├── Models/
├── XR/
├── XRI/                # XR Interaction Toolkit (package/samples)
├── TextMesh Pro/        # Package externe
└── ...                  # Autres dossiers de packages importés
```

Le préfixe `_Project/` regroupe tout le contenu créé pour ce projet, séparé des packages/imports externes, et remonte toujours en haut de la liste (tri alphabétique) pour faciliter la navigation.

## 🚀 Setup / Installation

### Prérequis
- [Unity Hub](https://unity.com/download) avec Unity **6000.5.0f1** (ou version compatible) installé
- [Git](https://git-scm.com/) et [Git LFS](https://git-lfs.github.com/) installés
- Un casque VR compatible OpenXR (Meta Quest, Valve Index, HTC Vive, etc.) et son runtime installé

### Cloner le projet

```bash
git lfs install
git clone git@github.com:samysame/vr-lab-kmutnb.git
```

> ⚠️ Assure-toi que Git LFS est installé **avant** de cloner, sinon les fichiers volumineux (modèles 3D, textures) ne seront pas récupérés correctement.

### Ouvrir dans Unity

1. Ouvre **Unity Hub**
2. Clique sur **Add** > sélectionne le dossier du projet cloné
3. Ouvre le projet avec la version Unity indiquée ci-dessus
4. Laisse Unity réimporter tous les assets (peut prendre plusieurs minutes au premier lancement)

## 🔄 Workflow Git (multi-postes)

Ce projet est travaillé depuis plusieurs machines (présentiel/distanciel). Pour éviter les conflits :

- **Avant de commencer à travailler** : `git pull`
- **Avant de changer de machine** : commit + `git push`, même en travail non terminé (`git commit -m "wip: ..."`)

### Paramètres Unity importants (à vérifier sur chaque poste)

- `Edit > Project Settings > Editor > Asset Serialization` → **Force Text**
- `Edit > Project Settings > Editor > Version Control` → **Visible Meta Files**

Ces réglages garantissent que les scènes/prefabs restent lisibles en diff Git (format YAML texte plutôt que binaire).

## 📝 Notes

- Les gros fichiers (`.fbx`, `.png`, `.psd`, `.wav`, `.mp4`, etc.) sont gérés via **Git LFS** — voir `.gitattributes` pour la liste complète des extensions trackées.
- `Library/`, `Logs/`, `Temp/`, `obj/`, `UserSettings/` sont exclus du versioning (`.gitignore`) car régénérés automatiquement par Unity.

## 🗺️ Roadmap

- [x] Nettoyage et structuration du projet
- [x] Setup Git / Git LFS
- [ ] Intégration XR Interaction Toolkit (caméra VR, contrôleurs)
- [ ] Interactions avec les instruments du labo
- [ ] Téléportation / déplacement en VR
- [ ] UI en VR

## 📄 Licence

_À définir_
