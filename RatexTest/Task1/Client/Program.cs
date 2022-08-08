using System.Net;
using System.Net.Sockets;
using System.Text;

// 127.0.0.1 5555 6000 test1.txt 500

if (args.Length < 5)
{
    Console.WriteLine("Not enough parameters:");
    Console.WriteLine(Helper.ClientHelperString());
    Console.ReadLine();
}
else
{
    string address;                  // адрес сервера (hostName или сам адресс)
    int port;                        // порт сервера
    int udpPort;                     // порт для отправки UDP пакетов
    string filePath;                 // путь к файлу
    int timeout;                     // таймаут на подтверждение UDP пакетов в милисекундах.

    IPHostEntry ipHost;              // информация о хосте
    IPAddress ipAddr;                // ip адресс хоста
    IPEndPoint ipEndPoint;           // локальная точка (ip + port), по которой будет общение
    Socket? tcpSender = null;        // tcp cокет для сообщений
    UdpClient? udpSender = null;     // udp сокет для передачт фала
    int packetSize = 8192;           // определяем размер сегмента при передаче файла
    IPEndPoint? sendEndPoint = null; // локальная точка (ip + port) для передачи файла 
    byte[] packetSend;
       
    try
    {
        address = args[0];
        port = Int32.Parse(args[1]);
        udpPort = Int32.Parse(args[2]);
        filePath = args[3];
        timeout = Int32.Parse(args[4]);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        Console.WriteLine(ex.GetType());
        Console.WriteLine(Helper.ClientHelperString());
        return;
    }

    try
    {
        ipHost = Dns.GetHostEntry(address);
        ipAddr = ipHost.AddressList[0];
        ipEndPoint = new IPEndPoint(ipAddr, port);
        tcpSender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        tcpSender.Connect(ipEndPoint);
        Console.WriteLine("socket connected to {0} ", tcpSender.RemoteEndPoint.ToString());

        FileInfo fileInfo = new FileInfo(filePath);

        if (!fileInfo.Exists || fileInfo.Length > 10485760)
        {
            Console.WriteLine("файл по пути: '{0}' не найден или превышен дупустимый размер файла в 10мб", filePath);           
            tcpSender.Send(Encoding.Unicode.GetBytes("FAIL"));
            return;
        }

        packetSend = Encoding.Unicode.GetBytes($"{fileInfo.Name} {udpPort}");
        tcpSender.Send(packetSend);  // send tcp  1 (имя и порт). 
        string response = receiveTcpMessage(tcpSender); // receive tcp 1
        Console.WriteLine("Ответ от сервера: {0}. Сервер получил имя файла и порт", response);

        if(response == "OK")
        {           
            using (FileStream fSource = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sendEndPoint = new IPEndPoint(ipAddr, udpPort);               
                udpSocket.Connect(sendEndPoint);
                      
                int numBytesToRead = (int)fSource.Length;
                int numBytesReaded = 0;
                tcpSender.ReceiveTimeout = timeout;
                int parts = (int)fSource.Length / packetSize;
                if ((int)fSource.Length % packetSize != 0) parts++;
                packetSend = BitConverter.GetBytes(parts);
                udpSocket.Send(packetSend); // udp send 1  (num parts) 
                response = receiveTcpMessage(tcpSender); // tcp 2 receive
                
                if (response == "OK")
                {                
                    int n = 0;
                    packetSend = new byte[packetSize];
                    
                    if(parts > 1)
                    {
                        for (int i = 0; i < parts - 1; i++)
                        {                           
                            n = fSource.Read(packetSend, 0, packetSize);
                            if (n == 0) break;                           
                            numBytesReaded += n;
                            numBytesToRead -= n;
                            udpSocket.Send(packetSend); // send udp n-1 parts 
                            response = receiveTcpMessage(tcpSender); // receiv tcp
                        }
                        packetSend = new byte[numBytesToRead];
                        n = fSource.Read(packetSend, 0, numBytesToRead);
                        udpSocket.Send(packetSend);     // send udp only or last part
                        response = receiveTcpMessage(tcpSender); // receiv tcp

                    }
                    else
                    {             
                        // single part
                        packetSend = new byte[numBytesToRead];
                        n = fSource.Read(packetSend, 0, numBytesToRead);
                        udpSocket.Send(packetSend); // send udp 
                        response = receiveTcpMessage(tcpSender);                  
                    }
                                     
                }
                else
                {
                    Console.WriteLine("fail. не получен корректный ответ от сервера о получении числа сегментов");
                }             
            }

            Console.WriteLine("файл отправлен.");

            packetSend = Encoding.Unicode.GetBytes("OK");
            tcpSender.Send(packetSend);  // tcp 3 

        }
        else
        {           
            Console.WriteLine("неудача. не получен корректный ответ от сервера об получении имени файла и порта");
        }

    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        Console.WriteLine(ex.GetType()); 
    }
    finally
    {
        if (tcpSender != null)
        {
            tcpSender.Shutdown(SocketShutdown.Both);
            tcpSender.Close();
        }
        if (udpSender != null) udpSender.Close();
    }
}

string receiveTcpMessage(Socket tcpSender)
{
    byte[] bytes = new byte[256];
    int bytesRec = tcpSender.Receive(bytes);
    return  Encoding.UTF8.GetString(bytes, 0, bytesRec);
}