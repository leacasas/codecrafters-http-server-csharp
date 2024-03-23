using System.Net;
using System.Net.Sockets;
using System.Text;

var server = new TcpListener(IPAddress.Any, 4221);

server.Start(); //listen for client requests

byte[] bytes = new byte[1024];// Buffer for reading data
string data;

Console.WriteLine("Waiting for a connection... ");

var client = server.AcceptTcpClient(); // blocking call to return a reference to the tcpclient to send and receive

Console.WriteLine("Connected!");

var networkStream = client.GetStream(); //get the network stream

int bytesRead = networkStream.Read(bytes, 0, bytes.Length);// Receive all the data sent by the client.

data = Encoding.ASCII.GetString(bytes.AsSpan(0, bytesRead));// Translate data bytes to a ASCII string

// process
var lines = data.Split(" ", StringSplitOptions.RemoveEmptyEntries);
var startLine = lines[1];

var responseBuilder = new StringBuilder("HTTP/1.1 ");
if (startLine == "/")
    responseBuilder.Append("200 OK\r\n\r\n");
else
    responseBuilder.Append("404 Not Found\r\n\r\n");

byte[] msg = Encoding.ASCII.GetBytes(responseBuilder.ToString());

networkStream.Write(msg);// Write buffer into the network stream

client.Close();// dispose tcp client and request tcp connection to close.