using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Solace.Channels
{
    class SolaceMessageFormatter : IClientMessageFormatter, IDispatchMessageFormatter
    {
        readonly string applicationMessageType;
        readonly string replyApplicationMessageType;
        readonly ReadOnlyCollection<RequestParameter> operationParameters;
        readonly Type returnType;
        readonly Func<JsonSerializerSettings> settingsProvider;

        public SolaceMessageFormatter(OperationDescription operation, Func<JsonSerializerSettings> settingsProvider)
        {
            var replyActionPrefix = operation.DeclaringContract.Namespace + operation.DeclaringContract.Name;
            var replyAction = operation.Messages[1].Action;

            applicationMessageType = operation.Name;
            replyApplicationMessageType = replyAction.StartsWith(replyActionPrefix)
                ? replyAction.Substring(replyActionPrefix.Length).TrimStart('/')
                : replyAction;

            var properties = new HashSet<string>(from parameter in (operation.TaskMethod ?? operation.SyncMethod).GetParameters()
                                                 where parameter.GetCustomAttributes(typeof(MessageParameterAttribute), true).Any()
                                                 select parameter.Name);

            returnType = operation.Messages[1].Body.ReturnValue.Type;
            operationParameters = operation.Messages[0].Body.Parts
                .Select(part => new RequestParameter
                {
                    Name = part.Name,
                    Index = part.Index,
                    Type = part.Type,
                    IsFromProperty = properties.Contains(part.Name),
                    IsRequired = properties.Contains(part.Name) ||
                        part.Type.IsValueType && (!part.Type.IsGenericType || part.Type.GetGenericTypeDefinition() != typeof(Nullable<>))
                }).ToList()
                .AsReadOnly();

            this.settingsProvider = settingsProvider;
        }

        public object DeserializeReply(Message message, object[] parameters)
        {
            var reader = GetJsonReaderAtMessageBody(message);
            var settings = settingsProvider();

            settings.MissingMemberHandling = MissingMemberHandling.Error;

            var serializer = JsonSerializer.Create(settings);

            serializer.Error += (sender, e) =>
            {
                if (reader.TokenType != JsonToken.PropertyName || (string)reader.Value != SolaceConstants.ErrorKey)
                    e.ErrorContext.Handled = true;
            };

            try
            {
                return serializer.Deserialize(reader, returnType);
            }
            catch (JsonSerializationException)
            {
                if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == SolaceConstants.ErrorKey)
                {
                    reader.Read();
                    throw new SolaceJsonException(JObject.Load(reader));
                }
                else
                    throw;
            }
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            var serializer = JsonSerializer.Create(settingsProvider());

            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    jsonWriter.WriteStartObject();
                    foreach (var part in operationParameters)
                    {
                        object paramValue = parameters[part.Index];

                        if (paramValue != null)
                        {
                            jsonWriter.WritePropertyName(part.Name);
                            serializer.Serialize(jsonWriter, paramValue);
                        }
                    }

                    jsonWriter.WriteEndObject();
                }

                var message = MessageBinaryHelper.SerializeMessage(stream.ToArray());

                message.Properties[SolaceConstants.ApplicationMessageTypeKey] = applicationMessageType;
                message.Properties[SolaceConstants.CorrelationIdKey] = new RequestCorrelationState();

                return message;
            }
        }

        static JsonTextReader GetJsonReaderAtMessageBody(Message message)
        {
            object reader;

            return message.Properties.TryGetValue(SolaceConstants.ReplyReaderKey, out reader)
                ? (JsonTextReader)reader
                : new JsonTextReader(
                    new StreamReader(
                        new MemoryStream(MessageBinaryHelper.ReadMessageBinary(message))))
                {
                    CloseInput = true
                };
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            foreach (var part in operationParameters.Where(p => p.IsFromProperty))
            {
                object property;
                if (message.Properties.TryGetValue(part.Name, out property))
                    parameters[part.Index] = property;
                else
                    throw new ArgumentException("Required parameter was not provided.", part.Name);
            }

            var filledParts = new bool[operationParameters.Count];

            using (var reader = GetJsonReaderAtMessageBody(message))
            {
                if (!reader.Read() || !reader.Read())
                    throw new JsonReaderException("Error reading the request");

                while (reader.TokenType != JsonToken.EndObject)
                {
                    if (reader.TokenType != JsonToken.PropertyName)
                        throw new JsonReaderException("Error reading the request");

                    var part = operationParameters.FirstOrDefault(x => x.Name == (string)reader.Value);

                    if (part.Name == null) // skip unexpected properties
                    {
                        reader.Read();

                        switch(reader.TokenType)
                        {
                            case JsonToken.StartArray:
                            case JsonToken.StartObject:
                                reader.Skip();
                                break;
                        }

                        reader.Read();
                        continue;
                    }

                    if (!reader.Read())
                        throw new JsonReaderException("Error reading the request");

                    try
                    {
                        var v = JsonSerializer.Create(settingsProvider()).Deserialize(reader, part.Type);

                        if (!reader.Read())
                            throw new JsonReaderException("Error reading the request");

                        if (v == null && part.IsRequired)
                            throw new ArgumentException("Required parameter was not provided.", part.Name);

                        parameters[part.Index] = v;
                    }
                    catch (ArgumentException ex) when (ex.ParamName == part.Name)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException("Error retrieving parameter value", part.Name, ex);
                    }

                    filledParts[part.Index] = true;
                }
            }

            var notFilled = operationParameters.FirstOrDefault(p => !p.IsFromProperty && p.IsRequired && !filledParts[p.Index]);

            if (notFilled.IsRequired)
                throw new ArgumentException("Required parameter was not provided.", notFilled.Name);
        }

        byte[] Serialize(object value)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                using (var jsonWriter = new JsonTextWriter(writer))
                    JsonSerializer.Create(settingsProvider()).Serialize(jsonWriter, value);

                return stream.ToArray();
            }
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            var reply = MessageBinaryHelper.SerializeMessage(Serialize(result));

            reply.Properties[SolaceConstants.ApplicationMessageTypeKey] = replyApplicationMessageType;

            return reply;
        }
    }
}