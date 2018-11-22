using System;
using System.Collections.Generic;
using System.Linq;

namespace Solace.Channels
{
    internal class OperationEqualityComparer : EqualityComparer<Tuple<IEnumerable<RequestParameter>, Type>>
    {
        public override bool Equals(Tuple<IEnumerable<RequestParameter>, Type> x, Tuple<IEnumerable<RequestParameter>, Type> y) =>
            x.Item1.Zip(y.Item1, new Func<RequestParameter, RequestParameter, bool>(Equals)).All((bool b) => b) && x.Item2 == y.Item2;

        public override int GetHashCode(Tuple<IEnumerable<RequestParameter>, Type> obj) =>
            obj.Item1.Aggregate(0,
                (int hash, RequestParameter item) =>
                    hash * 397 +
                    item.GetHashCode()) * 314159 +
                    obj.Item2.FullName.GetHashCode() * 219 +
                    obj.Item2.Assembly.FullName.GetHashCode();

        private static bool Equals(RequestParameter x, RequestParameter y) =>
            x.Index == y.Index &&
            x.IsFromProperty == y.IsFromProperty &&
            x.IsRequired == y.IsRequired &&
            x.Name == y.Name &&
            x.ProtoIndex == y.ProtoIndex &&
            x.Type == y.Type;
    }
}
