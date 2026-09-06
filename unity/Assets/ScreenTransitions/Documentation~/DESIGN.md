# ScreenTransitions 設計整理（v0.2 ドラフト）

2026-07-23 時点の現状把握と、閉じ中ループ / UniTask 化を含む仕様案。

## 1. 現状の実装

2系統が並立しており、共通インターフェースはまだ無い。

### A. ルール画像系（シェーダー駆動）
- `TransitionImage` : uGUI Image + 専用シェーダー。`Cutoff`(0..1) と `RuleTexture` が状態のすべて
- `TransitionPlayer` : PlayableDirector をラップ。閉じ/開き2本の TimelineAsset（カスタム `TransitionTrack` が Cutoff を書く）を再生
- パターン追加 = ルール画像1枚（12種同梱）

### B. オブジェクト系（コルーチン駆動）
- `ObjectTransition` 基底 : 子要素を初回再生時に手続き生成、`CloseRoutine`/`OpenRoutine` をコルーチンで実装、
  CanvasGroup で「再生中・閉じ中のみレイキャスト遮断」を自動管理
- 汎用8種 + 潜水艦テーマ10種。`ProceduralSprites`（円/リング/三角/グラデ/アイリス孔）で素材レス
- Timeline は使っていない

### 共通機能
- `PlaySequence(holdSeconds, LoadingIndicator, onClosed, onComplete)` : 閉じ→保持→開き
- `LoadingIndicator` : スピナー+NOW LOADING。トランジションより前面に配置

### 現状の課題
1. 2系統の API が別物（呼び出し側が型で分岐する必要がある）
2. ライフサイクル（状態遷移）が暗黙的で、閉じ切り中は静止画（画面が死ぬ）
3. コールバック地獄になりがち（コルーチン + Action）
4. `Time.deltaTime` 依存 → ロード中に timeScale=0 にするゲームで止まる
5. 実シーンロードを跨ぐ常駐（DontDestroyOnLoad）の作法が未定義

## 2. 仕様案: ライフサイクルの明文化

```
Open(idle) --CloseAsync--> Closing --完了--> Closed(HoldLoop 再生中)
Open(idle) <--完了-- Opening <--OpenAsync-- Closed
```

- **Closed 中は HoldLoop（ループアニメ）が自動再生される**。これが「画面が死なない」の正体
- 契約: `CloseAsync` 完了後〜`OpenAsync` 開始まで、画面は完全に覆われていることを保証する
- Busy 中の再入は v1 では拒否（将来: 進行度から逆再生できる Reversible 対応を検討）
- 時間は `Time.unscaledDeltaTime` 基準（ロード中の timeScale 操作に耐える）
- **中断（キャンセル・例外）の着地先は Closing / Opening のどちらからでも `Open`**。
  中途姿勢の子要素は破棄してルートを非アクティブにし、画面には何も残さない。
  次回 `CloseAsync` は `Build()` からやり直す。
  半端に閉じた蓋を残したまま `Closed` を主張すると、利用側は状態不一致で
  `CloseAsync` も `OpenAsync` も弾かれ、画面を復帰する手段が無くなるため

## 3. 利用側（呼び出す側）の API 案

```csharp
public interface IScreenTransition
{
    TransitionState State { get; }               // Open / Closing / Closed / Opening
    UniTask CloseAsync(CancellationToken ct = default);
    UniTask OpenAsync(CancellationToken ct = default);
}

// 典型的な利用（HoldLoop は Closed 中に勝手に回る）
await transition.CloseAsync();
await SceneManager.LoadSceneAsync("Undersea");   // この間ループアニメ + ローディング表示
await transition.OpenAsync();

// 糖衣: 実処理を渡す形
await transition.RunAsync(async ct => await SceneManager.LoadSceneAsync("Undersea"), loadingIndicator);
```

- 固定秒の `PlaySequence(2f, ...)` は `RunAsync(ct => UniTask.Delay(...))` の糖衣として残す
- LoadingIndicator は従来通り「上に重ねる別レイヤー」。HoldLoop（蓋絵自身の動き）とは役割を分ける

## 4. 独自蓋絵を作る側の API 案

```csharp
public abstract class ObjectTransition : MonoBehaviour, IScreenTransition
{
    protected abstract UniTask CloseRoutine(CancellationToken ct);
    protected abstract UniTask OpenRoutine(CancellationToken ct);

    /// Closed 中に回るループ。省略可（デフォルトは静止）。
    /// OpenAsync が呼ばれると ct がキャンセルされるので、無限ループで書いてよい。
    protected virtual async UniTask HoldLoop(CancellationToken ct)
    {
        await UniTask.WaitUntilCanceled(ct);
    }
}
```

- 基底が担保するもの: 状態管理 / CanvasGroup レイキャスト管理 / HoldLoop の起動とキャンセル / Tween ヘルパー / Ease / ProceduralSprites
- 作者が書くのは Close / Open / (任意で HoldLoop) の3メソッドだけ、という契約は現行を維持

### HoldLoop の実装例（派手すぎない常時ループ）
- WaveDive: 気泡がゆっくり立ち上り続ける
- SonarPing: スイープが回り続け、輝点がたまに明滅
- DeepFade: マリンスノーが漂い続ける
- 幾何学系: アクセントラインが低速で明滅 or 数ピクセルのドリフト

## 5. ルール画像系（Timeline）のループ方針

**「ループ用 Timeline + 再生制御側で切り替え」を採用**（カスタムのループ検知トラックは作らない）。

- Close.playable（末尾で Cutoff=1 のまま）→ 再生完了を検知したら Loop.playable に差し替えて
  `DirectorWrapMode.Loop` で回す → OpenAsync で Open.playable に差し替え
- 理由: Timeline には WrapMode.Loop が標準であり、「終了を検知してループする拡張トラック」を作っても
  結局アセットを分けた方がデザイナーが編集しやすい。制御側スイッチが最小実装で最も柔軟
- Loop.playable の中身例: Cutoff を 1.0↔0.97 で微揺らし（呼吸）、ルール画像の UV スクロール、
  ビネットの明滅など。カスタムトラックを足す場合も「ループ構造」ではなく「揺らし対象」を増やす方向

## 6. UniTask 化

- 依存: `com.cysharp.unitask`（Git URL 導入）。パッケージ公開時は asmdef の versionDefines で
  `UNITASK_SUPPORT` を切る手もあるが、まずはハード依存でシンプルに
- 置き換え: コルーチン `Tween` → `async UniTask Tween(float, Action<float>, CancellationToken)`
  （`UniTask.Yield(PlayerLoopTiming.Update, ct)` ベース、unscaled 時間）
- 既存の `PlayClose(Action)` 系は [Obsolete] で残すか、デモだけ移行して破棄するか要判断

## 7. 未決事項（要確認）

1. UniTask をハード依存にしてよいか（パッケージ利用者に UniTask 導入を強制する）
2. 旧コールバック API の扱い（残す / 消す）
3. シーンロード実運用: 常駐 TransitionCanvas プレハブ + DontDestroyOnLoad + サービス化
   （`ScreenTransitionService.Instance.RunAsync(...)`）まで踏み込むか
4. Busy 中再入の仕様は「拒否」で確定してよいか
