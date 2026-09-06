# Changelog

このプロジェクトの変更履歴は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) と
[Semantic Versioning](https://semver.org/lang/ja/) に従います。

## [Unreleased]

### 修正

- 中断（キャンセル・例外）時に蓋が画面に残る問題を修正。`ObjectTransition` の中断復帰を
  「常に `Open` へ着地する」方式に統一した。中途姿勢の子要素を破棄し、`Build()` を
  やり直せる状態に戻してルートを非アクティブにする
  - 以前は `CloseAsync` のキャンセル時に `State` だけ `Open` に戻し、子要素は中途姿勢のまま
    表示され続けていた。`blocksRaycasts` は `false` に戻るため「見えているのにクリックが透ける」状態になっていた
  - `OpenAsync` のキャンセル時の着地先を `Closed` から `Open` に変更（**破壊的変更**）。
    半分開いた蓋で `Closed` を主張すると「画面は完全に覆われている」契約を破り、
    利用側は状態不一致で `CloseAsync` も `OpenAsync` も弾かれて画面を復帰できなくなるため
  - `OperationCanceledException` 以外の例外でも同じ復帰処理を通すようにした。
    以前は `Closing` / `Opening` のまま固まり、以降のすべての呼び出しが無視されていた

### 未対応

- ライセンス未設定（`package.json` の `license` と `LICENSE.md`）
- `TransitionPlayer`（Timeline 系）と `ScreenTransitionService` にテストが無い。
  上記の中断復帰は `ObjectTransition` のみで、`TransitionPlayer` は未対応
- 「`CloseAsync` 完了時点で画面が完全に覆われている」ことのピクセル単位の検証が無い

## [0.1.0] - 2026-07-30

初回のパッケージ化。

### 追加

- `IScreenTransition`（`State` + `CloseAsync` / `OpenAsync`）と `RunAsync` 拡張メソッドによる統一 API
- ルール画像系: `UIRuleTransition` シェーダー / `TransitionImage` / `TransitionPlayer` / Timeline カスタムトラック、ルール画像 12 種
- ルール画像の手続き生成ツール（`Tools/Screen Transitions/Generate Rule Textures`）
- オブジェクト系: `ObjectTransition` 基底と遷移 20 種（汎用 8 / 潜水艦テーマ 10 / 没入系 2）
- `Closed` 中に自動再生される `HoldLoop`（DeepFade / SonarPing / SplitSlam / SubwayRide / TownCrowd / WaveDive が実装）
- `ScreenTransitionService`（DontDestroyOnLoad 常駐 Canvas + `Use<T>()`）と `LoadingIndicator`
- 素材レスで演出を組むための `ProceduralSprites` / `SilhouetteFactory` / `Ease`
- アセンブリ定義の分離（`ScreenTransitions` / `.Editor` / `.Tests` / `.EditorTests`）
- テスト 178 件: `ObjectTransition` 全派生型のライフサイクル契約 106 件（PlayMode）、
  Ease / ProceduralSprites / ルール画像アセットの検証 72 件（EditMode）
