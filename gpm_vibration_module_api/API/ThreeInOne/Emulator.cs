using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.ThreeInOne
{
    public class Emulator : SerialProtocolBase
    {
        public bool isParamReturErrorSimulate { get; set; } = false;
        byte[] returnBytes = new byte[8];
        public int Connect(string PortName)
        {
            return base.Open(PortName, 115200) ? 0 : (int)clsErrorCode.Error.CONNECT_FAIL;
        }
        internal override void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort sp = (SerialPort)sender;
                int numOfReadByte = sp.BytesToRead;
                byte[] buff = new byte[numOfReadByte];
                sp.Read(buff, 0, numOfReadByte);
                string cmd = Encoding.ASCII.GetString(buff);

                byte[] fakeData = new byte[3096];

                #region Fake Data Defined

                for (int i = 0; i < 512; i++)
                {
                    fakeData[i + 512] = (byte)i;
                    int r = (i * DateTime.Now.Second);
                    fakeData[i + 1024] = (byte)r;
                    fakeData[i + 2048] = (byte)r;
                }

                //t1 : 32.3
                fakeData[3072] = 0x42;
                //fakeData[3073] = 0x11;
                fakeData[3073] = (byte)DateTime.Now.Second;
                //fakeData[3074] = 0x73;
                fakeData[3074] = (byte)DateTime.Now.Second;
                //fakeData[3075] = 0x33;
                fakeData[3075] = (byte)DateTime.Now.Second;
                //p1 : 123.3
                fakeData[3076] = 0x42;
                //fakeData[3077] = 0xf6;
                fakeData[3077] = (byte)DateTime.Now.Second;
                fakeData[3078] = 0x99;
                fakeData[3079] = 0x9a;

                //h1 : 60.2
                fakeData[3080] = 0x42;
                fakeData[3081] = 0x70;
                fakeData[3082] = 0xcc;
                fakeData[3083] = 0xcd;


                //t2 : 34.0
                fakeData[3084] = 0x42;
                fakeData[3085] = 0x08;
                fakeData[3086] = 0x00;
                fakeData[3087] = 0x00;

                //p2 : 200.3
                fakeData[3088] = 0x43;
                fakeData[3089] = 0x48;
                fakeData[3090] = 0x4c;
                fakeData[3091] = 0xcd;

                //h2 : 94.0
                fakeData[3092] = 0x42;
                fakeData[3093] = 0xbc;
                fakeData[3094] = 0x00;
                fakeData[3095] = 0x00;

                #endregion
                //讀取數據
                if (cmd == "READVALUE\r\n")
                {
                    Thread.Sleep(700);
                    Console.WriteLine(cmd);
                    sp.Write(fakeData, 0, fakeData.Length);
                }

                if (cmd == "READSTVAL\r\n")
                {
                    sp.Write(returnBytes, 0, returnBytes.Length);
                }

                //參數設定 53 01 00 9f 00 00 00 00 00
                if (buff[0] == 0x53)
                {
                    Array.Copy(buff, 1, returnBytes, 0, 8);
                    if (isParamReturErrorSimulate)
                        returnBytes[3] = 0x32;//模擬封包回傳錯誤
                    sp.Write(returnBytes, 0, 8);
                }
            }
            catch (Exception ex)
            {
                return;
            }
            

        }

        public void Close()
        {
            base.Close();
        }
    }
}
