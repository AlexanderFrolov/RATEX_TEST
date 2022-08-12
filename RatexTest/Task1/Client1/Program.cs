using ClientServerUdpFileExchangeWithTcpConfirmation;
using System.Net.Sockets;
using System.Text;

if (args.Length < 5)
{
    Console.WriteLine("Not enough parameters:");
    Console.WriteLine(Client.Helper());
    Console.ReadLine();
    return;
}

Client c;

try
{
    c = new(args[0], Int32.Parse(args[1]), Int32.Parse(args[2]),  args[3], Int32.Parse(args[4]));
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.GetType());
    Console.WriteLine(Client.Helper());
    Console.ReadLine();
    return;
}

try
{
    c.TcpSender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    c.TcpSender.Connect(c.IpEndPoint);
    Console.WriteLine("socket connected to {0} ", c.TcpSender.RemoteEndPoint.ToString());

    if (!c.IsCorrectFilePath(c.FilePath))
    {
        Console.WriteLine("File path: '{0}' not found or exceeded the allowed file size of 10mb", c.FilePath);
        c.TcpSender.Send(c.TCP_FAIL_BYTE); // 1f send
        return;
    }

    byte[] packetSend = Encoding.Unicode.GetBytes($"{c.FilePath.Split('\\').Last()} {c.UdpPort}");
    c.TcpSender.Send(packetSend);  // send tcp  1 (имя и порт). 
    string response = c.ReceiveTcpMessage(); // receive tcp 1

    if (response != c.TCP_OK_STRING)
    {
        Console.WriteLine("An error occurred while transferring the file and port to the server.");
        return;
    }
  
    Console.WriteLine("Server received file name and udp port");

    bool result = c.StartUdpSendFile();
    if (!result) return;

    Console.WriteLine("File has been successfully sent to the server!");
    c.TcpSender.Send(c.TCP_OK_BYTE);  // tcp send OK 3 

}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.GetType());
}
finally
{
    if (c.TcpSender != null)
    {
       // c.TcpSender.Shutdown(SocketShutdown.Both);
        c.TcpSender.Close();
    }  
}