from pathlib import Path

path = Path.cwd() / "Assets/Editor/PA_FirstProductionR5Checks.cs"
if not path.exists():
    raise SystemExit(f"ERROR: missing {path}; run from Project_PA root.")

raw = path.read_bytes()
newline = "\r\n" if b"\r\n" in raw else "\n"
text = raw.decode("utf-8-sig").replace("\r\n", "\n").replace("\r", "\n")

old = '''    static void ConfigureRepository(SaveManager save)
    {
        var method = typeof(SaveManager).GetMethod(
            "SetRepositoryForValidation", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
            throw new MissingMethodException(nameof(SaveManager), "SetRepositoryForValidation");
        method.Invoke(save, new object[]
        {
            new LocalJsonSaveRepository(Path.GetFullPath(Path.Combine(Output, "Save")))
        });
    }
'''

new = '''    static void ConfigureRepository(SaveManager save)
    {
        var method = typeof(SaveManager).GetMethod(
            "SetRepositoryForValidation", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
            throw new MissingMethodException(nameof(SaveManager), "SetRepositoryForValidation");

        string root = Path.GetFullPath(Path.Combine(Output, "Save"));
        Directory.CreateDirectory(root);
        method.Invoke(save, new object[]
        {
            new LocalJsonSaveRepository(root)
        });
    }
'''

if new in text:
    print("UNCHANGED: P4-R5 validation save directory fix already present.")
    raise SystemExit(0)

if old not in text:
    raise SystemExit("ERROR: expected ConfigureRepository block not found. No file changed.")

text = text.replace(old, new, 1)
path.write_bytes(text.replace("\n", newline).encode("utf-8"))
print("PASS: P4-R5 validator now creates its custom save directory before SaveAsync.")
print(path)
