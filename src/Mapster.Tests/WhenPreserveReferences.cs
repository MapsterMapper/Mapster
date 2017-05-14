using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenPreserveReference
    {
        [TestCleanup]
        public void Teardown()
        {
            TypeAdapterConfig.GlobalSettings.Default.Settings.PreserveReference = false;
        }

        [TestMethod]
        public void Preserve_Reference_For_List()
        {
            TypeAdapterConfig.GlobalSettings.Default.Settings.PreserveReference = true;

            var poco = new SimplePoco {Id = Guid.NewGuid(), Name = "TestName"};

            var array = new[] {poco, poco};

            var array2 = TypeAdapter.Adapt<SimplePoco[], SimpleDto[]>(array);

            array2[0].ShouldBeSameAs(array2[1]);
        }

        [TestMethod]
        public void Preserve_Reference_For_Circular_Reference()
        {
            TypeAdapterConfig.GlobalSettings.Default.Settings.PreserveReference = true;

            var node1 = new LinkNode {Id = Guid.NewGuid()};
            var node2 = new LinkNode {Id = Guid.NewGuid()};
            node1.AttachRight(node2);

            var another = TypeAdapter.Adapt<LinkNode>(node1);
            var another2 = another.Right;
            another.ShouldBeSameAs(another2.Left);
        }

        #region TestClasses

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class SimpleDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class LinkNode
        {
            public Guid Id { get; set; }
            public LinkNode Left { get; set; }
            public LinkNode Right { get; set; }

            public void AttachRight(LinkNode another)
            {
                this.Right = another;
                another.Left = this;
            }
            public void AttachLeft(LinkNode another)
            {
                this.Left = another;
                another.Right = this;
            }
        }

        #endregion
    }
}