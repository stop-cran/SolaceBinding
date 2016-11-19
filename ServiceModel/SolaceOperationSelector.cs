﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using Newtonsoft.Json.Linq;

namespace Solace.ServiceModel
{
    class SolaceOperationSelector : IDispatchOperationSelector
    {
        public string SelectOperation(ref Message message)
        {
            return (string)message.Properties["ApplicationMessageType"];
        }
    }
}
