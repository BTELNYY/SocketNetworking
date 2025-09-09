using System.Runtime.Versioning;
using SocketNetworking.Client;

namespace QuicClient;

[RequiresPreviewFeatures]
public class Program
{
    public static void Main(string[] args)
    {
        QuicNetworkClient client = new QuicNetworkClient();
        client.Connect("127.0.0.1", 7777);
    }
}