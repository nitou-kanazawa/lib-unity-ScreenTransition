# Screen Transition

[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE)

## 概要

Unity 向けの画面遷移（蓋絵）ライブラリです。ルール画像ベースとオブジェクトベースの遷移を、
UniTask の `async`/`await` で統一的に扱えます。蓋が閉じ切っている間はループアニメが自動で回るため、
シーンロード中に画面が静止しません。

収録パッケージ:

| パッケージ | パス | 内容 |
|---|---|---|
| `com.waribashi.screen-transitions` | `unity/Assets/ScreenTransitions` | 画面遷移本体 |

## 特徴

- `IScreenTransition`（`CloseAsync` / `OpenAsync` / `RunAsync`）による統一 API
- 遷移 32 種を同梱（ルール画像 12 種 + オブジェクト系 20 種）。オブジェクト系は素材レス
- 中断（キャンセル・例外）は必ず「蓋が開いた状態」に着地する。画面が覆われたまま固まらない
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
