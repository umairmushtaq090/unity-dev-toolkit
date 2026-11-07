# Dev Toolkit for Unity

A small, dependency-free **Unity Editor toolkit** that handles three chores that come up in almost every project:

| Tool | What it does |
|---|---|
| 🧹 **Missing Scripts** | Scans open scenes (including inactive objects) and every prefab in `Assets/` for *"Missing (Mono Script)"* components. You can select each one or remove them all in one click. Scene changes support Undo. |
| ✏️ **Batch Rename** | Renames many GameObjects or assets at once with base name, find/replace, prefix, suffix, and zero-padded numbering (`Enemy_01`, `Enemy_02`, …). Shows a live preview first, and keeps Hierarchy order. |
| 🔍 **Find References** | Pick any asset to list every scene, prefab, material, or ScriptableObject that uses it. Run this before deleting an asset. |

Open it from **`Tools ▸ Dev Toolkit`**.

---

## Installation

### Option A: Unity Package Manager (Git URL), recommended
1. In Unity, open **Window ▸ Package Manager**.
2. Click **+ ▸ Add package from git URL…**
3. Paste:
   ```
   https://github.com/umairmushtaq090/unity-dev-toolkit.git
   ```

Or add this line to `Packages/manifest.json`:
```json
"com.umairmushtaq.devtoolkit": "https://github.com/umairmushtaq090/unity-dev-toolkit.git"
```

### Option B: Copy the files
Download the repo and copy it into your project's `Packages/` (or `Assets/`) folder.

**Requirements:** Unity 2021.3 LTS or newer. Editor-only, so nothing is added to your builds.

---

## Usage

### Missing Scripts
1. Choose *Open scenes* and/or *All prefabs*, then click **Scan**.
2. Click **Select** to ping an object, **Fix** to clean one entry, or **Remove ALL** to clean everything.

> Prefab fixes are saved directly to the prefab asset. Commit your work to version control first.

### Batch Rename
1. Select objects in the Hierarchy *or* assets in the Project window.
2. Set the options and check the preview.
3. Click **Rename**. GameObject renames can be undone with `Ctrl/Cmd + Z`.

### Find References
1. Drag an asset into the field (or click **Use Selection**).
2. Click **Find References**, then **Ping** any result to jump to it.

> This only finds references stored in serialized assets. Assets loaded from code (`Resources.Load`, Addressables by address) won't show up.

---

## Roadmap
- [ ] Find unused assets
- [ ] Replace references (swap asset A for B everywhere)
- [ ] Batch rename with regex
- [ ] Scene statistics (object/component counts)

Ideas and PRs are welcome. Please open an issue.

## Contributing
1. Fork the repo and create a branch: `git checkout -b feature/my-idea`
2. Keep tools editor-only and dependency-free.
3. Open a Pull Request describing the change.

## License
[MIT](LICENSE) © Umair Mushtaq
