#!/bin/bash
# Stardew Energistics - Steam Deck test launcher.
# Steam > Stardew Valley > Properties > Launch Options:
#     ~/stardew-energistics/launch.sh %command%
# Every time the game starts, this checks GitHub for a newer test build,
# installs it into the Mods folder, then starts the game as normal.
# If anything fails (no wifi, GitHub down), the game still starts.

HOME_DIR="$HOME/stardew-energistics"
LOG="$HOME_DIR/update.log"
CONF="$HOME_DIR/config"
STATE="$HOME_DIR/installed-build"
TAG="test-latest"
MOD_FOLDER="StardewEnergistics"

mkdir -p "$HOME_DIR"
exec 3>>"$LOG"
log() { echo "$(date '+%F %T') $*" >&3; }

update() {
  [ -f "$CONF" ] || { log "no config; run install.sh first"; return; }
  # shellcheck disable=SC1090
  source "$CONF"   # sets REPO and TOKEN

  # Find the game folder (internal storage or SD card).
  GAME=""
  for d in \
    "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" \
    "$HOME/.steam/steam/steamapps/common/Stardew Valley" \
    /run/media/*/steamapps/common/"Stardew Valley" \
    /run/media/*/*/steamapps/common/"Stardew Valley"; do
    [ -d "$d" ] && { GAME="$d"; break; }
  done
  [ -n "$GAME" ] || { log "game folder not found"; return; }
  MODS="$GAME/Mods"; mkdir -p "$MODS"

  AUTH=(); [ -n "$TOKEN" ] && AUTH=(-H "Authorization: Bearer $TOKEN")
  JSON=$(curl -sf -m 10 "${AUTH[@]}" -H "Accept: application/vnd.github+json" \
    "https://api.github.com/repos/$REPO/releases/tags/$TAG") || { log "could not reach GitHub; keeping current build"; return; }

  read -r ASSET_URL BUILD_ID < <(printf '%s' "$JSON" | python3 -c '
import json,sys
r=json.load(sys.stdin)
a=[x for x in r.get("assets",[]) if x["name"].endswith(".zip")]
print(a[0]["url"], str(a[0]["id"])+"-"+a[0]["updated_at"]) if a else print("", "")')
  [ -n "$ASSET_URL" ] || { log "release has no zip"; return; }
  if [ -f "$STATE" ] && [ "$(cat "$STATE")" = "$BUILD_ID" ] && [ -d "$MODS/$MOD_FOLDER" ]; then
    log "up to date ($BUILD_ID)"; return
  fi

  TMP=$(mktemp -d)
  curl -sfL -m 120 "${AUTH[@]}" -H "Accept: application/octet-stream" "$ASSET_URL" -o "$TMP/mod.zip" \
    || { log "download failed"; rm -rf "$TMP"; return; }
  python3 -c 'import zipfile,sys; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])' "$TMP/mod.zip" "$TMP/x" \
    || { log "unzip failed"; rm -rf "$TMP"; return; }
  SRC=$(dirname "$(find "$TMP/x" -name manifest.json | head -n1)")
  [ -f "$SRC/manifest.json" ] || { log "no manifest.json in zip"; rm -rf "$TMP"; return; }

  # Keep the player's config.json across updates.
  [ -f "$MODS/$MOD_FOLDER/config.json" ] && cp "$MODS/$MOD_FOLDER/config.json" "$TMP/config.json"
  rm -rf "$MODS/$MOD_FOLDER" && cp -r "$SRC" "$MODS/$MOD_FOLDER"
  [ -f "$TMP/config.json" ] && cp "$TMP/config.json" "$MODS/$MOD_FOLDER/config.json"
  echo "$BUILD_ID" > "$STATE"
  rm -rf "$TMP"
  log "installed $BUILD_ID into $MODS/$MOD_FOLDER"
}

update
exec "$@"
