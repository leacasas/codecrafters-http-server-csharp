using System.Net;
using System.Net.Sockets;
using System.Text;

public class Program
{
    public static async Task Main(string[] args)
    {
        TcpListener? server = null;

        server = new TcpListener(IPAddress.Any, 4221);
        server.Start(); //listen for client requests

        // listening loop, deals with multiple connections.
        while (true)
        {
            var client = await server.AcceptTcpClientAsync(); // blocking call to return a reference to the tcpclient to send and receive

            _ = Task.Run(() => ProcessTCPConnection(client));
        }
    }

    private static async Task ProcessTCPConnection(TcpClient client)
    {
        byte[] bytes = new byte[1024];// Buffer for reading data
        string data;

        var networkStream = client.GetStream(); //get the network stream

        int bytesRead = await networkStream.ReadAsync(bytes);// Receive all the data sent by the client.

        data = Encoding.UTF8.GetString(bytes.AsSpan(0, bytesRead));// Translate data bytes to a ASCII string

        // process
        var msgSegments = data.Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var startLine = msgSegments[0];
        var startLineSegments = startLine.Split(' ');
        var path = startLineSegments[1];

        var responseBuilder = new StringBuilder("HTTP/1.1 ");

        if (path == "/")
            responseBuilder.Append("200 OK\r\n\r\n");
        else if (path.StartsWith("/user-agent"))
        {
            var header = msgSegments.First(x => x.StartsWith("User-Agent"));
            var responseValue = header.Split(':', StringSplitOptions.TrimEntries)[1];
            responseBuilder.Append("200 OK\r\n");
            responseBuilder.Append("Content-Type: text/plain\r\n");//Content type
            responseBuilder.Append($"Content-Length: {responseValue.Length}\r\n");//Content length
            responseBuilder.Append("\r\n");//new line
            responseBuilder.Append($"{responseValue}");//content
        }
        else if (path.StartsWith("/echo/"))
        {
            var valueToEcho = path.Remove(0, 6); // "/echo/" is 6 chars
            responseBuilder.Append("200 OK\r\n");
            responseBuilder.Append("Content-Type: text/plain\r\n");//Content type
            responseBuilder.Append($"Content-Length: {valueToEcho.Length}\r\n");//Content length
            responseBuilder.Append("\r\n");//new line
            responseBuilder.Append($"{valueToEcho}");//content
        }
        else if (path.StartsWith("/files/"))
        {
            var filePathFromRequest = path.Remove(0, 7); // "/files/" is 7 chars
            var filePath = Path.Join(Directory.GetCurrentDirectory(),filePathFromRequest);

            if (File.Exists(filePath))
            {
                var fileContent = await File.ReadAllBytesAsync(filePath);

                responseBuilder.Append("200 OK\r\n");
                responseBuilder.Append("Content-Type: application/octet-stream\r\n");//Content type
                responseBuilder.Append($"Content-Length: {fileContent.Length}\r\n");//Content length
                responseBuilder.Append("\r\n");//new line

                byte[] responseHead = Encoding.UTF8.GetBytes(responseBuilder.ToString());

                await networkStream.WriteAsync(responseHead);
                await networkStream.WriteAsync(fileContent);

                networkStream.Close(); // close stream
                client.Close();// dispose tcp client and request tcp connection to close

                return;
            }
            else
            {
                responseBuilder.Append("404 Not Found\r\n\r\n");
            }
        }
        else
        {
            responseBuilder.Append("404 Not Found\r\n\r\n");
        }

        byte[] msg = Encoding.UTF8.GetBytes(responseBuilder.ToString());

        await networkStream.WriteAsync(msg);// Write buffer into the network stream

        networkStream.Close(); // close stream
        client.Close();// dispose tcp client and request tcp connection to close
    }
}

/*
 using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class SimpleHttpServer
{
    public static void Main(string[] args)
    {
        int port = 8080; // Choose a port for your server
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"Server started. Listening on port {port}...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Task.Run(() => HandleClient(client));
        }
    }

    static async Task HandleClient(TcpClient client)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new StreamReader(stream))
        using (StreamWriter writer = new StreamWriter(stream))
        {
            // Read the HTTP request
            string request = await reader.ReadLineAsync();
            Console.WriteLine($"Request received: {request}");

            // Generate a simple HTTP response
            string response = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nHello, World!";
            
            // Send the HTTP response
            await writer.WriteAsync(response);
        }
    }
}

 */