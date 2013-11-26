using System;
using System.Net.Sockets;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading;

namespace RTCQueue
{
	class MainClass
	{
		public TcpListener commandListenerSocket;
		public TcpListener clientListenerSocket;

		List<WebSocketClient> clients = new List<WebSocketClient>();

		ManualResetEvent commanderAccept = new ManualResetEvent(false);
		ManualResetEvent clientAccept = new ManualResetEvent(false);

		public static void Main (string[] args)
		{
			MainClass m = new MainClass();
		}

		public MainClass ()
		{
			




			this.commandListenerSocket = new TcpListener (IPAddress.Loopback, 8899);
			this.commandListenerSocket.Start ();




			Console.Write("Finished Setup");



			this.clientListenerSocket = new TcpListener (IPAddress.Any, 8888);
			this.clientListenerSocket.Start ();

			Thread commanderThread = new Thread(new ThreadStart(this.AcceptCommanderThread));
			Thread clientThread = new Thread(new ThreadStart(this.AcceptClientThread));
			clientThread.Start();
			commanderThread.Start();
		}

		public void AcceptCommanderThread ()
		{
			while (true) {
				commanderAccept.Reset ();
				this.commandListenerSocket.BeginAcceptTcpClient (new AsyncCallback (this.DoCommanderAcceptSocketCallback), commandListenerSocket);
				commanderAccept.WaitOne();
				Console.Write(".");
			}
		}

		public void AcceptClientThread ()
		{
			while (true) {
				clientAccept.Reset ();
				this.clientListenerSocket.BeginAcceptTcpClient (new AsyncCallback (this.DoClientAcceptSocketCallback), clientListenerSocket);
				clientAccept.WaitOne();
				Console.Write(".");
			}
		}



		public void DoCommanderAcceptSocketCallback (IAsyncResult asyncResult)
		{
			commanderAccept.Set();
			Console.WriteLine("Accepting Commander");
			TcpListener listener = (TcpListener)asyncResult.AsyncState;
			TcpClient commanderClient = listener.EndAcceptTcpClient(asyncResult);
			//byte[] recv = new byte[1];
			//int currentCount = 0;
			string recvString = null;

			commanderClient.ReceiveTimeout = 10000;



			StreamReader reader = new StreamReader(commanderClient.GetStream(), System.Text.Encoding.UTF8);


			while (recvString == null) {
				recvString = reader.ReadLine();
			//if (!commanderSocket.Connected) {
			//	break;
			//}
			//if (recvString == null) {
			//	continue;
			}
			JObject commanderJSON = JObject.Parse(recvString);
			this.sendChannel((string)commanderJSON["channel"], commanderJSON["data"].ToString());
			//stream.Write(System.Text.Encoding.ASCII.GetBytes("SENT"), 0, 4);
			commanderClient.Close();
		}

		public void sendChannel (string channel, string data)
		{
			Console.WriteLine("Sending to Channel "+channel+": "+data);
			foreach (WebSocketClient client in clients) {
				if (client.header.RequestPath == channel) {
					client.Send(data);
				}
			}
		}


		public void DoClientAcceptSocketCallback (IAsyncResult asyncResult)
		{
			clientAccept.Set();
			TcpListener listener = (TcpListener)asyncResult.AsyncState;
			TcpClient clientSocket = listener.EndAcceptTcpClient(asyncResult);
			this.clients.Add(new WebSocketClient(clientSocket));
			Console.WriteLine("Accepted new client");
		}

	}
}
