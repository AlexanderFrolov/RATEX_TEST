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

            try
            {
                do
                {
                    bytes = TcpSender.Receive(data);
                    builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
                }
                while (TcpSender.Available > 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.GetType());
            }
            return builder.ToString();
        }


        public byte[] receiveTcpByteMessage()
        {
            byte[] bytes = new byte[256];
            int numbytes = TcpSender.Receive(bytes);
            return bytes;
        }

        public bool StartUdpSendFile()
        {
            try
            {
                using (FileStream fSource = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
                {
                    UdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    IPEndPoint sendEndPoint = new IPEndPoint(IpAddress, UdpPort);
                    UdpSocket.Connect(sendEndPoint);

                    int numBytesToRead = (int)fSource.Length;
                    int numBytesReaded = 0;
                    TcpSender.ReceiveTimeout = Timeout;
                    int parts = (int)fSource.Length / PacketSize;

                    if ((int)fSource.Length % PacketSize != 0) parts++;

                    byte[] packetSend = BitConverter.GetBytes(parts);

                    UdpSocket.Send(packetSend); // udp send 1  (num parts) 
                    string response = ReceiveTcpMessage(); // tcp 2 receive

                    if (response != TCP_OK_STRING)
                    {
                        Console.WriteLine("Error sending the number of file parts to server.");
                        return false;
                    }

                    int n = 0;
                    packetSend = new byte[PacketSize];

                    if (parts > 1)
                    {
                        for (int i = 0; i < parts - 1; i++)
                        {
                            n = fSource.Read(packetSend, 0, PacketSize);
                            if (n == 0) break;
                            numBytesReaded += n;
                            numBytesToRead -= n;
                            UdpSocket.Send(packetSend); // send udp n-1 parts 
                            response = ReceiveTcpMessage(); // receive tcp n-1

                            if (response != TCP_OK_STRING)
                            {
                                Console.WriteLine("Error while sending part of a file to the server");
                                return false;
                            }

                        }
                        packetSend = new byte[numBytesToRead];
                        n = fSource.Read(packetSend, 0, numBytesToRead);
                        UdpSocket.Send(packetSend);     // send udp only or last part
                        response = ReceiveTcpMessage(); // receiv tcp

                        if (response != TCP_OK_STRING)
                        {
                            Console.WriteLine("Error while sending part of a file to the server");
                            return false;
                        }

                    }
                    else
                    {
                        // single part
                        packetSend = new byte[numBytesToRead];
                        n = fSource.Read(packetSend, 0, numBytesToRead);
                        UdpSocket.Send(packetSend); // send udp 
                        response = ReceiveTcpMessage();

                        if (response != TCP_OK_STRING)
                        {
                            Console.WriteLine("Error while sending part of a file to the server");
                            return false;
                        }
                    }
                }
                
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.GetType());
            }
            finally
            {
                if (UdpSocket != null) UdpSocket.Close();
            }

            return true;
        }
    }
}


