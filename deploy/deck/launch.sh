#!/bin/bash
# Stardew Energistics - Steam Deck test launcher.
# Steam > Stardew Valley > Properties > Launch Options:
#     ~/stardew-energistics/launch.sh %command%
#
# Every time the game starts this:
#   1. checks the GitHub release "test-latest" and installs a newer mod build
#      into Mods/StardewEnergistics (keeps the mod's config.json);
#   2. updates itself from the repo (takes effect next launch);
#   3. starts the game through SMAPI.
# If anything fails (no wifi, GitHub down, bad token) the game still starts
# with the last installed build. Log: ~/stardew-energistics/update.log
#
# Optional settings in ~/stardew-energistics/config (besides REPO and TOKEN):
#   TAG=test-latest     release to follow
#   AUTO_UPDATE=0       stop updating (keep the installed build)

APP_DIR="$HOME/stardew-energistics"
LOG="$APP_DIR/update.log"
CONF="$APP_DIR/config"
STATE="$APP_DIR/installed-build"
MOD_FOLDER="StardewEnergistics"

mkdir -p "$APP_DIR"
# Keep the log short.
[ -f "$LOG" ] && [ "$(wc -l < "$LOG")" -gt 500 ] && tail -n 200 "$LOG" > "$LOG.tmp" && mv "$LOG.tmp" "$LOG"
log() { echo "$(date '+%F %T') $*" >> "$LOG"; }

steam_libraries() {
  # Every Steam library on this Deck (internal, SD card, USB), from Steam's own list.
  local f p
  for f in "$HOME/.local/share/Steam/steamapps/libraryfolders.vdf" "$HOME/.steam/steam/steamapps/libraryfolders.vdf"; do
    [ -f "$f" ] || continue
    sed -n 's/^[[:space:]]*"path"[[:space:]]*"\(.*\)"[[:space:]]*$/\1/p' "$f" | sed 's/\\\\/\\/g'
  done
  echo "$HOME/.local/share/Steam"
  echo "$HOME/.steam/steam"
  for p in /run/media/*/steamapps /run/media/*/*/steamapps /run/media/*/*/*/steamapps; do
    [ -d "$p" ] && dirname "$p"
  done
}

find_game() {
  local lib d first=""
  while IFS= read -r lib; do
    d="$lib/steamapps/common/Stardew Valley"
    [ -f "$d/Stardew Valley.dll" ] || [ -e "$d/StardewValley" ] || continue
    [ -e "$d/StardewModdingAPI" ] && { echo "$d"; return; }
    [ -n "$first" ] || first="$d"
  done < <(steam_libraries)
  [ -n "$first" ] && echo "$first"
}

# Steam's launch environment (LD_PRELOAD overlay, runtime libraries) can break
# curl and python, so the updater runs with a clean library environment.
clean() { env -u LD_PRELOAD -u LD_LIBRARY_PATH "$@"; }

update_mod() {
  local GAME="$1"
  [ -n "$GAME" ] || { log "game folder not found"; return; }
  [ -f "$CONF" ] || { log "no config; run install.sh first"; return; }
  REPO=""; TOKEN=""; TAG="test-latest"; AUTO_UPDATE=1
  # shellcheck disable=SC1090
  source "$CONF"
  [ "$AUTO_UPDATE" = "0" ] && { log "auto-update is off (AUTO_UPDATE=0 in config)"; return; }
  [ -n "$REPO" ] || { log "REPO missing in config"; return; }

  local MODS="$GAME/Mods"; mkdir -p "$MODS"
  AUTH=(); [ -n "$TOKEN" ] && AUTH=(-H "Authorization: Bearer $TOKEN")

  local JSON CODE
  JSON=$(clean curl -s -m 10 -w '\n%{http_code}' "${AUTH[@]}" -H "Accept: application/vnd.github+json" \
    "https://api.github.com/repos/$REPO/releases/tags/$TAG")
  CODE="${JSON##*$'\n'}"; JSON="${JSON%$'\n'*}"
  case "$CODE" in
    200) ;;
    401) log "GitHub says the token is wrong or expired (401). Make a new one: see docs/steam-deck-setup.md"; return ;;
    404) log "release '$TAG' not found (404). Nothing published yet, or the token can't see the repo"; return ;;
    000) log "could not reach GitHub (offline?); keeping current build"; return ;;
    *)   log "GitHub answered $CODE; keeping current build"; return ;;
  esac

  # Every mod of the pack is its own zip on the release (deploy/mods.json): Energistics is
  # "StardewEnergistics-test.zip", the others "<name>-test.mod" (also a zip).
  # Each one is installed on its own, so one bad download never blocks the others.
  local LIST
  LIST=$(printf '%s' "$JSON" | clean python3 -c '
import json,sys
r=json.load(sys.stdin)
for a in sorted(r.get("assets",[]), key=lambda x: x["id"]):
    n=a["name"]
    for ext in ("-test.zip", "-test.mod"):
        if n.endswith(ext):
            print(n[:-len(ext)], a["url"], a["id"], a.get("label") or n)') || { log "could not read release info"; return; }
  [ -n "$LIST" ] || { log "release has no mod zips yet"; return; }
  local NAME ASSET_URL BUILD_ID VERSION
  while read -r NAME ASSET_URL BUILD_ID VERSION; do
    [ -n "$NAME" ] && install_one "$MODS" "$NAME" "$ASSET_URL" "$BUILD_ID" "$VERSION"
  done <<< "$LIST"
}

# install_one <Mods dir> <name> <asset url> <asset id> <version>
install_one() {
  local MODS="$1" NAME="$2" ASSET_URL="$3" BUILD_ID="$4" VERSION="$5"
  local STATE_FILE="$APP_DIR/installed-$NAME"
  # Older launchers kept Energistics' state in installed-build.
  [ "$NAME" = StardewEnergistics ] && [ ! -f "$STATE_FILE" ] && [ -f "$STATE" ] && cp "$STATE" "$STATE_FILE"
  local FOLDER=""; [ -f "$STATE_FILE" ] && FOLDER=$(sed -n 3p "$STATE_FILE")
  [ -n "$FOLDER" ] || FOLDER="$NAME"
  if [ -f "$STATE_FILE" ] && [ "$(head -n1 "$STATE_FILE")" = "$BUILD_ID" ] && [ -f "$MODS/$FOLDER/manifest.json" ]; then
    log "$NAME up to date ($VERSION)"; return
  fi

  local TMP; TMP=$(mktemp -d)
  if ! clean curl -sfL -m 120 "${AUTH[@]}" -H "Accept: application/octet-stream" "$ASSET_URL" -o "$TMP/mod.zip"; then
    log "$NAME: download failed; keeping current build"; rm -rf "$TMP"; return
  fi
  clean python3 -c 'import zipfile,sys; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])' "$TMP/mod.zip" "$TMP/x" \
    || { log "$NAME: unzip failed; keeping current build"; rm -rf "$TMP"; return; }
  local MANIFEST SRC
  MANIFEST=$(find "$TMP/x" -name manifest.json | awk '{ print length, $0 }' | sort -n | head -n1 | cut -d' ' -f2-)
  [ -n "$MANIFEST" ] || { log "$NAME: no manifest.json in zip; keeping current build"; rm -rf "$TMP"; return; }
  SRC=$(dirname "$MANIFEST")
  # The mod's folder name comes from the zip (ModBuildConfig puts the mod in a folder of its name).
  [ "$SRC" != "$TMP/x" ] && FOLDER=$(basename "$SRC")

  # Keep the player's config.json across updates.
  [ -f "$MODS/$FOLDER/config.json" ] && cp "$MODS/$FOLDER/config.json" "$TMP/config.json"
  rm -rf "$MODS/$FOLDER.new" && cp -r "$SRC" "$MODS/$FOLDER.new" \
    && rm -rf "$MODS/$FOLDER" && mv "$MODS/$FOLDER.new" "$MODS/$FOLDER" \
    || { log "$NAME: could not write to $MODS"; rm -rf "$TMP"; return; }
  [ -f "$TMP/config.json" ] && cp "$TMP/config.json" "$MODS/$FOLDER/config.json"
  printf '%s\n%s\n%s\n' "$BUILD_ID" "$VERSION" "$FOLDER" > "$STATE_FILE"
  rm -rf "$TMP"
  log "installed $NAME $VERSION into $MODS/$FOLDER"
}

# Fetch the newest launch.sh from the repo; only replace this file if it parses.
update_self() {
  [ -n "$REPO" ] && [ "$AUTO_UPDATE" != "0" ] || return
  local NEW="$APP_DIR/launch.sh.new"
  clean curl -sf -m 10 "${AUTH[@]}" -H "Accept: application/vnd.github.raw" \
    "https://api.github.com/repos/$REPO/contents/deploy/deck/launch.sh" -o "$NEW" || { rm -f "$NEW"; return; }
  if bash -n "$NEW" 2>/dev/null && ! cmp -s "$NEW" "$APP_DIR/launch.sh"; then
    chmod +x "$NEW" && mv "$NEW" "$APP_DIR/launch.sh" && log "launcher updated" && UPDATED_SELF=1
  else
    rm -f "$NEW"
  fi
}

load_config() {
  REPO=""; TOKEN=""; TAG="test-latest"; AUTO_UPDATE=1
  # shellcheck disable=SC1090
  [ -f "$CONF" ] && source "$CONF"
  AUTH=(); [ -n "$TOKEN" ] && AUTH=(-H "Authorization: Bearer $TOKEN")
}

# Update the launcher first; if it changed, run the new one right away (once).
load_config
UPDATED_SELF=0
if [ -z "$SE_LAUNCHER_RELAUNCHED" ]; then
  update_self
  [ "$UPDATED_SELF" = 1 ] && SE_LAUNCHER_RELAUNCHED=1 exec "$APP_DIR/launch.sh" "$@"
fi

GAME=$(find_game)
update_mod "$GAME"

# Start the game through SMAPI: swap the plain game binary in Steam's command
# for StardewModdingAPI (same as SMAPI's own '"…/StardewModdingAPI" %command%').
CMD=("$@")
[ -n "$GAME" ] && [ -f "$GAME/StardewModdingAPI" ] && [ ! -x "$GAME/StardewModdingAPI" ] && chmod +x "$GAME/StardewModdingAPI" 2>/dev/null
if [ -n "$GAME" ] && [ -x "$GAME/StardewModdingAPI" ]; then
  for i in "${!CMD[@]}"; do
    case "${CMD[$i]}" in
      */StardewValley|StardewValley) CMD[$i]="$GAME/StardewModdingAPI"; break ;;
    esac
  done
else
  log "SMAPI not found in the game folder; the game will start without mods"
fi
[ ${#CMD[@]} -gt 0 ] || { log "no game command; add %command% to the launch options"; exit 1; }
exec "${CMD[@]}"
