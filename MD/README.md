# RimWorld 翻譯工具

一個專為 RimWorld 模組設計的專業翻譯管理工具，採用現代化 WPF 介面，幫助您輕鬆管理、檢視和組織模組的翻譯狀態。

## 🌟 核心功能

### 📦 智慧模組管理
- **高效掃描引擎**：快速遞迴掃描指定目錄下的所有 RimWorld 模組
- **完整模組資訊**：提取並顯示模組名稱、作者、版本、PackageId、支援版本等詳細資料
- **預覽圖片載入**：自動載入並顯示模組的 About/Preview.png 圖片
- **版本相容性檢查**：即時檢查模組是否支援當前選擇的遊戲版本
- **啟用狀態管理**：透過 ModsConfig.xml 整合，顯示模組的啟用狀態

### 🌐 專業翻譯檢測
- **多語言支援檢測**：自動識別繁體中文和簡體中文翻譯檔案
- **翻譯補丁智能識別**：基於模組名稱、目錄結構和 DefInjected 內容分析，精準識別翻譯補丁
- **翻譯對應關係建立**：自動建立翻譯補丁與目標模組的對應關係
- **可翻譯性評估**：分析模組內容，評估其翻譯潛力和價值

### 🎨 現代化使用者介面
- **三頁籤設計**：設定、模組瀏覽、模組管理三大功能區域
- **狀態視覺化**：使用顏色編碼直觀顯示翻譯狀態（綠色=有，紅色=無，黃色=版本不相容）
- **即時預覽面板**：右側詳細資訊面板，顯示模組完整資訊和翻譯狀態
- **拖拽操作支援**：模組管理頁籤支援拖拽操作，直觀管理啟用狀態
- **進度追蹤系統**：掃描過程即時顯示進度條和狀態資訊

### ⚙️ 進階設定管理
- **自動設定儲存**：JSON 格式設定檔，自動儲存使用者偏好
- **多版本支援**：支援 RimWorld 1.0 至 1.6 所有版本
- **ModsConfig.xml 整合**：完整載入和解析遊戲模組配置檔案
- **路徑記憶功能**：自動記住上次使用的模組目錄和配置檔案路徑

## 🚀 快速開始

### 系統需求
- **作業系統**：Windows 10/11（64位元）
- **執行環境**：.NET 9.0 Runtime 或更高版本
- **硬體需求**：至少 4GB RAM，建議 8GB 以上
- **遊戲需求**：RimWorld 模組目錄（通常位於 Steam 安裝目錄下）

### 下載與安裝

#### 📦 方式一：GitHub Releases（推薦）
1. 前往專案 [Releases 頁面](../../releases)
2. 下載最新版本的 `RimWorldTranslationTool.zip`
3. 解壓縮到任意目錄（建議選擇無權限限制的資料夾）
4. 雙擊執行 `RimWorldTranslationTool.exe`

#### 🔧 方式二：自行編譯
如果您想自行編譯最新原始碼：
```bash
# 克隆專案
git clone [repository-url]
cd RimWorldTranslationTool

# 編譯發布版本
dotnet build -c Release

# 執行程式
dotnet run -c Release
```

### 📖 基本使用流程

#### 1️⃣ 初始設定
- **選擇模組目錄**：在「設定」頁籤中手動輸入路徑或點擊「瀏覽...」按鈕
- **載入 ModsConfig**：點擊「選擇檔案」載入遊戲的 ModsConfig.xml（可選）
- **設定遊戲版本**：從下拉選單選擇您使用的 RimWorld 版本

#### 2️⃣ 掃描模組
- 點擊「🔍 開始掃描」按鈕
- 等待掃描完成，系統會顯示找到的模組數量
- 掃描過程中會即時顯示進度條和處理狀態

#### 3️⃣ 瀏覽與分析
- 切換到「📦 模組瀏覽」頁籤
- 在左側列表中選擇模組查看詳細資訊
- 右側面板會顯示模組完整資訊、翻譯狀態和相關翻譯補丁

#### 4️⃣ 模組管理
- 切換到「🔧 模組管理」頁籤
- 使用拖拽或按鈕操作管理模組的啟用狀態
- 點擊「💾 儲存配置」儲存變更到 ModsConfig.xml

## 📖 詳細使用指南

### 🎨 介面功能說明

#### 三大功能頁籤
1. **⚙️ 設定**：初始配置和模組掃描
2. **📦 模組瀏覽**：查看和分析模組資訊
3. **🔧 模組管理**：管理模組啟用狀態

#### 狀態顏色編碼系統
| 狀態 | 顏色 | 說明 |
|------|------|------|
| 🟢 有/是 | 綠色背景 | 該模組包含對應的翻譯或功能 |
| 🔴 無/否 | 紅色背景 | 該模組不包含對應的翻譯或功能 |
| 🟡 版本不相容 | 黃色背景 | 模組不支援當前選擇的遊戲版本 |

### 🔍 翻譯補丁智能識別

工具採用多層次演算法自動識別翻譯補丁：

#### 識別策略
1. **名稱關鍵字匹配**：檢測模組名稱是否包含翻譯相關關鍵字
   - 繁體中文、繁中、漢化、翻譯、簡中
   - Chinese、Translation、中文

2. **目錄結構分析**：檢查是否包含翻譯目錄
   - `Languages/ChineseTraditional/`
   - `Languages/ChineseSimplified/`

3. **DefInjected 內容分析**：深度分析翻譯檔案內容
   - 解析 XML 檔案結構
   - 識別目標模組的 Def 名稱
   - 建立翻譯補丁與目標模組的精確對應關係

### 📋 ModsConfig.xml 整合

#### 載入配置檔案
1. 在「設定」頁籤點擊「選擇檔案」
2. 瀏覽至遊戲配置目錄：
   ```
   %LOCALAPPDATA%\Ludeon Studios\RimWorld by Ludeon Studios\Config\ModsConfig.xml
   ```
3. 工具會自動解析並標記已啟用的模組

#### 配置檔案功能
- **啟用狀態顯示**：在模組列表中顯示啟用狀態（✅ 啟用，❌ 未啟用）
- **載入順序排序**：按照 ModsConfig.xml 中的順序排序模組
- **配置儲存**：支援修改後儲存回配置檔案

### 🎯 模組管理操作

#### 拖拽操作
- **從模組池到啟用列表**：啟用選中的模組
- **從啟用列表到模組池**：停用選中的模組
- **多選支援**：按住 Ctrl 或 Shift 鍵進行多選操作

#### 按鈕操作
- **✅ 啟用選中**：將選中的模組移至啟用列表
- **❌ 停用選中**：將選中的模組移回模組池
- **🔄 重新整理**：重新整理模組列表顯示
- **💾 儲存配置**：將變更儲存到 ModsConfig.xml

## 🔧 技術架構與規格

### 🏗️ 技術架構
- **框架**：WPF (Windows Presentation Foundation)
- **語言**：C# 12
- **執行環境**：.NET 9.0
- **UI 設計**：現代化 Material Design 風格
- **資料格式**：JSON 設定檔案，XML 模組解析

### 📁 支援的檔案格式
| 檔案類型 | 路徑 | 用途 |
|----------|------|------|
| **About.xml** | `About/About.xml` | 模組基本資訊和元資料 |
| **Preview.png** | `About/Preview.png` | 模組預覽圖片 |
| **ModsConfig.xml** | 遊戲配置目錄 | RimWorld 模組啟用配置 |
| **DefInjected/*.xml** | `Languages/*/DefInjected/` | 翻譯定義檔案 |
| **Settings.json** | 執行目錄 | 工具設定檔案 |

### ⚙️ 掃描引擎演算法

#### 模組識別流程
1. **遞迴目錄掃描**：深度優先掃描所有子目錄
2. **模組驗證**：檢查是否存在 `About/About.xml` 檔案
3. **XML 解析**：使用 XDocument 解析模組元資料
4. **資料提取**：提取名稱、作者、版本、PackageId 等資訊
5. **翻譯檢測**：分析翻譯目錄和檔案結構
6. **補丁對應**：建立翻譯補丁與目標模組的對應關係

#### 效能優化策略
- **非同步處理**：使用 `async/await` 避免 UI 阻塞
- **批次更新**：減少 Dispatcher.Invoke 調用頻率
- **檔案限制**：限制 XML 檢查數量以提高掃描速度
- **記憶體管理**：適時釋放圖片資源避免記憶體洩漏

### 💾 設定檔案結構
工具在執行目錄下自動建立 `RimWorldTranslationTool_Settings.json`：

```json
{
  "ModsDirectory": "D:\\Steam\\steamapps\\common\\RimWorld\\Mods",
  "ModsConfigPath": "C:\\Users\\Username\\AppData\\Local\\Ludeon Studios\\RimWorld by Ludeon Studios\\Config\\ModsConfig.xml",
  "GameVersion": "1.6"
}
```

#### 設定檔案特性
- **自動儲存**：使用者操作後自動儲存設定
- **自動載入**：程式啟動時自動恢復上次設定
- **容錯處理**：檔案損壞時自動使用預設值
- **路徑驗證**：啟動時驗證檔案路徑有效性

## 🐛 故障排除與最佳化

### 🔧 常見問題解決

#### 掃描相關問題
**Q: 掃描時程式閃退或無回應**
```
A: 請檢查以下項目：
✓ 模組目錄路徑正確且可存取
✓ 具有足夠的權限讀取模組檔案
✓ 模組目錄結構完整（包含 About/About.xml）
✓ 防毒軟體未阻擋程式執行
```


#### 顯示相關問題
**Q: 無法載入預覽圖片**
```
A: 這是正常現象：
✓ 工具會自動忽略載入失敗的圖片檔案
✓ 不影響其他功能的正常使用
✓ 可能是圖片檔案損壞或格式不支援
```

**Q: 翻譯補丁識別不準確**
```
A: 識別演算法說明：
✓ 基於多層次分析：名稱、目錄結構、DefInjected 內容
✓ 可能存在誤判，特別是對於非標準命名翻譯模組
✓ 建議手動驗證重要的翻譯補丁對應關係
```

#### 配置檔案問題
**Q: ModsConfig.xml 載入失敗**
```
A: 請確認：
✓ 檔案格式正確（XML 語法無誤）
✓ 檔案未被其他程式佔用（如遊戲正在執行）
✓ 檔案路徑正確且具有讀寫權限
✓ 檔案未損壞（可嘗試重新啟動遊戲生成）
```

## 🤝 開發與貢獻

### 🛠️ 開發環境設定
- **.NET SDK**：9.0 或更高版本
- **IDE**：Visual Studio 2022 或 VS Code
- **框架**：WPF + C# 12
- **套件管理**：NuGet

### 📦 專案結構
```
RimWorldTranslationTool/
├── App.xaml                 # 應用程式資源和樣式定義
├── App.xaml.cs             # 應用程式入口點
├── MainWindow.xaml         # 主視窗 UI 定義
├── MainWindow.xaml.cs      # 主要業務邏輯
├── RimWorldTranslationTool.csproj # 專案檔案
└── README.md               # 專案說明文件
```

### 🔧 建置與執行
```bash
# 克隆專案
git clone [repository-url]
cd RimWorldTranslationTool

# 還原依賴套件
dotnet restore

# 建置專案
dotnet build -c Release

# 執行程式
dotnet run -c Release

# 發布單一檔案
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### 🧪 核心類別說明

#### ModInfo 類別
模組資訊的核心資料結構，包含：
- 基本資訊：名稱、作者、PackageId、版本
- 翻譯狀態：繁中、簡中、翻譯補丁、可翻譯性
- 視覺化屬性：顏色編碼、背景色、預覽圖片

#### AppSettings 類別
應用程式設定管理：
- 模組目錄路徑
- ModsConfig.xml 路徑
- 遊戲版本設定

#### 主要功能方法
- `LoadModInfo()`：載入單一模組資訊
- `BuildTranslationMappings()`：建立翻譯補丁對應關係
- `ScanModsAsync()`：非同步掃描模組目錄

### 🌟 貢獻指南

#### 報告問題
- 使用 GitHub Issues 回報錯誤
- 提供詳細的錯誤訊息和重現步驟
- 包含系統環境資訊

#### 功能建議
- 在 Issues 中使用 "enhancement" 標籤
- 描述功能需求和使用場景
- 考慮對現有功能的影響

#### 程式碼貢獻
1. Fork 專案到您的 GitHub
2. 建立功能分支：`git checkout -b feature/your-feature`
3. 提交變更：`git commit -m 'Add some feature'`
4. 推送分支：`git push origin feature/your-feature`
5. 建立 Pull Request

## 📦 發布資訊

### 🚀 版本發布策略
- **穩定版本**：透過 GitHub Releases 定期發布
- **開發版本**：可從 `main` 分支自行編譯最新功能
- **發布頻率**：根據功能完成度和錯誤修復需求

### 📋 發布檔案清單
每個正式 Release 包含：
```
RimWorldTranslationTool/
├── RimWorldTranslationTool.exe          # 主執行檔
├── RimWorldTranslationTool.deps.json     # 依賴設定
├── RimWorldTranslationTool.runtimeconfig.json # 運行時設定
└── [其他必要的 DLL 檔案]
```

### 📊 版本控制規範
遵循 [語義化版本](https://semver.org/lang/zh-TW/) 規範：
- **主版本**：不相容的 API 變更
- **次版本**：向下相容的功能新增
- **修訂版本**：向下相容的錯誤修復

### 📝 更新日誌
詳細的更新日誌請查看 [Releases 頁面](../../releases)

## 📄 授權條款

本專案採用 **MIT 授權條款**，您可自由：
- ✅ 使用、複製、修改、合併、發布、分發、再授權
- ✅ 商業使用
- ✅ 私人使用

條件：
- ⚠️ 需包含版權聲明和授權條款
- ⚠️ 軟體按"原樣"提供，不提供任何擔保

## 🌐 添加翻譯指南

### 📝 如何添加新語言支援

本工具採用原生 .NET ResourceManager 實現國際化，添加新語言非常簡單：

#### 1. 準備翻譯檔案
在 `Resources/` 目錄下創建對應語言的 `.resx` 檔案：
```
Resources/
├── Resources.resx           # 預設（通常是英文）
├── Resources.zh-TW.resx     # 繁體中文
├── Resources.en-US.resx     # 英文
└── Resources.[語言代碼].resx # 新語言檔案
```

#### 2. 語言代碼格式
使用標準的 [RFC 4646](https://tools.ietf.org/html/rfc4646) 語言代碼：
- `zh-TW` - 繁體中文 (台灣)
- `zh-CN` - 簡體中文 (中國)
- `ja-JP` - 日文 (日本)
- `ko-KR` - 韓文 (韓國)
- `fr-FR` - 法文 (法國)
- `de-DE` - 德文 (德國)
- `es-ES` - 西班牙文 (西班牙)
- `ru-RU` - 俄文 (俄羅斯)

#### 3. 翻譯檔案結構
每個 `.resx` 檔案包含相同的 key，但值為對應語言的文字：

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <!-- 視窗標題 -->
  <data name="WindowTitle" xml:space="preserve">
    <value>RimWorld 翻譯工具</value>
  </data>
  
  <!-- 按鈕文字 -->
  <data name="Browse" xml:space="preserve">
    <value>瀏覽...</value>
  </data>
  
  <!-- 狀態訊息 -->
  <data name="ReadyToScan" xml:space="preserve">
    <value>準備掃描模組...</value>
  </data>
</root>
```

#### 4. 添加語言選項
在 `MainWindow.xaml.cs` 的 `InitializeLanguages()` 方法中添加新語言：

```csharp
private void InitializeLanguages()
{
    var languages = new[]
    {
        new { Code = "zh-TW", Name = "繁體中文" },
        new { Code = "en-US", Name = "English" },
        new { Code = "ja-JP", Name = "日本語" },  // 新增
        new { Code = "ko-KR", Name = "한국어" }    // 新增
    };
    
    // ... 其餘程式碼保持不變
}
```

#### 5. 翻譯注意事項

**✅ 最佳實踐**
- 保持所有語言檔案的 key 數量一致
- 使用語境適當的翻譯，避免直譯
- 測試 UI 佈局，確保文字不會溢出
- 保持專業術語的一致性

**⚠️ 常見問題**
- **特殊字符**：確保 XML 檔案使用 UTF-8 編碼
- **佔位符**：保留 `{0}`、`{1}` 等格式化佔位符
- **快捷鍵**：如果包含 `&` 字符，確保不會與現有快捷鍵衝突
- **長度限制**：按鈕和標籤文字不宜過長

#### 6. 編譯與測試
1. 編譯專案：`dotnet build`
2. 執行程式測試語言切換
3. 檢查所有 UI 元素是否正確顯示翻譯文字
4. 驗證語言切換功能正常工作

#### 7. 提交翻譯
完成翻譯後，您可以：
- 建立 Pull Request 貢獻翻譯
- 在 Issues 中分享翻譯檔案
- 聯繫開發團隊整合翻譯

### 🎨 翻譯風格指南

#### 語氣風格
- **專業友善**：保持專業性，同時易於理解
- **一致性**：同一概念使用相同術語
- **簡潔明瞭**：避免冗長的表達

#### 術語對照
| 中文 | 英文 | 說明 |
|------|------|------|
| 模組 | Mod | RimWorld 模組的統一術語 |
| 掃描 | Scan | 檢測和分析模組 |
| 啟用 | Enable | 啟動模組功能 |
| 瀏覽 | Browse | 選擇檔案或目錄 |
| 設定 | Settings | 程式配置選項 |

---

## 🙏 致謝與鳴謝

### 🎮 RimWorld 社群
- 感謝 Ludeon Studios 開發這款優秀的遊戲
- 感謝廣大模組開發者的創意與貢獻
- 感謝翻譯社群的無私奉獻

---

**📦 當前版本**: 1.0.0  
**🔄 最後更新**: 2025年1月  
**🚀 發布方式**: GitHub Releases  
**⬇️ 下載位置**: [Releases 頁面](../../releases)  
**📧 聯絡方式**: 請透過 GitHub Issues 聯繫開發團隊
