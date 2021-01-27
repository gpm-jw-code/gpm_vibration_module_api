#define W710
//#define CE
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using gpm_vibration_module_api;

namespace gpm_vibration_module_api
{
    internal static class KeyproMdule
    {
        internal struct API
        {

            /// <summary>
            /// 偵測到KEY被移除的事件
            /// </summary>
            internal static event Action<DateTime> KeyProRemoveEvent;
            /// <summary>
            /// 偵測到KEY被插入的事件
            /// </summary>
            internal static event Action<DateTime> KeyProInsertEvent;

            private static Thread keyproCheckTh;
            /// <summary>
            /// 確認Key是不是有插在USB Port上
            /// </summary>
            /// <returns></returns>
            internal static gpm_vibration_module_api.clsEnum.KeyPro.KEYPRO_EXIST_STATE IsKeyInsert()
            {
                StartKeyproMonitor();
                return BaseMethod.MixCheck() == true ? gpm_vibration_module_api.clsEnum.KeyPro.KEYPRO_EXIST_STATE.Exist : gpm_vibration_module_api.clsEnum.KeyPro.KEYPRO_EXIST_STATE.NoInsert;
            }

            /// <summary>
            /// 開始執行確認KEY狀態的實時監控
            /// </summary>
            private static void StartKeyproMonitor()
            {
                if (keyproCheckTh == null)
                {
                    keyproCheckTh = new Thread(keyprocheckMonitor) { IsBackground = true };
                    keyproCheckTh.Start();
                }
            }

            private static void keyprocheckMonitor()
            {
                while (true)
                {
                    try
                    {
                        if (BaseMethod.MixCheck())
                        {
                            KeyProInsertEvent.Invoke(DateTime.Now);
                        }
                        else
                        {
                            KeyProRemoveEvent.Invoke(DateTime.Now);
                        }
                    }
                    catch (Exception exp)
                    {

                    }
                    Thread.Sleep(1);

                }
            }
        }

        internal struct BaseMethod
        {
            private static PurchaseCode KeyPurchaseCode
            {
                set
                {
                    switch (value)
                    {
                        case PurchaseCode.KLXSS:
                            p1 = 0x6331;
                            p2 = 0x5350;
                            p3 = 0xaf0b;
                            p4 = 0xa090;
                            break;
                        case PurchaseCode.ACTCI:
                            p1 = 0x5331;
                            p2 = 0x5ca0;
                            p3 = 0x5f0b;
                            p4 = 0x509c;
                            break;
                    }

                }
            }
            public enum PurchaseCode
            {
                KLXSS = 0, ACTCI = 1
            }

            private static ushort p1 = 0x6331;
            private static ushort p2 = 0x5350;
            private static ushort p3 = 0xaf0b;
            private static ushort p4 = 0xa090;

            [DllImport("Rockey4ND.dll", EntryPoint = "Rockey")]
            public static extern ushort Rockey(ushort function, ref ushort handle, ref uint lp1, ref uint lp2, ref ushort p1, ref ushort p2, ref ushort p3, ref ushort p4, byte[] buffer);
            [DllImport("Rockey4SClass_x64.dll", EntryPoint = "Rockey4SmartClass.Rockey4Smart")]
            private static extern ushort Rockey64(ushort function, ref ushort handle, ref uint lp1, ref uint lp2, ref ushort p1, ref ushort p2, ref ushort p3, ref ushort p4, byte[] buffer);

            private static object _lock = new object();
            /// <summary>
            /// 確認所有密碼的Keypro
            /// </summary>
            /// <returns></returns>
            public static bool MixCheck()
            {

                lock (_lock)
                {
                    // move dll to C://Windows
                    //File.Copy(".//Rockey4ND.dll", "\\Windows\\Rockey4ND.dll", true);
                    bool isexist = false;
                    if (KeyProInsertCheck(PurchaseCode.KLXSS) == gpm_vibration_module_api.clsEnum.KeyPro.KEYPRO_EXIST_STATE.Exist)
                    {
                        isexist = true;
                    }
                    else
                    {
                        if (KeyProInsertCheck(PurchaseCode.ACTCI) == gpm_vibration_module_api.clsEnum.KeyPro.KEYPRO_EXIST_STATE.Exist)
                            isexist = true;
                        else
                            isexist = false;
                    }
                    return isexist;
                }
            }

            private static int FindKey(ushort handle, uint lp1, uint lp2, ushort pass1, ushort pass2, ushort pass3, ushort pass4, byte[] buffer)
            {
                try
                {
                    Assembly A = Assembly.LoadFrom("Rockey4SClass_x86.dll");
                    Type AT = A.GetType("R4SmartClass.Rockey4S");
                    object o = System.Activator.CreateInstance(AT);
                    Type t = o.GetType();
                    MethodInfo MI = AT.GetMethod("Rockey");
                    object result = MI.Invoke(o, new object[] { (ushort)gpm_vibration_module_api.clsEnum.KeyPro.RY4CMD.RY_FIND, handle, lp1, lp2, p1, p2, p3, p4, buffer });
                    return Convert.ToInt32(result);
                }
                catch (Exception exp)
                {
                    return -999;
                }
            }

            public static gpm_vibration_module_api.clsEnum.KeyPro.KEYPRO_EXIST_STATE KeyProInsertCheck(PurchaseCode purchaseCode)
            {
                try
                {
                    KeyPurchaseCode = purchaseCode;
                    byte[] buffer = new byte[1000];
                    ushort handle = 0;
                    uint lp1 = 0;
                    uint lp2 = 0;
                    uint[] uiarrRy4ID = new uint[32];
#if (CE)
                object ret = Rockey((ushort)Ry4Cmd.RY_FIND, ref handle, ref lp1, ref lp2, ref p1, ref p2, ref p3, ref p4, buffer);//Find Ry4S
#endif
#if (W710)
                    //Rockey4SmartClass.Rockey4Smart rockey = new Rockey4Smart();
                    object ret = FindKey(handle, lp1, lp2, p1, p2, p3, p4, buffer);//Find Ry4S
#endif
                    if (0 != Convert.ToInt32(ret))
                    {
                        return clsEnum.KeyPro.KEYPRO_EXIST_STATE.NoInsert;
                    }
                    else
                        return clsEnum.KeyPro.KEYPRO_EXIST_STATE.Exist;
                }
                catch (Exception EXP)
                {
                    return clsEnum.KeyPro.KEYPRO_EXIST_STATE.NoInsert;
                }
            }

            private static string os;
            private static object staKeyprocheckByOS(ushort handle, uint lp1, uint lp2, ushort pass1, ushort pass2, ushort pass3, ushort pass4, byte[] buffer)
            {
                os = Environment.OSVersion.ToString();
                bool is64OS = (os == "64") ? true : false;
                if (is64OS == true)
                {
                    Assembly A = Assembly.LoadFrom(".\\NetRockey4NDControl.dll");
                    Type AT = A.GetType("Rockey4SmartClass.Rockey4Smart");
                    object o = System.Activator.CreateInstance(AT);
                    Type t = o.GetType();
                    MethodInfo MI = AT.GetMethod("Rockey");
                    object result = MI.Invoke(o, new object[] { (ushort)gpm_vibration_module_api.clsEnum.KeyPro.RY4CMD.RY_FIND, handle, lp1, lp2, pass1, pass2, pass3, pass4, buffer });
                    return result;
                }
                else
                {
                    Assembly A = Assembly.LoadFrom(".\\NetRockey4NDControl.dll");
                    Type AT = A.GetType("R4SmartClass.Rockey4S");
                    object o = System.Activator.CreateInstance(AT);
                    Type t = o.GetType();
                    MethodInfo MI = AT.GetMethod("Rockey");
                    object result = MI.Invoke(o, new object[] { (ushort)gpm_vibration_module_api.clsEnum.KeyPro.RY4CMD.RY_FIND, handle, lp1, lp2, pass1, pass2, pass3, pass4, buffer });
                    return result;
                }
            }

        }
    }
}
