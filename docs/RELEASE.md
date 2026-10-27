# Day102 Twin Bell Shooter — 開発・リリース手順書

認証情報（トークン・パスワード）はこの文書にも Git にも書かない。

## 0. 対象
| 項目 | 値 |
|---|---|
| 編集場所 | `/Volumes/開発用SSD/開発データ/Unity/Projects/TitanUnity100/Days/day102/Project` |
| Unity | 6000.3.26f1（Unity Hub に「Project（…/day102/Project）」で登録済み） |
| シューターのコード | `Assets/Scripts/Tb*.cs`、`Assets/Editor/TbBuild.cs`、シーン `Assets/Scenes/TbShooter.unity` |
| 触らないもの | ランナー試作（`Day102Bootstrap.cs` 等）と `TitanBuild.cs` |
| ソース保存先 | GitHub `titan11111/336-twin-bell-shooter`（main ブランチ） |
| 公開先 | 同リポジトリの `gh-pages` ブランチ → https://titan11111.github.io/336-twin-bell-shooter/ |

## 1. 編集 → 直接編集
- C# は `Assets/Scripts/Tb*.cs` を直接編集し、関連する修正はまとめて保存する。
- シーンは手で配線しない（`TbShooterGame` がカメラ・UI・コントローラーをコードで作る）。

## 2. コンパイル
- Unity が前面にあるままファイルを書き換えると自動更新されない → **Assets > Refresh（⌘R）**。
- Console のエラー 0 を確認（既存ランナーの `Obstacle.cs` の非推奨警告 1 件は既知）。
- 「コンパイル成功」と「動作成功」は分けて記録する。

## 3. Play 確認（マウス）
`Assets/Scenes/TbShooter.unity` を開いて Play。ゲームビュー下 30% がコントローラー。
確認項目（まとめて一度に試す）：
1. 十字キーで移動（加速・減速、機体が傾く）、画面端で止まる
2. A タップで連射・ボタンが沈み波紋が出る／A・B 長押しでため → 離すとバズーカ
3. ゲーム画面をなぞると指の移動量だけ移動＋自動連射、離すと射撃が止まる
4. 命中で白フラッシュ・ノックバック、撃破で爆発・スコア加算、敵弾（マゼンタ）が見える
5. SELECT で揺れ 強/弱/OFF（2 連打しても 1 回分：Console に `[Tb] SELECT -> shake level` が 1 行）
6. START で一時停止、ゲームオーバー／クリア後は START か A でリトライ
- エディタ専用：`9` キー＝ボス直前へワープ＋無敵（ボス確認用）。
- マウス確認とスマホ実機確認は分けて記録する。

## 4. Web ビルド
- メニュー **Titan > Tb Shooter > Build Web**
- 出力：`day102/Builds/TbWeb/`（`index.html`、`Build/TbWeb.{loader.js,framework.js,data,wasm}`、`TemplateData/`）
- 圧縮は Disabled（GitHub Pages で確実に動かすため）。`index.html` にはスマホ用のズーム／スクロール／長押しメニュー防止が自動で入る。
- 結果は `Builds/TbWeb/build_result.txt`（例：`Succeeded 39739886 bytes 0 errors`）。古いビルドと混同しないよう日時を確認する。

## 5. Git 差分確認 → Push（ソース）
- 含める：`Assets`（.meta 含む）、`Packages`、`ProjectSettings`、`README.md`、`.gitignore`、`docs/`、`CLAUDE.md`、`AGENTS.md`
- 除外：`Library/ Temp/ Obj/ Logs/ UserSettings/ Builds/`、バックアップ（`_tb_backup/`）、キャッシュ、認証情報
- `git status` と `git ls-files` で追加前後の一覧を確認し、秘密情報が無いことを確かめてから main に push。
- push 後、リモートのコミット ID が手元と一致することを確認する。

## 6. Web 公開（ビルド生成物）
- `gh-pages` ブランチの直下に `index.html`・`Build/`・`TemplateData/`・空の `.nojekyll` を置いて push（ソースとは別ブランチ）。
- Settings > Pages：Source = Deploy from a branch、Branch = `gh-pages` / `(root)`。
- プレイ URL を `?fresh=日時` 付きで開き、ロード・開始・移動・射撃を確認する。

## 7. Slack 報告
- 送信先 `#general`（ID `C0AMVQGBWFP`）。送信前にチャンネル名と ID の一致を確認。
- 「day102 更新」：主な変更／検証済み／未確認／プレイ URL（公開成功時のみ）／リポジトリ URL／コミット ID。1 回だけ送信し、投稿リンクを確認する。

## 確認方法の区別
- コンパイル成功 ≠ 動作成功。マウス確認 ≠ スマホ実機確認。ブラウザのスマホ擬似操作（Playwright・タッチ再現）は「実機」ではない。
- 実機で試せない項目は「未確認」と書く。
