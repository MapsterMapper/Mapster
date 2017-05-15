using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Shouldly;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenMappingComplexClasses
    {
        [Test, Timeout(30000)]
        public void Classes_With_Complex_Dependencies_Can_Be_Mapped_In_A_Reasonable_Time_When_PreserveReference_Set_For_One_Class()
        {
            TypeAdapterConfig<Blackberry, Blackberry>.NewConfig().PreserveReference(true);
            TypeAdapter<Blackberry, Blackberry>.Map(new Blackberry());
        }

        class Watermelon
        {
            public ICollection<Cowberry> Prop0 { get; set; }
            public ICollection<Cherry> Prop1 { get; set; }
            public ICollection<Cherry> Prop2 { get; set; }
            public ICollection<Lycium> Prop3 { get; set; }
            public ICollection<Strawberry> Prop4 { get; set; }
            public ICollection<Blackberry> Prop7 { get; set; }
            public ICollection<BogBilberry> Prop9 { get; set; }
            public ICollection<Bearberry> Prop10 { get; set; }
            public ICollection<Rowanberry> Prop11 { get; set; }
        }

        class Cowberry
        {
            public ICollection<Crowberry> Prop0 { get; set; }
            public ICollection<RedCurrant> Prop1 { get; set; }
            public ICollection<Watermelon> Prop2 { get; set; }
            public ICollection<Rowanberry> Prop3 { get; set; }
            public ICollection<Honeysuckle> Prop4 { get; set; }
            public ICollection<Crowberry> Prop6 { get; set; }
        }

        class Crowberry
        {
            public ICollection<Pigeonberry> Prop0 { get; set; }
        }

        class Pigeonberry
        {
            public ICollection<Honeysuckle> Prop0 { get; set; }
            public ICollection<Crowberry> Prop1 { get; set; }
            public ICollection<Crowberry> Prop2 { get; set; }
            public ICollection<Crowberry> Prop3 { get; set; }
        }

        class Honeysuckle
        {
            public ICollection<Cowberry> Prop0 { get; set; }
            public ICollection<Cherry> Prop1 { get; set; }
            public ICollection<Shadberry> Prop2 { get; set; }
            public ICollection<Blackberry> Prop4 { get; set; }
            public ICollection<Pigeonberry> Prop5 { get; set; }
        }

        class Cherry
        {
            public ICollection<Crowberry> Prop0 { get; set; }
            public ICollection<RedCurrant> Prop1 { get; set; }
            public ICollection<Watermelon> Prop2 { get; set; }
            public ICollection<Watermelon> Prop3 { get; set; }
            public ICollection<Rowanberry> Prop4 { get; set; }
            public ICollection<Honeysuckle> Prop5 { get; set; }
            public ICollection<Rowanberry> Prop7 { get; set; }
            public ICollection<Crowberry> Prop8 { get; set; }
        }

        class RedCurrant
        {
            public ICollection<BlackCurrant> Prop1 { get; set; }
        }

        class BlackCurrant
        {
            public ICollection<WhiteCurrant> Prop0 { get; set; }
            public ICollection<BlackChokeberry> Prop1 { get; set; }
            public ICollection<RedCurrant> Prop2 { get; set; }
        }

        class WhiteCurrant
        {
            public ICollection<BlackCurrant> Prop0 { get; set; }
        }

        class BlackChokeberry
        {
            public ICollection<BlackCurrant> Prop0 { get; set; }
        }

        class Rowanberry
        {
            public ICollection<MayApple> Prop0 { get; set; }
            public ICollection<Hip> Prop1 { get; set; }
            public ICollection<Watermelon> Prop2 { get; set; }
            public ICollection<Silverweed> Prop3 { get; set; }
            public ICollection<Guelder> Prop4 { get; set; }
        }

        class MayApple
        {
            public ICollection<Bearberry> Prop0 { get; set; }
            public ICollection<Ephedra> Prop1 { get; set; }
            public ICollection<Cloudberry> Prop2 { get; set; }
            public ICollection<Rowanberry> Prop3 { get; set; }
        }

        class Bearberry
        {
            public ICollection<MayApple> Prop0 { get; set; }
            public ICollection<Bilberry> Prop1 { get; set; }
            public ICollection<Watermelon> Prop2 { get; set; }
            public ICollection<Guelder> Prop3 { get; set; }
        }

        class Bilberry
        {
            public ICollection<SeaBuckthorn> Prop0 { get; set; }
            public ICollection<Bearberry> Prop1 { get; set; }
        }

        class SeaBuckthorn
        {
            public ICollection<Crowberry> Prop0 { get; set; }
            public ICollection<RedCurrant> Prop1 { get; set; }
            public ICollection<Crowberry> Prop3 { get; set; }
            public ICollection<Bilberry> Prop4 { get; set; }
            public ICollection<Hip> Prop5 { get; set; }
            public ICollection<Hip> Prop6 { get; set; }
        }

        class Hip
        {
            public ICollection<SeaBuckthorn> Prop0 { get; set; }
            public ICollection<SeaBuckthorn> Prop1 { get; set; }
            public ICollection<Rowanberry> Prop2 { get; set; }
        }

        class Guelder
        {
            public ICollection<MayApple> Prop0 { get; set; }
            public ICollection<Ephedra> Prop1 { get; set; }
        }

        class Ephedra
        {
            public ICollection<MayApple> Prop0 { get; set; }
        }

        class Cloudberry
        {
            public ICollection<MayApple> Prop0 { get; set; }
            public ICollection<Raspberry> Prop1 { get; set; }
        }

        class Raspberry
        {
            public ICollection<Cloudberry> Prop0 { get; set; }
        }

        class Silverweed
        {
            public ICollection<Rowanberry> Prop0 { get; set; }
            public ICollection<Gooseberry> Prop1 { get; set; }
        }

        class Gooseberry
        {
            public ICollection<Cranberry> Prop0 { get; set; }
            public ICollection<Gooseberry> Prop1 { get; set; }
            public ICollection<Gooseberry> Prop2 { get; set; }
            public ICollection<Shadberry> Prop3 { get; set; }
            public ICollection<Silverweed> Prop4 { get; set; }
            public ICollection<BogBilberry> Prop9 { get; set; }
            public ICollection<Lycium> Prop10 { get; set; }
            public ICollection<Guelder> Prop11 { get; set; }
        }

        class Cranberry
        {
            public ICollection<Strawberry> Prop0 { get; set; }
            public ICollection<Gooseberry> Prop1 { get; set; }
            public ICollection<BogBilberry> Prop2 { get; set; }
            public ICollection<Guelder> Prop4 { get; set; }
        }

        class Strawberry
        {
            public ICollection<Cranberry> Prop0 { get; set; }
            public ICollection<Watermelon> Prop1 { get; set; }
            public ICollection<Shadberry> Prop2 { get; set; }
        }

        class Shadberry
        {
            public ICollection<Gooseberry> Prop0 { get; set; }
            public ICollection<Strawberry> Prop1 { get; set; }
            public ICollection<Honeysuckle> Prop2 { get; set; }
            public ICollection<WildStrawberry> Prop3 { get; set; }
            public ICollection<BogBilberry> Prop4 { get; set; }
        }

        class WildStrawberry
        {
            public ICollection<Shadberry> Prop1 { get; set; }
        }

        class BogBilberry
        {
            public ICollection<Watermelon> Prop0 { get; set; }
            public ICollection<Foxberry> Prop1 { get; set; }
            public ICollection<Lycium> Prop2 { get; set; }
            public ICollection<Cranberry> Prop3 { get; set; }
            public ICollection<Watermelon> Prop4 { get; set; }
            public ICollection<Gooseberry> Prop5 { get; set; }
            public ICollection<Shadberry> Prop6 { get; set; }
            public ICollection<Blackberry> Prop7 { get; set; }
        }

        class Foxberry
        {
            public ICollection<BogBilberry> Prop0 { get; set; }
        }

        class Lycium
        {
            public ICollection<Watermelon> Prop0 { get; set; }
            public ICollection<Rowanberry> Prop1 { get; set; }
            public ICollection<Blackberry> Prop2 { get; set; }
            public ICollection<BogBilberry> Prop4 { get; set; }
            public ICollection<Gooseberry> Prop6 { get; set; }
        }

        class Blackberry
        {
            public ICollection<Watermelon> Prop0 { get; set; }
            public ICollection<Honeysuckle> Prop1 { get; set; }
            public ICollection<SweetCherry> Prop2 { get; set; }
            public ICollection<Lycium> Prop3 { get; set; }
            public ICollection<BogBilberry> Prop4 { get; set; }
            public ICollection<Guelder> Prop6 { get; set; }
        }

        class SweetCherry
        {
            public ICollection<Blackberry> Prop0 { get; set; }
        }
    }
}
