# RimWorld 翻譯工具

一個專為 RimWorld 模組設計的翻譯管理工具，幫助您輕鬆管理和檢視模組的翻譯狀態。

## 🌟 功能特色

### 📁 模組管理
- **自動掃描**：快速掃描指定目錄中的所有 RimWorld 模組
- **詳細資訊**：顯示模組名稱、作者、版本、PackageId 等完整資訊
- **預覽圖片**：載入並顯示模組預覽圖
- **版本相容性**：檢查模組是否支援指定的遊戲版本

### 🌐 翻譯檢測
- **繁體中文檢測**：自動檢測模組是否包含繁體中文翻譯
- **簡體中文檢測**：自動檢測模組是否包含簡體中文翻譯
- **翻譯補丁識別**：智能識別翻譯補丁模組及其對應的目標模組
- **可翻譯性分析**：分析模組是否適合進行翻譯

### 🎨 視覺化介面
- **狀態顏色編碼**：使用顏色直觀顯示翻譯狀態
- **詳細預覽面板**：右側面板顯示模組詳細資訊
- **翻譯補丁列表**：顯示可用的翻譯補丁及其詳細資訊
- **進度追蹤**：掃描進度即時顯示

### ⚙️ 設定管理
- **自動儲存設定**：自動儲存使用偏好設定
- **版本選擇**：支援多個 RimWorld 版本（1.0-1.6）
- **ModsConfig 整合**：載入和管理 ModsConfig.xml 檔案
- **路徑記憶**：記住上次使用的模組目錄路徑

## 🚀 快速開始

### 系統需求
- Windows 10/11
- .NET 9.0 或更高版本
- RimWorld 模組目錄

### 下載與安裝

#### 方式一：GitHub Releases（推薦）
1. 前往 [Releases 頁面](../../releases)
2. 下載最新版本的 `RimWorldTranslationTool.zip`
3. 解壓縮到任意目錄
4. 執行 `RimWorldTranslationTool.exe`

#### 方式二：自行編譯
如果您想自行編譯最新版本：
```bash
git clone [repository-url]
cd RimWorldTranslationTool
dotnet build -c Release
```

### 基本使用
1. **選擇模組目錄**：
   - 手動輸入路徑或點擊「瀏覽...」按鈕
   - 工具會自動記住您的選擇

2. **設定遊戲版本**：
   - 從下拉選單選擇您使用的 RimWorld 版本
   - 工具會自動檢查版本相容性

3. **掃描模組**：
   - 點擊「掃描模組」按鈕
   - 等待掃描完成，查看結果

4. **查看詳細資訊**：
   - 在左側列表中選擇模組
   - 右側面板會顯示詳細資訊和翻譯狀態

## 📖 使用指南

### 模組狀態說明

| 狀態 | 顏色 | 說明 |
|------|------|------|
| 🟢 有/是 | 綠色背景 | 該功能已實現 |
| 🔴 無/否 | 紅色背景 | 該功能未實現 |
| 🟡 版本不相容 | 黃色背景 | 版本不支援 |

### 翻譯補丁識別
工具會自動識別以下類型的翻譯補丁：
- 模組名稱包含「繁體中文」、「繁中」、「漢化」、「翻譯」等關鍵字
- 包含 `Languages/ChineseTraditional` 目錄的模組
- 通過 DefInjected 內容分析目標模組

### ModsConfig 整合
1. 點擊「選擇檔案」按鈕
2. 選擇您的 `ModsConfig.xml` 檔案（通常位於 `%LOCALAPPDATA%\Ludeon Studios\RimWorld by Ludeon Studios\Config\`）
3. 工具會自動標記已啟用的模組

## 🔧 技術規格

### 支援的檔案格式
- **About.xml**：模組資訊檔案
- **Preview.png**：模組預覽圖片
- **ModsConfig.xml**：RimWorld 模組配置檔案
- **DefInjected/*.xml**：翻譯定義檔案

### 掃描邏輯
1. 遞迴掃描指定目錄下的所有子目錄
2. 檢查每個目錄是否包含 `About/About.xml` 檔案
3. 解析 XML 檔案提取模組資訊
4. 檢查翻譯目錄和檔案
5. 建立翻譯補丁對應關係

### 設定檔案
工具會在執行目錄下建立 `RimWorldTranslationTool_Settings.json` 檔案：
```json
{
  "ModsDirectory": "您的模組目錄路徑",
  "ModsConfigPath": "ModsConfig.xml 路徑",
  "GameVersion": "1.6"
}
```

## 🐛 故障排除

### 常見問題

**Q: 掃描時程式閃退**
A: 請確保：
- 模組目錄路徑正確且可存取
- 有足夠的權限讀取模組檔案
- 模組目錄結構完整

**Q: 無法載入預覽圖片**
A: 這是正常現象，工具會忽略載入失敗的圖片檔案

**Q: 翻譯補丁識別不準確**
A: 翻譯補丁識別基於檔案名稱和內容分析，可能會有誤判情況

**Q: ModsConfig.xml 載入失敗**
A: 請確保檔案格式正確且未被其他程式佔用

### 效能優化建議
- 對於大型模組集合，掃描可能需要一些時間
- 建議關閉不必要的程式以獲得最佳效能
- 工具會自動限制 XML 檔案檢查數量以提高速度

## 🤝 貢獻

歡迎提交問題報告和功能建議！

### 開發環境
- .NET 9.0
- WPF
- C# 12

### 建置專案
```bash
git clone [repository-url]
cd RimWorldTranslationTool
dotnet build
dotnet run
```

## 📦 發布資訊

### 版本發布
- **穩定版本**：透過 GitHub Releases 發布
- **開發版本**：可從 `main` 分支自行編譯
- **發布頻率**：根據功能更新和錯誤修復而定

### 發布檔案
每個 Release 包含：
- `RimWorldTranslationTool.exe` - 主程式
- `RimWorldTranslationTool.deps.json` - 依賴設定
- `RimWorldTranslationTool.runtimeconfig.json` - 運行時設定
- 其他必要的 DLL 檔案

### 版本控制
- 遵循 [語義化版本](https://semver.org/lang/zh-TW/) 規範
- 格式：`主版本.次版本.修訂版本`
- 範例：`1.0.0`, `1.1.0`, `1.1.1`

### 更新日誌
請查看 [Releases 頁面](../../releases) 獲取詳細的更新日誌。

## 📄 授權

本專案採用 MIT 授權條款。

## 🙏 致謝

- RimWorld 社群
- 所有翻譯貢獻者
- 模組開發者們

---

**版本**: 1.0.0  
**最後更新**: 2025年1月  
**發布方式**: GitHub Releases  
**下載位置**: [Releases 頁面](../../releases)
