using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace gpm_module_api.Module
{
    public class TCPIPModule
    {
        public class SocketTCPIP
        {
            public event Action<byte[]> recieveEvent;
            public SocketTCPIP(string IP, int Port = 5000)
            {
                this.IP = IP;
                this.Port = Port;
                //Connect();
            }
            public class State
            {
                public static int bufferSize = 1024;
                public byte[] buffer = new byte[bufferSize];
                internal Socket socket;
                public string message_ASCII = "";
            }

            public Socket _sck;
            private string IP;
            private int Port;
            public Tuple<bool, string> Connect()
            {
                try
                {
                    _sck = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
                    _sck.Connect(new IPEndPoint(IPAddress.Parse(IP), Port));
                    State state = new State() { socket = _sck };
                    _sck.BeginReceive(state.buffer, 0, State.bufferSize, SocketFlags.None, new AsyncCallback(RecieveCallBack), state);
                    return new Tuple<bool, string>(true, "");
                }
                catch (Exception ex)
                {
                    return new Tuple<bool, string>(false, ex.Message);
                }

            }

            public void Send(byte[] dat)
            {
                State state = new State() { buffer = dat, socket = _sck };
                _sck.BeginSend(state.buffer, 0, dat.Length, SocketFlags.None, new AsyncCallback(SendCallBack), state);
            }

            private void SendCallBack(IAsyncResult ar)
            {
            }

            private void RecieveCallBack(IAsyncResult ar)
            {
                try
                {
                    State _state = (State)ar.AsyncState;
                    Socket _sck = _state.socket;
                    var ret = _sck.EndReceive(ar);
                    if (ret != 0)
                    {
                        byte[] rev = new byte[ret];
                        Array.Copy(_state.buffer,0,rev,0,ret);
                        recieveEvent?.Invoke(rev);
                        _state = new State { socket = _sck };
                        _sck.BeginReceive(_state.buffer, 0, State.bufferSize, SocketFlags.None, new AsyncCallback(RecieveCallBack), _state);

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\r\nSource:" + ex.StackTrace);
                    return;
                }
            }

            private void ClientCallBack(IAsyncResult ar)
            {
                throw new NotImplementedException();
            }

            private void MsgRecieve()
            {
                throw new NotImplementedException();
            }

            internal void Close()
            {
                try
                {
                    _sck.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                }
                try
                {
                    _sck.Close();
                }
                catch (Exception ex)
                {
                }
            }
        }
    }
}
