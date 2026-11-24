#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$project_dir"
image="${CLICK_BUILD_IMAGE:-clickable/amd64-ut24.04-2.x-arm64:latest}"
mkdir -p artifacts/click
docker run --rm --user "$(id -u):$(id -g)" --mount "type=bind,src=$project_dir,dst=/work" \
  --workdir /work --entrypoint bash "$image" scripts/build-native.sh linux-arm64
dotnet publish src/Calculator/Calculator.csproj -c Release -r linux-arm64 --self-contained true \
  -p:SkipNativeBuild=true -p:UseSharedCompilation=false --disable-build-servers -m:1 -o artifacts/linux-arm64
stage_dir="$(mktemp -d "$project_dir/artifacts/click/stage.XXXXXX")"
mkdir -p "$stage_dir/app"
cp -a artifacts/linux-arm64/. "$stage_dir/app/"
cp packaging/click/* "$stage_dir/"
cp -a third-party "$stage_dir/"
chmod +x "$stage_dir/launch.sh" "$stage_dir/app/Avalonia.Calculator"
desktop-file-validate "$stage_dir/calculator.desktop"
docker run --rm --user "$(id -u):$(id -g)" --mount "type=bind,src=$project_dir/artifacts/click,dst=/work" \
  --workdir /work --entrypoint click "$image" build --no-validate "${stage_dir##*/}"
package_name="$(python3 -c 'import json; m=json.load(open("packaging/click/manifest.json")); print("{name}_{version}_{architecture}.click".format(**m))')"
sha256sum "artifacts/click/$package_name" | tee artifacts/click/SHA256SUMS
docker image inspect "$image" --format '{{.Id}}' > artifacts/click/build-image.txt
set +e
docker run --rm --user "$(id -u):$(id -g)" --mount "type=bind,src=$project_dir/artifacts/click,dst=/work,readonly" \
  --workdir /work --entrypoint click-review "$image" "$package_name" > artifacts/click/review.txt 2>&1
review_status=$?
set -e
cat artifacts/click/review.txt
printf 'Click review exit status: %s\n' "$review_status"
exit "$review_status"
