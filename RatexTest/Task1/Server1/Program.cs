using ClientServerUdpFileExchangeWithTcpConfirmation;
using System.Net.Sockets;
using System.Text;

if (args.Length < 3)
{
    Console.WriteLine("Not enough parameters:");
    Console.WriteLine(Server.Helper());
    Console.ReadLine();
    return;
}

Server s;

try
{
    s = new(args[0], Int32.Parse(args[1]), args[2]);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.GetType());
    Console.WriteLine(Server.Helper());
    return;
}

try
{
    s.TcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    s.TcpSocket.Bind(s.IpEndPoint);
    s.TcpSocket.Listen(10);
    Console.Write("Server started. ");

    while (true)
    {
        try
        {
            Console.WriteLine("Waiting for connections...");

            s.Handler = null;
            s.Handler = s.TcpSocket.Accept();        
            string tcpResponse = s.ReceiveTcpMessage(); // tcp receive 1 (filename and udpPort)

            if (tcpResponse == s.TCP_FAIL_STRING)
            {
                Console.WriteLine("Client side error. File not found or file size exceeded");
                continue;
            }

            string[] msg = tcpResponse.Split(" ");
            string fileName = tcpResponse.Split(" ")[0];
            int udpPort = Int32.Parse(tcpResponse.Split(" ")[1]);
            Console.WriteLine("successfully received from client filename and udp port: {0}", tcpResponse);
         
            s.Handler.Send(s.TCP_OK_BYTE); // tcp 1 send
            s.StartUdpReceiveFile(udpPort, fileName);

            tcpResponse = s.ReceiveTcpMessage2(); // tcp receive 3
            if (tcpResponse == s.TCP_OK_STRING)
            {
                Console.WriteLine("Client has successfully received all confirmations. File passed successfully!");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.GetType());
        }
        finally
        {
            if(s.Handler != null)
            {
                s.Handler.Shutdown(SocketShutdown.Both);
                s.Handler.Close();
            }       
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.GetType());
}
finally
{
    if (s.TcpSocket!= null)
    {
        s.TcpSocket.Shutdown(SocketShutdown.Both);
        s.TcpSocket.Close();
        s.TcpSocket = null;
    }
}


