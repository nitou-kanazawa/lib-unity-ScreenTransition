# Screen Transitions

ルール画像ベースとオブジェクトベースの画面遷移（蓋絵）を、UniTask の `async`/`await` で統一的に扱う Unity ライブラリです。

蓋が閉じ切っている間はループアニメ（HoldLoop）が自動で回るため、シーンロード中に画面が静止しません。

## 必要環境

- Unity 6000.0 以降
- [UniTask](https://github.com/Cysharp/UniTask) 2.5.0 以降 — **手動導入が必要です**

UniTask は公式レジストリに存在しないため `package.json` の `dependencies` には含めていません。
本パッケージを入れる前に、プロジェクトの `Packages/manifest.json` へ次を追加してください。

```json
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
```

`com.unity.timeline` と `com.unity.ugui` は `dependencies` で自動解決されます。

## インストール

Package Manager の **Add package from git URL** に以下を入力します。

```
https://github.com/nitou-kanazawa/lib-unity-ScreenTransition.git?path=unity/Assets/ScreenTransitions
```

## 用語

「蓋絵」= 画面を覆う物、を **Curtain** と呼びます。開ける / 閉じる以上の意味は持たせていません。

| 型 | 役割 |
|---|---|
| `ICurtain` | 蓋絵の契約（`State` / `CloseAsync` / `OpenAsync`） |
| `ObjectCurtain` | オブジェクトベース蓋絵の基底。素材レスで子要素を手続き生成する |
| `RuleImageCurtain` | ルール画像ベース蓋絵の再生役。Timeline で `Cutoff` を駆動する |

## 使い方

```csharp
using Waribashi.ScreenTransitions;

// 常駐サービス経由（DontDestroyOnLoad の最前面 Canvas を自動構築）
var curtain = ScreenTransitionService.Instance.Use<FadeTransition>();

await ScreenTransitionService.Instance.RunAsync(curtain,
    async ct => await SceneManager.LoadSceneAsync("Next").ToUniTask(cancellationToken: ct));
```

`RunAsync` は「閉じる → 処理 → 開く」の糖衣です。手で制御する場合は次のようになります。

```csharp
await curtain.CloseAsync();          // 完了時点で画面は完全に覆われている
await LoadEverythingAsync();         // この間 HoldLoop が回り続ける
await curtain.OpenAsync();
```

### ライフサイクル契約

```
Open(idle) --CloseAsync--> Closing --完了--> Closed(HoldLoop 再生中)
Open(idle) <--完了-- Opening <--OpenAsync-- Closed
```

- `CloseAsync` 完了後〜`OpenAsync` 開始まで、画面は完全に覆われていることを保証します
- `Closed` 中は実装側の `HoldLoop` が自動で回り、`OpenAsync` で自動キャンセルされます
- Busy（`Closing` / `Opening`）中の再入は無視されます
- 時間はすべて `unscaledDeltaTime` 基準なので、`timeScale = 0` にしても完走します

### 中断時の挙動（実装によって異なります）

**`ObjectCurtain` 派生**は、中断（`CancellationToken` のキャンセル、および例外）が必ず `Open` に着地します。
中途姿勢の蓋は破棄されるので画面には何も残りません。`Closing` 中でも `Opening` 中でも同じです。
呼び出し側には `OperationCanceledException` がそのまま伝わります。

```csharp
// 中断しても画面は必ず復帰する（蓋が残って操作不能になることはない）
try
{
    await curtain.RunAsync(LoadAsync, ct: token);
}
catch (OperationCanceledException)
{
    // この時点で curtain.State == Open、画面には何も覆っていない
}
```

**`RuleImageCurtain` は未対応です。** `OpenAsync` のキャンセル時は `Closed` に戻り、ループを再開します。
`ICurtain` 越しに扱うコードで両者を混ぜる場合、中断後の状態が実装によって変わる点に注意してください。

## 同梱している蓋絵

### ルール画像系（シェーダー + Timeline 駆動）

`TransitionImage` + `RuleImageCurtain`。ルール画像 12 種を同梱し、`Tools/Screen Transitions/Generate Rule Textures` で再生成できます。パターン追加はルール画像 1 枚を足すだけです。

### オブジェクト系（UniTask 駆動、素材レス）

`ObjectCurtain` 派生の汎用 9 種。子要素は初回再生時に手続き生成するため、アートアセットを必要としません。

Fade / StripeSlide / DiagonalSlab / TilePop / SplitSlam / SlatFlip / SpinSquare / CurtainDrop / ZigzagWipe

テーマ性の強い蓋絵（潜水艦 10 種・没入系 2 種）は本体には含めず、**サンプルとして同梱**しています。
そのまま使うより、実装例として読んで自分のテーマに置き換えることを想定しています。

## 独自の蓋絵を作る

`ObjectCurtain` を継承し、3 メソッドを実装します。

```csharp
public class MyCurtain : ObjectCurtain
{
    protected override void Build() { /* 子要素を手続き生成 */ }

    protected override async UniTask CloseRoutine()
        => await Tween(0.4f, t => { /* 覆う */ });

    protected override async UniTask OpenRoutine()
        => await Tween(0.4f, t => { /* 開く */ });

    // 任意。Closed 中に回るループ。ct は OpenAsync / 破棄でキャンセルされる
    protected override async UniTask HoldLoop(CancellationToken ct) { /* ... */ }
}
```

基底が担保するもの: 状態管理 / 中断時の `Open` への復帰 / `CanvasGroup` によるレイキャスト制御 /
`HoldLoop` の起動とキャンセル / `Tween`・`Ease`・`ProceduralSprites` などのヘルパー。

## サンプル

Package Manager の本パッケージのページから **Samples > Demo > Import** で取り込めます。内容は次のとおりです。

- 全蓋絵を一覧できるデモシーン 2 本（`TransitionDemo` / `TransitionUndersea`）とシーンビルダー
- テーマ固有の蓋絵 12 種 — 潜水艦 10 種（WaveDive, SonarPing, PeriscopeIris ほか）、没入系 2 種（TownCrowd, SubwayRide）

## テスト

`Tests/Runtime`（PlayMode）と `Tests/Editor`（EditMode）を同梱しています。
PlayMode 側は `ObjectCurtain` の派生型を反射で列挙してライフサイクル契約を検証するため、蓋絵を追加すると自動的に対象になります。
列挙はロード済みアセンブリ全体を走査するので、**サンプル側やプロジェクト側で定義した蓋絵も同じ契約テストにかかります**。

Git URL などで導入した（= 変更不可な）パッケージのテストは、既定では Test Runner に出ません。
出したい場合はプロジェクトの `Packages/manifest.json` に次を追加してください。

```json
"testables": [ "com.waribashi.screen-transitions" ]
```

## ライセンス

**未設定です。** 公開前に決めてください（`package.json` の `license` フィールドと `LICENSE.md` の追加が必要）。
