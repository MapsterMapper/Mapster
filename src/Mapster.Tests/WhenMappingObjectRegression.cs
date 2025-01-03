using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingObjectRegression
    {
        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/524 
        /// </summary>
        [TestMethod]
        public void TSourceIsObjectUpdate()
        {
            var source = new Source524 { X1 = 123 };
            var _result = Somemap(source);

            _result.X1.ShouldBe(123);
        }

        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/524
        /// </summary>
        [TestMethod]
        public void TSourceIsObjectUpdateUseDynamicCast()
        {
            var source = new Source524 { X1 = 123 };
            var _result = SomemapWithDynamic(source);

            _result.X1.ShouldBe(123);
        }

        [TestMethod]
        public void UpdateManyDest()
        {
            var source = new Source524 { X1 = 123 };
            var _result = SomemapManyDest(source);

            _result.X1.ShouldBe(123);
            _result.X2.ShouldBe(127);
        }

        [TestMethod]
        public void UpdateToRealObject()
        {
            var source = new Source524 { X1 = 123 };
            var RealObject = new Object();

            var _result = source.Adapt(RealObject);

            _result.ShouldBeOfType<Source524>();
            ((Source524)_result).X1.ShouldBe(source.X1);

        }

        [TestMethod]
        public void RealObjectCastToDestination() /// Warning potential Infinity Loop in ObjectAdapter!!!
        {
            var source = new Source524 { X1 = 123 };
            var RealObject = new Object();

            var _result = RealObject.Adapt(source);

            _result.ShouldBeOfType<Source524>();
            ((Source524)_result).X1.ShouldBe(source.X1);
        }

        [TestMethod]
        public void UpdateObjectInsaider()
        {
            var _source = new InsaderObject() { X1 = 1 };
            var _Destination = new InsaderObject() { X1 = 2 };

            var _result = _source.Adapt(_Destination);

            _result.X1.ShouldBe(_source.X1);
        }

        [TestMethod]
        public void UpdateObjectInsaiderToObject()
        {
            var _source = new InsaderObject() { X1 = 1 };
            var _Destination = new InsaderObject() { X1 = new Object() };

            var _result = _source.Adapt(_Destination);

            _result.X1.ShouldBe(_source.X1);
        }

        [TestMethod]
        public void UpdateObjectInsaiderWhenObjectinTSource()
        {
            var _source = new InsaderObject() { X1 = new Object() };
            var _Destination = new InsaderObject() { X1 = 3 };

            var _result = _source.Adapt(_Destination);

            _result.X1.ShouldBe(_source.X1); 
        }


        #region TestFunctions

        Dest524 Somemap(object source)
        {
            var dest = new Dest524 { X1 = 321 };
            var dest1 = source.Adapt(dest);

            return dest;
        }

        ManyDest524 SomemapManyDest(object source)
        {
            var dest = new ManyDest524 { X1 = 321, X2 = 127 };
            var dest1 = source.Adapt(dest);

            return dest;
        }

        Dest524 SomemapWithDynamic(object source)
        {
            var dest = new Dest524 { X1 = 321 };
            var dest1 = source.Adapt(dest, source.GetType(), dest.GetType());

            return dest;
        }

        #endregion TestFunctions

        #region TestClasses
        class Source524
        {
            public int X1 { get; set; }
        }
        class Dest524
        {
            public int X1 { get; set; }
        }

        class ManyDest524
        {
            public int X1 { get; set;}

            public int X2 { get; set;}  
        }

        class InsaderObject
        {
            public Object X1 { get; set;}
        }


        #endregion TestClasses
    }
}
