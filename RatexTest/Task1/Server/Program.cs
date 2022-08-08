using System.Net;
using System.Net.Sockets;
using System.Text;

//Server.exe 127.0.0.1 5555 temp

if (args.Length < 3)
{
    Console.WriteLine("Not enough parameters:");
    Console.WriteLine(Helper.ServerHelperString());
    Console.ReadLine();
}
else
{
    string address;             // адрес сервера (hostName или сам адресс)
    int port;                   // порт сервера
    string dirName;             // каталог для хранения файлов

    int udpPort;                // переданный клиентом порт для приема файла по udp
    string fileName;            // переданное клиентом имя файла
    IPHostEntry ipHost;         // информация о хосте
    IPAddress ipAddr;           // ip адресс хоста
    IPEndPoint ipEndPoint;      // локальной точкой (ip + port), по которой будем принимать данные
    Socket? tcpSocket = null;   // tcp cокет для прослушивания
    Socket? udpSocket = null;   // udp сокет для передачт фала
    int packetSize = 8192;      // определяем размер получаемых сегментов файла  
    StringBuilder builder;  
    
    try
    {
        address = args[0];             
        port = Int32.Parse(args[1]);    
        dirName = args[2];              
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        Console.WriteLine(ex.GetType());
        Console.WriteLine(Helper.ServerHelperString());
        return;
    }

    try
    {
        ipHost = Dns.GetHostEntry(address);
        ipAddr = ipHost.AddressList[0];
        ipEndPoint = new IPEndPoint(ipAddr, port);
        tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        tcpSocket.Bind(ipEndPoint);
        tcpSocket.Listen(10);
        Console.Write("Сервер запущен. ");

        while (true)
        {
            Console.WriteLine("Ожидание подключений...");
            Socket? handler = null;

            try
            {
                handler = tcpSocket.Accept();
                builder = receiveTcpMessage(handler, packetSize); // tcp receive 1 (имя файла и порт для udp)

                if(builder.ToString() == "FAIL")
                {
                    Console.WriteLine("ошибка на стороне клиента. не найден файл или превышен размер файла");
                    continue;
                }
                
                Console.WriteLine("получено от client имя файла и udp порт: {0}", DateTime.Now.ToShortTimeString() + ": " + builder.ToString());
                string[] result = builder.ToString().Split(" ").ToArray();
                fileName = result[0];
                udpPort = Int32.Parse(result[1]);
              
                byte[] msg = Encoding.UTF8.GetBytes("OK");
                handler.Send(msg); // tcp 1 send
                                    
                udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint localIP = new IPEndPoint(ipAddr, udpPort);
                udpSocket.Bind(localIP);

                byte[] packetReceive = new byte[packetSize]; 
                int bytes;
                EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
                bytes = udpSocket.ReceiveFrom(packetReceive, ref remoteIp); // udp receive 1  (количество сегментов)
                int parts = BitConverter.ToInt32(packetReceive, 0);               
                handler.Send(msg); // tcp 2 send
                Directory.CreateDirectory(dirName);
                string path = dirName + @"\" + fileName;

                using (FileStream fStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {                   
                    for (int i = 0; i < parts; i++)
                    {                       
                        bytes = udpSocket.ReceiveFrom(packetReceive, ref remoteIp); // udp receive parts
                        fStream.Write(packetReceive, 0, bytes);
                        handler.Send(msg); // tcp 3 send                     
                    }
                }
                
                builder = receiveTcpMessage(handler, packetSize); // tcp receive 2
                if(builder.ToString() == "OK")
                {
                    Console.WriteLine("клиент успешно получил все подтверждения. файл передан успешно!");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.GetType());             
            }
            finally
            {
                if (udpSocket != null)
                {
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }         
                if (udpSocket != null)
                {
                    udpSocket.Shutdown(SocketShutdown.Both);
                    udpSocket.Close();
                    udpSocket = null;
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
        if (tcpSocket != null)
        {
            tcpSocket.Shutdown(SocketShutdown.Both);
            tcpSocket.Close();
            tcpSocket = null;
        }
    }
}

StringBuilder receiveTcpMessage(Socket handler, int packetSize)
{
    StringBuilder builder = new StringBuilder();
    int bytes = 0; 
    byte[] data = new byte[256];
    do
    {
        bytes = handler.Receive(data);
        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
    }
    while (handler.Available > 0);

    return builder;
}