using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace gpm_module_api.License
{
    public enum Enum_license_product_type
    {
        None = 0,
        IDMS = 1,
        API = 2,
        IPQC = 3,
        Spindel_Recorder = 4,
        UV = 5,
        Change_Tool = 6
    }
    class GPMLicenseKey
    {
        private string ContainerName;
        private string PublicKey, PrivateKey;
        private Dictionary<string, string> information_dict = new Dictionary<string, string>();
        private RSACryptoServiceProvider rsa;

        //Constructor
        public GPMLicenseKey(string ContainerName)
        {
            //建立保管庫
            this.ContainerName = ContainerName;
            CspParameters cp = new CspParameters();
            cp.KeyContainerName = this.ContainerName;
            //建立公私鑰
            rsa = new RSACryptoServiceProvider(2048, cp);
            this.PublicKey = rsa.ToXmlString(false);
            this.PrivateKey = rsa.ToXmlString(true);
        }


        //把string轉成license key(加密)
        public string Encoding_license_key(string content = "")
        {
            //不給input直接把設定轉為string作為input
            if (content == "")
            {
                content = Dictionary2String(this.information_dict);
            }
            string license_key;
            ////取得保管金鑰的KeyContainer
            //CspParameters cp = new CspParameters();
            //cp.KeyContainerName = this.ContainerName;
            //// 建立 RSA 加解密物件，建立同時指出要從哪個Container取得金鑰。
            //RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048, cp);
            // 將資料切成byte後計算雜湊值跟加密。
            byte[] dataToSign = Encoding.Default.GetBytes(content);
            byte[] signature = rsa.SignData(dataToSign, new SHA1CryptoServiceProvider());
            // 將數位簽章的內容轉成 Base64 字串，以便顯示。
            license_key = Convert.ToBase64String(signature);
            return license_key;
        }


        //用license key驗證string(解密)
        public bool Check_license_key(string license_key, Enum_license_product_type product = Enum_license_product_type.IDMS, string content = "", string public_key = "")
        {
            bool license;
            string str_public_key = "";

            switch (product)
            {
                case Enum_license_product_type.IDMS:
                    str_public_key = "<RSAKeyValue><Modulus>myCFuIvL17hl54z75B7yd5S+799jwe9Kuueim5h5LjTOq6vMISzmqYy8Nx1Mc5bmH7nh38ajOlqCqwlwe7VVRhNO/T7UA0BKCm86JFLzgX0VfKSPqTBC9A9fHIp2KZXCtCZAtT5c6Yp7VZmt0cncsA4vP1lKAerhab0ppjBMNCRfRyq5mGScGaYpwyET7uzbIGxwPXOKuznzgKeHqhwP/lcmMBil+W/g/sxB7kkXqEHSo121a4grhBWaKG7ze2hY6973L8VNnv7J1zn59oE8owC/vyv4DQJdANXCrYSctNGNu/t+hJSpw0CAk5R910tNkFHl6/VjqHxHuG30lhimrw==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
                    break;
                case Enum_license_product_type.API:
                    str_public_key = "<RSAKeyValue><Modulus>pVvhhVy+cpgc/ZZNll7rkmeVuE467a/2A9xkkDtPc/U9nx8wqLyaJdD440dMAlGE0IWsiX7QUa4I5fA2PPXaScisrUpf9UsMWuGkx144vkru6Hh/rhSSl207u5fkkSHeXft1SYRSb7VbArFlqPiUlVY0vClJrinxusgRRr+V7gxs3OLEU9IBy2YMpTyv0ivFz9GQHfbIr3NuyZOdSV21rOfO4yGWbQGJJrT08JLM2tv7ZG6mxfeO2n8TH6GzdN5m/bXyhwSOkb63CGA+naXtEfKAN7xhUiKWiR4g9bZkcpWTFvt0WRzLJyYJj2iafsv2Pv7Pu8c+IILZxMvFhueg7Q==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
                    break;
                case Enum_license_product_type.IPQC:
                    str_public_key = "<RSAKeyValue><Modulus>3oVkkopkicMw3CsrAGW2k17nKXyoKDiO3yJbRM3YFNlJBxx13kDh205JrB/e7n+069W7ZF1kHKOeaIytUSXOv6W+POMkVJA85OPdEwfWs4e+mMp49PN1xcF1sfrkrtn9J+hQKQTJIA++GHeFFu3Tprp/HJaDVI2KrIsErl/FIe+E7zoqTZS8gcvE9G7pf/NFdoJtUztx9dvPV6y4bqr2NvIiIHlPVBfY7UfOZk0Kzy0iqEYkIIg9LaHv2FTLDrLpqj1gkXa4zv8c5/JbQMggW7HhjckQtkKC/a2OAcuoJdimAV6NepjeXlXKwREFMoaz9im5TISExtX9m9rEFnlXdQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
                    break;
                case Enum_license_product_type.Spindel_Recorder:
                    str_public_key = "<RSAKeyValue><Modulus>24Of60TO0ym904cdYnq5a/k4+DMoZVs0xn4z7MRfP8YjLM78E62TvQ5SiIcL3kwCJ+YzJednYOrJz+tWzKQEh26F8RWbSIylZQNgH/sSb5EbVbYjOgecfUuOOuZEbHmedRotuEawFxnoha63aAQsvQNZGtZhUKdszLflbeo7hQPjs+CKljakHOHcRlEPKLKWmmM2BVAWSHPlAVjm90Q6KzUq6bppx2E9UrF1+eUH9pI6FkDXaO0Mn8wb02Crp0gLYmhVS4HLZOY7mwlySbf+MGLm5GFJieg/WOk7D7FbiE4A5i0qmT57l/55scLSsY1qEompI0Vqp//cXiTGW5rE9Q==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
                    break;
                case Enum_license_product_type.UV:
                    str_public_key = "<RSAKeyValue><Modulus>m798EJDhVTjfa266pTCdrQTBQTiWn7xPhPBJy/pny4zoQ3RHE4i0PHEKqf/Xurp3u7j/fKsofCdKR15qoF1gj/WfcnzDUNfm50Wzrp0BBvhZj7TnGCJZtVnAd/wytLbfyPZHs/QgsBnxe6tVU9wuLFbEKIbRqSD9UWsRCEtcx5S1N5KTX0j1r4idHd3+gjv+LfefULRtWnrNZMSyaW+UDpBdjGI6iOgf65LN0bQGLW3UR1Dlhfj0Yqrg+xLzAzlCaq24pLAd1TzZV/DrOPH4SrWvN/VqCCDCdsih5pIx+cHdxkr2903UkYhCdoH4JHdmZ9xFgX+Yrf6bSsRJJ0GATQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
                    break;
                case Enum_license_product_type.Change_Tool:
                    str_public_key = "<RSAKeyValue><Modulus>tNU7AptuXea4QpFQ2CEUdZ2+qiCjd9ud1IumqAAzHMnGdCPXgpsnMhozRbVKPaRzzWIn5aINleNZww4D1F4+Ro1OWESG1ogxJMqhT9NmN8NpNv99beFRid1XA/3NEiH0tpOZC/kdOnOZsV30sM24NGkV+Hup/W68tQjABq+V/zwN9BsAUV6qi3c2E+iGUbuCSgp22SvaqHieOdjYM1OFs7D42xzSl1LezFpKJQxJ+plaBUUdaNHHb6sbJREs4xCXPYlDJwUe9/elG0BQjXX0yoS9tRub4RP23GHD/oMnGiTBt64wnPjEa9L44RWcuRYEjKaNBMGctLgnWDgS0jm9RQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
                    break;
                case Enum_license_product_type.None:
                    str_public_key = public_key;
                    break;

            }

            //不給string則將設定值轉為string做驗證
            if (content == "")
            {
                content = Dictionary2String(this.information_dict);
            }
            //載入設定並將設定轉為string取得原文
            byte[] orgData = Encoding.Default.GetBytes(content);
            //取得license key
            byte[] signedData = Convert.FromBase64String(license_key);
            // 建立 RSA 解解密物件，建立同時以直接設定的方式設定公鑰。
            RSACryptoServiceProvider rsa_client = new RSACryptoServiceProvider(2048);
            rsa_client.FromXmlString(str_public_key);
            //驗證
            license = rsa_client.VerifyData(orgData, new SHA1CryptoServiceProvider(), signedData);

            return license;
        }

        //把dictionary轉成string
        public string Dictionary2String(Dictionary<string, string> dic)
        {
            string str;
            str = "{" + string.Join(",", dic.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}";
            return str;
        }

        //把string轉成dictionary
        public Dictionary<string, string> String2Dictionary(string str)
        {
            string[] key_value;
            Dictionary<string, string> dic = new Dictionary<string, string>();
            str = str.Replace("{", "");
            str = str.Replace("}", "");
            string[] key_val_pair = str.Split(',');
            for (int i = 0; i < key_val_pair.Length; i++)
            {
                key_value = key_val_pair[i].Split('=');
                dic.Add(key_value[0], key_value[1]);
            }
            return dic;
        }

        //外部檔案匯入私鑰
        public void Update_private_key(string xml_string)
        {
            rsa.FromXmlString(xml_string);
            this.PublicKey = rsa.ToXmlString(false);
            this.PrivateKey = rsa.ToXmlString(true);
        }

        //set&get
        public void Set_information(Dictionary<string, string> dict)
        {
            this.information_dict = dict;
        }

        public Dictionary<string, string> Get_information()
        {
            return this.information_dict;
        }

        public string Get_public_key()
        {
            return this.PublicKey;
        }

        public string Get_private_key()
        {
            return this.PrivateKey;
        }
    }
}
