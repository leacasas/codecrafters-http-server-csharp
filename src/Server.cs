using System.Net;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_http_server.src;

public class Program
{
    public static async Task Main(string[] args)
    {
        TcpListener? server = null;

        // processing directory for GET /files/
        string? directoryPath = null;
        if (args.Length > 0 && args[0] == "--directory")
            directoryPath = args[1];

        server = new TcpListener(IPAddress.Any, 4221);
        server.Start(); //listen for client requests

        // listening loop, deals with multiple connections.
        while (true)
        {
            var client = await server.AcceptTcpClientAsync(); // blocking call to return a reference to the tcpclient to send and receive

            _ = Task.Run(() => ProcessTCPConnection(client, directoryPath));
        }
    }

    private static async Task ProcessTCPConnection(TcpClient client, string? directoryPath)
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
        var method = startLineSegments[0];
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
            var filePath = Path.Join(directoryPath, filePathFromRequest);

            if (method == "GET")
            {
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
            else if (method == "POST")
            {
                var requestPayload = msgSegments.Last();
                var fileContents = Encoding.UTF8.GetBytes(requestPayload);

                await File.WriteAllBytesAsync(filePath, fileContents);

                responseBuilder.Append("201 Created\r\n");
                responseBuilder.Append("\r\n");
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