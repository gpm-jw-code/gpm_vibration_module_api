using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm_module_api.License
{

    public class LicenseCheck : ICheckAction
    {
        public  LicenseCheckState Check(string licFilePath)
        {
            LicenseCheckState state = new LicenseCheckState();

            #region License Check 邏輯
            //存檔路徑
            string file_path = Path.GetDirectoryName(licFilePath);
            string file_name = Path.GetFileName(licFilePath);
            Tuple<bool, string> result = Read_file(file_path, file_name);
            if (result.Item1)
            {
                //有lic檔          

                //宣告物件
                GPMLicenseKey client_license = new GPMLicenseKey("Client");
                Dictionary<string, string> information_dict = new Dictionary<string, string>();

                try
                {
                    //把lic檔內容用:分開，前半是license，後半是內容
                    string[] ProductKeyArray = result.Item2.Split(':');

                    //把內容轉回dict
                    information_dict = client_license.String2Dictionary(ProductKeyArray[1]);

                    //dict餵到lic產生器
                    client_license.Set_information(information_dict);

                    //check有沒有過
                    bool license = client_license.Check_license_key(ProductKeyArray[0], Enum_license_product_type.API);

                    //沒過
                    if (!license)
                    {
                        state.CHECK_RESULT = CHECK_RESULT.FAIL;
                        state.Comment = "License驗證未通過";
                        return state;
                    }
                    else
                    {
                        //License過了還要檢查時間是不是還沒到期
                        if (information_dict["Experience"] != "Infinite")
                        {
                            DateTime dt = DateTime.ParseExact(information_dict["Experience"], "yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture);
                            if ((dt - DateTime.Now).TotalDays < -1)
                            {
                                //過期了，觸發過期event
                                state.CHECK_RESULT = CHECK_RESULT.EXPIRED;
                                state.Comment = "License 已過期";
                                return state;
                            }

                            if ((dt - DateTime.Now).TotalDays < 5)
                            {
                                //即將到期
                                state.CHECK_RESULT = CHECK_RESULT.PASS;
                                state.Comment = "License 即將到期";
                                return state;
                            }
                        }
                        state.CHECK_RESULT = CHECK_RESULT.PASS;
                        state.Comment = "License 通過";
                        return state;
                    }
                }
                catch
                {
                    //lic檔不合法，觸發過期的event
                    state.CHECK_RESULT = CHECK_RESULT.FAIL;
                    state.Comment = "License驗證未通過";
                    return state;
                }

            }
            else
            {
                //找不到lic檔，觸發過期的event
                state.CHECK_RESULT = CHECK_RESULT.LOSS;
                state.Comment = "找不到License檔";
                return state;
            }
            #endregion License Check 邏輯

        }

        /// <summary>
        /// 安全讀檔，檔案不存在return false
        /// </summary>
        /// <param name="file_path"></param>
        /// <param name="file_name"></param>
        /// <returns></returns>
        private static Tuple<bool, string> Read_file(string file_path, string file_name)
        {
            bool file_exist_flag = false;
            string line = "";
            try
            {
                string full_path = ""; 
                //看是使用相對路徑還絕對路徑
                if (file_path!="")
                {
                    full_path = file_path + "//" + file_name;
                }
                else
                {
                    full_path = file_name;
                }

                // 判斷檔案是否存在 
                //檔案存在，讀檔
                if (System.IO.File.Exists(Path.GetFullPath(full_path)))
                {
                    //讀檔
                    using (var file = new FileStream(Path.GetFullPath(full_path), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var reader = new StreamReader(file);
                        line = reader.ReadLine();
                    }
                    file_exist_flag = true;
                }
                return Tuple.Create(file_exist_flag, line);
            }
            catch (Exception ex)
            {
                file_exist_flag = false;
                line = "";
                return Tuple.Create(file_exist_flag, line);
            }
        }
    }
}
