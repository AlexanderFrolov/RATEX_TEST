using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClientServerUdpFileExchangeWithTcpConfirmation
{
    public class Server : ConfirmationMessage
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public string DirectoryName { get; set; }
        public IPAddress IpAddress { get; set; }
        public IPEndPoint IpEndPoint { get; set; }
        public Socket? TcpSocket { get; set; }
        public Socket? Handler { get; set; }
        public int PacketSize { get; set; }
        public Socket? UdpSocket { get; set; }

        public Server(string address, int port, string dirName) : base()
        {
            Address = address;
            Port = port;
            DirectoryName = dirName;
            PacketSize = 8192;
            IPHostEntry IpHostEntry = Dns.GetHostEntry(Address);
            IpAddress = IpHostEntry.AddressList[0];
            IpEndPoint = new IPEndPoint(IpAddress, Port);    
        }  
        
        public static string Helper() => "Server <ip> <port> <dirName> \n" +
                         "\t ip - ip address of the server (hostName or address) \n " +
                         "\t port - port to connect to the server \n" +
                         "\t dirName - directory for storing files";

        public string ReceiveTcpMessage()
        {
            StringBuilder builder = new StringBuilder();
            int bytes = 0;
            byte[] data = new byte[256];

            try
            {
                do
                {
                    bytes = Handler.Receive(data);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                }
                while (Handler.Available > 0);     
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.GetType());
            }
            return builder.ToString();
        }

        public byte[] StartUdpReceiveFile(int udpPort)
        {
            List<byte> data = new List<byte>();
            try
            {
                UdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint localIP = new(IpAddress, udpPort);
                UdpSocket.Bind(localIP);
               
                byte[] packetReceive = new byte[PacketSize];
                int bytes;
                EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
                bytes = UdpSocket.ReceiveFrom(packetReceive, ref remoteIp); // udp receive 1  (count file parts)
                int parts = BitConverter.ToInt32(packetReceive, 0);
                Handler.Send(TCP_OK_BYTE); // tcp 2 send
            
                for (int i = 0; i < parts; i++)
                {
                    bytes = UdpSocket.ReceiveFrom(packetReceive, ref remoteIp); // udp receive parts

                    if (i == parts - 1 || parts == 1)
                    {
                        byte[] newBuffer = new byte[bytes];
                        Buffer.BlockCopy(packetReceive, 0, newBuffer, 0, bytes);
                        data.AddRange(newBuffer);
                        Handler.Send(TCP_OK_BYTE); // tcp 3 send  
                        continue;
                    }
                  
                    data.AddRange(packetReceive);
                    Handler.Send(TCP_OK_BYTE); // tcp 3 send  
                }              
            }   
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.GetType());
            }
            finally
            {
                if (UdpSocket != null)
                {                  
                    UdpSocket.Shutdown(SocketShutdown.Both);
                    UdpSocket.Close();
                    UdpSocket = null;
                }
            }

            return data.ToArray();
        }
    }       
}