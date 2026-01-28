# 程式重構計劃：MVVM 架構轉型與解藕

## 🎯 重構目標
將程式從目前的「上帝物件 (God Object)」模式轉向標準的 **MVVM (Model-View-ViewModel)** 架構。消除 `MainWindow` 的沉重負擔，實現業務邏輯與 UI 顯示的徹底解藕。

---

## 🏗️ 技術設計

### 1. **模型層重構 (Model Refinement)**
- **解藕 `ModInfo`**:
  - 將目前的 `ModInfo` 拆分為 `ModModel` (純數據模型) 與 `ModViewModel` (UI 顯示模型)。
  - `ModModel` 放在 `Models/` 目錄，僅包含基礎數據，不引用 WPF 相關組件（如 `Brush`, `BitmapImage`）。
  - `ModViewModel` 負責 UI 綁定、顏色轉換邏輯及圖片資源管理。

### 2. **視圖模型層建立 (ViewModel Layer)**
- **引入 `MainViewModel`**:
  - 將 `MainWindow.xaml.cs` 中的所有狀態（如 `Mods` 列表、`GamePath`、`IsScanning`）移至 `MainViewModel`。
  - 實作 `RelayCommand` 模式，將 UI 點擊事件轉化為 ViewModel 中的命令處理。
- **狀態管理**:
  - 使用 `ObservableCollection` 統一管理模組列表，確保數據變動能即時反映在 UI。

### 3. **依賴注入 (Dependency Injection)**
- **中心化服務管理**:
  - 在 `App.xaml.cs` 中引入 DI 容器（如 `Microsoft.Extensions.DependencyInjection`）。
  - 統一管理 `IPathService`, `ILoggerService`, `IModScannerService` 等服務的生命週期。
  - 透過建構函數注入 (Constructor Injection) 解決服務之間的耦合。

### 4. **視圖層瘦身 (View Thinning)**
- **MainWindow 清理**:
  - 目標是讓 `MainWindow.xaml.cs` 的代碼量減少 80% 以上。
  - 僅保留與 UI 視窗控制相關的邏輯（如拖放事件的初步處理），其餘邏輯全部委派給 ViewModel。

---

## 📝 實施步驟

### **第一階段：基礎架構準備**
1. [ ] 在 `Models/` 創建 `ModModel` 與 `ModDependency` 模型。
2. [ ] 在 `ViewModels/` 創建 `ModViewModel` 與 `BaseViewModel`。
3. [ ] 在 `App.xaml.cs` 配置服務容器。

### **第二階段：ViewModel 遷移**
1. [ ] 實作 `MainViewModel`，定義屬性與初始化邏輯。
2. [ ] 將 `MainWindow` 中的掃描邏輯 (`ScanModsAsync`) 遷移至 `MainViewModel`。
3. [ ] 實作命令 (Commands) 以替代 XAML 中的事件處理器 (Event Handlers)。

### **第三階段：UI 綁定與解藕**
1. [ ] 更新 `MainWindow.xaml` 使用 `Binding` 連接 ViewModel。
2. [ ] 移除 `MainWindow.xaml.cs` 中手動實例化服務的代碼。
3. [ ] 修正 `SettingsController` 與 View 的依賴關係，改為與 ViewModel 互動。

### **第四階段：驗證與優化**
1. [ ] 確保 ECS (Error, Crash, Save) 體系在新的 ViewModel 結構下運作正常。
2. [ ] 檢查圖片資源的釋放機制，防止內存洩漏。

---

## ❓ 待確認事項
- 是否考慮引入開源的 MVVM 框架（如 `CommunityToolkit.Mvvm`）來加速開發？
- 是否需要為 ViewModel 增加單元測試案例？

**請確認此重構計劃，確認後我將開始分階段執行。**