#!/usr/bin/env python3
"""Set Stardew Valley's Steam Launch Options in every Steam account on this Deck.

Usage: python3 set_launch_options.py "<launch options>"
Steam must be fully closed (it rewrites localconfig.vdf on exit).
Keeps a backup next to each file: localconfig.vdf.stardew-energistics.bak
"""
import glob, os, shutil, sys

APP_ID = "413150"  # Stardew Valley


def tokenize(text):
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if c.isspace():
            i += 1
        elif c == "/" and text.startswith("//", i):
            j = text.find("\n", i)
            i = n if j < 0 else j
        elif c in "{}":
            yield c
            i += 1
        elif c == '"':
            i += 1
            out = []
            while i < n and text[i] != '"':
                if text[i] == "\\" and i + 1 < n:
                    out.append(text[i:i + 2])  # keep escapes exactly as Steam wrote them
                    i += 2
                else:
                    out.append(text[i])
                    i += 1
            i += 1
            yield ("s", "".join(out))
        else:  # unquoted token
            j = i
            while j < n and not text[j].isspace() and text[j] not in '{}"':
                j += 1
            yield ("s", text[i:j])
            i = j


def parse(text):
    """Returns a list of [key, value] where value is a str or a nested list."""
    toks = list(tokenize(text))
    pos = 0

    def block():
        nonlocal pos
        items = []
        while pos < len(toks):
            t = toks[pos]
            if t == "}":
                pos += 1
                return items
            if t == "{":
                raise ValueError("unexpected {")
            key = t[1]
            pos += 1
            v = toks[pos]
            pos += 1
            if v == "{":
                items.append([key, block()])
            else:
                items.append([key, v[1]])
        return items

    return block()


def esc(s):
    """Escape a new value. Values read from the file are kept already escaped."""
    return s.replace("\\", "\\\\").replace('"', '\\"')


def dump(items, depth=0):
    tab = "\t" * depth
    out = []
    for k, v in items:
        if isinstance(v, list):
            out.append(f'{tab}"{k}"\n{tab}{{\n{dump(v, depth + 1)}{tab}}}\n')
        else:
            out.append(f'{tab}"{k}"\t\t"{v}"\n')
    return "".join(out)


def child(items, key, create=True):
    for kv in items:
        if kv[0].lower() == key.lower() and isinstance(kv[1], list):
            return kv[1]
    if not create:
        return None
    new = []
    items.append([key, new])
    return new


def set_options(path, options):
    with open(path, encoding="utf-8", errors="surrogateescape") as f:
        root = parse(f.read())
    store = child(root, "UserLocalConfigStore")
    apps = child(child(child(child(store, "Software"), "Valve"), "Steam"), "apps")
    app = child(apps, APP_ID)
    for kv in app:
        if kv[0].lower() == "launchoptions":
            kv[1] = esc(options)
            break
    else:
        app.append(["LaunchOptions", esc(options)])
    shutil.copy2(path, path + ".stardew-energistics.bak")
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8", errors="surrogateescape") as f:
        f.write(dump(root))
    os.replace(tmp, path)


def main():
    options = sys.argv[1]
    home = os.path.expanduser("~")
    files = set()
    for base in (".local/share/Steam", ".steam/steam"):
        for p in glob.glob(os.path.join(home, base, "userdata", "*", "config", "localconfig.vdf")):
            files.add(os.path.realpath(p))
    if not files:
        print("No Steam account found on this Deck.")
        return 1
    ok = 0
    for p in sorted(files):
        try:
            set_options(p, options)
            ok += 1
        except Exception as e:  # leave that account alone
            print(f"Couldn't update {p}: {e}")
    print(f"Launch Options set for {ok} Steam account(s).")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
