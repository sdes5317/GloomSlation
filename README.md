# GloomStation
An unofficial Gloomwood translation project.

## Languages
This mod currently contains translation on:
- English - original text, to be used like reference and starting point
- [Russian](https://github.com/ErisOrder/GloomSlation-RU) - by [@pipo-cxx](https://github.com/pipo-cxx)
- [Traditional Chinese](./Mods/GloomSlation/TraditionalChinese/) - by [sdes5317](https://github.com/sdes5317)

Some translations are split into submodules.

## 繁體中文版使用方式

目前已在 **Windows x64、Gloomwood 0.889（Unity 2021.3.45f2）、MelonLoader 0.7.3 x64** 上測試；其他遊戲或載入器版本尚未驗證。

1. 關閉遊戲，取得 `GloomSlation-TraditionalChinese-YYYYMMDD_N.zip`（例如 `GloomSlation-TraditionalChinese-20260927_1.zip`）。若要自行產生 ZIP，請依照[繁中建置與打包說明](./docs/TRADITIONAL_CHINESE.md)操作。
2. 若已安裝模組，先備份 `Mods/GloomSlation/cfg.toml` 等個人設定。將 ZIP **解壓並覆蓋**到 `Gloomwood.exe` 所在資料夾；解壓後 `version.dll`、`MelonLoader/` 和 `Mods/` 應與遊戲執行檔同層。
3. 確認 `Mods/GloomSlation/cfg.toml` 中的 `language = "TraditionalChinese"`，再啟動遊戲。字型包隨 ZIP 提供，玩家不需要安裝 Unity Editor。

目前包含選單、物品、文件、日誌、對話、提示、地區與製作名單八類翻譯。若模組沒有載入或出現缺字，可查看遊戲目錄下的 `MelonLoader/Latest.log`。

## Preferences
Mod preferences are stored in file `Mods/GloomSlation/cfg.toml`. 
File is created on first launch (and exit).
It contains entries, such as
- `language` - is a name of currently chosen language directory
- `debug` - a boolean which, when `true`, enables extended logging

## Building
Before running build, you should install [MelonLoader](https://github.com/LavaGang/MelonLoader)
on your Gloomwood and then create symbolic link to Gloomwood root folder and name it `Gloomwood`.
This can be done using powershell with admin privileges (in this project's root):
```powershell
New-Item -Path .\Gloomwood -ItemType SymbolicLink -Value <path-to-your-Gloomwood>
```

## [Creating new translation](./docs/adding-new-translation.md)
## [Building and packaging Traditional Chinese](./docs/TRADITIONAL_CHINESE.md)
## Notes

### About using `UnityExplorer`
For some reason `UnityExplorer`'s library, `UniverseLib`, cannot be loaded from `UserLibs`
and needs to be placed in `Gloomwood_Data/Managed` folder

### Versioning
First part of the mod version is the latest tested game version, second part is mod version.

## Credits
- `pipo-cxx` for initiating project and making russian translation.
