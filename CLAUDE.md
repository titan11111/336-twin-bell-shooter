# Project entry
@../../../Rules/CORE.md
@../../../Rules/INPUT.md
@../../../Rules/WORKFLOW.md
@../../../Rules/PUBLISH.md
@../../../Knowledge/INDEX.md
@../SPEC.md
@../HANDOFF.md

# Day102 Tb Shooter — 作業ルール（2026-10-27 追記）
- 依頼範囲を守る。検証依頼から機能追加（ボス・チャージ・新UI等）へ広げない。追加したくなったら提案だけする。
- 勝手に削除・巻き戻ししない。変更前に差分（Gitが無い場合はバックアップとmd5）を確認し、既存の変更を守る。
- C#は直接ファイル編集し、関連修正はまとめて保存する。Unity画面操作は「コンパイル結果」と「実際の動作確認」に絞る。
- Unity再起動・強制再インポート・設定変更は根拠がある時だけ。
- 同じ失敗が2回続いたら操作を繰り返さず、ログ／コードから原因を調べる。解決できなければ止めて報告。
- 検証用機能は既存のものを優先（新設は最小限）。
- 報告では「コンパイル成功」と「動作成功」、「マウス確認」と「スマホ実機確認」を区別する。未実施は未確認と書く。
- 開始時に所要時間の目安、5分超の作業は約5分ごとに「完了／動作確認済み／詰まり／次の一手」を報告。

# Day102 Tb Shooter — 環境メモ（確認できた事実のみ）
- 対象：/Volumes/開発用SSD/開発データ/Unity/Projects/TitanUnity100/Days/day102/Project（Gitリポジトリではない）
- Unity：6000.3.26f1（Built-in描画、Input Manager＝Both）。Unity Hubに「Project（…/day102/Project）」として登録済み。
- シューターのコード：Assets/Scripts/Tb*.cs、Assets/Editor/TbBuild.cs。ランナー（Day102Bootstrap等・TitanBuild.cs）は別物で触らない。
- 起動：Assets/Scenes/TbShooter.unity を開いてPlay（シーン配線不要。Day102Bootstrapが無いシーンならTbShooterGameが自動起動）。
- 再コンパイル：Unityが前面のままファイルを書き換えると自動更新されない → Assets > Refresh（⌘R）を使う。
- 検証：Playでマウス操作（画面下30%がコントローラー）。エディタ専用の9キー＝ボス直前ワープ＋無敵。
- Webビルド：メニュー Titan > Tb Shooter > Build Web → day102/Builds/TbWeb（圧縮なし、index.htmlにスマホ用のズーム/スクロール防止を自動追記、結果は build_result.txt）。
- 公開先の前例：Unityの日次作品は「1日1リポジトリ＋Webビルドのみ」をGitHub Pagesで公開（例 titan11111/335-coin-garden）。Day102シューターは未公開。

- リリース手順の詳細：docs/RELEASE.md（編集→直接編集→コンパイル→Play確認→Webビルド→Git差分確認→Push→Web公開確認→Slack報告）。
- ソース保存先：GitHub titan11111/336-twin-bell-shooter（main）。公開：同リポジトリ gh-pages ブランチ。
