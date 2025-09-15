using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Write("Server IP (or localhost): ");
        string server = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(server)) server = "localhost";

        Console.Write("Path to file to send: ");
        string filePath = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Console.WriteLine("[Client] File not found. Exiting.");
            return;
        }

        const int port = 8080;

        try
        {
            using (var client = new TcpClient())
            {
                Console.WriteLine($"[Client] Connecting to {server}:{port} ...");
                await client.ConnectAsync(server, port);
                using (NetworkStream ns = client.GetStream())
                using (var bw = new BinaryWriter(ns, System.Text.Encoding.UTF8, leaveOpen: true))
                {
                    string fileName = Path.GetFileName(filePath);
                    long fileLength = new FileInfo(filePath).Length;

                    bw.Write(fileName);
                    bw.Write(fileLength);
                    bw.Flush(); 

                    Console.WriteLine($"[Client] Sending {fileName} ({fileLength} bytes) ...");

                    // send file bytes in chunks
                    using (var fs = File.OpenRead(filePath))
                    {
                        byte[] buffer = new byte[8192];
                        int read;
                        while ((read = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await ns.WriteAsync(buffer, 0, read);
                        }
                    }

                    Console.WriteLine("[Client] File sent.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Client] Error: {ex.Message}");
        }
    }
}
