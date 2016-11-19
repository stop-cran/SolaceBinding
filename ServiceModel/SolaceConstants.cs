using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Solace.ServiceModel
{
    static class SolaceConstants
    {
        public const string MethodKey = "method";
        public const string ErrorKey = "error";
        public const string RequestIdMessageProperty = "jsonRpcRequestId";
        public const string JObjectMessageProperty = "MessageAsJObject";
    }
}
