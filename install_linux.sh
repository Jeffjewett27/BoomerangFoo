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

GAME_DIR="$(get_game_dir)" || { echo "Could not locate app $appid"; exit 1; }
echo "GAME_DIR=$GAME_DIR"