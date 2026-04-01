# LineBrowsers

ブラウザのWebViewを横に並べる簡易アプリです。

[ダウンロードページ](https://github.com/azulamb/LineBrowsers/releases)

## 機能

### セッション

新規パネルを追加する際に既存セッションの使いまわしをすることで、同じSNSの別画面を別ラインで開くことができます。

### 遷移/プレビュー

操作方法や遷移先ドメインによって挙動が変わります。

* リンクをクリック
  * 同じドメインなら同じ画面で開く
  * 違うドメインならプレビューで開く
   * プレビューはクリックしたセッションで継続
* リンクをホイールクリック
  * デフォルトブラウザで開く

### スマートフォンモード

スマートフォンモードにするとデバイスエミュレーションで動作します。

### JS/CSSの注入

WebView毎にJSとCSSを注入できます。作者が使っているものはリポジトリの [memo/](https://github.com/azulamb/LineBrowsers/tree/main/memo) にあります。

### プライベートモード

引数に `--private` を追加すると完全に設定等を引き継がず保存しないプライベートモードで起動します。

```bat
@echo off
start "" "%~dp0LineBrowsers.exe" --private
```

## 開発環境

* VisualStudio 2026
