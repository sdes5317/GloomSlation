# GloomSlation 繁體中文

此包包含目前八個分類的繁中翻譯、GloomSlation 模組、Noto Sans TC 字型包與 MelonLoader 0.7.3 x64。

## 安裝

1. 關閉 Gloomwood。若已安裝 GloomSlation 或 MelonLoader，先備份將被覆蓋的檔案，尤其是 `Mods/GloomSlation/cfg.toml`。
2. 將 ZIP 內全部內容解壓到 `Gloomwood.exe` 所在目錄。解壓後 `version.dll`、`MelonLoader/`、`Mods/` 應與遊戲執行檔同層。
3. 啟動遊戲；隨附設定已選用 `TraditionalChinese`。檢查選單、物品、文件和字幕。若有載入錯誤，請查看 `MelonLoader/Latest.log`。

字型使用 Unity TextMeshPro SDF `font.bundle` 和 `fontMap.json`；Noto Sans TC 的授權見 `Mods/GloomSlation/TraditionalChinese/OFL.txt`。

要移除翻譯，關閉遊戲後移除本包的 `Mods/GloomSlation.dll` 與 `Mods/GloomSlation/TraditionalChinese/`，並還原先前備份的設定或模組。若另外移除 MelonLoader，只刪除本包安裝的 `version.dll` 與 `MelonLoader/`；請保留其他模組的檔案。
