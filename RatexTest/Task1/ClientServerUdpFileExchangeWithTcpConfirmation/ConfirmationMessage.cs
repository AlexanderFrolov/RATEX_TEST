using System.Text;

namespace ClientServerUdpFileExchangeWithTcpConfirmation
{
    public abstract class ConfirmationMessage
    {
        public byte[] TCP_OK_BYTE { get; private set; }
        public byte[] TCP_FAIL_BYTE { get; private set; }
        public string TCP_OK_STRING { get; private set; }
        public string TCP_FAIL_STRING { get; private set; }

        public ConfirmationMessage()
        {
            TCP_OK_STRING = "<TCP_OK>";
            TCP_FAIL_STRING = "<TCP_FAIL>";
            TCP_OK_BYTE = Encoding.UTF8.GetBytes("<TCP_OK>");
            TCP_FAIL_BYTE = Encoding.UTF8.GetBytes("<TCP_FAIL>");
        }
    }
}
