using System.Net.Sockets;
using System.Text;

namespace InventorySystem
{
    public class Robot
    {
        public const int urscriptPort = 30002, dashboardPort = 29999;
        public string IpAddress = "localhost";

        public void SendString(int port, string message)
        {
            using var client = new TcpClient(IpAddress, port);
            using var stream = client.GetStream();
            stream.Write(Encoding.ASCII.GetBytes(message));
        }

        public void SendUrscript(string urscript)
        {
            SendString(dashboardPort, "brake release\n");  // wakes the robot

            // ✅ Make sure URScript ends with newline
            if (!urscript.EndsWith("\n")) urscript += "\n";

            // ✅ Send to port 30002 (this actually runs your program)
            SendString(urscriptPort, urscript);
        }
    }
}