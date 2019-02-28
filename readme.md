## Dynasor [![Build status](https://ci.appveyor.com/api/projects/status/ocngx0piu37y4eru/branch/master?svg=true)](https://ci.appveyor.com/project/0x0001F36D/dynasor/branch/master)


## 概要
  > 這個儲存庫 **Dynasor** [ˋdaInәˏsɒr] 意在解決
  - 熱更新
  - .NET組件/模組動態抽換
  - On-The-Fly code
  
  > 將此組件引用並"織入"你的程式裡，只要函式的簽章不變，透過自行引用、
  > 實作的通訊協定傳輸加解密程式碼後丟入至特定函式即可獲得實例化後的方法，並由織入位置引動 (Invoke)。<br>
  > 你不再需要懂MSIL就能寫Emit，只要我喜歡，有什麼不可以!!!

## 注意
  > 這個專案的內容為實驗性質，只支援編譯函式，且具有以下限制、支援
  - #### 須知
    - 函式將會編譯為靜態函式以供引動
    - 函式支援標籤 (Attribute) 及外部宣告 (extern)
  - #### 需求
    - 需安裝 .NET Framework 4.7 (語言使用 C# 7.3)
    - .NET Core 2.1
  - #### 支援
    - C# 7.3 語法

## 前身今世
  > Credit by [Erinus' NDynamicCompile](https://github.com/erinus/NDynamicCompile) (CodeDOM)

## 授權
Apache 2.0