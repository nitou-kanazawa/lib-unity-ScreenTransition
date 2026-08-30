---
description: PRのタイトルと説明を生成して作成
allowed-tools: Bash(git log:*), Bash(git diff:*), Bash(git branch:*), Bash(gh pr:*), Bash(gh issue:*)
---

# pr

現在のブランチからPull Requestを作成する。

## 前提条件

- GitHub CLI (`gh`) がインストール済み
- `gh auth login` で認証済み

## 手順

### 1. 現在のブランチ情報を取得

```bash
git branch --show-current
git log main..HEAD --oneline
git diff main...HEAD --stat
```

### 2. 関連Issueの確認

ブランチ名やコミットメッセージからIssue番号を検出した場合：
```bash
gh issue view <issue-number>
```

### 3. PR内容の生成

以下の形式でPR内容を生成する：

```markdown
## タイトル
<type>: <変更の要約>

## 概要
<!-- 変更の目的と背景 -->

## 変更内容
- <主な変更点1>
- <主な変更点2>
- <主な変更点3>

## 関連Issue
- closes #<issue-number> （あれば）

## テスト
- [ ] 単体テスト
- [ ] 動作確認

## スクリーンショット
<!-- UI変更がある場合 -->

## レビュー観点
<!-- レビュアーに見てほしいポイント -->
```

### 4. 確認

```
📝 PR作成

タイトル: <title>
ベースブランチ: main
ヘッドブランチ: <current-branch>

---
<生成した説明文>
---

この内容でPRを作成しますか？ (yes/no/edit)
- yes: PRを作成
- no: キャンセル
- edit: 内容を修正
```

### 5. PR作成

```bash
gh pr create --title "<title>" --body "<body>" --base main
```

### 6. 完了報告

```
✅ PRを作成しました

URL: <pr-url>
タイトル: <title>
```

## オプション

引数で追加オプションを指定可能：

- `--draft`: ドラフトPRとして作成
- `--reviewer <user>`: レビュアーを指定
- `--label <label>`: ラベルを付与

例: `/project:pr --draft --reviewer teammate`

## Conventional Commits タイプ

| タイプ | 用途 |
|--------|------|
| feat | 新機能 |
| fix | バグ修正 |
| refactor | リファクタリング |
| docs | ドキュメント |
| style | フォーマット変更 |
| test | テスト |
| chore | 雑務 |
| perf | パフォーマンス改善 |
