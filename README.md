# Screen Transition

[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE)

## 概要

Unity 向けの画面遷移（蓋絵）ライブラリです。ルール画像ベースとオブジェクトベースの遷移を、
UniTask の `async`/`await` で統一的に扱えます。蓋が閉じ切っている間はループアニメが自動で回るため、
シーンロード中に画面が静止しません。

リポジトリの構成:

| パッケージ | パス | 内容 |
|---|---|---|
| `com.waribashi.screen-transitions` | `unity/Assets/ScreenTransitions` | パッケージ本体 |
| （サンプル） | `unity/Assets/Demo` | デモシーンとテーマ固有の蓋絵。リリース時に本体の `Samples~/Demo` へ同期される |

## 特徴

- `ICurtain`（`CloseAsync` / `OpenAsync` / `RunAsync`）による統一 API
- 蓋絵を 21 種同梱（ルール画像 12 種 + 素材レスのオブジェクト系 9 種）。テーマ固有の 12 種はサンプル同梱
- `ObjectCurtain` 派生は中断（キャンセル・例外）が必ず「蓋が開いた状態」に着地する。画面が覆われたまま固まらない
- 時間は `unscaledDeltaTime` 基準なので `timeScale = 0` でも完走する
- Timeline カスタムトラック対応

## セットアップ
#### 要件 / 開発環境
- Unity 6000.0
- [UniTask](https://github.com/Cysharp/UniTask) 2.5.0 以降（公式レジストリに無いため手動導入が必要）

#### インストール

1. Window > Package ManagerからPackage Managerを開く
2. 「+」ボタン > Add package from git URL
3. 以下のURLを入力する
```
https://github.com/nitou-kanazawa/lib-unity-ScreenTransition.git?path=unity/Assets/ScreenTransitions
```

あるいはPackages/manifest.jsonを開き、dependenciesブロックに以下を追記
```
{
    "dependencies": {
        "com.waribashi.screen-transitions": "https://github.com/nitou-kanazawa/lib-unity-ScreenTransition.git?path=unity/Assets/ScreenTransitions",
        "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
    }
}
```


## ドキュメント

- [パッケージ README](unity/Assets/ScreenTransitions/README.md) — 使い方・ライフサイクル契約・遷移一覧
- [設計メモ](unity/Assets/ScreenTransitions/Documentation~/DESIGN.md)
- [CHANGELOG](unity/Assets/ScreenTransitions/CHANGELOG.md)

## 開発

デモの実体は `unity/Assets/Demo` にあります。パッケージが `Assets/` 配下にあるため、
配布用の `Samples~/Demo` は Unity から無視され、開発プロジェクトでは編集も再生もできません。
そこで実体を `Assets/Demo` で開発し、そのコピーを `Samples~` に持たせています。

デモを変更したら同期してコミットしてください。

```bash
tools/sync-samples.sh
```

UPM は `?path=...#<tag>` を「そのタグ時点のリポジトリ」から解決するため、`Samples~` は
**タグを打つ前のコミットに入っている必要があります**（タグ push をトリガーにした
ワークフローでは間に合いません）。同期漏れは CI (`.github/workflows/samples.yml`) と
リリースワークフローの両方で検出して落とします。
