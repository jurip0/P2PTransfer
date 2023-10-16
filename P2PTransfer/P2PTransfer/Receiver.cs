using Cryptography;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace P2PTransfer
{
    public class Receiver
    {
        public async Task Start()
        {

            var upnp = await NAT.DiscoverAsync();

            // check if router has UPNP enabled
            Console.WriteLine($"UPnP enabled: {upnp}");

            if (upnp)
            {

                // pick next available port
                var localPort = (new UdpClient(0, AddressFamily.InterNetwork).Client.LocalEndPoint as IPEndPoint).Port;
                var publicPort = (new UdpClient(0, AddressFamily.InterNetwork).Client.LocalEndPoint as IPEndPoint).Port;

                var localIp = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                var publicIp = await NAT.GetExternalIPAsync();// await new HttpClient().GetStringAsync(new Uri("https://api.ipify.org"));

                var password = Guid.NewGuid().ToString("n").Substring(0, 5);
                var aes = new Aes256(password);

                //Console.WriteLine($"LocalIp: {localIp?.ToString()}:{localPort}");
                //Console.WriteLine($"PublicIp: {publicIp?.ToString()}:{_publicPort}");

                //close Nat ports before closing console window
                AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
                {
                    NAT.DeleteForwardingRuleAsync(publicPort, ProtocolType.Tcp).Wait();
                };
                Console.CancelKeyPress += (sender, eventArgs) =>
                        {
                            NAT.DeleteForwardingRuleAsync(publicPort, ProtocolType.Tcp).Wait();
                            Environment.Exit(0);
                        };

                await NAT.ForwardPortAsync(localIp?.ToString(), localPort, publicPort, ProtocolType.Tcp, "P2P_UPNP_Transfer");

                Console.WriteLine($"Tells this to the peer: {publicIp}:{publicPort}");
                Console.WriteLine($"Password: {password} ");

                var ipEndPoint = IPEndPoint.Parse($"{localIp?.ToString()}:{localPort}");
                var fileInfo = "";

                try
                {
                    TcpListener listener = new(ipEndPoint);

                    try
                    {
                        listener.Start();

                        TcpClient handler = await listener.AcceptTcpClientAsync();
                        await using NetworkStream stream = handler.GetStream();


                        byte[] buffer = new byte[4096];

                        await stream.ReadAsync(buffer);

                        fileInfo = Encoding.UTF8.GetString(await aes.DecryptStringFromBytes_Aes(buffer));
                        await stream.FlushAsync();

                        var folderPath = Directory.GetCurrentDirectory();

                        var fileName = fileInfo.Split(':')[0];
                        var fileSize = long.Parse(fileInfo.Split(':')[1]);

                        var filePath = @$"{folderPath}\{fileName.Split('.')[0]}.{fileName.Split('.')[1]}";

                        Console.WriteLine($"Receiving file: {fileName} ({ConsoleUtility.FormatFileSize(fileSize)})");


                        //confirm
                        await stream.WriteAsync(Encoding.UTF8.GetBytes(" "));
                        await stream.FlushAsync();



                        using (FileStream fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write))
                        {
                            await aes.DecryptStreamWithProgressBarAsync(stream, fileStream, 81920, fileSize, CancellationToken.None);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex.Message}");
                    }
                    finally
                    {

                        listener.Stop();
                        await NAT.DeleteForwardingRuleAsync(publicPort, ProtocolType.Tcp);

                    }

                }
                catch (Exception ex)
                {

                    Console.WriteLine($"{ex.Message}");
                    Console.WriteLine($"Connection lost");
                }
            }
        }

    }
}
