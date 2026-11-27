#!/bin/bash
# One-time setup on each Steam Deck (Desktop Mode > Konsole). Usage:
#   bash install.sh OWNER/REPO [TOKEN]
# TOKEN is a GitHub fine-grained token with read-only "Contents" access
# to this one repo. Leave it out if the repo is public.
set -e
REPO="$1"; TOKEN="$2"
[ -n "$REPO" ] || { echo "Usage: bash install.sh OWNER/REPO [TOKEN]"; exit 1; }
DIR="$HOME/stardew-energistics"; mkdir -p "$DIR"
printf 'REPO=%q\nTOKEN=%q\n' "$REPO" "$TOKEN" > "$DIR/config"
chmod 600 "$DIR/config"
AUTH=(); [ -n "$TOKEN" ] && AUTH=(-H "Authorization: Bearer $TOKEN")
curl -fsSL "${AUTH[@]}" -H "Accept: application/vnd.github.raw" \
  "https://api.github.com/repos/$REPO/contents/deploy/deck/launch.sh" -o "$DIR/launch.sh"
chmod +x "$DIR/launch.sh"
echo
echo "Done. Now in Steam: Stardew Valley > Properties > Launch Options, paste:"
echo
echo "    ~/stardew-energistics/launch.sh %command%"
echo
echo "Then start the game normally. Update log: ~/stardew-energistics/update.log"
