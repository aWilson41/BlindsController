using System;
using System.IO;
using Windows.Networking.Sockets;

namespace PiSwitcherNET
{
	class SocketEventArgs : EventArgs
	{
		public string message;

		public SocketEventArgs(string message)
		{
			this.message = message;
		}
	}

	// A coherent listener for the entire app
	static class StaticListener
	{
		static StreamSocketListener listenSocket = new StreamSocketListener();

		// Events & Delegates
		public delegate void MessageReceivedHandler(object sender, SocketEventArgs e);
		public static event MessageReceivedHandler MessageReceived;

		static StaticListener()
		{
			StartListening();
		}

		public static void StartListening()
		{
			listenSocket.ConnectionReceived += ConnectionReceived;
			listenSocket.Control.KeepAlive = true;
			listenSocket.BindServiceNameAsync("12000");
		}

		public static void StopListening()
		{
			listenSocket.ConnectionReceived -= ConnectionReceived;
			listenSocket.Dispose();
		}

		private static async void ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
		{
			// Read the line from the remote client
			Stream inStream = args.Socket.InputStream.AsStreamForRead();
			StreamReader reader = new StreamReader(inStream);
			string request = reader.ReadLine();

			MessageReceived(sender, new SocketEventArgs(request));
		}
	}
}