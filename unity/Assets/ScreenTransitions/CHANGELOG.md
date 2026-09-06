# Changelog

このプロジェクトの変更履歴は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) と
[Semantic Versioning](https://semver.org/lang/ja/) に従います。

## [Unreleased]

### 破壊的変更

- 蓋絵の契約と基底クラスを Curtain 系にリネームした。「画面を覆う物」という以上の意味を
  型名に持たせないため。パッケージ名・namespace・アセンブリ名は `ScreenTransitions` のまま
  - `IScreenTransition` → `ICurtain`
  - `TransitionState` → `CurtainState`
  - `ScreenTransitionExtensions` → `CurtainExtensions`
  - `ObjectTransition` → `ObjectCurtain`
  - `TransitionPlayer` → `RuleImageCurtain`
  - 個別の蓋絵（`FadeTransition` 等）の型名は据え置き。`Curtain` は覆う仕組み、
    `Transition` は個々の演出、という粒度で使い分ける
- テーマ固有の蓋絵 12 種をパッケージ本体から外し、サンプルへ移した（**要 Samples の Import**）
  - 潜水艦 10 種（`WaveDiveTransition` ほか）、没入系 2 種（`TownCrowdTransition` /
    `SubwayRideTransition`）と、それらだけが使う `SilhouetteFactory`
  - namespace も `Waribashi.ScreenTransitions.Demo.Submarine` /
    `.Demo.Immersive` へ移動。サンプルがライブラリの namespace に型を生やさないようにするため
  - 本体に残るのは汎用 9 種とルール画像 12 種。ランタイムのコード量は約 3,700 行から約 1,800 行に

### 変更

- `ICurtain` の契約から「`Closed` 中は画面が完全に覆われている」を降ろした。覆う範囲は
  実装ごとの性質とし、全画面を塗り潰す蓋絵も画面の一部を横切るカットインも同じ `ICurtain`
  として扱えるようにした。シーンロードを隠す用途に全画面の実装が要ることは README に明記
- `ObjectCurtain.BlocksRaycasts`（`virtual`、既定 `true`）を追加。従来は再生中・`Closed` 中の
  レイキャスト遮断が無条件だったため、画面の一部しか覆わない蓋絵が、覆っていない領域の入力まで
  殺していた。`false` を返せば opt-out できる
- 契約テストのレイキャスト検証を、`BlocksRaycasts` の宣言との一致を見る形に変更した。
  併せて opt-out したテスト用の蓋絵 `NonBlockingTestCurtain` を追加し、
  基底クラス単体で契約を満たすことも検証対象にした
- デモの網羅テストがテストアセンブリの蓋絵まで対象にしていたのを除外した

- 配布用の `Samples~/Demo` を追加し、開発用の `Assets/Demo` から同期する仕組みを入れた
  （`tools/sync-samples.sh` と、同期漏れを落とす CI）。`package.json` の `samples` が
  実体の無いパスを指していて Package Manager に失敗する Import ボタンが出ていたのを解消
- 契約テストの型の列挙を、自アセンブリ限定からロード済みアセンブリ全体の走査に変更した。
  テーマ固有の蓋絵が別アセンブリへ移ったことで、対象が「減るだけで赤くならない」形で
  取りこぼされるのを防ぐため。サンプル側とプロジェクト側の蓋絵も同じ契約テストにかかる
- README の中断時の挙動の記述を修正。「必ず `Open` に着地する」は `ObjectCurtain` のみの
  保証であり、`RuleImageCurtain` は `Closed` に戻る。契約として断言していたのを実装別に書き分けた

### 修正

- ルール画像アセットのテストがパッケージのパスを直書きしていたため、移設後に 4 件が落ちていた
  のを修正。テストスクリプト自身の位置からパッケージルートを辿るようにし、`Assets/` 配下でも
  `Packages/` 配下でも通るようにした
- デモシーンに `FadeTransition` が配置されていなかったのを追加（README の「20 種」という
  数え間違いの原因でもあった）。網羅テストが検出した


- 中断（キャンセル・例外）時に蓋が画面に残る問題を修正。`ObjectCurtain` の中断復帰を
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
- `RuleImageCurtain`（Timeline 系）と `ScreenTransitionService` にテストが無い。
  上記の中断復帰は `ObjectCurtain` のみで、`RuleImageCurtain` は未対応
- 「`CloseAsync` 完了時点で画面が完全に覆われている」ことのピクセル単位の検証が無い

## [0.1.0] - 2026-07-30

初回のパッケージ化。

### 追加

- `ICurtain`（`State` + `CloseAsync` / `OpenAsync`）と `RunAsync` 拡張メソッドによる統一 API
- ルール画像系: `UIRuleTransition` シェーダー / `TransitionImage` / `RuleImageCurtain` / Timeline カスタムトラック、ルール画像 12 種
- ルール画像の手続き生成ツール（`Tools/Screen Transitions/Generate Rule Textures`）
- オブジェクト系: `ObjectCurtain` 基底と遷移 20 種（汎用 8 / 潜水艦テーマ 10 / 没入系 2）
- `Closed` 中に自動再生される `HoldLoop`（DeepFade / SonarPing / SplitSlam / SubwayRide / TownCrowd / WaveDive が実装）
- `ScreenTransitionService`（DontDestroyOnLoad 常駐 Canvas + `Use<T>()`）と `LoadingIndicator`
- 素材レスで演出を組むための `ProceduralSprites` / `SilhouetteFactory` / `Ease`
- アセンブリ定義の分離（`ScreenTransitions` / `.Editor` / `.Tests` / `.EditorTests`）
- テスト 178 件: `ObjectCurtain` 全派生型のライフサイクル契約 106 件（PlayMode）、
  Ease / ProceduralSprites / ルール画像アセットの検証 72 件（EditMode）
