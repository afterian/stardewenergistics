#!/bin/bash
# One-time setup on each Steam Deck (Desktop Mode > Konsole). Usage:
#   bash install.sh OWNER/REPO [TOKEN]
# TOKEN is a GitHub fine-grained token with read-only "Contents" access
# to this one repo. Leave it out if the repo is public. Running it again
# just replaces the saved token (use that when a token expires).
set -e
REPO="$1"; TOKEN="$2"
[ -n "$REPO" ] || { echo "Usage: bash install.sh OWNER/REPO [TOKEN]"; exit 1; }
DIR="$HOME/stardew-energistics"; mkdir -p "$DIR"
AUTH=(); [ -n "$TOKEN" ] && AUTH=(-H "Authorization: Bearer $TOKEN")

echo "Checking the token..."
CODE=$(curl -s -o /dev/null -w '%{http_code}' "${AUTH[@]}" "https://api.github.com/repos/$REPO")
case "$CODE" in
  200) echo "  OK, the Deck can see $REPO." ;;
  401) echo "  GitHub says the token is wrong or expired. Make a new one and try again."; exit 1 ;;
  404) echo "  GitHub can't find $REPO with this token. Check the token's repository access."; exit 1 ;;
  *)   echo "  Could not reach GitHub (answer: $CODE). Check the wifi and try again."; exit 1 ;;
esac

( umask 077; printf 'REPO=%q\nTOKEN=%q\nTAG=test-latest\n' "$REPO" "$TOKEN" > "$DIR/config" )
curl -fsSL "${AUTH[@]}" -H "Accept: application/vnd.github.raw" \
  "https://api.github.com/repos/$REPO/contents/deploy/deck/launch.sh" -o "$DIR/launch.sh"
chmod +x "$DIR/launch.sh"

GAME=""
for d in "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" /run/media/*/steamapps/common/"Stardew Valley" /run/media/*/*/steamapps/common/"Stardew Valley"; do
  [ -d "$d" ] && { GAME="$d"; break; }
done
echo
if [ -z "$GAME" ]; then
  echo "Warning: Stardew Valley's folder wasn't found. Install the game first."
elif [ ! -x "$GAME/StardewModdingAPI" ]; then
  echo "Warning: SMAPI isn't installed in $GAME yet. Install it (guide step 2), then carry on."
else
  echo "Found the game and SMAPI in: $GAME"
fi
echo
echo "Done. Now in Steam: Stardew Valley > Properties > Launch Options, paste:"
echo
echo "    ~/stardew-energistics/launch.sh %command%"
echo
echo "Then start the game normally. Update log: ~/stardew-energistics/update.log"
