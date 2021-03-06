﻿using Network.Utils;
using System;
using LiteNetLib.Utils;

namespace Network.Messages.Connection
{
	public class MouseScrollMessage: BaseMessage
	{
		public MouseScrollMessage()
			: base((ushort)CustomMessageType.MouseScroll)
		{
		}

		public override void WritePayload(NetDataWriter message)
		{
			base.WritePayload(message);

		}

		public override void ReadPayload(NetDataReader message)
		{
			base.ReadPayload(message);
		}
	}
}
