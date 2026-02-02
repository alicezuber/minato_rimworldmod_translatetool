# RimWorld Translation Tool

一個專為 RimWorld 遊戲設計的翻譯工具，支援模組的翻譯管理和處理。

## 專案概述

這是一個基於 WPF 和 .NET 9.0 開發的桌面應用程式，專門用於管理和處理 RimWorld 遊戲模組的翻譯內容。工具提供了完整的翻譯工作流程，包括模組掃描、翻譯映射、設定管理等核心功能。

## 技術架構

- **框架**: .NET 9.0 Windows
- **UI 框架**: WPF (Windows Presentation Foundation)
- **架構模式**: MVVM (Model-View-ViewModel)
- **依賴注入**: Microsoft.Extensions.DependencyInjection
- **支援語言**: 繁體中文 (zh-TW)、英文 (en-US)

## 核心功能

### 🌐 翻譯管理
- 模組翻譯內容的掃描與解析
- 翻譯映射服務，支援多語言處理
- XML 格式的翻譯檔案處理

### 📦 模組管理
- 模組資訊服務，自動識別模組結構
- 模組瀏覽器，提供直觀的模組檢視介面
- 模組掃描服務，快速掃描本地模組

### ⚙️ 系統功能
- 完整的錯誤處理與崩潰報告機制
- 緊急儲存服務，確保資料安全
- 主題服務，支援介面主題切換
- 設定管理，包含備份與驗證功能

### 🛡️ 安全與穩定性
- ECS (Error Control System) 安全防護體系
- 全域異常處理機制
- 日誌服務，提供完整的操作記錄

## 專案結構

```
├── Controllers/          # 控制器層
├── Controls/            # 自定義控制項
├── Converters/          # 資料轉換器
├── Models/              # 資料模型
├── Services/            # 業務邏輯服務
│   ├── CrashReporting/  # 崩潰報告服務
│   ├── Dialogs/         # 對話框服務
│   ├── ECS/            # 錯誤控制系統
│   ├── EmergencySave/  # 緊急儲存服務
│   ├── ErrorHandling/  # 錯誤處理服務
│   ├── Infrastructure/ # 基礎設施服務
│   ├── Localization/   # 本地化服務
│   ├── Logging/        # 日誌服務
│   ├── Paths/          # 路徑服務
│   ├── Scanning/       # 掃描服務
│   ├── Settings/       # 設定服務
│   └── Theme/          # 主題服務
├── Styles/              # 樣式資源
├── ViewModels/          # 視圖模型
├── Views/               # 視圖
└── Resources/           # 資源檔案
```

## 開發環境

### 系統需求
- Windows 10/11
- .NET 9.0 Runtime
- Visual Studio 2022 (建議)

### 建置專案
```bash
# 克隆專案
git clone <repository-url>
cd translate

# 建置專案
dotnet build

# 執行專案
dotnet run
```

### 開發工具
- **程式碼分析**: Microsoft.CodeAnalysis.Analyzers, StyleCop.Analyzers
- **行為框架**: Microsoft.Xaml.Behaviors.Wpf
- **設定檔**: 支援 JSON 格式的設定檔案

## 主要特色

### 🔧 模組化設計
採用模組化架構，各功能服務獨立且可測試，便於維護與擴展。

### 🎨 現代化 UI
基於 WPF 的現代化使用者介面，支援主題切換和響應式設計。

### 🌍 多語言支援
內建本地化服務，支援繁體中文和英文介面。

### 📊 完整的錯誤處理
實作了完整的錯誤控制系統，包含崩潰報告、緊急儲存和異常恢復機制。

## 授權

本專案採用開源授權，詳細授權條款請參考 LICENSE 檔案。

## 貢獻

歡迎提交 Issue 和 Pull Request 來改善這個專案。

## 聯絡資訊

如有任何問題或建議，請透過 GitHub Issues 聯繫開發團隊。
