using System;
using System.Collections.Generic;
using System.Text;
using Shouldly;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingComplexClasses
    {
        [TestMethod, Timeout(30000)]
        public void Classes_With_Complex_Dependencies_Can_Be_Mapped_In_A_Reasonable_Time_When_PreserveReference_Set()
        {
            var config = new TypeAdapterConfig();
            config.Default.PreserveReference(true);
            new Blackberry().Adapt<Blackberry, Blackberry>(config);
        }

        [TestMethod, Timeout(30000)]
        public void Classes_With_Complex_Dependencies_Can_Be_Mapped_In_A_Reasonable_Time_When_AvoidInlineMapping_Set()
        {
            var config = new TypeAdapterConfig();
            config.Default.AvoidInlineMapping(true);
            new Blackberry().Adapt<Blackberry, Blackberry>(config);
        }

        class Watermelon
        {
            public Cowberry Prop0 { get; set; }
            public Cherry Prop1 { get; set; }
            public Cherry Prop2 { get; set; }
            public Lycium Prop3 { get; set; }
            public Strawberry Prop4 { get; set; }
            public Blackberry Prop7 { get; set; }
            public BogBilberry Prop9 { get; set; }
            public Bearberry Prop10 { get; set; }
            public Rowanberry Prop11 { get; set; }
        }

        class Cowberry
        {
            public Crowberry Prop0 { get; set; }
            public RedCurrant Prop1 { get; set; }
            public Watermelon Prop2 { get; set; }
            public Rowanberry Prop3 { get; set; }
            public Honeysuckle Prop4 { get; set; }
            public Crowberry Prop6 { get; set; }
        }

        class Crowberry
        {
            public Pigeonberry Prop0 { get; set; }
        }

        class Pigeonberry
        {
            public Honeysuckle Prop0 { get; set; }
            public Crowberry Prop1 { get; set; }
            public Crowberry Prop2 { get; set; }
            public Crowberry Prop3 { get; set; }
        }

        class Honeysuckle
        {
            public Cowberry Prop0 { get; set; }
            public Cherry Prop1 { get; set; }
            public Shadberry Prop2 { get; set; }
            public Blackberry Prop4 { get; set; }
            public Pigeonberry Prop5 { get; set; }
        }

        class Cherry
        {
            public Crowberry Prop0 { get; set; }
            public RedCurrant Prop1 { get; set; }
            public Watermelon Prop2 { get; set; }
            public Watermelon Prop3 { get; set; }
            public Rowanberry Prop4 { get; set; }
            public Honeysuckle Prop5 { get; set; }
            public Rowanberry Prop7 { get; set; }
            public Crowberry Prop8 { get; set; }
        }

        class RedCurrant
        {
            public BlackCurrant Prop1 { get; set; }
        }

        class BlackCurrant
        {
            public WhiteCurrant Prop0 { get; set; }
            public BlackChokeberry Prop1 { get; set; }
            public RedCurrant Prop2 { get; set; }
        }

        class WhiteCurrant
        {
            public BlackCurrant Prop0 { get; set; }
        }

        class BlackChokeberry
        {
            public BlackCurrant Prop0 { get; set; }
        }

        class Rowanberry
        {
            public MayApple Prop0 { get; set; }
            public Hip Prop1 { get; set; }
            public Watermelon Prop2 { get; set; }
            public Silverweed Prop3 { get; set; }
            public Guelder Prop4 { get; set; }
        }

        class MayApple
        {
            public Bearberry Prop0 { get; set; }
            public Ephedra Prop1 { get; set; }
            public Cloudberry Prop2 { get; set; }
            public Rowanberry Prop3 { get; set; }
        }

        class Bearberry
        {
            public MayApple Prop0 { get; set; }
            public Bilberry Prop1 { get; set; }
            public Watermelon Prop2 { get; set; }
            public Guelder Prop3 { get; set; }
        }

        class Bilberry
        {
            public SeaBuckthorn Prop0 { get; set; }
            public Bearberry Prop1 { get; set; }
        }

        class SeaBuckthorn
        {
            public Crowberry Prop0 { get; set; }
            public RedCurrant Prop1 { get; set; }
            public Crowberry Prop3 { get; set; }
            public Bilberry Prop4 { get; set; }
            public Hip Prop5 { get; set; }
            public Hip Prop6 { get; set; }
        }

        class Hip
        {
            public SeaBuckthorn Prop0 { get; set; }
            public SeaBuckthorn Prop1 { get; set; }
            public Rowanberry Prop2 { get; set; }
        }

        class Guelder
        {
            public MayApple Prop0 { get; set; }
            public Ephedra Prop1 { get; set; }
        }

        class Ephedra
        {
            public MayApple Prop0 { get; set; }
        }

        class Cloudberry
        {
            public MayApple Prop0 { get; set; }
            public Raspberry Prop1 { get; set; }
        }

        class Raspberry
        {
            public Cloudberry Prop0 { get; set; }
        }

        class Silverweed
        {
            public Rowanberry Prop0 { get; set; }
            public Gooseberry Prop1 { get; set; }
        }

        class Gooseberry
        {
            public Cranberry Prop0 { get; set; }
            public Gooseberry Prop1 { get; set; }
            public Gooseberry Prop2 { get; set; }
            public Shadberry Prop3 { get; set; }
            public Silverweed Prop4 { get; set; }
            public BogBilberry Prop9 { get; set; }
            public Lycium Prop10 { get; set; }
            public Guelder Prop11 { get; set; }
        }

        class Cranberry
        {
            public Strawberry Prop0 { get; set; }
            public Gooseberry Prop1 { get; set; }
            public BogBilberry Prop2 { get; set; }
            public Guelder Prop4 { get; set; }
        }

        class Strawberry
        {
            public Cranberry Prop0 { get; set; }
            public Watermelon Prop1 { get; set; }
            public Shadberry Prop2 { get; set; }
        }

        class Shadberry
        {
            public Gooseberry Prop0 { get; set; }
            public Strawberry Prop1 { get; set; }
            public Honeysuckle Prop2 { get; set; }
            public WildStrawberry Prop3 { get; set; }
            public BogBilberry Prop4 { get; set; }
        }

        class WildStrawberry
        {
            public Shadberry Prop1 { get; set; }
        }

        class BogBilberry
        {
            public Watermelon Prop0 { get; set; }
            public Foxberry Prop1 { get; set; }
            public Lycium Prop2 { get; set; }
            public Cranberry Prop3 { get; set; }
            public Watermelon Prop4 { get; set; }
            public Gooseberry Prop5 { get; set; }
            public Shadberry Prop6 { get; set; }
            public Blackberry Prop7 { get; set; }
        }

        class Foxberry
        {
            public BogBilberry Prop0 { get; set; }
        }

        class Lycium
        {
            public Watermelon Prop0 { get; set; }
            public Rowanberry Prop1 { get; set; }
            public Blackberry Prop2 { get; set; }
            public BogBilberry Prop4 { get; set; }
            public Gooseberry Prop6 { get; set; }
        }

        class Blackberry
        {
            public Watermelon Prop0 { get; set; }
            public Honeysuckle Prop1 { get; set; }
            public SweetCherry Prop2 { get; set; }
            public Lycium Prop3 { get; set; }
            public BogBilberry Prop4 { get; set; }
            public Guelder Prop6 { get; set; }
        }

        class SweetCherry
        {
            public Blackberry Prop0 { get; set; }
        }
    }
}