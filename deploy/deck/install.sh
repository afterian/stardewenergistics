#!/bin/bash
# Stardew Energistics - one-time Steam Deck setup (Desktop Mode > Konsole).
#   - installs or updates SMAPI (native Linux version)
#   - saves the GitHub token (private repo only) to ~/stardew-energistics/config
#   - installs the auto-update launcher
# Usage:  bash install.sh [TOKEN]        (asks for the token if the repo needs one)
#         bash install.sh OWNER/REPO [TOKEN]
# Running it again is safe: it updates SMAPI and replaces the saved token.
set -e
REPO="afterian/stardewenergistics"
case "$1" in */*) REPO="$1"; shift ;; esac
TOKEN="$1"
DIR="$HOME/stardew-energistics"; mkdir -p "$DIR"
say()  { echo; echo "== $*"; }
fail() { echo; echo "!! $*"; exit 1; }
ask()  { local v; read -r -p "$1" v < /dev/tty; echo "$v"; }

# --- 1. GitHub access --------------------------------------------------------
say "Checking GitHub access to $REPO"
check() {
  local auth=(); [ -n "$TOKEN" ] && auth=(-H "Authorization: Bearer $TOKEN")
  curl -s -o /dev/null -w '%{http_code}' "${auth[@]}" "https://api.github.com/repos/$REPO" || true
}
CODE=$(check)
if [ "$CODE" = 404 ] && [ -z "$TOKEN" ]; then
  echo "The repo is private, so this Deck needs the read-only token (guide part 2)."
  read -r -s -p "Paste the token (nothing shows while you paste), then press Enter: " TOKEN < /dev/tty; echo
  CODE=$(check)
fi
case "$CODE" in
  200) echo "OK." ;;
  401) fail "GitHub says the token is wrong or expired. Make a new one and run this again." ;;
  404) fail "GitHub can't find $REPO with this token. Check the token's repository access." ;;
  *)   fail "Could not reach GitHub (answer: $CODE). Check the wifi and run this again." ;;
esac
AUTH=(); [ -n "$TOKEN" ] && AUTH=(-H "Authorization: Bearer $TOKEN")

# --- 2. Find the game --------------------------------------------------------
say "Looking for Stardew Valley"
GAME=""
for d in "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" \
         "$HOME/.steam/steam/steamapps/common/Stardew Valley" \
         /run/media/*/steamapps/common/"Stardew Valley" \
         /run/media/*/*/steamapps/common/"Stardew Valley" \
         /run/media/*/*/*/steamapps/common/"Stardew Valley"; do
  [ -d "$d" ] && { GAME="$d"; break; }
done
[ -n "$GAME" ] || fail "Stardew Valley isn't installed (or hasn't been started once). Install it in Steam, start it once, quit, and run this again."
echo "Found: $GAME"
if [ -f "$GAME/Stardew Valley.exe" ] && [ ! -e "$GAME/StardewValley" ]; then
  fail "This is the Windows (Proton) version. In Steam: Stardew Valley > Properties > Compatibility, untick 'Force the use of a specific Steam Play compatibility tool', let it update, then run this again."
fi

# --- 3. SMAPI ----------------------------------------------------------------
if [ -x "$GAME/StardewModdingAPI" ]; then
  say "SMAPI is already installed"
  ANS=$(ask "Check for a newer SMAPI and install it? [y/N] ")
else
  say "SMAPI isn't installed yet; installing it"
  ANS=y
fi
if [[ "$ANS" =~ ^[Yy] ]]; then
  # SMAPI is public on GitHub; the token is never sent there.
  URL=$(curl -fsSL -m 20 https://api.github.com/repos/Pathoschild/SMAPI/releases/latest | python3 -c '
import json,sys
for a in json.load(sys.stdin)["assets"]:
    n=a["name"].lower()
    if n.endswith(".zip") and "installer" in n and "developer" not in n:
        print(a["browser_download_url"]); break') || true
  [ -n "$URL" ] || fail "Could not find the SMAPI download. Install it by hand from https://smapi.io, then run this again."
  echo "Downloading $(basename "$URL")"
  TMP=$(mktemp -d)
  curl -fsSL -m 300 "$URL" -o "$TMP/smapi.zip" || fail "SMAPI download failed. Check the wifi and run this again."
  python3 -c 'import zipfile,sys; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])' "$TMP/smapi.zip" "$TMP/x" || fail "Could not unzip SMAPI."
  INSTALLER=$(find "$TMP/x" -name "install on Linux.sh" | head -n1)
  [ -n "$INSTALLER" ] || fail "The SMAPI download didn't contain 'install on Linux.sh'."
  echo "Running the SMAPI installer. If it asks anything, choose to install; at the end press Enter."
  ( cd "$(dirname "$INSTALLER")" && bash "./install on Linux.sh" --install --game-path "$GAME" < /dev/tty ) || true
  rm -rf "$TMP"
  [ -x "$GAME/StardewModdingAPI" ] || fail "SMAPI didn't install. Try the manual steps in the guide (part 3, 'If the automatic SMAPI install fails')."
  echo "SMAPI installed."
fi

# --- 4. Launcher -------------------------------------------------------------
say "Installing the auto-update launcher"
( umask 077; printf 'REPO=%q\nTOKEN=%q\nTAG=test-latest\n' "$REPO" "$TOKEN" > "$DIR/config" )
curl -fsSL "${AUTH[@]}" -H "Accept: application/vnd.github.raw" \
  "https://api.github.com/repos/$REPO/contents/deploy/deck/launch.sh" -o "$DIR/launch.sh" \
  || fail "Could not download the launcher. Check the wifi and run this again."
chmod +x "$DIR/launch.sh"
echo "Done."

echo
echo "=============================================================="
echo " Last step, in Steam: Stardew Valley > Properties > General >"
echo " Launch Options. Replace what's there with exactly:"
echo
echo "     ~/stardew-energistics/launch.sh %command%"
echo
echo " Then go back to Game Mode and start the game."
echo "=============================================================="
