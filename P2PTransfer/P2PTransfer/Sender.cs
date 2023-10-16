using Cryptography;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace P2PTransfer
{
    internal class Sender
    {
        public async Task Start()
        {

            #region Sender init

            var filePath = string.Empty;
            var connEndPoint = string.Empty;
            var password = string.Empty;

            while (string.IsNullOrEmpty(connEndPoint))
            {
                Console.Write($"Connect to: ");
                connEndPoint = Console.ReadLine();

            }

            while (string.IsNullOrEmpty(password))
            {
                Console.Write($"Password: ");
                password = Console.ReadLine();

            }


            #endregion


            var endPoint = IPEndPoint.Parse(connEndPoint.Trim());
            var aes = new Aes256(password);

            using TcpClient client = new();

            Console.WriteLine($"Connecting...");

            for (var retries = 1; retries <= 5; retries++)
            {
                try
                {
                    await client.ConnectAsync(endPoint);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Connection failed, retrying...{retries}");
                    continue;
                }
                if (client.Connected) break;
            }

            if (client.Connected) Console.WriteLine($"Connected");

            while (string.IsNullOrEmpty(filePath))
            {
                Console.Write($"File path: ");
                filePath = Console.ReadLine()?.Trim('"');

                if (!File.Exists(filePath)) filePath = string.Empty;

            }


            if (client.Connected)
            {

                var file = new FileInfo(filePath);

                Console.WriteLine();
                Console.WriteLine($"Sending file: {file.Name} ({ConsoleUtility.FormatFileSize(file.Length)})");

                await using NetworkStream stream = client.GetStream();

                try
                {
                    //send fileName and fileSize
                    await stream.WriteAsync(await aes.EncryptStringToBytes_Aes(Encoding.UTF8.GetBytes($"{file.Name}:{file.Length}")));
                    await stream.FlushAsync();

                    //wait for response
                    byte[] buffer = new byte[1];
                    await stream.ReadAsync(buffer);
                    await stream.FlushAsync();


                    using (FileStream source = File.Open(filePath, FileMode.Open, FileAccess.Read))
                    {
                        await aes.EncryptStreamWithProgressBarAsync(source, stream, 81920, file.Length, CancellationToken.None);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    Console.WriteLine(string.Empty);
                    Console.WriteLine("Done");
                    client.Close();
                }
            }
            else
            {
                Console.WriteLine($"Connection failed");

                client.Close();
            }

        }


    }
}
