using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener? server = null;

server = new TcpListener(IPAddress.Any, 4221);
server.Start(); //listen for client requests

// listening loop, deals with multiple connections.
while (true)
{
    var client = await server.AcceptTcpClientAsync(); // blocking call to return a reference to the tcpclient to send and receive

    await Task.Run(() => ProcessTCPConnection(client));
}

static async Task ProcessTCPConnection(TcpClient client)
{
    byte[] bytes = new byte[1024];// Buffer for reading data
    string data;
    var networkStream = client.GetStream(); //get the network stream

    int bytesRead = networkStream.Read(bytes);// Receive all the data sent by the client.

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

    await networkStream.WriteAsync(msg);// Write buffer into the network stream

    networkStream.Close(); // close stream
    client.Close();// dispose tcp client and request tcp connection to close
}