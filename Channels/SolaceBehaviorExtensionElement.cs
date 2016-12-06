﻿using Newtonsoft.Json;
using System;
using System.ServiceModel.Configuration;

namespace Solace.Channels
{
    public class SolaceBehaviorExtensionElement : BehaviorExtensionElement
    {
        public override Type BehaviorType
        {
            get
            {
                return typeof(SolaceEndpointBehavior);
            }
        }

        protected virtual JsonSerializerSettings GetSerializerSettings()
        {
            return new JsonSerializerSettings();
        }

        protected override object CreateBehavior()
        {
            return new SolaceEndpointBehavior(GetSerializerSettings);
        }
    }
}
