#!/bin/bash
# Stardew Energistics - one-time Steam Deck setup. No typing needed.
# Normally started by double-clicking "Install-Stardew-Energistics.desktop"
# in Desktop Mode. It:
#   1. checks GitHub (asks for a token only if the repo is private)
#   2. installs or updates SMAPI (native Linux version)
#   3. installs the auto-update launcher in ~/stardew-energistics
#   4. sets Stardew's Steam Launch Options (closes and reopens Steam)
# Usage: bash install.sh [TOKEN]   or   bash install.sh OWNER/REPO [TOKEN]
# Running it again is safe (updates SMAPI, refreshes everything).
REPO="afterian/stardewenergistics"
case "$1" in */*) REPO="$1"; shift ;; esac
TOKEN="${1:-$SE_TOKEN}"
DIR="$HOME/stardew-energistics"; mkdir -p "$DIR"
LAUNCH_OPTIONS="$DIR/launch.sh %command%"
# Everything shown is also saved to ~/stardew-energistics/install.log.
exec > >(tee "$DIR/install.log") 2>&1
HAVE_GUI=0; command -v kdialog >/dev/null && [ -n "$DISPLAY$WAYLAND_DISPLAY" ] && HAVE_GUI=1

say() { echo; echo "== $*"; }
popup() { [ $HAVE_GUI = 1 ] && kdialog --title "Stardew Energistics" "$@" 2>/dev/null; }
fail() {
  echo; echo "!! $*"
  if [ $HAVE_GUI = 1 ]; then popup --error "$*"; else read -r -t 60 -p "Press Enter to close." _ < /dev/tty 2>/dev/null; fi
  exit 1
}

# --- 1. GitHub access --------------------------------------------------------
say "Checking GitHub access to $REPO"
check() {
  local auth=(); [ -n "$TOKEN" ] && auth=(-H "Authorization: Bearer $TOKEN")
  curl -s -m 20 -o /dev/null -w '%{http_code}' "${auth[@]}" "https://api.github.com/repos/$REPO" || true
}
CODE=$(check)
if [ "$CODE" = 404 ] && [ -z "$TOKEN" ]; then
  echo "The repo is private, so this Deck needs the read-only token."
  if [ $HAVE_GUI = 1 ]; then
    TOKEN=$(popup --password "The repo is private. Paste the read-only GitHub token (right-click > Paste):")
  else
    read -r -s -p "Paste the token, then press Enter: " TOKEN < /dev/tty; echo
  fi
  CODE=$(check)
fi
case "$CODE" in
  200) echo "OK." ;;
  401) fail "GitHub says the token is wrong or expired. Make a new one and run the installer again." ;;
  404) fail "GitHub can't find $REPO. If it's private, check the token's repository access." ;;
  *)   fail "Couldn't reach GitHub (answer: $CODE). Check the wifi and run the installer again." ;;
esac
AUTH=(); [ -n "$TOKEN" ] && AUTH=(-H "Authorization: Bearer $TOKEN")
fetch() {  # fetch <repo path> <output file>
  curl -fsSL -m 30 "${AUTH[@]}" -H "Accept: application/vnd.github.raw" \
    "https://api.github.com/repos/$REPO/contents/$1" -o "$2"
}

# --- 2. Find the game --------------------------------------------------------
say "Looking for Stardew Valley"
# Pick the folder that really has the game in it (an empty leftover folder can
# exist on internal storage when the game is on the SD card). Prefer one with SMAPI.
GAME=""
for d in "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" \
         "$HOME/.steam/steam/steamapps/common/Stardew Valley" \
         /run/media/*/steamapps/common/"Stardew Valley" \
         /run/media/*/*/steamapps/common/"Stardew Valley" \
         /run/media/*/*/*/steamapps/common/"Stardew Valley"; do
  [ -f "$d/Stardew Valley.dll" ] || [ -e "$d/StardewValley" ] || [ -f "$d/Stardew Valley.exe" ] || continue
  if [ -e "$d/StardewModdingAPI" ] || [ -f "$d/StardewModdingAPI.dll" ]; then GAME="$d"; break; fi
  [ -n "$GAME" ] || GAME="$d"
done
[ -n "$GAME" ] || fail "Stardew Valley isn't installed yet. Install it in Steam, start it once, quit, then run the installer again."
echo "Found: $GAME"
if [ -f "$GAME/Stardew Valley.exe" ] && [ ! -e "$GAME/StardewValley" ]; then
  fail "This is the Windows (Proton) version of the game. In Steam: Stardew Valley > Properties > Compatibility, turn OFF 'Force the use of a specific Steam Play compatibility tool', let it update, then run the installer again."
fi

# --- 3. SMAPI ----------------------------------------------------------------
smapi_installed() {
  [ -f "$GAME/StardewModdingAPI" ] && [ ! -x "$GAME/StardewModdingAPI" ] && chmod +x "$GAME/StardewModdingAPI" 2>/dev/null
  [ -x "$GAME/StardewModdingAPI" ] || [ -f "$GAME/StardewModdingAPI.dll" ]
}
if smapi_installed; then
  say "SMAPI is already installed; keeping it"
else
  say "Installing SMAPI"
  # SMAPI is public on GitHub; the token is never sent there.
  URL=$(curl -fsSL -m 20 https://api.github.com/repos/Pathoschild/SMAPI/releases/latest | python3 -c '
import json,sys
for a in json.load(sys.stdin)["assets"]:
    n=a["name"].lower()
    if n.endswith(".zip") and "installer" in n and "developer" not in n:
        print(a["browser_download_url"]); break' 2>/dev/null)
  [ -n "$URL" ] || fail "Couldn't find the SMAPI download. Install SMAPI by hand (guide: 'If the automatic SMAPI install fails'), then run this installer again."
  echo "Downloading $(basename "$URL")"
  TMP=$(mktemp -d)
  curl -fsSL -m 300 "$URL" -o "$TMP/smapi.zip" || fail "SMAPI download failed. Check the wifi and run the installer again."
  python3 -c 'import zipfile,sys; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])' "$TMP/smapi.zip" "$TMP/x" || fail "Couldn't unzip SMAPI."
  INSTALLER=$(find "$TMP/x" -name "install on Linux.sh" | head -n1)
  [ -n "$INSTALLER" ] || fail "The SMAPI download didn't contain 'install on Linux.sh'."
  # --install/--game-path/--no-prompt skip SMAPI's questions; stdin from /dev/null answers anything left.
  ( cd "$(dirname "$INSTALLER")" && bash "./install on Linux.sh" --install --game-path "$GAME" --no-prompt < /dev/null ) || true
  rm -rf "$TMP"
  smapi_installed || fail "SMAPI didn't install. Install it by hand (guide: 'If the automatic SMAPI install fails'), then run this installer again; it will skip SMAPI."
  echo "SMAPI is ready."
fi

# --- 4. Launcher -------------------------------------------------------------
say "Installing the auto-update launcher"
( umask 077; printf 'REPO=%q\nTOKEN=%q\nTAG=test-latest\n' "$REPO" "$TOKEN" > "$DIR/config" )
fetch deploy/deck/launch.sh "$DIR/launch.sh" || fail "Couldn't download the launcher. Check the wifi and run the installer again."
fetch deploy/deck/set_launch_options.py "$DIR/set_launch_options.py" || fail "Couldn't download a setup file. Check the wifi and run the installer again."
chmod +x "$DIR/launch.sh"
echo "Done."

# --- 5. Steam Launch Options -------------------------------------------------
say "Setting Stardew's Launch Options in Steam"
SET_OK=0
STEAM_WAS_RUNNING=0
if pgrep -x steam >/dev/null; then
  STEAM_WAS_RUNNING=1
  echo "Closing Steam for a moment (it saves its settings on exit)..."
  steam -shutdown >/dev/null 2>&1 &
  for _ in $(seq 1 60); do pgrep -x steam >/dev/null || break; sleep 1; done
fi
if pgrep -x steam >/dev/null; then
  echo "Steam didn't close."
else
  python3 "$DIR/set_launch_options.py" "$LAUNCH_OPTIONS" && SET_OK=1
fi
if [ $STEAM_WAS_RUNNING = 1 ] && ! pgrep -x steam >/dev/null; then
  echo "Starting Steam again..."
  (setsid steam >/dev/null 2>&1 &)
fi

echo
if [ $SET_OK = 1 ]; then
  MSG="All set! Go back to Gaming Mode (double-click 'Return to Gaming Mode' on the desktop) and start Stardew Valley. The first start takes a few extra seconds."
else
  MSG="Almost done. One step left: in Steam, open Stardew Valley > Properties > General > Launch Options and enter:

$LAUNCH_OPTIONS

Then go back to Gaming Mode and start the game."
fi
echo "$MSG"
if [ $HAVE_GUI = 1 ]; then popup --msgbox "$MSG"; else read -r -t 120 -p "Press Enter to close." _ < /dev/tty 2>/dev/null; fi
