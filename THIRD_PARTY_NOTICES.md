# 第三者ライセンス

Docklet 自体の [MIT License](LICENSE) は、第三者のコード・ランタイム・SDK のライセンスを変更しません。
以下は 2026-09-20 に、`Docklet.csproj`、復元済みの `obj/project.assets.json`、
NuGet パッケージの `.nuspec` と同梱ライセンスを照合した一覧です。
依存関係を更新した場合は、この一覧も更新してください。

## NuGet パッケージ

| パッケージ | 確認したバージョン | 適用される条件 / パッケージ内の原文 |
| --- | --- | --- |
| [Microsoft.Windows.SDK.BuildTools](https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools/10.0.28000.2705) | 10.0.28000.2705 | Microsoft Windows SDK ライセンス。パッケージの License リンクを参照 |
| [Microsoft.Windows.SDK.BuildTools.WinApp](https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools.WinApp/0.6.1) | 0.6.1 | MIT（`.nuspec` の SPDX 表記） |
| [Microsoft.WindowsAppSDK](https://www.nuget.org/packages/Microsoft.WindowsAppSDK/2.4.0) | 2.4.0 | Microsoft Windows App SDK Software License Terms / `license.txt` |
| Microsoft.WindowsAppSDK.AI | 2.4.4 | 同上 / `license.txt` |
| Microsoft.WindowsAppSDK.Base | 2.0.4 | 同上 / `license.txt` |
| Microsoft.WindowsAppSDK.DWrite | 2.1.0 | 同上 / `license.txt` |
| Microsoft.WindowsAppSDK.Foundation | 2.3.9 | 同上 / `license.txt` |
| Microsoft.WindowsAppSDK.InteractiveExperiences | 2.1.6 | 同上 / `license.txt` |
| Microsoft.WindowsAppSDK.Runtime | 2.4.0 | 同上 / `license.txt` |
| Microsoft.WindowsAppSDK.Search | 2.4.4 | 同上 / `license.txt` |
| Microsoft.WindowsAppSDK.Widgets | 2.0.5 | 同上 / `license.txt` |
| Microsoft.WindowsAppSDK.WinUI | 2.3.6 | 同上 / `license.txt` |
| Microsoft.WindowsAppSDK.ML | 2.1.74 | Microsoft Windows App SDK / Windows Machine Learning (ML) Terms / `license.txt` |
| Microsoft.Windows.AI.MachineLearning | 2.1.74 | Microsoft Windows ML Runtime Terms / `license.txt` |
| Microsoft.Windows.SDK.BuildTools.MSIX | 1.7.251221100 | Microsoft Windows SDK Terms / `sdk_license.txt` |
| [Microsoft.Web.WebView2](https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.3719.77) | 1.0.3719.77 | Microsoft 著作権表記の BSD 3-Clause 形式 / `LICENSE.txt`（SDK パッケージの条件） |
| [System.Numerics.Tensors](https://www.nuget.org/packages/System.Numerics.Tensors/9.0.0) | 9.0.0 | MIT / `LICENSE.TXT` |

先頭 3 件が直接参照で、残りは間接依存です。間接依存に含まれる AI・ML・WebView2 が
Docklet の画面機能として使われているという意味ではありません。
WebView2 Runtime など、別途取得・同梱する製品にはその製品の条件が適用されます。

Windows App SDK の GitHub ソースが MIT であっても、NuGet バイナリのライセンスを
一律に MIT と扱うことはできません。今回確認した Windows App SDK / WinUI の
`license.txt` には再配布の規定があります。

## Go / .NET と画像

- Go エージェントは標準ライブラリのみを使用し、外部 Go モジュールへの依存はありません。
  コンパイル済みエージェントには Go ランタイム・標準ライブラリが含まれます。
  [Go の BSD 3-Clause ライセンス](https://go.dev/LICENSE) と、使用した Go 配布物に含まれる追加の第三者表記を保持してください。
- .NET ランタイムを同梱する場合は、使用した配布物の `LICENSE` と `THIRD-PARTY-NOTICES` を保持してください。
  参照: [.NET runtime のライセンス](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT)。
- `Assets/` のアイコンは本プロジェクト向けに AI で生成し、サイズを調整した画像です。
  外部のアイコンパック、Docker の公式ロゴ、フォントファイルは同梱していません。
  OS のフォントは Windows 環境から利用します。

## バイナリ配布時の対応

このファイルは依存関係の案内であり、各ライセンス原文や配布時の同意条件の代わりにはなりません。

1. 配布するバージョンを固定し、実際の publish / MSIX 出力に含まれる第三者ファイルを棚卸しします。
2. 対応する NuGet パッケージ、.NET、Go から `license.txt`、`LICENSE`、`NOTICE.txt`、
   `THIRD-PARTY-NOTICES` などの必要な原文を集め、配布物に含めます。元の著作権表記を削除しません。
3. Windows App SDK のライセンス第 3 条にある再配布要件を確認します。
   再配布対象の範囲に加え、エンドユーザー・再配布者に対する条件などが定められています。
   Docklet の MIT 本文を同梱するだけで、これらの条件への対応が完了するわけではありません。
4. SDK の開発ツール一式や NuGet キャッシュをそのまま配布しません。

`Docklet.csproj` は NuGet 復元先にあるライセンス・NOTICE、Go と標準ライブラリ内の第三者表記を
`dotnet publish` の出力の `licenses/` に同梱します。Actions はその出力を ZIP にまとめます。
ソースリポジトリにランタイムのバイナリや生成したライセンス一式をコミットする必要はありません。
