using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using gpm_vibration_module_api;
using gpm_vibration_module_api.Tools;

namespace gpm_module_api.UVSensor
{
    internal class UVSensorBase : ClsModuleBase
    {
        internal override byte[] GetAccData_HighSpeedWay(out long timespend, out bool IsTimeout)
        {
            SocketState state = new SocketState();
            isBusy = true;
            acc_data_read_task_token_source = new CancellationTokenSource();
            bulk_use = false;
            try
            {
                WaitForBufferRecieveDone = new ManualResetEvent(false);
                SocketBufferClear();
                var cmdbytes = Encoding.ASCII.GetBytes("READUVVAL\r\n");
                module_socket.Send(cmdbytes, 0, cmdbytes.Length, SocketFlags.None);
                var Datalength = 4;
                SocketState.Packet_Receive_Size = Datalength;
                state = new SocketState()
                {
                    window_size_ = Datalength,
                    buffer_ = new byte[Datalength],
                    work_socket_ = module_socket,
                    task_of_now = TIMEOUT_CHEK_ITEM.Read_Acc_Data,
                    data_rev_ = new byte[Datalength],
                    is_data_recieve_done_flag_ = false,
                    time_spend_ = -1,
                };
                Logger.Event_Log.Log($"receiveCallBack_begining.");
                module_socket.BeginReceive(state.buffer_, 0, SocketState.Packet_Receive_Size, 0, new AsyncCallback(receiveCallBack), state);
                var task = Task.Run(() => TimeoutCheck(state));
                WaitForBufferRecieveDone.WaitOne();
                timespend = task.Result; //1 tick = 100 nanosecond  = 0.0001 毫秒
                IsTimeout = state.is_data_recieve_timeout_;
                Logger.Event_Log.Log($"Timeout Detector ..Task{ state.task_of_now}..[In Time] , Spend:{timespend} ms");
                return state.data_rev_;

            }
            catch (Exception exp)
            {
                isBusy = false;
                state.is_data_recieve_done_flag_ = true;
                timespend = -1;
                WaitForBufferRecieveDone.Set();
                IsTimeout = false;
                return new byte[0];
            }
        }
    }
}
