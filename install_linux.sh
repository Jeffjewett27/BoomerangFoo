#!/usr/bin/env bash
set -u

APPID="965680"
REPO="Jeffjewett27/BoomerangFoo"

say() { printf '%s\n' "$*"; }
warn() { printf 'WARN: %s\n' "$*" >&2; }
die() { printf 'ERROR: %s\n' "$*" >&2; exit 1; }

pt() {
  say "Running protontricks with args: $@"
  if command -v protontricks >/dev/null 2>&1; then
    protontricks "$@"
  elif command -v flatpak >/dev/null 2>&1 && flatpak info com.github.Matoking.protontricks >/dev/null 2>&1; then
    flatpak run com.github.Matoking.protontricks "$@"
  else
    return 127
  fi
}

need_cmd() { command -v "$1" >/dev/null 2>&1 || die "Missing dependency: $1"; }

# likely Steam roots on Steam Deck (native + Flatpak)
steam_roots=(
  "$HOME/.local/share/Steam"
  "$HOME/.steam/root"
  "$HOME/.var/app/com.valvesoftware.Steam/.local/share/Steam"
)

get_game_dir() {
  local root lib manifest installdir
  for root in "${steam_roots[@]}"; do
    # common place for libraries metadata
    for libfile in "$root/steamapps/libraryfolders.vdf" "$root/config/libraryfolders.vdf"; do
      [[ -f "$libfile" ]] || continue

      # grab library paths from libraryfolders.vdf (good enough for Deck layouts)
      while IFS= read -r lib; do
        manifest="$lib/steamapps/appmanifest_${APPID}.acf"
        if [[ -f "$manifest" ]]; then
          installdir=$(awk -F'"' '/"installdir"/ {print $4; exit}' "$manifest")
          [[ -n "${installdir:-}" ]] || continue
          echo "$lib/steamapps/common/$installdir"
          return 0
        fi
      done < <(grep -oE '"path"[[:space:]]+"[^"]+"' "$libfile" | sed -E 's/^"path"[[:space:]]+"([^"]+)".*$/\1/')
    done

    # fallback: default library under that root
    manifest="$root/steamapps/appmanifest_${APPID}.acf"
    if [[ -f "$manifest" ]]; then
      installdir=$(awk -F'"' '/"installdir"/ {print $4; exit}' "$manifest")
      echo "$root/steamapps/common/$installdir"
      return 0
    fi
  done
  return 1
}

# --- main ---
need_cmd curl
need_cmd unzip
need_cmd awk
need_cmd grep
need_cmd sed

TAG="${1:-latest}"

GAME_DIR="$(get_game_dir)" || die "Could not locate Boomerang Fu install folder (AppID $APPID). Is it installed?"
[[ -d "$GAME_DIR" ]] || die "Game folder not found: $GAME_DIR"

say "Game folder: $GAME_DIR"

api_url=""
if [[ "$TAG" == "latest" ]]; then
  api_url="https://api.github.com/repos/$REPO/releases/latest"
else
  api_url="https://api.github.com/repos/$REPO/releases/tags/$TAG"
fi

say "Fetching release metadata: $TAG"
json="$(curl -fsSL -H "Accept: application/vnd.github+json" "$api_url")" \
  || die "Failed to fetch release info for '$TAG'"

# Pick the first .zip asset (prefer BoomerangFoo-*.zip if present)
dl_url="$(
  printf '%s' "$json" \
  | grep -oE '"browser_download_url"[[:space:]]*:[[:space:]]*"[^"]+"' \
  | sed -E 's/^.*"browser_download_url"[[:space:]]*:[[:space:]]*"([^"]+)".*$/\1/' \
  | grep -E 'BoomerangFoo-.*\.zip$' \
  | head -n 1
)"

if [[ -z "${dl_url:-}" ]]; then
  dl_url="$(
    printf '%s' "$json" \
    | grep -oE '"browser_download_url"[[:space:]]*:[[:space:]]*"[^"]+"' \
    | sed -E 's/^.*"browser_download_url"[[:space:]]*:[[:space:]]*"([^"]+)".*$/\1/' \
    | grep -E '\.zip$' \
    | head -n 1
  )"
fi

[[ -n "${dl_url:-}" ]] || die "Could not find a .zip asset in the release ($TAG)."

tmpdir="$(mktemp -d)"
zipfile="$tmpdir/mod.zip"

say "Downloading: $dl_url"
curl -fL "$dl_url" -o "$zipfile" || die "Download failed"

say "Installing (overwrite matching files; do not delete anything)..."
# -o overwrite, -q quiet; unzip never deletes extra files in destination
unzip -o -q "$zipfile" -d "$GAME_DIR" || die "Unzip failed"

# Proton/Wine DLL override (optional)
if  pt --version >/dev/null 2>&1; then
  say "Setting Wine DLL override winhttp=native,builtin (via protontricks)..."
  say "Running protontricks with appid $APPID"
#   if pt -c \
#     "wine reg add 'HKCU\Software\Wine\DllOverrides' /v winhttp /t REG_SZ /d native,builtin /f" "$APPID" 2>&1; then
#     say "DLL override set (or already was set)."
#   else
#     warn "protontricks is installed but the registry command failed."
#     warn "You can do it manually in Proton/Wine: winecfg -> Libraries -> add override for winhttp (native,builtin)."
#   fi
  flatpak run com.github.Matoking.protontricks -c "wine reg add 'HKCU\Software\Wine\DllOverrides' /v winhttp /t REG_SZ /d native,builtin /f" "$APPID"
else
  warn "protontricks not installed; skipping Wine DLL override."
  warn "If the mod doesn't load under Proton, install protontricks and add winhttp override (native,builtin)."
fi

say "Done."
say "Tip: launch the game once, then check: $GAME_DIR/BepInEx/LogOutput.log"

if [[ -t 0 ]]; then
  echo
  read -r -p "Press Enter to close..." _
fi