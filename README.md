# **GPM Module SDK**

#  **Introduction**

本文件主要說明GPM Module API 中的**振動模組API**如何使用，以及示範相關的程式範例。

# **Release Notes/History**

| **Date** | **Author** | **Version** | **Change Description** |
| ------ | ------ | ----- | ----- |
|  2019/7/25  | J.W.H | V1.0 | Document Release. | 
|  2020/7/21 | J.W.H | V1.1 | Update content follow newest API design. |
|  2020/7/22 | J.W.H | V1.2 |Update 物件宣告/Connect 說明  Update ErrorCode content. |
|  2020/10/14 | J.W.H | V1.10.14.0 |新增ACC RAW DATA可自動儲存功能(Option) |
|  2020/11/09 | J.W.H | V1.11.09.0 |Timeout相關參數新增get屬性、修正Keypro檢查邏輯、修改參數讀寫重試次數預設值為2、優化韌體燒錄功能流程 |

# Overview

### [SETUP](#SETUP)
### Programming 
- [物件宣告](#宣告虛擬感測器物件)
- [連線](#連線)
- [量測範圍設定](#量測範圍設定)
- [獲取數據](#獲取數據)


# **SETUP (For C# developer)** <a name="SETUP"></a>

## DLL匯入Dll Import and Using Namespace
 請將GPM所提供的 gpm_module_api.dll加入參考，並使用命名空間
      
    using gpm_module_api;
    using gpm_module_api.VibrationSensor;
    using gpm_module_api.Data;



# **程式撰寫 Programming**

 以下將說明如何建立與GPM IDMS Module的連線，以及如何設定量測範圍、讀取感測數據。

## **宣告虛擬感測器物件** <a name="宣告虛擬感測器物件"></a>

    GPMModuleAPI module = newGPMModuleAPI ();

![](RackMultipart20200722-4-1pq752s_html_7b281d7559e44134.gif)

---
## **連線** <a name="連線"></a>

 使用GPMModuleAPI中的 **Connect** 方法。該方法將返回一int數值，回傳值為0表示連線OK；回傳非0表示連線異常，可參考Error Code說明。

    int errCode = await GPMModuleAPI.Connect(模組IP,模組port);

其中模組 IP與Port係根據模組網路設定。

### Example:
      int errCode = await module.Connect("192.168.0.3" , 5000);
      if(errCode == 0)
      { 
        //連線OK, Do something you want.
      }
      else
      { 
        //連線異常, Do something you want. 
      }

---
## **設定量測範圍** <a name="量測範圍設定"></a>

確認步驟B. Module連線成功後，設定量測範圍。

   Task<int> GPMModuleAPI.Measure_Range_Setting(clsEnum.Module_Setting_Enum.MEASURE_RANGE Range)

clsEnum.Module_Setting_Enum.MEASURE_RANGE **為量測範圍列舉**

### Example:
    var retCode;
    if (retCode = await module.Measure_Range_Setting(clsEnum.Module_Setting_Enum.MEASURE_RANGE._2g) == 0) 
        MessageBox.Show("OK");
    else //返回值非0,表示設定異常
        MessageBox.Show("ERROR");

---
## **讀取感測數據** <a name="獲取數據"></a>

- 使用 GPMModuleAPI中的 **GetData** 方法。該方法將返回一 **DataSet** 物件。DataSet中的 **ErrorCode** 回傳值為0表示數據可讀取；回傳非0表示異常，可參考Error Code說明。
- 確認 **DataSet.ErrorCode** 為0後，各項數值存於DataSet類別的屬性中, 取出使用即可。
### Example:

    DataSet data = module.GetData(); //送出抓取Data的request
    if (data.ErrorCode == 0) //,返回0值表示Data可抓取
    { 
        //各項數值存於DataSet類別的屬性中, 取出使用即可
        double[] Gx = data.AccData.X; //x軸 G值
        double[] Gy = data.AccData.Y; // x軸 G值
        double[] Gz = data.AccData.Z; // x軸 G值
        double[] FFTx = data.FFTData.X; //x軸 FFT
        double[] FFTy = data.FFTData.Y; //y軸 FFT
        double[] FFTz = data.FFTData.Z; //z軸 FFT
        double rmsx = data.Features.AccRMS.X; //x軸 RMS
        double rmsy = data.Features.AccRMS.Y; // y軸 RMS
        double rmsz = data.Features.AccRMS.Z; // z軸 RMS
        double TEx = data.Features.VibrationEnergy.X; // x軸 Energy
        double TEy = data.Features.VibrationEnergy.Y; // y軸 Energy
        double TEz = data.Features.VibrationEnergy.Z; // z軸 Energy
        double p2px = data.Features.AccP2P.X; // x軸 P2P
        double p2py = data.Features.AccP2P.Y; // y軸 P2P
        double p2pz = data.Features.AccP2P.Z; // z軸 P2P
    }
    else
    {
        //
    }




## **Error Code**

上述操作中，回傳Error Code對照的說明與排除方式如下表所述。

| **Error Code** | **說明** | **排除方式** |
| ----- | ----- | ----- |
| **401** | API試用期已超過 | |
| **403** | Keypro不正確，並非為GPM SSM API使用 | 洽GPM更換KeyPro |
| **404** | 偵測不到Keypro | 將KeyPro插入USB Port |
| **601** | IP輸入值不合法 | 需檢查並修改IP輸入值 |
| **602** | Port輸入值不合法 | 需檢查並修改Port輸入值 |
| **603** | 嘗試與Sensor連線失敗 | 需檢查IP、Port設定，以及網路連線狀態。 |
| **604** | 未與模組連線。通常發生在未連線就嘗試抓取DATA | 確認已透過Connect方法成功與模組進行連線。 |
| **605** | 接收加速度資料時發生timeout |確認網路連線/確認感測器與控制器硬體連接是否異常。|
| **606** | 設定模組參數時發生timeout |確認網路連線/確認感測器與控制器硬體連接是否異常。|
| **704** | 找不到License檔案(License.lic) |確認與dll同路徑下的資料夾中，是否有License.lic檔案，若檔案不存在請洽詢GPM。|
| **705** | License已超出使用期限 |請洽詢GPM以延長使用期限。|
| **706** | License檢查失敗 |請洽詢GPM索取新的License檔案。|
| **1506** | 偵測到感測器未連接控制器上/感測器異常 ||
| **16606** | Host沒有回應 ||
| **16607** | 模組處於數據擷取狀態(busy) ||
| **16608** | 進行參數設定時，模組回傳的參數與寫入的參數不匹配 ||
| **144444** | Api內部程式碼錯誤 ||



