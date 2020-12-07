using gpm_module_api.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gpm_module_api.Tools
{
    public class EthernetControllerTool
    {
        private const string ClassName = "EthernetControllerTool";
        private static int uPort = 1460;
        private static IPEndPoint UdpRemoteEP;
        private static clsSckUDP sckUDP = new clsSckUDP();
        public static List<sSEC> ModulesFound = new List<sSEC>();
        private static byte[] SearchMsg = new byte[4] { 0x46, 0x49, 0x4E, 0x44 };
        public static void Initialize()
        {
            try
            {
                sckUDP.DataArrival += SckUDP_DataArrival;
                UdpRemoteEP = new IPEndPoint(IPAddress.Broadcast, uPort);
                sckUDP.Bind(0);
            }
            catch (Exception ex)
            {
                Logger.Code_Error_Log.Log(ex.Message + ex.StackTrace);
            }

        }

        public struct sSEC
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Op;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] MAC;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] Ver;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] LocalIP;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] GatewayIP;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] SubnetMask;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] DnsIP;
            public byte IpMode;
            public byte Debug;
        }

        public static sSEC BytesToSec(byte[] dat)
        {
            sSEC sec = new sSEC();
            int size = Marshal.SizeOf(sec);
            IntPtr pPoint = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(dat, 0, pPoint, size);
                sec = (sSEC)Marshal.PtrToStructure(pPoint, sec.GetType());

                return sec;
            }
            catch (Exception ex)
            {
                Logger.Code_Error_Log.Log(ex.Message + ex.StackTrace);
            }
            finally
            {
                Marshal.FreeHGlobal(pPoint);

            }
            return sec;

        }
        public static byte[] SecToBytes(sSEC sec)
        {
            int size = 0;
            try
            {
                size = Marshal.SizeOf(sec);
            }
            catch (Exception exp)
            {
                Tools.Logger.Code_Error_Log.Log($"[{ClassName}] " + exp.Message);
            }
            IntPtr buffer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(sec, buffer, true);
            byte[] bytes = new byte[size];
            Marshal.Copy(buffer, bytes, 0, size);
            return bytes;
        }
        private static void SckUDP_DataArrival(clsSckUDP.clsDataArrival obj)
        {
            sSEC sec = new sSEC();
            switch (Encoding.ASCII.GetString(obj.dat, 0, 4))
            {
                case "FIND":
                    sec = BytesToSec(obj.dat);
                    ModulesFound.Add(sec);
                    Console.WriteLine("Find Controller");
                    break;
                case "SETT":
                    sec = BytesToSec(obj.dat);
                    ModulesFound.Add(sec);
                    Console.WriteLine("SETTING Finish");
                    break;
                case "FIRM":
                    break;
                default:
                    break;
            }
        }

        public class clsSckUDP
        {

            public struct clsDataArrival
            {
                public int len;
                public byte[] dat;
            }


            private Socket sck;
            private bool _isBroadcast;
            public event Action<clsDataArrival> DataArrival;

            public bool isBroadcase
            {
                get { return _isBroadcast; }
                set { _isBroadcast = value; }
            }

            public class StateObject
            {
                public Socket wSck = null;
                public EndPoint RemoteEP;
                public static int bufSize = 2048;
                public byte[] buf = new byte[bufSize];

            }

            private int GetRndPort()
            {
                int ret = 0;
                Random rnd = new Random(DateTime.Now.Millisecond);
                ret = rnd.Next(1, 65535);
                return ret;
            }

            public void Bind(int port)
            {
                try
                {
                    IPAddress lIP = null;
                    foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            lIP = ip;
                            break;
                        }
                    }

                    EndPoint LocalEP = new IPEndPoint(lIP, port);
                    sck = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    sck.EnableBroadcast = true;
                    sck.Bind(LocalEP);
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, port);
                    ReceiveFrom(remoteEP);
                }
                catch (Exception exp)
                {
                    Tools.Logger.Code_Error_Log.Log($"[{ClassName}] " + exp.Message);
                }

            }

            private void ReceiveFrom(EndPoint remoteEP)
            {
                try
                {
                    StateObject state = new StateObject();
                    state.wSck = sck;
                    state.RemoteEP = remoteEP;
                    sck.BeginReceiveFrom(state.buf, 0, StateObject.bufSize, SocketFlags.None, ref remoteEP, new AsyncCallback(ref ReceiveFromCallback), state);
                }
                catch (Exception exp)
                {
                    Tools.Logger.Code_Error_Log.Log($"[{ClassName}] " + exp.Message);
                }
            }

            private void ReceiveFromCallback(IAsyncResult ar)
            {
                if (ar.IsCompleted)
                {
                    StateObject state = (StateObject)ar.AsyncState;
                    sck = state.wSck;
                    int bytesRead = 0;
                    try
                    {
                        bytesRead = sck.EndReceiveFrom(ar, ref state.RemoteEP);
                        if (bytesRead > 0)
                        {
                            byte[] dat = new byte[bytesRead];
                            Array.Copy(state.buf, 0, dat, 0, bytesRead);
                            DataArrival.Invoke(new clsDataArrival { dat = dat, len = bytesRead });
                        }
                        sck.BeginReceiveFrom(state.buf, 0, StateObject.bufSize, SocketFlags.None, ref state.RemoteEP, new AsyncCallback(ref ReceiveFromCallback), state);
                    }
                    catch (Exception exp)
                    {
                        Tools.Logger.Code_Error_Log.Log($"[{ClassName}] " + exp.Message);
                    }
                }
            }

            public void SendTo(byte[] dat, IPEndPoint remoteEP)
            {
                try
                {
                    sck.BeginSendTo(dat, 0, dat.Length, SocketFlags.None, remoteEP, new AsyncCallback(ref SendToCallback), sck);
                }
                catch (Exception exp)
                {
                    Tools.Logger.Code_Error_Log.Log($"[{ClassName}] " + exp.Message);
                }
            }

            private void SendToCallback(IAsyncResult ar)
            {
                if (ar.IsCompleted)
                {
                    Socket _sck = (Socket)ar.AsyncState;
                    int byteSend = 0;
                    try
                    {
                        byteSend = sck.EndSendTo(ar);
                    }
                    catch (SocketException ex)
                    {
                        Tools.Logger.Code_Error_Log.Log($"[{ClassName}] " + ex.Message);
                        Console.WriteLine(ex.Message);
                        Initialize();
                    }
                    catch(Exception ex)
                    {
                        Tools.Logger.Code_Error_Log.Log($"[{ClassName}] " + ex.Message);
                        Console.WriteLine(ex.Message);
                        Initialize();
                    }

                }
            }

            public void Close()
            {
                sck.Close();
            }
        }

        public class clsSettingItems
        {
            public string LocalIP;
            public string GatewayIP;
            public string SubnetMask;
            public string DnsIP;
        }

        /// <summary>
        /// 利用重寫參數設定的方式嘗試Reboot控制器
        /// </summary>
        public async static Task<bool> Reboot(string IP)
        {
            if (UdpRemoteEP == null)
            {
                Initialize();
            }
            try
            {
                Search();
                sSEC gSec = ModulesFound.Find(i => IpMatch(i, IpStrToBytes(IP)));
                int cnt = 0;
                while (IpMatch((gSec = ModulesFound.Find(i => IpMatch(i, IpStrToBytes(IP)))), IpStrToBytes(IP)) == false)
                {
                    Search();
                    cnt++;
                    if (cnt >= 5000)
                    {
                        return false;
                    }
                    Thread.Sleep(1);
                }
                Setting(IP, gSec);
                return true;
            }
            catch (SocketException exp)
            {
                return false;
            }
            catch (Exception exp)
            {
                Tools.Logger.Code_Error_Log.Log($"[{ClassName}] " + exp.Message);
                return false;
            }

        }

        public static void Setting(string IP_Ori, sSEC sec)
        {
            sec.Op = Encoding.ASCII.GetBytes("SETT");
            sec.Debug = 0x01;
            var dat = SecToBytes(sec);
            sckUDP.SendTo(dat, new IPEndPoint(IPAddress.Broadcast, uPort));
        }

        public static void Setting(string IP_Ori, clsSettingItems settingItems)
        {
            //找到要設定的目標
            sSEC gSec = ModulesFound.Find(i => IpMatch(i, IpStrToBytes(IP_Ori)));
            sSEC sec = new sSEC();
            sec.Op = Encoding.ASCII.GetBytes("SETT");

            //固定的
            sec.Ver = gSec.Ver;
            sec.MAC = gSec.MAC;
            //
            //設定的
            sec.LocalIP = IpStrToBytes(settingItems.LocalIP);
            sec.GatewayIP = IpStrToBytes(settingItems.GatewayIP);
            sec.SubnetMask = IpStrToBytes(settingItems.SubnetMask);
            sec.DnsIP = IpStrToBytes(settingItems.DnsIP);
            //
            sec.Debug = 0x01;
            //要寫入的data
            var dat = SecToBytes(sec);
            sckUDP.SendTo(dat, new IPEndPoint(IPAddress.Broadcast, uPort));

        }

        /// <summary>
        /// 找模組?
        /// Tri~~~vago
        /// </summary>
        public static void Search()
        {
            ModulesFound.Clear();
            sckUDP.SendTo(SearchMsg, UdpRemoteEP);
        }

        private static byte[] IpStrToBytes(string Ip)
        {
            byte[] ret = new byte[4];
            string[] sIP = Ip.Split('.');
            if (sIP.Length == 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    try
                    {
                        ret[i] = Convert.ToByte(sIP[i]);
                    }
                    catch (Exception)
                    {
                        return new byte[0]; //fail
                    }

                }
            }
            else
                return new byte[0]; //fail
            return ret;
        }

        private static bool IpMatch(sSEC s, byte[] IP_Cadicate)
        {
            try
            {
                if (IP_Cadicate.Length != 4) return false;
                var ip = s.LocalIP;
                if (ip == null) return false;
                bool m1 = ip[0] == IP_Cadicate[0];
                bool m2 = ip[1] == IP_Cadicate[1];
                bool m3 = ip[2] == IP_Cadicate[2];
                bool m4 = ip[3] == IP_Cadicate[3];
                return (m1 == m2 == m3 == m4);
            }
            catch (Exception exp)
            {
                Tools.Logger.Code_Error_Log.Log($"[{ClassName}] " + exp.Message);
                return false;
            }
        }

    }
}
