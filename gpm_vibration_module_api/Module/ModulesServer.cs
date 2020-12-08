using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using gpm_vibration_module_api;
using  gpm_module_api;
using static gpm_vibration_module_api.clsEnum;

namespace gpm_module_api
{
    public class GPMModulesServer
    {
        /// <summary>
        /// Class for Clinet state 
        /// 類別儲存Client模式的感測模組連入後的各項屬性
        /// </summary>
        public class ConnectInState
        {
            /// <summary>
            /// Clinet模組 IP
            /// </summary>
            public string IP;
            /// <summary>
            /// Client Socket
            /// </summary>
            public Socket ClientSocket;
            /// <summary>
            /// 自動生成的API物件與Module為Server Mode時的用法相同
            /// </summary>
            public gpm_module_api.VibrationSensor.GPMModuleAPI ClientModuleAPI;
            /// <summary>
            /// 連入的模組感測類型
            /// </summary>
            public MODULE_TYPE CLIENT_MODULE_TYPE;
        }
        public static Socket SensorServerSocket;
        // Thread signal.  
        private static ManualResetEvent allDone = new ManualResetEvent(false);
        private static object lockThis = new System.Object();
        private static event Action<ConnectInState> SensorConnectInEvent;

        /// <summary>
        /// 存放已連線模組的字典; Key:感測模組種類: Value:Dictiony<string,GPMModuleAPI> 以IP為Key,GPMModuleAPI物件為Value的字典:存放GPMModuleAPI物件
        /// </summary>
        public static Dictionary<MODULE_TYPE, Dictionary<string, gpm_module_api.VibrationSensor.GPMModuleAPI>> ModuleClientList = new Dictionary<MODULE_TYPE, Dictionary<string, gpm_module_api.VibrationSensor.GPMModuleAPI>>()
        {
            { MODULE_TYPE.VIBRATION , new Dictionary<string, gpm_module_api.VibrationSensor.GPMModuleAPI>()},
            { MODULE_TYPE.UV , new Dictionary<string, gpm_module_api.VibrationSensor.GPMModuleAPI>()},
            { MODULE_TYPE.PARTICAL , new Dictionary<string,gpm_module_api.VibrationSensor. GPMModuleAPI>()},
        };

        private static void ModuleClinetListInitialize()
        {
            ModuleClientList = new Dictionary<MODULE_TYPE, Dictionary<string, gpm_module_api.VibrationSensor.GPMModuleAPI>>()
        {
            { MODULE_TYPE.VIBRATION , new Dictionary<string,gpm_module_api.VibrationSensor. GPMModuleAPI>()},
            { MODULE_TYPE.UV , new Dictionary<string,gpm_module_api.VibrationSensor. GPMModuleAPI>()},
            { MODULE_TYPE.PARTICAL , new Dictionary<string, gpm_module_api.VibrationSensor.GPMModuleAPI>()},
        };
        }

        /// <summary>
        /// 模組連線成功事件
        /// </summary>
        public static Action<ConnectInState> ModuleConnected;

        public static bool ServerStartUp(string ServerIP, int ServerPort, out string ErrorCode)
        {
            ModuleClinetListInitialize();
            SensorConnectInEvent += ModulesServer_SensorConnectInEvent; ;
            return SensorServerBuliding(ServerIP, ServerPort, out ErrorCode);
        }

        private static void ModulesServer_SensorConnectInEvent(ConnectInState obj)
        {
            GPMModuleAPI api_module = null;
            var module_type = Tools.ModuleWhoAreYou.Judege(ref obj.ClientSocket);
            obj.CLIENT_MODULE_TYPE = module_type;
            
            switch (module_type)
            {
                case MODULE_TYPE.VIBRATION:
                    obj.ClientModuleAPI = new VibrationSensor.GPMModuleAPI(obj);
                    break;
                case MODULE_TYPE.UV:
                    obj.ClientModuleAPI = new UVSensor.UVSensorAPI();
                    break;
                case MODULE_TYPE.PARTICAL:
                    obj.ClientModuleAPI = new ParticalSensor.ParticleModuleAPI();
                    break;
                case MODULE_TYPE.UNKNOW:
                    obj.ClientModuleAPI = new VibrationSensor.GPMModuleAPI(obj);
                    break;
                default:
                    break;
            }
            obj.CLIENT_MODULE_TYPE = obj.CLIENT_MODULE_TYPE == MODULE_TYPE.UNKNOW ?  MODULE_TYPE.VIBRATION : obj.CLIENT_MODULE_TYPE;
            var module_api_ls = new Dictionary<string, gpm_module_api.VibrationSensor.GPMModuleAPI>();
            ModuleClientList.TryGetValue(obj.CLIENT_MODULE_TYPE, out module_api_ls);
            if (module_api_ls != null)
            {
                if (!module_api_ls.ContainsKey(obj.IP))
                    module_api_ls.Add(obj.IP, obj.ClientModuleAPI);
                else
                    module_api_ls[obj.IP] = obj.ClientModuleAPI;
            }
            else
            {

            }
            ModuleConnected.BeginInvoke(obj, null, null);
        }

        internal static bool SensorServerBuliding(string strServerLocalIP, int intServerLocalPort, out string errorcode)
        {
            ServerClose();
            errorcode = "";
            SensorConnectInEvent += Inner_SensorConnectInEvent;
            try
            {
                SensorServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                SensorServerSocket.ReceiveBufferSize = 655350;
                SensorServerSocket.ReceiveTimeout = 30000;
                SensorServerSocket.Bind(new IPEndPoint(IPAddress.Parse(strServerLocalIP), intServerLocalPort));
                SensorServerSocket.Listen(999);
                Thread th = new Thread(ServerConnectedListen) { IsBackground = true };
                th.Start();
                return true;
            }
            catch (Exception exp)
            {
                errorcode = exp.Message;
                return false;
            }
        }

        public static void ServerClose()
        {
            try
            {
                SensorServerSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
            }
            try
            {
                SensorServerSocket.Close();
            }
            catch (Exception)
            {

            }
        }

        private static void Inner_SensorConnectInEvent(ConnectInState obj)
        {
            Console.WriteLine(obj.IP + " Connected");
        }
        /// <summary>
        /// 等待Sensor連線進來
        /// </summary>
        private static void ServerConnectedListen()
        {

            try
            {
                allDone.Reset();
                SensorServerSocket.BeginAccept(new AsyncCallback(SensorAcceptCallback),
                    SensorServerSocket);
                allDone.WaitOne();
            }
            catch (Exception exp)
            {
                //StaSystem.ConsoleLog("[ServerConnectedListen]" + exp.StackTrace);
            }
        }

        private static void SensorAcceptCallback(IAsyncResult ar)
        {
            lock (lockThis)
            {
                string IP = "";
                try
                {
                    allDone.Set();
                    Socket listener = (Socket)ar.AsyncState;
                    Socket SensorSocket = listener.EndAccept(ar);
                    SensorSocket.ReceiveBufferSize = 655350;
                    SensorSocket.ReceiveTimeout = 30000;
                    SensorSocket.Blocking = true;
                    SensorSocket.NoDelay = true;
                    IP = GetIPFromSocketObj(SensorSocket);
                    SensorConnectInEvent.Invoke(new ConnectInState { IP = IP, ClientSocket = SensorSocket });
                }
                catch (Exception exp)
                {

                }
            }
            ServerConnectedListen();
        }
        internal static string GetIPFromSocketObj(Socket sensorSocket)
        {
            IPEndPoint IPP = (IPEndPoint)sensorSocket.RemoteEndPoint;
            string Ip = IPP.Address.ToString();
            return Ip;
        }
    }
}
