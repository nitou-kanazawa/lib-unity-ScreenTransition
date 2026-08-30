---
description: Conventional形式でブランチを作成
argument-hint: [type] [description]
allowed-tools: Bash(git status:*), Bash(git branch:*), Bash(git checkout:*), Bash(git switch:*)
---

# branch

Conventional形式でブランチを作成して切り替える。

## 引数

- $1: タイプ (feat / fix / refactor / docs / chore / test / hotfix)
- $2: 説明（英語、ケバブケース）

引数がない場合は、対話的にタイプと説明を確認する。

## ブランチ命名規則

`<type>/<description>`

例:
- `feat/user-authentication`
- `fix/null-reference-error`
- `refactor/extract-helper-class`
- `docs/update-readme`

## 手順

### 1. 現在の状態を確認

```bash
git status
git branch --show-current
```

### 2. 未コミットの変更チェック

未コミットの変更がある場合：
```
⚠️ 未コミットの変更があります

変更ファイル:
- <ファイル一覧>

続行しますか？ (yes/no/stash)
- yes: そのまま続行
- no: キャンセル
- stash: 変更をstashしてから続行
```

### 3. ブランチ名の生成と確認

```
🌿 ブランチ作成

タイプ: <type>
説明: <description>
ブランチ名: <type>/<description>

このブランチを作成しますか？ (yes/no/edit)
```

### 4. ブランチ作成＆切り替え

```bash
git switch -c <branch-name>
```

### 5. 完了報告

```
✅ ブランチを作成しました: <branch-name>
```

## タイプ一覧

| タイプ | 用途 |
|--------|------|
| feat | 新機能開発 |
| fix | バグ修正 |
| refactor | リファクタリング |
| docs | ドキュメント |
| chore | 雑務（設定変更等） |
| test | テスト追加・修正 |
| hotfix | 緊急修正 |
