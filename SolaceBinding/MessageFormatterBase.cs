using SolaceBinding.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;

namespace Solace.Channels
{
    public class MessageFormatterBase
    {
        protected string ApplicationMessageType { get; }

        protected string ReplyApplicationMessageType { get; }

        protected bool IsOneWay { get; }

        protected IReadOnlyList<RequestParameter> OperationParameters { get; }

        public MessageFormatterBase(OperationDescription operation)
        {
            ApplicationMessageType = operation.Name;
            ReplyApplicationMessageType = (operation.IsOneWay ? null : GetReplyApplicationMessageType(operation));
            IsOneWay = operation.IsOneWay;
            OperationParameters = GetRequestParameters(operation).ToList().AsReadOnly();
        }

        private static IEnumerable<RequestParameter> GetRequestParameters(OperationDescription operation)
        {
            var properties = new HashSet<string>(from parameter in (operation.TaskMethod ?? operation.SyncMethod).GetParameters()
                                                 where parameter.GetCustomAttributes(typeof(FromMessagePropertyAttribute), true).Any<object>()
                                                 select parameter.Name);
            var protoIndexes = (operation.TaskMethod ?? operation.SyncMethod).GetParameters().ToDictionary((ParameterInfo x) => x.Name,
                x => x.GetCustomAttributes(typeof(ProtoMemberAttribute), false).Cast<ProtoMemberAttribute>().FirstOrDefault()?.Order);

            return from part in operation.Messages[0].Body.Parts
                   let type = part.Type
                   let isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                   select new RequestParameter
                   {
                       Name = part.Name,
                       Index = part.Index,
                       ProtoIndex = (protoIndexes[part.Name] ?? (part.Index + 1)),
                       Type = type,
                       IsFromProperty = properties.Contains(part.Name),
                       IsRequired = (properties.Contains(part.Name) || (type.IsValueType && !isNullable)),
                       IsNullable = isNullable,
                       NullableTypeArgument = (isNullable ? type.GetGenericArguments()[0] : null)
                   };
        }

        private static string GetReplyApplicationMessageType(OperationDescription operation)
        {
            string text = operation.DeclaringContract.Namespace + operation.DeclaringContract.Name;
            string action = operation.Messages[1].Action;

            return action.StartsWith(text)
                ? action.Substring(text.Length).TrimStart('/')
                : action;
        }

        protected void DeserializeMessageProperties(Message message, object[] parameters)
        {
            foreach (var requestParameter in from p in OperationParameters
                                             where p.IsFromProperty
                                             select p)
                if (message.Properties.TryGetValue(requestParameter.Name, out object value))
                    parameters[requestParameter.Index] = Convert.ChangeType(value, requestParameter.IsNullable ? requestParameter.NullableTypeArgument : requestParameter.Type);
                else
                {
                    if (!requestParameter.IsNullable)
                        throw new ArgumentException("Required parameter was not provided.", requestParameter.Name);

                    parameters[requestParameter.Index] = null;
                }
        }

        protected static byte[] ReadMessageBinaryHelper(Message message)
        {
            var array = MessageBinaryHelper.ReadMessageBinary(message);

            if (!array.SequenceEqual(empty))
                return array;

            return new byte[0];
        }

        private static readonly byte[] empty = Encoding.UTF8.GetBytes("{}");
    }
}
