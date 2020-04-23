using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace gpm_vibration_module_api.Module
{
    public static class ModulesServer
    {
        public class ConnectObj
        {
            public string IP;
            public Socket ModuleSocket;
        }
        public static Socket SensorServerSocket;
        // Thread signal.  
        private static ManualResetEvent allDone = new ManualResetEvent(false);
        private static System.Object lockThis = new System.Object();
        public static event Action<ConnectObj> SensorConnectInEvent;
        public static bool SensorServerBuliding(string strServerLocalIP, int intServerLocalPort,out string errorcode)
        {
            errorcode = "";
               SensorConnectInEvent += Inner_SensorConnectInEvent;
            try
            {
                SensorServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                SensorServerSocket.ReceiveTimeout = 2300;
                SensorServerSocket.SendTimeout = 100;
                SensorServerSocket.ReceiveBufferSize = 81920;
                SensorServerSocket.SendBufferSize = 100;
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

        private static void Inner_SensorConnectInEvent(ConnectObj obj)
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

        public static void SensorAcceptCallback(IAsyncResult ar)
        {
            lock (lockThis)
            {
                string IP = "";
                try
                {
                    allDone.Set();
                    Socket listener = (Socket)ar.AsyncState;
                    Socket SensorSocket = listener.EndAccept(ar);
                    SensorSocket.ReceiveBufferSize = 800000;
                    SensorSocket.SendBufferSize = 100;
                    SensorSocket.ReceiveTimeout = 10000;
                    SensorSocket.SendTimeout = 10000;
                    IP = GetIPFromSocketObj(SensorSocket);
                    SensorConnectInEvent.Invoke(new ConnectObj { IP = IP, ModuleSocket = SensorSocket });
                }
                catch (Exception exp)
                {

                }
            }
            ServerConnectedListen();
        }
        public static string GetIPFromSocketObj(Socket sensorSocket)
        {
            IPEndPoint IPP = (IPEndPoint)sensorSocket.RemoteEndPoint;
            string Ip = IPP.Address.ToString();
            return Ip;
        }
    }
}
