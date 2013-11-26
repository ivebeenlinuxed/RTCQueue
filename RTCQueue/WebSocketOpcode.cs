using System;

namespace RTCQueue
{
	public enum WebSocketOpcode
	{
		Continuation = 0x00,
		Text = 0x01,
		Binary = 0x02,
		ConnectionClose = 0x08,
		Ping = 0x9,
		Pong = 0xA
	}
}

