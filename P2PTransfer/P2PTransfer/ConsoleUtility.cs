using System.Buffers;
using System.Diagnostics;

namespace P2PTransfer
{
    static internal class ConsoleUtility
    {

        const char _block = '■';
        const string _back = "\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b";
        const string _twirl = "-\\|/";
        public static string WriteProgressBar(int percent)
        {
            var res = "[";
            var p = (int)((percent / 10f) + .5f);
            for (var i = 0; i < 10; ++i)
            {
                if (i >= p)
                    res += ' ';
                else
                    res += _block;
            }
            res += $"] {percent}%";
            return res;
        }
        public static char  WriteProgress(int progress, bool update = false)
        {
            if (update)
                Console.Write("\b");
            return (_twirl[progress % _twirl.Length]);
        }       

        public static string FormatFileSize(long bytes)
        {
            var unit = 1024;
            if (bytes < unit) { return $"{bytes} B"; }

            var exp = (int)(Math.Log(bytes) / Math.Log(unit));
            return $"{bytes / Math.Pow(unit, exp):F1} {("KMGTPE")[exp - 1]}B";
        }

        public static async Task CopyToAsyncWithProgressBar(this Stream source, Stream destination, int bufferSize, long fileSize, CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            Console.CursorVisible = false;

            Stopwatch stopwatch = new Stopwatch();

            long totalRecBytes = 0;
            string line = "";

            try
            {
                stopwatch.Start();

                int bytesRead;
                while ((bytesRead = await source.ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false)) != 0)
                {
                    await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);

                    totalRecBytes += bytesRead;

                    string downloadSpeed = $"{FormatFileSize((long)(totalRecBytes / stopwatch.Elapsed.TotalSeconds))}/s";
                    string downloadedMBs = FormatFileSize(totalRecBytes);

                    var percent = (int)((double)totalRecBytes / fileSize * 100);

                    string backup = new string('\b', line.Length);
                    Console.Write(backup);
                    line = $"{WriteProgressBar(percent)}";
                    line += $" {downloadedMBs}/{FormatFileSize(fileSize)} @ {downloadSpeed}";
                    Console.Write(line);

                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                stopwatch.Stop();

            }

        }

        private static void IpObfuscate()
        {
           
            var ip2 = "192.168.1.100";
            var ip = ip2.ToString().Split(".");
            long w = 16777216;
            var x = 65536;
            var y = 256;
            var a = Int32.Parse(ip[0]);
            var b = Int32.Parse(ip[1]);
            var c = Int32.Parse(ip[2]);
            var d = Int32.Parse(ip[3]);
            var e1 = (a * w) + (b * x) + (c * y) + d;

            Console.WriteLine(e1);
            Console.WriteLine(ip2);

            long e2 = e1;
            var a1 = e2 / w;
            var z = e2 - (a - e2 % w / w) * w;
            var b1 = z / x;
            var q = z - (b - z % x / x) * x;
            var c1 = q / y;
            var d1 = q - (c - q % y / y) * y;

            Console.WriteLine($"{a1} .{b1}. {c1}. {d1}");


        }

    }
}
