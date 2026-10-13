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

        [TestMethod]
        public void MapSameReferenceToDifferentTypes()
        {
            var config = new TypeAdapterConfig();
            config.Default.PreserveReference(true);

            var employee = new Employee { Id = 1, Name = "Name" };

            var department = new Department
            {
                Manager = employee,
                Supervisor = employee
            };

            var result = department.Adapt<DepartmentDto>(config);
            result.Manager.Id.ShouldBe(employee.Id);
            result.Manager.Name.ShouldBe(employee.Name);
            result.Supervisor.Id.ShouldBe(employee.Id);
            result.Supervisor.Name.ShouldBe(employee.Name);
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

        public class DepartmentDto
        {
            public EmployeeDto Manager { get; set; }
            public PersonDto Supervisor { get; set; }
        }

        public class Department
        {
            public Employee Manager { get; set; }
            public Employee Supervisor { get; set; }
        }

        public class Employee
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class EmployeeDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class PersonDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        #endregion
    }
}