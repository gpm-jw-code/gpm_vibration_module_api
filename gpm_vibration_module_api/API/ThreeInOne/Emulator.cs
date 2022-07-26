﻿using System;
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
        public bool ShortDataTest = true;
        byte[] returnBytes = new byte[8];
        public int Connect(string PortName)
        {
            return base.Open(PortName, 115200) ? 0 : (int)clsErrorCode.Error.CONNECT_FAIL;
        }

        private double base_Pressure_1 = 0;
        private double base_Pressure_2 = 0;
        private double calibration_Humidity_1 = 0;
        private double calibration_Humidity_2 = 0;
        internal override void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort sp = (SerialPort)sender;
                int numOfReadByte = sp.BytesToRead;
                byte[] buff = new byte[numOfReadByte];
                sp.Read(buff, 0, numOfReadByte);
                string cmd = Encoding.ASCII.GetString(buff);
                var fakeData = FakeDataGen();
                //讀取數據
                if (cmd == "READVALUE\r\n")
                {
                    Thread.Sleep(1);
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
                    HumidityCalibrationDefine(returnBytes);
                    sp.Write(returnBytes, 0, 8);
                }
            }
            catch (Exception ex)
            {
                return;
            }


        }

        private byte[] FakeDataGen()
        {

            double fakeTemp1 = 23.335 + new Random(DateTime.Now.Second).NextDouble();
            var bytes_fakeTemp1 = FloatToIEEE754Bytes((float)fakeTemp1);

            double fakePressure1 = 1253.5 + new Random(DateTime.Now.Second).NextDouble() - base_Pressure_1;
            byte[] bytes_FakP1 = FloatToIEEE754Bytes((float)fakePressure1);

            double fakeHumidity1 = 44.55 + new Random(DateTime.Now.Second).NextDouble() + calibration_Humidity_1;
            var bytes_fakeHumidity1 = FloatToIEEE754Bytes((float)fakeHumidity1);

            double fakeTemp2 = 24.55 + new Random(DateTime.Now.Second).NextDouble();
            var bytes_fakeTemp2 = FloatToIEEE754Bytes((float)fakeTemp2);

            double fakePressure2 = 1333.5 + new Random(DateTime.Now.Second).NextDouble() - base_Pressure_2;
            byte[] bytes_FakP2 = FloatToIEEE754Bytes((float)fakePressure2);

            double fakeHumidity2 = 42.55 + new Random(DateTime.Now.Second).NextDouble() + calibration_Humidity_2;
            var bytes_fakeHumidity2 = FloatToIEEE754Bytes((float)fakeHumidity2);

            byte[] fakeData = new byte[ShortDataTest ? 22 : 3096];//3072 / 6 = 512 ;   6/6
            int single_axis_data_cnt = (ShortDataTest ? 1 : 512);
            for (int i = 0; i < single_axis_data_cnt; i++)
            {
                fakeData[i + single_axis_data_cnt * 3] = (byte)(DateTime.Now.Millisecond*new Random(2).Next(1,5));
                int r = ShortDataTest? DateTime.Now.Millisecond : (i * (int)DateTime.Now.Second);
                fakeData[i + single_axis_data_cnt * 4] = (byte)r;
                fakeData[i + single_axis_data_cnt * 5] = (byte)(DateTime.Now.Millisecond * new Random(2).Next(1, 5));
            }
            int TR_Data_start_index = ShortDataTest ? 6 : 3072;

            if (ShortDataTest)
            {
                //t1 : 32.3
                fakeData[TR_Data_start_index] = bytes_fakeTemp1[3];
                fakeData[TR_Data_start_index + 1] = bytes_fakeTemp1[2];
                fakeData[TR_Data_start_index + 2] = bytes_fakeTemp1[1];
                fakeData[TR_Data_start_index + 3] = bytes_fakeTemp1[0];

                //h1 : 60.2
                fakeData[TR_Data_start_index + 4] = bytes_fakeHumidity1[3];
                fakeData[TR_Data_start_index + 5] = bytes_fakeHumidity1[2];
                fakeData[TR_Data_start_index + 6] = bytes_fakeHumidity1[1];
                fakeData[TR_Data_start_index + 7] = bytes_fakeHumidity1[0];

                //t2 : 34.0
                fakeData[TR_Data_start_index + 8] = bytes_fakeTemp2[3];
                fakeData[TR_Data_start_index + 9] = bytes_fakeTemp2[2];
                fakeData[TR_Data_start_index + 10] = bytes_fakeTemp2[1];
                fakeData[TR_Data_start_index + 11] = bytes_fakeTemp2[0];
                fakeData[TR_Data_start_index + 12] = bytes_fakeHumidity2[3];
                fakeData[TR_Data_start_index + 13] = bytes_fakeHumidity2[2];
                fakeData[TR_Data_start_index + 14] = bytes_fakeHumidity2[1];
                fakeData[TR_Data_start_index + 15] = bytes_fakeHumidity2[0];
                return fakeData;
            }

            //t1 : 32.3
            fakeData[3072] = bytes_fakeTemp1[3];
            fakeData[3073] = bytes_fakeTemp1[2];
            fakeData[3074] = bytes_fakeTemp1[1];
            fakeData[3075] = bytes_fakeTemp1[0];
            fakeData[3076] = bytes_FakP1[3];
            fakeData[3077] = bytes_FakP1[2];
            fakeData[3078] = bytes_FakP1[1];
            fakeData[3079] = bytes_FakP1[0];


            //h1 : 60.2
            fakeData[3080] = bytes_fakeHumidity1[3];
            fakeData[3081] = bytes_fakeHumidity1[2];
            fakeData[3082] = bytes_fakeHumidity1[1];
            fakeData[3083] = bytes_fakeHumidity1[0];


            //t2 : 34.0
            fakeData[3084] = bytes_fakeTemp2[3];
            fakeData[3085] = bytes_fakeTemp2[2];
            fakeData[3086] = bytes_fakeTemp2[1];
            fakeData[3087] = bytes_fakeTemp2[0];

            //p2 : 200.3
            fakeData[3088] = bytes_FakP2[3];
            fakeData[3089] = bytes_FakP2[2];
            fakeData[3090] = bytes_FakP2[1];
            fakeData[3091] = bytes_FakP2[0];

            //h2 : 94.0
            fakeData[3092] = bytes_fakeHumidity2[3];
            fakeData[3093] = bytes_fakeHumidity2[2];
            fakeData[3094] = bytes_fakeHumidity2[1];
            fakeData[3095] = bytes_fakeHumidity2[0];

            return fakeData;

        }

        private void HumidityCalibrationDefine(byte[] returnBytes)
        {
            calibration_Humidity_1 = GetHumidityCalibration(returnBytes[6]);
            calibration_Humidity_2 = GetHumidityCalibration(returnBytes[7]);

        }
        private double GetHumidityCalibration(byte settingByte)
        {
            switch (settingByte)
            {
                case 3:
                    return 0;
                case 4:
                    return -10;
                case 5:
                    return -9;
                case 6:
                    return -8;
                case 7:
                    return -7;
                case 8:
                    return -6;
                case 9:
                    return -5;
                case 10:
                    return -4;
                case 11:
                    return -3;
                case 12:
                    return -2;
                case 13:
                    return -1;
                case 14:
                    return 1;
                case 15:
                    return 2;
                case 16:
                    return 3;
                case 17:
                    return 4;
                case 18:
                    return 5;
                case 19:
                    return 6;
                case 20:
                    return 7;
                case 21:
                    return 8;
                case 22:
                    return 9;
                case 23:
                    return 10;
                default:
                    return 0;

            }
        }

        public void Close()
        {
            base.Close();
        }

        private byte[] FloatToIEEE754Bytes(float data)
        {
            int nValue = 0;
            int nSign;
            if (data >= 0)
                nSign = 0x00;
            else
            {
                nSign = 0x01;
                data = data * (-1);
            }
            int nHead = (int)data;
            float fTail = data % 1;
            String str = Convert.ToString(nHead, 2);
            int nHead_Length = str.Length;
            nValue = nHead;
            int nShift = nHead_Length;
            while (nShift < 24)   // (nHead_Length + nShift < 23)
            {
                if ((fTail * 2) >= 1)
                    nValue = (nValue << 1) | 0x00000001;
                else
                    nValue = (nValue << 1);
                fTail = (fTail * 2) % 1;
                nShift++;
            }

            int nExp = nHead_Length - 1 + 127;
            nExp = nExp << 23;
            nValue = nValue & 0x7FFFFF;
            nValue = nValue | nExp;
            nSign = nSign << 31;
            nValue = nValue | nSign;

            int data1, data2, data3, data4;
            data1 = nValue & 0x000000FF;
            data2 = (nValue & 0x0000FF00) >> 8;
            data3 = (nValue & 0x00FF0000) >> 16;
            data4 = (nValue >> 24) & 0x000000FF;

            if (data == 0)
            {
                data1 = 0x00;
                data2 = 0x00;
                data3 = 0x00;
                data4 = 0x00;
            }
            return new byte[4] { (byte)data1, (byte)data2, (byte)data3, (byte)data4 };

        }

    }
}
