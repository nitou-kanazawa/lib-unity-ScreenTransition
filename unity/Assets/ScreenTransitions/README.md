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

## 使い方

```csharp
using Waribashi.ScreenTransitions;
using Waribashi.ScreenTransitions.Submarine;

// 常駐サービス経由（DontDestroyOnLoad の最前面 Canvas を自動構築）
var transition = ScreenTransitionService.Instance.Use<WaveDiveTransition>();

await ScreenTransitionService.Instance.RunAsync(transition,
    async ct => await SceneManager.LoadSceneAsync("Undersea").ToUniTask(cancellationToken: ct));
```

`RunAsync` は「閉じる → 処理 → 開く」の糖衣です。手で制御する場合は次のようになります。

```csharp
await transition.CloseAsync();          // 完了時点で画面は完全に覆われている
await LoadEverythingAsync();            // この間 HoldLoop が回り続ける
await transition.OpenAsync();
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
- **中断（`CancellationToken` のキャンセル、および例外）は必ず `Open` に着地します。**
  中途姿勢の蓋は破棄されるので画面には何も残りません。`Closing` 中でも `Opening` 中でも同じです。
  呼び出し側には `OperationCanceledException` がそのまま伝わります

```csharp
// 中断しても画面は必ず復帰する（蓋が残って操作不能になることはない）
try
{
    await transition.RunAsync(LoadAsync, ct: token);
}
catch (OperationCanceledException)
{
    // この時点で transition.State == Open、画面には何も覆っていない
}
```

## 同梱している遷移

### ルール画像系（シェーダー + Timeline 駆動）

`TransitionImage` + `TransitionPlayer`。ルール画像 12 種を同梱し、`Tools/Screen Transitions/Generate Rule Textures` で再生成できます。パターン追加はルール画像 1 枚を足すだけです。

### オブジェクト系（UniTask 駆動、素材レス）

`ObjectTransition` 派生の 20 種。子要素は初回再生時に手続き生成するため、アートアセットを必要としません。

| カテゴリ | 遷移 |
|---|---|
| 汎用 | StripeSlide / DiagonalSlab / TilePop / SplitSlam / SlatFlip / SpinSquare / CurtainDrop / ZigzagWipe |
| 潜水艦テーマ | WaveDive / SubBoarding / DepthGauge / BubbleBurst / SonarPing / PeriscopeIris / DeepFade / FishSchool / HatchSlam / DepthZones |
| 没入系 | TownCrowd / SubwayRide |

## 独自の蓋絵を作る

`ObjectTransition` を継承し、3 メソッドを実装します。

```csharp
public class MyTransition : ObjectTransition
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

基底が担保するもの: 状態管理 / `CanvasGroup` によるレイキャスト制御 / `HoldLoop` の起動とキャンセル / `Tween`・`Ease`・`ProceduralSprites` などのヘルパー。

## サンプル

Package Manager の本パッケージのページから **Samples > Demo > Import** で、全遷移を一覧できるデモシーン 2 本とビルダーを取り込めます。

## テスト

`Tests/Runtime`（PlayMode）と `Tests/Editor`（EditMode）を同梱しています。PlayMode 側は `ObjectTransition` の全派生型を反射で列挙してライフサイクル契約を検証するため、遷移を追加すると自動的に対象になります。

Git URL などで導入した（= 変更不可な）パッケージのテストは、既定では Test Runner に出ません。
出したい場合はプロジェクトの `Packages/manifest.json` に次を追加してください。
`Packages/` 直下に置いた埋め込みパッケージの場合は、この指定なしで自動的に認識されます。

```json
"testables": [ "com.waribashi.screen-transitions" ]
```

## ライセンス

**未設定です。** 公開前に決めてください（`package.json` の `license` フィールドと `LICENSE.md` の追加が必要）。
