# ECS 模塊化與解藕重構計劃

## 🎯 重構目標
將 **Error Handling (E)**、**Crash Reporting (C)** 與 **Emergency Saving (S)** 三個核心防護模塊進行深度解藕與模塊化，建立一個統一、可擴展且不依賴 UI 的全域防護體系。

---

## 🏗️ 技術設計

### 1. **核心協調層 (ECS Orchestration)**
- **引入 `IECSManager` / `ECSManager`**:
  - 作為 E、C、S 三個模塊的中樞調度器。
  - 封裝全域異常的處理流轉邏輯：`Log` -> `Generate Report` -> `Emergency Save` -> `Notify UI` -> `Shutdown`。
  - 讓 `App.xaml.cs` 僅需依賴此管理器，實現高層級業務與底層實現的解藕。

### 2. **錯誤處理層 (E - Error Handling) 解藕**
- **UI 彈窗解藕**:
  - 在 `IErrorHandler` 中新增 `event EventHandler<ErrorOccurredEventArgs> ErrorOccurred`。
  - `ErrorHandler` 不再直接依賴 `IDialogService`。
  - 當錯誤發生時，觸發事件，由 UI 層或其他訂閱者（如 `DialogService` 的包裝類）決定是否顯示對話框。
- **動態恢復策略**:
  - 使恢復策略完全插件化，支持在運行時動態註冊，而非硬編碼在構造函數中。

### 3. **緊急儲存層 (S - Emergency Saving) 模塊化**
- **組件註冊機制 (Observer Pattern)**:
  - 定義 `ISavableComponent` 介面，包含 `string ComponentName` 與 `Task SaveAsync()`。
  - `EmergencySaveService` 提供註冊接口，不再需要知道具體的業務服務（如 Settings, ModData）。
  - 各業務服務（如 `SettingsService`）在初始化時主動向 `IEmergencySaveService` 註冊。

### 4. **崩潰報告層 (C - Crash Reporting) 實現**
- **實作 `CrashReportService`**:
  - 按照現有介面實作系統環境收集（OS、.NET 版本、記憶體狀態等）。
  - 實作報告的本地持久化邏輯。

---

## 📝 實施步驟

### **第一階段：基礎設施構建**
1. [ ] 定義 `ISavableComponent` 介面。
2. [ ] 在 `IErrorHandler` 中添加事件機制。
3. [ ] 創建 `IECSManager` 及其預設實作 `ECSManager`。

### **第二階段：模塊實作與解藕**
1. [ ] 實作 `CrashReportService` 的核心邏輯。
2. [ ] 重構 `EmergencySaveService` 為基於註冊的模式。
3. [ ] 修改 `ErrorHandler`，移除對 `IDialogService` 的直接調用，改為觸發事件。

### **第三階段：全域整合**
1. [ ] 讓 `SettingsService` 實作 `ISavableComponent` 並註冊到 `EmergencySaveService`。
2. [ ] 修改 `App.xaml.cs`，將所有全域異常處理邏輯委派給 `ECSManager`。
3. [ ] 建立 `ECSNotificationBridge`，將 `ErrorHandler` 的事件橋接到 `IDialogService` 以維持 UI 提示功能。

### **第四階段：清理與優化**
1. [ ] 移除 `App.xaml.cs` 中重複的 Log 與 MessageBox 代碼。
2. [ ] 驗證 Fatal 錯誤引發的完整防護鏈（E -> C -> S -> UI -> Exit）。

---

## ❓ 待確認事項
- 是否需要支持「遠端報告發送」？（目前計劃僅限本地保存）
- 是否需要對緊急存檔的檔案進行備份或版本管理？

**請確認此計劃，確認後我將開始執行重構。**