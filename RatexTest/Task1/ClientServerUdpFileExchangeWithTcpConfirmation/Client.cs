using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClientServerUdpFileExchangeWithTcpConfirmation
{
    public class Client : ConfirmationMessage
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public int UdpPort { get; set; }
        public string FilePath { get; set; }
        public int Timeout { get; set; }

        public int PacketSize { get; set; }
        public IPAddress IpAddress { get; set; }
        public IPEndPoint IpEndPoint { get; set; }
        public Socket? TcpSender { get; set; }
        public Socket? UdpSocket { get; set; }

        public Client(string address, int port, int udpPort, string filePath, int timeout) : base()
        {
            Address = address;
            Port = port;
            UdpPort = udpPort;
            FilePath = filePath;
            Timeout = timeout;
            PacketSize = 8192;

            IPHostEntry IpHostEntry = Dns.GetHostEntry(Address);
            IpAddress = IpHostEntry.AddressList[0];
            IpEndPoint = new IPEndPoint(IpAddress, Port);
        }

        public static string Helper() =>

            "Client <ip> <port> <udpPort> <filePath> <timeout> \n" +
            "\t ip - ip address of the server (hostName or address) \n " +
            "\t port - port to connect to the server \n" +
            "\t udpPort - port for sending UDP packets \n" +
            "\t filePath - file path \n" +
            "\t timeout - UDP packet confirmation timeout in milliseconds";

        public bool IsCorrectFilePath(string path)
        {
            FileInfo fileInfo = new FileInfo(path);

            if (!fileInfo.Exists || fileInfo.Length > 10485760) // 10mb
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public string ReceiveTcpMessage()
        {
            StringBuilder builder = new StringBuilder();
            int bytes = 0;
            byte[] data = new byte[256];
          
            do
            {
                bytes = TcpSender.Receive(data);
                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
            }
            while (TcpSender.Available > 0);
        
            return builder.ToString();
        }

        public bool StartUdpSendFile(byte[] data)
        {
            bool fail = true;
            try
            {
                UdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint sendEndPoint = new IPEndPoint(IpAddress, UdpPort);
                UdpSocket.Connect(sendEndPoint);
                
                TcpSender.ReceiveTimeout = Timeout;
                int parts = data.Length / PacketSize;

                if (data.Length % PacketSize != 0) parts++;

                byte[] packetSend = BitConverter.GetBytes(parts);
                UdpSocket.Send(packetSend); // udp send 1  (num parts) 
                string? response = ReceiveTcpMessage(); // tcp 2 receive

                if (response != TCP_OK_STRING)
                {
                    Console.WriteLine("Error sending the number of file parts to server.");
                    return fail;
                }

                response = null;
                var chunks = data.Chunk<byte>(PacketSize);

                foreach (var chunk in chunks)
                {
                    UdpSocket.Send(chunk); 
                    try
                    {
                        response = ReceiveTcpMessage();
                    }
                    catch(SocketException ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("repeat message ...");
                        response = ReceiveTcpMessage();
                    }
                                            
                    if (response != TCP_OK_STRING)
                    {
                        Console.WriteLine("Error sending the number of file parts to server.");
                        return fail;
                    }
                    response = null;
                }

                fail = false;

            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.GetType());
            }
            finally
            {
                if (UdpSocket != null) UdpSocket.Close();                
            }

            return fail;
        }
    }
}


