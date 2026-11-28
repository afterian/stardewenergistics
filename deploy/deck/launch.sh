#!/bin/bash
# Hand-off launcher. The mod pack (installing and updating every mod on the Steam Decks)
# moved to Stardew Mod Master: https://github.com/afterian/stardewmodmaster
#
# Decks set up before the move still start the game with
#     ~/stardew-energistics/launch.sh %command%
# The old launcher updates itself from this file, so on its next start it becomes this
# hand-off: it sets up ~/stardew-modmaster once and runs the Mod Master launcher, which
# installs and updates the whole pack. Running the Mod Master Deck installer later points
# the Launch Options straight at ~/stardew-modmaster and removes ~/stardew-energistics.
# If the hand-off can't download the Mod Master launcher (no wifi), the game still starts
# through SMAPI with the mods already installed.
# Delete this file once every Deck has run the Mod Master installer.

OLD_DIR="$HOME/stardew-energistics"
NEW_DIR="$HOME/stardew-modmaster"
NEW_REPO="afterian/stardewmodmaster"
LOG="$OLD_DIR/update.log"
mkdir -p "$OLD_DIR" "$NEW_DIR"
log() { echo "$(date '+%F %T') $*" >> "$LOG"; }
clean() { env -u LD_PRELOAD -u LD_LIBRARY_PATH "$@"; }

# Mod Master's repo is public, so no token is needed (an old token may not cover it).
if [ ! -f "$NEW_DIR/config" ]; then
  ( umask 077; printf 'REPO=%s\nTOKEN=\nTAG=test-latest\n' "$NEW_REPO" > "$NEW_DIR/config" )
  log "made $NEW_DIR/config (following $NEW_REPO)"
fi
if [ ! -x "$NEW_DIR/launch.sh" ]; then
  NEW="$NEW_DIR/launch.sh.new"
  if clean curl -sf -m 20 -H "Accept: application/vnd.github.raw" \
       "https://api.github.com/repos/$NEW_REPO/contents/deploy/deck/launch.sh" -o "$NEW" && bash -n "$NEW" 2>/dev/null; then
    chmod +x "$NEW" && mv "$NEW" "$NEW_DIR/launch.sh" && log "downloaded the Mod Master launcher"
  else
    rm -f "$NEW"; log "could not download the Mod Master launcher; starting with the installed mods"
  fi
fi
if [ -x "$NEW_DIR/launch.sh" ]; then
  log "handing over to $NEW_DIR/launch.sh (log: $NEW_DIR/update.log)"
  unset SE_LAUNCHER_RELAUNCHED
  exec "$NEW_DIR/launch.sh" "$@"
fi

# Fallback: start the game through SMAPI with whatever is installed.
find_game() {
  local f lib d
  {
    for f in "$HOME/.local/share/Steam/steamapps/libraryfolders.vdf" "$HOME/.steam/steam/steamapps/libraryfolders.vdf"; do
      [ -f "$f" ] && sed -n 's/^[[:space:]]*"path"[[:space:]]*"\(.*\)"[[:space:]]*$/\1/p' "$f" | sed 's/\\\\/\\/g'
    done
    echo "$HOME/.local/share/Steam"; echo "$HOME/.steam/steam"
  } | while IFS= read -r lib; do
    d="$lib/steamapps/common/Stardew Valley"
    [ -e "$d/StardewModdingAPI" ] && { echo "$d"; break; }
  done
}
GAME=$(find_game)
CMD=("$@")
if [ -n "$GAME" ]; then
  [ -x "$GAME/StardewModdingAPI" ] || chmod +x "$GAME/StardewModdingAPI" 2>/dev/null
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
