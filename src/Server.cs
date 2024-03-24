using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener? server = null;

try
{
    server = new TcpListener(IPAddress.Any, 4221);
    server.Start(); //listen for client requests

    byte[] bytes = new byte[1024];// Buffer for reading data
    string data;

    // listening loop, deals with multiple connections.
    while (true)
    {
        Console.WriteLine("Waiting for a connection... ");

        var client = server.AcceptTcpClient(); // blocking call to return a reference to the tcpclient to send and receive

        Console.WriteLine("Connected!");

        var networkStream = client.GetStream(); //get the network stream

        int bytesRead = networkStream.Read(bytes, 0, bytes.Length);// Receive all the data sent by the client.

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
        else
            responseBuilder.Append("404 Not Found\r\n\r\n");

        byte[] msg = Encoding.UTF8.GetBytes(responseBuilder.ToString());

        networkStream.Write(msg);// Write buffer into the network stream

        client.Close();// dispose tcp client and request tcp connection to close
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Exception: {ex}");
}
finally
{

#pragma warning disable CS8602 // Dereference of a possibly null reference.
    server.Stop();
#pragma warning restore CS8602

}