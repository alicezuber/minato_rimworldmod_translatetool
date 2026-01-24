# 路徑服務解耦總結

## 🎯 **解耦目標**
將 RimWorld 相關的路徑計算邏輯從各處抽離，建立統一的路徑服務，避免世界各地重複寫路徑推導邏輯。

## 🏗️ **新架構設計**

### **路徑服務層 (Services/Paths/)**
```
Services/Paths/
├── IPathService.cs           # 路徑服務介面
├── PathService.cs           # 路徑服務實現
└── PathConstants.cs         # 路徑常數定義
```

## 📋 **職責分離**

### **IPathService**
- ✅ 根據遊戲路徑推導工作坊路徑
- ✅ 獲取設定資料夾路徑
- ✅ 獲取 ModsConfig.xml 路徑
- ✅ 獲取存檔資料夾路徑
- ✅ 獲取本地模組路徑
- ✅ 驗證遊戲路徑有效性
- ✅ 自動偵測可能的安裝路徑
- ✅ 模組相關路徑計算

### **PathConstants**
- ✅ 集中管理所有路徑常數
- ✅ 支援跨平台執行檔名稱
- ✅ 官方擴展資料夾列表
- ✅ 模組標準資料夾結構

### **PathService**
- ✅ 智能路徑推導邏輯
- ✅ 多平台支援
- ✅ 錯誤處理與日誌記錄
- ✅ 路徑驗證與檢查

## 🔄 **重構變更**

### **MainWindow.xaml.cs 簡化**
```csharp
// 舊架構 - 分散的路徑邏輯
private string WorkshopPath => !string.IsNullOrEmpty(_gamePath) ? 
    Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(_gamePath)) ?? "", 
                "workshop", "content", "294100") : "";

private string ConfigPath => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
    "..", "LocalLow", "Ludeon Studios", "RimWorld by Ludeon Studios");

// 新架構 - 統一路徑服務
private readonly Services.Paths.IPathService _pathService;
private string WorkshopPath => _pathService.GetWorkshopPath(_gamePath);
private string ConfigPath => _pathService.GetConfigPath();
```

### **設定服務更新**
```csharp
// 舊方式 - 硬編碼路徑計算
string configPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "..", "LocalLow", "Ludeon Studios", "RimWorld by Ludeon Studios", 
    "Config", "ModsConfig.xml");

// 新方式 - 使用路徑服務
string configPath = _pathService.GetModsConfigPath();
```

## 📊 **核心功能**

### **1. 智能路徑推導**
```csharp
// 自動推導工作坊路徑
D:\01_Game\PC_Platform\Steam\steamapps\common\RimWorld
↓
D:\01_Game\PC_Platform\Steam\steamapps\workshop\content\294100
```

### **2. 跨平台支援**
```csharp
// Windows
RimWorldWin64.exe

// Linux  
RimWorldLinux

// macOS
RimWorldMac.app
```

### **3. 路徑驗證**
```csharp
public PathValidationResult IsValidGamePath(string path)
{
    // 檢查主程式檔案
    // 檢查 Data 資料夾
    // 檢查 Core 資料夾
    // 返回詳細驗證結果
}
```

### **4. 自動偵測**
```csharp
public List<string> GetPossibleGamePaths()
{
    // Steam 安裝路徑
    // GOG Galaxy 路徑  
    // 常見遊戲安裝路徑
    // 返回所有可能路徑
}
```

## 🌍 **全球統一效益**

### **1. 單一來源**
- ✅ 所有路徑邏輯集中在一個服務中
- ✅ 避免世界各地重複實現相同邏輯
- ✅ 修改路徑計算只需改一處

### **2. 易於維護**
- ✅ 路徑結構變更時統一更新
- ✅ 新增支援的安裝方式容易擴展
- ✅ 錯誤修復一次生效

### **3. 跨平台相容**
- ✅ 支援 Windows/Linux/macOS
- ✅ 自動偵測不同安裝來源
- ✅ 處理特殊字符和路徑格式

### **4. 測試友好**
- ✅ 可獨立測試路徑計算邏輯
- ✅ 模擬不同作業系統環境
- ✅ 驗證邊界條件處理

## 🔧 **編譯狀態**
- ✅ **編譯成功**
- ✅ **所有錯誤已修復**
- ✅ **功能保持完整**

## 🚀 **使用範例**

### **基本使用**
```csharp
// 初始化路徑服務
var pathService = new PathService();

// 推導工作坊路徑
string gamePath = @"D:\Steam\steamapps\common\RimWorld";
string workshopPath = pathService.GetWorkshopPath(gamePath);

// 驗證遊戲路徑
var result = pathService.IsValidGamePath(gamePath);
if (result.IsValid)
{
    Console.WriteLine("有效的遊戲路徑");
}
```

### **設定服務整合**
```csharp
// 在設定服務中使用
public class SettingsService
{
    private readonly IPathService _pathService;
    
    public async Task<bool> DetectModsConfigAsync()
    {
        string configPath = _pathService.GetModsConfigPath();
        return File.Exists(configPath);
    }
}
```

## 📝 **總結**
成功建立了統一的路徑服務架構，將原本分散在各處的路徑計算邏輯集中管理。現在無論在世界哪個地方使用這個程式，都不需要重複寫路徑推導邏輯，只需要注入和使用 `IPathService` 即可。這大大提高了程式碼的可維護性和重用性！
