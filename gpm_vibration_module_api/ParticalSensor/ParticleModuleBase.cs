using gpm_vibration_module_api.Tools;
using gpm_vibration_module_api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace gpm_module_api.ParticalSensor
{
    internal class ParticleModuleBase : ClsModuleBase
    {
        public  SocketState state = new SocketState();


        internal new  long TimeoutCheck()
        {
            Logger.Event_Log.Log("Timeout Check Active !");
            timeout_task_cancel_source = new CancellationTokenSource();

            Stopwatch timer = new Stopwatch();
            timer.Start();
            var timeoutPeriod = state.task_of_now == TIMEOUT_CHEK_ITEM.Read_Acc_Data ? acc_data_rev_timeout : fw_parm_rw_timeout;
            while (state.is_data_recieve_done_flag_ == false)
            {
               // Console.WriteLine(state.is_data_recieve_done_flag_);
                Thread.Sleep(1);
                try
                {

                    if (timeout_task_cancel_source.Token.IsCancellationRequested)
                    {
                        timeout_task_cancel_source.Token.ThrowIfCancellationRequested();
                    }

                    if (state.task_of_now == TIMEOUT_CHEK_ITEM.Read_Acc_Data && acc_data_read_task_token_source.IsCancellationRequested)
                    {
                        acc_data_read_task_token_source.Token.ThrowIfCancellationRequested();
                    }
                    if (state.task_of_now == TIMEOUT_CHEK_ITEM.FW_Param_RW && param_setting_task_cancel_token_Source.IsCancellationRequested)
                    {
                        param_setting_task_cancel_token_Source.Token.ThrowIfCancellationRequested();
                    }
                }
                catch (OperationCanceledException ex)
                {
                    state.is_data_recieve_done_flag_ = true;
                    state.is_data_recieve_timeout_ = false;
                    Logger.Event_Log.Log("[TimeoutCheck]使用者中斷");
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Code_Error_Log.Log($"[TimeoutCheck]系統例外.,.{ex.Message + "\r\n" + ex.StackTrace}");
                    break;
                }


                if (timer.ElapsedMilliseconds >= timeoutPeriod) //TODO 外部可調
                {
                    timer.Stop();
                    state.time_spend_ = timer.ElapsedMilliseconds;
                    Logger.Event_Log.Log($"Timeout Detector ..Task{ state.task_of_now}..[Timeout] , Spend:{timer.ElapsedMilliseconds} ms");
                    state.is_data_recieve_timeout_ = true;
                    if (state.task_of_now == TIMEOUT_CHEK_ITEM.Read_Acc_Data)
                    {
                        isBusy = false;
                        WaitForBufferRecieveDone.Set();
                        acc_data_read_task_token_source.Cancel();
                    }
                    if (state.task_of_now == TIMEOUT_CHEK_ITEM.FW_Param_RW)
                    {
                        param_setting_task_cancel_token_Source.Cancel();
                    }
                    return timer.ElapsedMilliseconds;
                }
            }
            WaitForBufferRecieveDone.Set();
            Logger.Event_Log.Log($"Timeout Detector ..Task{ state.task_of_now}..[InTime] , Spend:{timer.ElapsedMilliseconds} ms");
            timer.Stop();

            return timer.ElapsedMilliseconds;
        }

        internal override SocketState GetAccData_HighSpeedWay(out long timespend, out bool IsTimeout)
        {
            WaitForBufferRecieveDone = new ManualResetEvent(false);
           
            isBusy = true;
            acc_data_read_task_token_source = new CancellationTokenSource();
            bulk_use = false;
            try
            {
                SocketBufferClear();
                var cmdbytes = Encoding.ASCII.GetBytes("READALVAL" + "\r\n");
                var sendNum = module_socket.Send(cmdbytes, 0, cmdbytes.Length, SocketFlags.None);
                Logger.Event_Log.Log($"Send Num: {sendNum}");
                var Datalength = 62;
                SocketState.Packet_Receive_Size = Datalength;
                byte[] Datas = new byte[Datalength];
                state = new SocketState()
                {
                    window_size_ = Datalength,
                    buffer_ = new byte[Datalength*2],
                    work_socket_ = module_socket,
                    task_of_now = TIMEOUT_CHEK_ITEM.Read_Acc_Data,
                    data_rev_ = new byte[Datalength],
                    is_data_recieve_done_flag_ = false,
                    time_spend_ = -1,
                };
                Logger.Event_Log.Log($"receiveCallBack_begining.");
                var task = Task.Run(()=> TimeoutCheck());
                module_socket.BeginReceive(state.buffer_, 0, state.window_size_, 0, new AsyncCallback(receiveCallBack), state);
                WaitForBufferRecieveDone.Reset();
              
                Logger.Event_Log.Log($"recieve_process_Start WaitOne.");
                WaitForBufferRecieveDone.WaitOne();
                Logger.Event_Log.Log($"recieve_process_End WaitOne.");
                timespend = task.Result; //1 tick = 100 nanosecond  = 0.0001 毫秒
                IsTimeout = state.is_data_recieve_timeout_;
                //Logger.Event_Log.Log($"Timeout Detector ..Task{ state.task_of_now}..[In Time] , Spend:{timespend} ms");
                return state;
            }
            catch (Exception exp)
            {
                isBusy = false;
                state.is_data_recieve_done_flag_ = true;
                timespend = -1;
                WaitForBufferRecieveDone.Set();
                IsTimeout = false;
                return state;
            }
        }

        internal override void receiveCallBack(IAsyncResult ar)
        {
            Logger.Event_Log.Log($"Receive : receiveCallBack ");
            isBusy = true;
            state = (SocketState)ar.AsyncState;
            try
            {
                if (acc_data_read_task_token_source.IsCancellationRequested)
                    acc_data_read_task_token_source.Token.ThrowIfCancellationRequested();
                var client = state.work_socket_;
                int bytesRead = client.EndReceive(ar);
                Logger.Event_Log.Log($"Receive : {bytesRead}");

                if (bytesRead > 0)
                {
                    var rev = new byte[bytesRead];
                    Array.Copy(state.buffer_, 0, rev, 0, bytesRead);
                    state.temp_rev_data.AddRange(rev);
                    if (state.temp_rev_data.Count >= state.window_size_)
                    {
                        state.data_rev_ = new byte[state.window_size_];
                        Array.Copy(state.temp_rev_data.ToArray(), 0, state.data_rev_, 0, state.window_size_);
                        state.is_data_recieve_done_flag_ = true;
                        // WaitForBufferRecieveDone.Set();
                        Logger.Event_Log.Log($"Receive work finish. Set is_data_recieve_done_flag = {state.is_data_recieve_done_flag_}");
                    }
                    else
                    {
                        state.is_data_recieve_done_flag_ = false;
                        client.BeginReceive(state.buffer_, 0, SocketState.Packet_Receive_Size, 0,
                             new AsyncCallback(receiveCallBack), state);
                    }
                }
                else
                {
                    Logger.Event_Log.Log($"封包接收,大小:{0}");
                    client.BeginReceive(state.buffer_, 0, SocketState.Packet_Receive_Size, 0,
                            new AsyncCallback(receiveCallBack), state);
                }

            }
            catch (OperationCanceledException ex)
            {
                Logger.Event_Log.Log("[receiveCallBack] OperationCanceledException 使用者中斷");
                isBusy = false;
                state.is_data_recieve_done_flag_ = true;
                // WaitForBufferRecieveDone.Set();
            }
            catch (SocketException ex)
            {
                Logger.Event_Log.Log($"[receiveCallBack] SocketException {ex.Message + "\r\n" + ex.StackTrace}");
                isBusy = false;
                // WaitForBufferRecieveDone.Set();
            }
            catch (Exception ex)
            {
                Logger.Event_Log.Log($"[receiveCallBack] Exception {ex.Message + "\r\n" + ex.StackTrace}");
                isBusy = false;
                // WaitForBufferRecieveDone.Set();
            }
            //            AccDataBuffer.AddRange(state.buffer);
        }
    }
}
