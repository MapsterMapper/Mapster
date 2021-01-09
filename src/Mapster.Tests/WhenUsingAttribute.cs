using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenUsingAttribute
    {
        [TestMethod]
        public void Using_Attributes()
        {
            var id = Guid.NewGuid();
            var poco = new SimplePoco(id) { Name = "test" };
            var dto = poco.Adapt<SimpleDto>();
            dto.Id.ShouldBe(id);
            dto.Name.ShouldBeNull();
        }

        [TestMethod]
        public void Specify_MemberSide()
        {
            var user = new User {UserIdentification = 123};
            var dto = user.Adapt<UserDto>();
            var info = dto.Adapt<UserInfo>();
            info.UserId.ShouldBe(123);
        }

        public class SimplePoco
        {
            public SimplePoco(Guid id) { this.id = id; }

            [AdaptMember("Id")]
            private Guid id { get; }

            [AdaptIgnore]
            public string Name { get; set; }
        }

        public class SimpleDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class User
        {
            public int UserIdentification {get; set;}
        }

        public class UserDto
        {
            [AdaptMember("UserIdentification", MemberSide.Destination)]
            public int UserId {get; set;}
        }

        public class UserInfo
        {
            public int UserId {get; set;}
        }
    }
}
