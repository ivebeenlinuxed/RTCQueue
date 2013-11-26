using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using RTCQueue.WebSocket.rfc6455;
using RTCQueue.WebSocket;

using DataFrame = RTCQueue.WebSocket.rfc6455.DataFrame;

namespace RTCQueue
{
	public class WebSocketClient
	{
		public TcpClient client;
		public string channel;
		StreamReader reader;
		NetworkStream stream;
		public Header header;

		public string method;
		public string http_version;

		public RTCQueue.WebSocket.rfc6455.DataFrame currentFrame;

		public delegate void ReceivedFrameEventHandler(WebSocketClient client);

		public event ReceivedFrameEventHandler RecievedFrame;

		public WebSocketClient (TcpClient client)
		{
			this.client = client;
			this.Setup ();
		}

		private void Setup ()
		{

			client.ReceiveTimeout = 10000;

			
			reader = new StreamReader(client.GetStream());
			stream = client.GetStream();


			this.Authenticate();


		}

		public void StartRecv() {
			while (true) {
				byte[] buffer = new byte[1024];
				this.stream.Read(buffer, 0, 1024);
				currentFrame.Append(buffer);
				switch (this.currentFrame.State)
                    {
                        case DataFrame.DataState.Complete:
							this.RecievedFrame(this);
                            break;
                        case DataFrame.DataState.Closed:
                            DataFrame closeFrame = new DataFrame();
							closeFrame.State = DataFrame.DataState.Closed;
							closeFrame.Append(new byte[] { 0x8 }, true);
							this.Send(closeFrame, false, true);
                            break;
                        case DataFrame.DataState.Ping:
                            currentFrame.State = DataFrame.DataState.Complete;
							DataFrame pongFrame = new DataFrame();
                            pongFrame.State = DataFrame.DataState.Pong;
                            List<ArraySegment<byte>> pingData = currentFrame.AsRaw();
                            foreach (var item in pingData)
                            {
                                pongFrame.Append(item.Array);
                            }
                            this.Send(pongFrame);
                            break;
                        case DataFrame.DataState.Pong:
                            this.currentFrame.State = DataFrame.DataState.Complete;
                            break;
                    }
			}
		}



		public bool Authenticate ()
		{
			string recvString = "";
			string strHeader = "";
			while ((recvString = reader.ReadLine()) != "") {
				strHeader += recvString+"\r\n";
			}

			header = new Header(strHeader);

			if (header ["upgrade"].ToLower () != "websocket" || this.header.Method != "GET") {
				this.client.Close();
				return false;
			}

			SHA1 sha = new SHA1CryptoServiceProvider();

			string acceptHash = System.Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(header["sec-websocket-key"]+"258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));

			string response = "HTTP/1.1 101 Web Socket Protocol Handshake\r\n"
			                                   +"Upgrade: websocket\r\n"
			                                   +"Connection: Upgrade\r\n"
			                                   +"Sec-WebSocket-Accept: "+acceptHash+"\r\n"
			                                   +"Access-Control-Allow-Origin: "+header["origin"]+"\r\n"
					+"\r\n";


			client.GetStream().Write(Encoding.ASCII.GetBytes(response), 0, response.Length);
			Console.Write(response);
			return true;
		}

		public bool Send (DataFrame frame, bool raw, bool close)
		{
			if (raw) {
				this.SendRaw (frame);
			} else {
				this.Send (frame);
			}
			this.Close();
			return true;
		}

		public bool Close ()
		{
			client.Close();
			return true;
		}

		public bool Send (string data)
		{
			DataFrame frame = new DataFrame();
			frame.Append(data);
			return this.Send(frame);
		}

		public bool SendRaw (DataFrame frame)
		{
			if (!client.Client.Connected) {
				return false;
			}

			try {
				client.Client.Send (frame.AsRaw ());
			} catch (Exception e) {

			}
			return true;
		}

		public bool Send (DataFrame frame) {
			if (!client.Client.Connected) {
				return false;
			}
			try {
				client.Client.Send(frame.AsFrame());
			} catch (Exception e) {

			}
			return true;
		}
	}
}

