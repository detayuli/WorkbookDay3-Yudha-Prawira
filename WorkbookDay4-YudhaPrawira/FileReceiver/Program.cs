using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        const int port = 8080;
        string receiveFolder = Path.Combine(Directory.GetCurrentDirectory(), "ReceivedFiles");
        Directory.CreateDirectory(receiveFolder);

        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"[Server] Listening on port {port}. Receive folder: {receiveFolder}");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClientAsync(client, receiveFolder)); 
        }
    }

    static async Task HandleClientAsync(TcpClient client, string receiveFolder)
    {
        var remoteEP = client.Client.RemoteEndPoint;
        Console.WriteLine($"[Server] Connected: {remoteEP}");

        try
        {
            using (NetworkStream ns = client.GetStream())
            using (var br = new BinaryReader(ns, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                string fileName = br.ReadString();
                long fileLength = br.ReadInt64();

                string safeName = Path.GetFileName(fileName);
                string outPath = Path.Combine(receiveFolder, safeName);

                Console.WriteLine($"[Server] Receiving: {safeName} ({fileLength} bytes)");

                using (var fs = File.Create(outPath))
                {
                    byte[] buffer = new byte[8192];
                    long remaining = fileLength;
                    while (remaining > 0)
                    {
                        int toRead = (int)Math.Min(buffer.Length, remaining);
                        int read = await ns.ReadAsync(buffer, 0, toRead);
                        if (read == 0) break; // connection tutup
                        await fs.WriteAsync(buffer, 0, read);
                        remaining -= read;
                    }
                }

                Console.WriteLine($"[Server] Saved file: {outPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Server] Error: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine("[Server] Connection closed.");
        }
    }
}
