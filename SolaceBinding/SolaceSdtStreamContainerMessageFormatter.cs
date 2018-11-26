using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Solace.Channels
{
	public class SolaceSdtStreamContainerMessageFormatter : MessageFormatterBase, IClientMessageFormatter, IDispatchMessageFormatter
	{
		public SolaceSdtStreamContainerMessageFormatter(OperationDescription operation) : base(operation)
		{
			if (!IsOneWay)
				throw new NotImplementedException("SDT container formatter can be applied only to one way operations.");
		}

		public object DeserializeReply(Message message, object[] parameters)
		{
			throw new NotImplementedException();
		}

		public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
		{
			throw new NotImplementedException();
		}

		public void DeserializeRequest(Message message, object[] parameters)
		{
			DeserializeMessageProperties(message, parameters);
			var list = message.Properties.TryGetValue("SdtStreamContainer") as IList<object>;

			if (list != null)
				foreach (var v in list.Zip(from x in OperationParameters
				where !x.IsFromProperty
				select x, (x, y) => new
				{
					y.Index,
					Value = x
				}))
					parameters[v.Index] = v.Value;
		}

		public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
		{
			throw new NotImplementedException();
		}
	}
}
