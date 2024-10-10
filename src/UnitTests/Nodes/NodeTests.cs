using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Reko.Core.Expressions;
using Reko.Core.Types;
using SeaOfNodes.Nodes;

namespace SeaOfNodes.UnitTests.Nodes
{
    [TestFixture]
    public class NodeTests
    {
        private NodeFactory factory;

        [SetUp]
        public void Setup()
        {
            factory = new NodeFactory();
        }

        private ConstantNode Const(int n)
        {
            return factory.Constant(Constant.Create(PrimitiveType.Word32, n));
        }

        [TestCase(arg1:"1", arg2:"1", arg3:"")]
        [TestCase(arg1:"1,3,2,4", arg2:"1", arg3:"4,3,2")]
        [TestCase(arg1:"1,3,1,4", arg2:"1", arg3: "4,3")]
        public void Node_RemoveUse(string list, string sItemToDelete, string sExpected)
        {
            var sUses = list.Split(",");
            var dict = sUses.Distinct().ToDictionary(n => n, n => Const(int.Parse(n.Trim())));
            var uses = sUses.Select(n => dict[n]).ToArray();
            var itemToDelete = dict[sItemToDelete];

            var node = new ConstantNode(-1, null!, Constant.Create(PrimitiveType.Int32, -1));
            node.RemoveUse(factory.StopNode);
            foreach (var use in uses)
                node.AddUse(use);

            node.RemoveUse(itemToDelete);

            var sActual = string.Join(
                ",",
                node.OutNodes.Cast<ConstantNode>().Where(c => c is not null).Select(c => c.Value.ToInt32()));

            Assert.That(sActual, Is.EqualTo(sExpected));
        }
    }
}
