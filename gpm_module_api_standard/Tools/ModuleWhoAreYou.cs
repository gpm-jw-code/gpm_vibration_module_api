using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static gpm_module_api.Tools.EthernetControllerTool.clsSckUDP;
using static gpm_vibration_module_api.clsEnum;

namespace gpm_module_api.Tools
{
    /// <summary>
    /// 該類別用來判斷module屬於何種類型
    /// </summary>
    public class ModuleWhoAreYou
    {
        private static MODULE_TYPE DetectedType = MODULE_TYPE.UNKNOW;
        private static StateObject RecieveSckVM;
        private static ManualResetEvent WaitEvent;

        /// <summary>
        /// 給我socket 我try到爆
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public static MODULE_TYPE Judege(ref Socket socket)
        {
            try
            {
                RecieveSckVM = new StateObject()
                {
                    wSck = socket,
                    buf = new byte[3072],
                };

                WaitEvent = new ManualResetEvent(false);
                StateObject ClinetSckVM = new StateObject()
                {
                    buf = Encoding.ASCII.GetBytes("READVALUE\r\n"),
                    wSck = socket
                };
                var offset = 0;
                var size = ClinetSckVM.buf.Length;
                socket.BeginReceive(RecieveSckVM.buf, 0, 0, SocketFlags.None, RecieveCallBack, RecieveSckVM);
                socket.BeginSend(ClinetSckVM.buf, 0, size, SocketFlags.None, SendCallBack, ClinetSckVM);
                WaitEvent.WaitOne();
                if (DetectedType != MODULE_TYPE.UNKNOW)
                    return DetectedType;
                WaitEvent.Reset();
                ClinetSckVM = new StateObject()
                {
                    buf = Encoding.ASCII.GetBytes("READUVVAL\r\n"),
                    wSck = socket
                };
                socket.BeginSend(ClinetSckVM.buf, 0, size, SocketFlags.None, SendCallBack, ClinetSckVM);
                WaitEvent.WaitOne();
                if (DetectedType != MODULE_TYPE.UNKNOW)
                    return DetectedType;

                ClinetSckVM = new StateObject()
                {
                    buf = Encoding.ASCII.GetBytes("READALVAL\r\n"),
                    wSck = socket
                };
                socket.BeginSend(ClinetSckVM.buf, 0, size, SocketFlags.None, SendCallBack, ClinetSckVM);
                WaitEvent.WaitOne();
                if (DetectedType != MODULE_TYPE.UNKNOW)
                    return DetectedType;

                return DetectedType;
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message + "|" + exp.StackTrace);
                return MODULE_TYPE.UNKNOW;
            }
        }
        private static void SendCallBack(IAsyncResult ar)
        {
            Task.Run(() =>
            {
                int cnt = 0;
                while (cnt < 1000)
                {
                    if (DetectedType != MODULE_TYPE.UNKNOW)
                    {
                        return;
                    }
                    cnt++;
                    Thread.Sleep(1);
                };
                DetectedType = MODULE_TYPE.UNKNOW;
                WaitEvent.Set();
            });
        }

        private static void RecieveCallBack(IAsyncResult ar)
        {
            var State = (StateObject)ar.AsyncState;
            var availabel = State.wSck.Available;
            switch (availabel)
            {
                case 3072:
                    DetectedType = MODULE_TYPE.VIBRATION;
                    break;
                case 4:
                    DetectedType = MODULE_TYPE.UV;
                    break;
                case 62:
                    DetectedType = MODULE_TYPE.PARTICAL;
                    break;
                case 0:
                    DetectedType = MODULE_TYPE.UNKNOW;
                    break;
                default:
                    DetectedType = MODULE_TYPE.VIBRATION;
                    break;
            }
            byte[] buff = new byte[availabel];
            try
            {
                State.wSck.Receive(buff, availabel, SocketFlags.None);
            }
            catch (Exception ex)
            {
                WaitEvent.Set();
                throw ex;
                return;
            }

            WaitEvent.Set();
        }
    }
}
