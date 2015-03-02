using System;
using System.Collections.Generic;

namespace Mapster.Tests.Classes
{
    public class TypeTestClassA
    {
        public int A { get; set; }
        public int B { get; set; }
        public Nullable<double> C { get; set; }
    }

    public class TypeTestClassB
    {
        public double? A { get; set; }
        public int B { get; set; }
        public decimal C { get; set; }
    }

    public class ConfigTestClassA
    {
        public int A { get; set; }
        public string B { get; set; }
        public Nullable<double> C { get; set; }
    }

    public class ConfigTestClassB
    {
        public double? A { get; set; }
        public int B { get; set; }
        public string C { get; set; }
    }

    public class MaxDepthTestSource
    {
        public string Name { get; set; }
        public MaxDepthTestDest Source { get; set; }
    }

    public class MaxDepthTestDest
    {
        public string Name { get; set; }
        public MaxDepthTestSource Source { get; set; }
    }

    public class MaxDepthTestSourceDTO
    {
        public string Name { get; set; }
        public MaxDepthTestDestDTO Source { get; set; }
    }

    public class MaxDepthTestDestDTO
    {
        public string Name { get; set; }
        public MaxDepthTestSourceDTO Source { get; set; }
    }


    public class MaxDepthTestListSource
    {
        public string Name { get; set; }
        public ICollection<MaxDepthTestListDest> Source { get; set; }
    }

    public class MaxDepthTestListDest
    {
        public string Name { get; set; }
        public ICollection<MaxDepthTestListSource> Source { get; set; }
    }

    public class MaxDepthTestListSourceDTO
    {
        public string Name { get; set; }
        public IEnumerable<MaxDepthTestDestDTO> Source { get; set; }
    }

    public class MaxDepthTestListDestDTO
    {
        public string Name { get; set; }
        public IEnumerable<MaxDepthTestSourceDTO> Source { get; set; }
    }

    public class DefaultMaxDepthTestSource
    {
        public string Name { get; set; }
        public DefaultMaxDepthTestDest Source { get; set; }
    }

    public class DefaultMaxDepthTestDest
    {
        public string Name { get; set; }
        public DefaultMaxDepthTestSource Source { get; set; }
    }

    public class DefaultMaxDepthTestSourceDTO
    {
        public string Name { get; set; }
        public DefaultMaxDepthTestDestDTO Source { get; set; }
    }

    public class DefaultMaxDepthTestDestDTO
    {
        public string Name { get; set; }
        public DefaultMaxDepthTestSourceDTO Source { get; set; }
    }

    public class TestNewInstanceA
    {
        public string Name { get; set; }
        public TestNewInstanceC Child { get; set; }
    }

    public class TestNewInstanceB
    {
        public string Name { get; set; }
        public TestNewInstanceC Child { get; set; }
    }

    public class TestNewInstanceC
    {
        public string Name { get; set; }
    }


    public class TestNewInstanceD
    {
        public string Name { get; set; }
        public TestNewInstanceF Child { get; set; }
    }

    public class TestNewInstanceE
    {
        public string Name { get; set; }
        public TestNewInstanceF Child { get; set; }
    }

    public class TestNewInstanceF
    {
        public string Name { get; set; }
    }
}
