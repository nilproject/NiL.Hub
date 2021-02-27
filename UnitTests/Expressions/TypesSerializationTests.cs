using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiL.Exev;

namespace HubUnitTests.Expressions
{
    [TestClass]
    public sealed class TypesSerializationTests
    {
        [TestMethod]
        public void ExternalTypesSerialization()
        {
            var serializer = new ExpressionSerializer();
            var deserializer = new ExpressionDeserializer();
            var list = new List<int>();
            Expression<Func<List<int>, int>> expression = l => l.Count;

            var serialized = serializer.Serialize(expression);

            var deserialized = deserializer.Deserialize(serialized);
        }
    }
}
