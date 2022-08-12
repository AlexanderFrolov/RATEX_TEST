public static class Helper
{
    public static string ClientHelperString() =>

        "Client <ip> <port> <udpPort> <filePath> <timeout> \n" +
        "\t ip - ip address of the server (hostName or address) \n " +
        "\t port - port to connect to the server \n" +
        "\t udpPort - port for sending UDP packets \n" +
        "\t filePath - file path \n" +
        "\t timeout - UDP packet confirmation timeout in milliseconds";
}
