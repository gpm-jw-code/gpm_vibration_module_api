using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.NET
{
    public class UDPServer
    {
        Socket Server;
        IPEndPoint remoteIP;
        internal UDPServer()
        {
            remoteIP = new IPEndPoint(IPAddress.Broadcast, 1688); //定義廣播區域跟Port
            Server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Server.EnableBroadcast = true;
            Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            byte[] pushdata = new byte[1024]; //定義要送出的封包大小
        }
        internal async Task Send(byte[] pushdata)
        {
            Server.SendTo(pushdata,0,pushdata.Length, SocketFlags.None ,remoteIP); //送出的資料跟目的
        }
    }
}
