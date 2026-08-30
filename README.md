# <パッケージ名>

[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE)

## 概要


## 特徴



## セットアップ
#### 要件 / 開発環境
- Unity 6000.0

#### インストール

1. Window > Package ManagerからPackage Managerを開く
2. 「+」ボタン > Add package from git URL
3. 以下のURLを入力する
```
https://github.com/nitou-kanazawa/<リポジトリ名>.git?path=<パッケージパス>
```

あるいはPackages/manifest.jsonを開き、dependenciesブロックに以下を追記
```
{
    "dependencies": {
        "jp.nitou.<パッケージ名>": "https://github.com/nitou-kanazawa/<リポジトリ名>.git?path=<パッケージパス>"
    }
}
```


## ドキュメント
