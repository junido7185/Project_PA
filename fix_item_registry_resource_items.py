from pathlib import Path

path = Path.cwd() / "Assets/Scripts/ItemRegistry.cs"
if not path.exists():
    raise SystemExit(f"ERROR: missing {path}; run from Project_PA root.")

raw = path.read_bytes()
newline = "\r\n" if b"\r\n" in raw else "\n"
text = raw.decode("utf-8-sig").replace("\r\n", "\n").replace("\r", "\n")

old = '''    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
'''

new = '''    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        RegisterResourceItems();
    }

    // ItemRegistry remains the single lookup authority, but Resources/Items is also a canonical
    // item source. Merge newly added resource items so save/load does not depend on manually
    // updating every scene's serialized allItems list.
    void RegisterResourceItems()
    {
        if (allItems == null) allItems = new List<Item>();

        foreach (var item in Resources.LoadAll<Item>("Items"))
            if (item != null && !allItems.Contains(item))
                allItems.Add(item);
    }
'''

if new in text:
    print("UNCHANGED: ItemRegistry Resources/Items merge already present.")
    raise SystemExit(0)

if old not in text:
    raise SystemExit("ERROR: expected ItemRegistry.Awake block not found. No file changed.")

path.write_bytes(text.replace(old, new, 1).replace("\n", newline).encode("utf-8"))
print("PASS: ItemRegistry now merges canonical Resources/Items before save lookup.")
print(path)
