using ExpressionDebugger;
using ExpressionDebugger.Helpers;
using ExpressionDebugger.Helpers.GeneratedAttributes;
using Mapster;
using Mapster.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TemplateTest
{
    [TestClass]
    public class CreateMapExpressionTest
    {
        [TestMethod]
        public void TestCreateMapExpression()
        {
            TypeAdapterConfig.GlobalSettings.SelfContainedCodeGeneration = true;
            var foo = default(Customer);
            var def = new ExpressionDefinitions
            {
                IsStatic = true,
                MethodName = "Map",
                Namespace = "Benchmark",
                TypeName = "CustomerMapper"
            };
            var code = foo.BuildAdapter()
                .CreateMapExpression<CustomerDTO>()
                .ToScript(def);

            Assert.IsNotNull(code);
        }

        [TestMethod]
        public void TestCreateMapToTargetExpression()
        {
            TypeAdapterConfig.GlobalSettings.SelfContainedCodeGeneration = true;
            var foo = default(Customer);
            var def = new ExpressionDefinitions
            {
                IsStatic = true,
                MethodName = "Map",
                Namespace = "Benchmark",
                TypeName = "CustomerMapper"
            };
            var code = foo.BuildAdapter()
                .CreateMapToTargetExpression<CustomerDTO>()
                .ToScript(def);

            Assert.IsNotNull(code);
        }

        [TestMethod]
        public void TestCreateProjectionExpression()
        {
            TypeAdapterConfig.GlobalSettings.SelfContainedCodeGeneration = true;
            var foo = default(Customer);
            var def = new ExpressionDefinitions
            {
                IsStatic = true,
                MethodName = "Map",
                Namespace = "Benchmark",
                TypeName = "CustomerMapper"
            };
            var code = foo.BuildAdapter()
                .CreateProjectionExpression<CustomerDTO>()
                .ToScript(def);

            Assert.IsNotNull(code);
        }

        /// <summary>
        /// https://github.com/MapsterMapper/Mapster/issues/399
        /// </summary>
        [TestMethod]
        public void TestRegressionMapperGenerationTranslation()
        {
            var S = new MapsterToolGeneratedMapperAttribute("Test");

            var config = new TypeAdapterConfig();
            config.SelfContainedCodeGeneration = true;
           
            var definitions = new TypeDefinitions
            {
                Implements = new[] { typeof(IMyTypeMapper), typeof(IMyTypeMapperIntenal) },
                Namespace = "Benchmark",
                TypeName = "CustomerMapper",
                IsInternal = false, 
                GeneratedAttributes = new(new[] {new MapsterToolGeneratedMapperAttribute("Test") })
            };

            var translator = new ExpressionTranslator(definitions);

            translator.CreateFromInterface(definitions, config);

            var code = translator.ToString();

            Assert.IsTrue(code.Contains("public partial class CustomerMapper")); // mapper class is public
            Assert.IsTrue(code.Contains("AddressDTO TemplateTest.IMyTypeMapper.Map"));
            Assert.IsTrue(code.Contains("[MapsterToolGeneratedMapper]"));

            Assert.IsTrue(code.Contains("internal AddressDTO Map")); // create internal method in public interface

            // create as internal because declarate in internal interface and using internal type AddressInternal
            Assert.IsTrue(code.Contains("internal AddressInternal MapInternal"));
            Assert.IsTrue(code.Contains("internal Expression<Func<AddressInternal, Address>> ProjectionInternal"));

            
            Assert.IsTrue(code.Contains("public AddressDTO MapPublicClassInInternalInterface")); // create public method in internal interface because using public types

            // method using public types in internal interface but marked as internal create as internal method
            Assert.IsTrue(code.Contains("internal AddressDTO MapPublicClassInInternalInterfaceWithMarkInternal"));
        }

        [TestMethod]
        public void CreateForceInternalMapper()
        {
            var config = new TypeAdapterConfig();
            config.SelfContainedCodeGeneration = true;

            var definitions = new TypeDefinitions
            {
                Implements = new[] { typeof(IMyTypeMapperForce)},
                Namespace = "Benchmark",
                TypeName = "CustomerMapper",
                IsInternal = true, // force create internal mapper
                GeneratedAttributes = new(new[] { new MapsterToolGeneratedMapperAttribute("Test") })
            };

            var translator = new ExpressionTranslator(definitions);

            translator.CreateFromInterface(definitions, config);

            var code = translator.ToString();

            Assert.IsTrue(code.Contains("internal partial class CustomerMapper")); // mapper class is internal

            // force create internal method using only public types because mapper class is internal
            Assert.IsTrue(code.Contains("internal AddressDTO Map")); 
        }



    }

   
    public interface IMyTypeMapper
    {
       internal AddressDTO Map(Address p1);
       public Expression<Func<AddressDTO, Address>> Projection { get; }
    }

    internal interface IMyTypeMapperIntenal
    {
        AddressInternal MapInternal(Address p1);
        Expression<Func<AddressInternal, Address>> ProjectionInternal { get; }
        AddressDTO MapPublicClassInInternalInterface(Address p1);
        internal AddressDTO MapPublicClassInInternalInterfaceWithMarkInternal(Address p1);
    }

    public interface IMyTypeMapperForce
    {
        AddressDTO Map(Address p1);
    }

    internal class AddressInternal
    {
        public int Id { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }

    public class Address
    {
        public int Id { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }

    public class AddressDTO
    {
        public int Id { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal? Credit { get; set; }
        public Address Address { get; set; }
        public Address HomeAddress { get; set; }
        public Address[] Addresses { get; set; }
        public ICollection<Address> WorkAddresses { get; set; }
    }

    public class CustomerDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public AddressDTO Address { get; set; }
        public AddressDTO HomeAddress { get; set; }
        public AddressDTO[] Addresses { get; set; }
        public List<AddressDTO> WorkAddresses { get; set; }
        public string AddressCity { get; set; }
    }

    static class GenerateMappersExtensions
    {
        public static void CreateFromInterface(this ExpressionTranslator translator, TypeDefinitions definitions, TypeAdapterConfig config)
        {
            if (definitions.Implements == null)
                return;

            foreach (var interfaceType in definitions.Implements)
            {
                bool? _isForceInternal = definitions.IsInternal ? true : null;

                foreach (var method in interfaceType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(x => x.IsPublicOrInternal())
                    )
                {
                    if (method.IsGenericMethod)
                        continue;
                    if (method.ReturnType == typeof(void))
                        continue;
                    var methodArgs = method.GetParameters();
                    if (methodArgs.Length < 1 || methodArgs.Length > 2)
                        continue;
                    var tuple = new TypeTuple(methodArgs[0].ParameterType, method.ReturnType);
                    var expr = config.CreateMapExpression(
                        tuple,
                        methodArgs.Length == 1 ? MapType.Map : MapType.MapToTarget
                    );
                    translator.VisitLambdaForGenerateMappers(
                        expr,
                        ExpressionTranslator.LambdaType.PublicMethod,
                        interfaceType,
                        method.Name,
                       _isForceInternal ?? !method.IsPublic
                    );
                }

                foreach (var prop in interfaceType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Where(x => x.IsGetterPublicOrInternal())
                        )
                {
                    if (!prop.PropertyType.IsGenericType)
                        continue;
                    if (prop.PropertyType.GetGenericTypeDefinition() != typeof(Expression<>))
                        continue;
                    var propArgs = prop.PropertyType.GetGenericArguments()[0];
                    if (!propArgs.IsGenericType)
                        continue;
                    if (propArgs.GetGenericTypeDefinition() != typeof(Func<,>))
                        continue;
                    var funcArgs = propArgs.GetGenericArguments();
                    var tuple = new TypeTuple(funcArgs[0], funcArgs[1]);
                    var expr = config.CreateMapExpression(tuple, MapType.Projection);
                    translator.VisitLambdaForGenerateMappers(
                        expr,
                        ExpressionTranslator.LambdaType.PublicLambda,
                        interfaceType,
                        prop.Name,
                        _isForceInternal ?? (!prop.GetMethod?.IsPublic ?? false)
                    );
                }
            }
        }    
    }
}