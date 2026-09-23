# 繁體中文建置與打包

繁中來源在 `Mods/GloomSlation/TraditionalChinese/`，包含 `Areas`、`Credits`、`Dialogue`、`Documents`、`Items`、`Journal`、`Menus`、`Prompts` 八個分類檔，以及 `fontMap.json`。打包時直接使用這些檔案；不要執行舊版 `scripts/prepare_traditional_chinese.py`，它會重寫翻譯。

## 準備環境

- 安裝 .NET SDK、Unity Editor **2021.3.45f2**。`tools/FontBundle/Packages/manifest.json` 已指定 TextMeshPro **3.0.6**。
- 本機需要 Gloomwood 的 `Gloomwood_Data/Managed` 組件供模組編譯；遊戲組件不會放進發行包。
- 從 [MelonLoader 0.7.3 官方版本](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3) 取得 `MelonLoader.x64.zip`，放在 `bin/deps/MelonLoader.x64.zip`。打包腳本會核對 SHA-256 `5b2b2f3d1cd42b59ec886c5bdc2663edae87a0097a4f4a8f58c0965a99dda416`。
- 將同一個 MelonLoader ZIP 解壓到 `bin/deps/`，供 .NET 編譯引用 `bin/deps/MelonLoader/net35`。例如在專案根目錄執行：

```powershell
Expand-Archive -LiteralPath .\bin\deps\MelonLoader.x64.zip -DestinationPath .\bin\deps -Force
```

## 每次翻譯更新後

在專案根目錄使用 Unity 2021.3.45f2 匯出字型；將第一行改為自己的 Unity 安裝位置：

```powershell
$unity = 'D:\Program Files\Unity Editor\2021.3.45f2\Editor\Unity.exe'
& $unity -batchmode -nographics -quit -projectPath (Join-Path (Get-Location) 'tools/FontBundle') -executeMethod BuildChineseFont.Build -logFile (Join-Path (Get-Location) 'bin/font-build.log')
```

腳本掃描目前繁中分類檔，建立 TMP SDF 字型資產，產生 `font.bundle`，並複製到 `Mods/GloomSlation/TraditionalChinese/font.bundle`。新增字元後務必重新匯出；打包腳本只檢查檔案存在，不會自行產字或證明字型涵蓋最新文字。遊戲會依 `fontMap.json` 載入 bundle 內的 `Assets/NotoSansTC.asset`。

字型來源是 [Noto Sans TC](https://github.com/google/fonts/tree/main/ofl/notosanstc) 的 Regular 靜態字型；授權條款在 `tools/FontBundle/Assets/Fonts/OFL.txt`，會附入發行包。建置流程沿用原專案的 Unity/TMP 字型 bundle 載入方式。

## 編譯與打包

在專案根目錄執行：

```powershell
dotnet build .\GloomSlation.csproj --configuration Release "-p:GloomwoodPath=<Gloomwood 安裝目錄>"
.\scripts\package_traditional_chinese.ps1
```

把 `<Gloomwood 安裝目錄>` 換成實際路徑；若專案根目錄已有指向遊戲的 `Gloomwood` 連結，可省略整個 `-p:GloomwoodPath=...` 參數。若 MelonLoader DLL 不在 `bin/deps/MelonLoader/net35`，可指定 `-p:MelonLoaderNet35=...`。打包腳本不會修改遊戲目錄或翻譯來源。它讀取 Release DLL、目前八個翻譯檔、`font.bundle`、`fontMap.json`、設定範本、字型授權及官方載入器 ZIP，輸出 `dist/GloomSlation-TraditionalChinese.zip`。ZIP 根目錄有 `MelonLoader/`、`version.dll`、`Mods/`、`README.md`；不含遊戲組件。既有的 `dist/` 其他檔案不會被清除。

## 遊戲內驗收

關閉遊戲後，將 ZIP 全部內容解壓至 `Gloomwood.exe` 所在目錄。若已有舊版模組或自訂 `Mods/GloomSlation/cfg.toml`，先備份會被覆蓋的檔案。啟動後檢查選單、道具、文件與字幕是否缺字、換行錯誤或文字截斷。遇到載入問題，可查遊戲目錄下的 `MelonLoader/Latest.log`。
