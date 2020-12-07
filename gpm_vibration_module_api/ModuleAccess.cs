using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static gpm_module_api.VibrationSensor.ClsModuleBase;

namespace gpm_module_api
{
    /// <summary>
    /// 與模組之間透過Socket進行資料讀取、寫入
    /// </summary>
    internal class ModuleAccess
    {

        private SocketState socketState = new SocketState();

        public ModuleAccess(Socket socket)
        {
            socketState.socket = socket;
        }

        private void Send(byte[] dat)
        {
            socketState.socket.BeginSend(dat, 0, dat.Length, SocketFlags.None, new AsyncCallback(SendCallBack), socketState);
        }

        private void SendCallBack(IAsyncResult ar)
        {
            throw new NotImplementedException();
        }




    }
    public class SocketState
    {

        public static int BufferSize = 1024;
        public byte[] Buffer = new byte[BufferSize];
        public Socket socket;
        public IList<ArraySegment<byte>> LsBuffer = new List<ArraySegment<byte>>();
    }


}
