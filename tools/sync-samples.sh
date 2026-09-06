#!/usr/bin/env bash
#
# 開発用のデモ (unity/Assets/Demo) を、配布用のサンプル
# (unity/Assets/ScreenTransitions/Samples~/Demo) へ複製する。
#
# なぜ複製が要るのか:
#   Samples~ は末尾のチルダによって Unity のアセットパイプラインから無視される。
#   おかげで Package Manager の「Samples > Import」で配れる一方、開発プロジェクトからは
#   編集も再生もできない。そこで実体は Assets/Demo に置いて開発し、そのコピーを
#   Samples~ に持たせる。
#
# なぜリリース時のコピーではダメなのか:
#   UPM は `<repo>.git?path=<pkg>#<tag>` を「そのタグ時点のリポジトリ」から解決する。
#   タグ push をトリガーにしたワークフローは、そのタグの中身をもう変えられない。
#   Samples~ はタグを打つ前のコミットに入っている必要がある。
#
# 使い方:
#   tools/sync-samples.sh          同期する (コミットは自分でする)
#   tools/sync-samples.sh --check  同期済みかを検証する。ズレていたら exit 1 (CI 用)
#
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC="$REPO_ROOT/unity/Assets/Demo"
DST="$REPO_ROOT/unity/Assets/ScreenTransitions/Samples~/Demo"

if [ ! -d "$SRC" ]; then
    echo "error: 同期元が見つかりません: $SRC" >&2
    exit 1
fi

if [ "${1:-}" = "--check" ]; then
    if [ ! -d "$DST" ]; then
        echo "error: $DST がありません。tools/sync-samples.sh を実行してコミットしてください。" >&2
        exit 1
    fi
    if ! diff -r "$SRC" "$DST" > /tmp/sync-samples.diff 2>&1; then
        echo "error: Assets/Demo と Samples~/Demo がズレています。" >&2
        echo "       tools/sync-samples.sh を実行してコミットしてください。" >&2
        echo >&2
        cat /tmp/sync-samples.diff >&2
        exit 1
    fi
    echo "Samples~/Demo は Assets/Demo と同期しています。"
    exit 0
fi

rm -rf "$DST"
mkdir -p "$(dirname "$DST")"
cp -r "$SRC" "$DST"
echo "同期しました: Assets/Demo -> Samples~/Demo"
echo "変更が出た場合はコミットしてください (タグを打つ前に入っている必要があります)。"
