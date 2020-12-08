using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static gpm_vibration_module_api.clsEnum;
using static gpm_vibration_module_api.ClsModuleBase;

namespace gpm_module_api.Tools
{
    /// <summary>
    /// 該類別用來判斷module屬於何種類型
    /// </summary>
    public class ModuleWhoAreYou
    {
        private static MODULE_TYPE DetectedType = MODULE_TYPE.UNKNOW;
        private static SocketState RecieveSckVM;
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
                RecieveSckVM = new SocketState()
                {
                    work_socket_ = socket,
                    buffer_ = new byte[3072],
                };

                WaitEvent = new ManualResetEvent(false);
                SocketState ClinetSckVM = new SocketState()
                {
                    buffer_ = Encoding.ASCII.GetBytes("READVALUE\r\n"),
                    work_socket_ = socket
                };
                var offset = 0;
                var size = ClinetSckVM.buffer_.Length;
                socket.BeginReceive(RecieveSckVM.buffer_, 0, 0, SocketFlags.None, RecieveCallBack, RecieveSckVM);
                socket.BeginSend(ClinetSckVM.buffer_, 0, size, SocketFlags.None, SendCallBack, ClinetSckVM);
                WaitEvent.WaitOne();
                if (DetectedType != MODULE_TYPE.UNKNOW)
                    return DetectedType;
                WaitEvent.Reset();
                ClinetSckVM = new SocketState()
                {
                    buffer_ = Encoding.ASCII.GetBytes("READUVVAL\r\n"),
                    work_socket_ = socket
                };
                socket.BeginSend(ClinetSckVM.buffer_, 0, size, SocketFlags.None, SendCallBack, ClinetSckVM);
                WaitEvent.WaitOne();
                if (DetectedType != MODULE_TYPE.UNKNOW)
                    return DetectedType;

                ClinetSckVM = new SocketState()
                {
                    buffer_ = Encoding.ASCII.GetBytes("READALVAL\r\n"),
                    work_socket_ = socket
                };
                socket.BeginSend(ClinetSckVM.buffer_, 0, size, SocketFlags.None, SendCallBack, ClinetSckVM);
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
            var State = (SocketState)ar.AsyncState;
            var availabel = State.work_socket_.Available;
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
                State.work_socket_.Receive(buff, availabel, SocketFlags.None);
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
