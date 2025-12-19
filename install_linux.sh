appid=965680

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
        manifest="$lib/steamapps/appmanifest_${appid}.acf"
        if [[ -f "$manifest" ]]; then
          installdir=$(awk -F'"' '/"installdir"/ {print $4; exit}' "$manifest")
          echo "$lib/steamapps/common/$installdir"
          return 0
        fi
      done < <(grep -oE '"path"[[:space:]]+"[^"]+"' "$libfile" | sed -E 's/^"path"[[:space:]]+"([^"]+)".*$/\1/')
    done

    # fallback: default library under that root
    manifest="$root/steamapps/appmanifest_${appid}.acf"
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