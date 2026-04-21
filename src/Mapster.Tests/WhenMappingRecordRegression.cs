using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text.Json;
using static Mapster.Tests.WhenExplicitMappingRequired;
using System.Linq.Expressions;
using System.Reflection;
using static Mapster.Tests.WhenMappingDerived;

namespace Mapster.Tests
{
	/// <summary>
	/// Tests for https://github.com/MapsterMapper/Mapster/issues/537
	/// </summary>
	[TestClass]
	public class WhenMappingRecordRegression
	{
		/// <summary>
		/// Gets the full expression tree string via the internal DebugView property.
		/// Body.ToString() only shows a summary for block expressions and hides
		/// backing field references. DebugView shows the complete tree.
		/// </summary>
		private static string GetExpressionDebugView(Expression expression)
		{
			var prop = typeof(Expression).GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic);
			return prop?.GetValue(expression) as string ?? expression.ToString();
		}

		[TestMethod]
		public void AdaptRecordToRecord()
		{
			TypeAdapterConfig<TestRecordY, TestRecordY>
				.NewConfig()
				.Ignore(dest => dest.Y);

			var _source = new TestRecord() { X = 700 };
            var _destination = new TestRecordY() { X = 500 , Y = 200 };

			var _destination2 = new TestRecordY() { X = 300, Y = 400 };
			var _result = _source.Adapt(_destination);

			var result2 = _destination.Adapt(_destination2);

			_result.X.ShouldBe(700);
			_result.Y.ShouldBe(200);
			object.ReferenceEquals(_result, _destination).ShouldBeFalse();
		}

		[TestMethod]
		public void AdaptPositionalRecordToPositionalRecord()
		{
			var _sourcePositional = new TestRecordPositional(600);
			var _destinationPositional = new TestRecordPositional(900);
			var _positionalResult = _sourcePositional.Adapt(_destinationPositional);

			_positionalResult.X.ShouldBe(600);
			object.ReferenceEquals(_destinationPositional, _positionalResult).ShouldBeFalse();
		}

		[TestMethod]
		public void AdaptRecordStructToRecordStruct()
		{
			var _sourceStruct = new TestRecordStruct() { X = 1000 };
			var _destinationStruct = new TestRecordStruct() { X = 800 };
			var _structResult = _sourceStruct.Adapt(_destinationStruct);

			_structResult.X.ShouldBe(1000);
			_destinationStruct.X.Equals(_structResult.X).ShouldBeFalse();
		}

		[TestMethod]
		public void AdaptRecordToClass()
		{
            var _sourсe = new TestRecordPositional(200);
			var _destination = new TestClassProtectedCtr(400);
            var _result = _sourсe.Adapt(_destination);

			_destination.ShouldBeOfType<TestClassProtectedCtr>();
			_destination.X.ShouldBe(200);
			object.ReferenceEquals(_destination, _result).ShouldBeTrue();
		}

		[TestMethod]
		public void AdaptClassToRecord()
		{
            var _sourсe = new TestClassProtectedCtr(200);
			var _destination = new TestRecordPositional(400);
            var _result = _sourсe.Adapt(_destination);

			_destination.ShouldBeOfType<TestRecordPositional>();
			_result.X.ShouldBe(200);
			object.ReferenceEquals(_destination, _result).ShouldBeFalse();
		}

		[TestMethod]
		public void AdaptToSealtedRecord()
		{
			var _sourceRecord = new TestRecord() { X = 2000 };
			var _destinationSealtedRecord = new TestSealedRecord() { X = 3000 };
			var _RecordResult = _sourceRecord.Adapt(_destinationSealtedRecord);

			_RecordResult.X.ShouldBe(2000);
			object.ReferenceEquals(_destinationSealtedRecord, _RecordResult).ShouldBeFalse();
		}

		[TestMethod]
		public void AdaptToSealtedPositionalRecord()
		{
			var _sourceRecord = new TestRecord() { X = 2000 };
			var _destinationSealtedPositionalRecord = new TestSealedRecordPositional(4000);
			var _RecordResult = _sourceRecord.Adapt(_destinationSealtedPositionalRecord);

			_RecordResult.X.ShouldBe(2000);
			object.ReferenceEquals(_destinationSealtedPositionalRecord, _RecordResult).ShouldBeFalse();
		}

		[TestMethod]
		public void AdaptClassToClassPublicCtrIsNotInstanse()
		{
			var _source = new TestClassPublicCtr(200);
			var _destination = new TestClassPublicCtr(400);
			var _result = _source.Adapt(_destination);

			_destination.ShouldBeOfType<TestClassPublicCtr>();
			_destination.X.ShouldBe(200);
			object.ReferenceEquals(_destination, _result).ShouldBeTrue();
		}

		[TestMethod]
		public void AdaptClassToClassProtectdCtrIsNotInstanse()
		{
			var _source = new TestClassPublicCtr(200);
			var _destination = new TestClassProtectedCtr(400);
			var _result = _source.Adapt(_destination);

			_destination.ShouldBeOfType<TestClassProtectedCtr>();
			_destination.X.ShouldBe(200);
			object.ReferenceEquals(_destination, _result).ShouldBeTrue();
		}

		/// <summary>
		/// https://github.com/MapsterMapper/Mapster/issues/615
		/// </summary>
		[TestMethod]
		public void AdaptClassIncludeStruct()
		{
			TypeAdapterConfig<SourceWithClass, DestinationWithStruct>
				.ForType()
				.Map(x => x.TestStruct, x => x.SourceWithStruct.TestStruct);

			var source = new SourceWithClass
			{
				SourceWithStruct = new SourceWithStruct
				{
					TestStruct = new TestStruct("A")
				}
			};

			var destination = source.Adapt<DestinationWithStruct>();
			destination.TestStruct.Property.ShouldBe("A");
		}

		/// <summary>
		/// https://github.com/MapsterMapper/Mapster/issues/482
		/// </summary>
		[TestMethod]
		public void AdaptClassToClassFromPrivatePropertyIsNotInstanse()
		{
			var _source = new TestClassPublicCtr(200);
			var _destination = new TestClassProtectedCtrPrivateProperty(400, "Me");
			var _result = _source.Adapt(_destination);

			_destination.ShouldBeOfType<TestClassProtectedCtrPrivateProperty>();
			_destination.X.ShouldBe(200);
			_destination.Name.ShouldBe("Me");
			object.ReferenceEquals(_destination, _result).ShouldBeTrue();
		}

		/// <summary>
		/// https://github.com/MapsterMapper/Mapster/issues/427
		/// </summary>
		[TestMethod]
		public void UpdateNullable()
		{
			var _source = new UserAccount("123", "123@gmail.com", new DateTime(2023, 9, 24));
			var _update = new UpdateUser
			{
				Id = "123",
			};
			var configDate = new TypeAdapterConfig();

			configDate.ForType<UpdateUser, UserAccount>()
				.Map(dest => dest.Modified, src => new DateTime(2025, 9, 24))
				.IgnoreNullValues(true);

			_update.Adapt(_source, configDate);

			var _sourceEmailUpdate = new UserAccount("123", "123@gmail.com", new DateTime(2023, 9, 24));
			var _updateEmail = new UpdateUser
			{
				Email = "245@gmail.com",
			};

			var config = new TypeAdapterConfig();
			config.ForType<UpdateUser, UserAccount>()
				.IgnoreNullValues(true);

			var _resultEmail = _updateEmail.Adapt(_sourceEmailUpdate, config);

			_source.Id.ShouldBe("123");
			_source.Created.ShouldBe(new DateTime(2023, 9, 24));
			_source.Modified.ShouldBe(new DateTime(2025, 9, 24));
			_source.Email.ShouldBe("123@gmail.com");
			_sourceEmailUpdate.Id.ShouldBe("123");
			_sourceEmailUpdate.Created.ShouldBe(new DateTime(2023, 9, 24));
			_sourceEmailUpdate.Modified.ShouldBe(null);
			_sourceEmailUpdate.Email.ShouldBe("245@gmail.com");

		}

		/// <summary>
		/// https://github.com/MapsterMapper/Mapster/issues/569
		/// </summary>
		[TestMethod]
		public void ImplicitOperatorCurrentWorkFromClass()
		{
			var guid = Guid.NewGuid();
			var pocoWithGuid1 = new PocoWithGuid { Id = guid };
			var pocoWithId2 = new PocoWithId { Id = new Id(guid) };

			var pocoWithId1 = pocoWithGuid1.Adapt<PocoWithId>();
			var pocoWithGuid2 = pocoWithId2.Adapt<PocoWithGuid>();

			pocoWithId1.Id.ToString().Equals(guid.ToString()).ShouldBeTrue();
			pocoWithGuid2.Id.Equals(guid).ShouldBeTrue();

			var _result = pocoWithId1.Adapt(pocoWithGuid2);

			_result.Id.ToString().Equals(guid.ToString()).ShouldBeTrue(); // Guid value transmitted
			object.ReferenceEquals(_result, pocoWithGuid2).ShouldBeTrue(); // Not created new instanse from class pocoWithGuid2
			_result.ShouldBeOfType<PocoWithGuid>();

		}

		[TestMethod]
		public void DetectFakeRecord()
		{
			var _source = new TestClassPublicCtr(200);
			var _destination = new FakeRecord { X = 300 };
			var _result = _source.Adapt(_destination);
			_destination.X.ShouldBe(200);
			object.ReferenceEquals(_destination, _result).ShouldBeTrue();
		}

		[TestMethod]
		public void OnlyInlineRecordWorked()
		{
            var _sourcePoco = new InlinePoco501() { MyInt = 1 , MyString = "Hello" };
			var _sourceOnlyInitRecord = new OnlyInitRecord501 { MyInt = 2, MyString = "Hello World" };

			var _resultOnlyinitRecord = _sourcePoco.Adapt<OnlyInitRecord501>();
			var _updateResult = _sourceOnlyInitRecord.Adapt(_resultOnlyinitRecord);

			_resultOnlyinitRecord.MyInt.ShouldBe(1);
			_resultOnlyinitRecord.MyString.ShouldBe("Hello");
			_updateResult.MyInt.ShouldBe(2);
			_updateResult.MyString.ShouldBe("Hello World");
		}

		[TestMethod]
		public void MultyCtorRecordWorked()
		{
			var _sourcePoco = new InlinePoco501() { MyInt = 1, MyString = "Hello" };
            var _sourceMultyCtorRecord = new MultiCtorRecord (2, "Hello World");

			var _resultMultyCtorRecord = _sourcePoco.Adapt<MultiCtorRecord>();
			var _updateResult = _sourceMultyCtorRecord.Adapt(_resultMultyCtorRecord);

			_resultMultyCtorRecord.MyInt.ShouldBe(1);
			_resultMultyCtorRecord.MyString.ShouldBe("Hello");
			_updateResult.MyInt.ShouldBe(2);
			_updateResult.MyString.ShouldBe("Hello World");
		}

		[TestMethod]
		public void MultiCtorAndInlineRecordWorked()
		{
            var _sourcePoco = new MultiCtorAndInlinePoco() { MyInt = 1, MyString = "Hello", MyEmail = "123@gmail.com", InitData="Test"};
			var _sourceMultiCtorAndInline = new MultiCtorAndInlineRecord(2, "Hello World") { InitData = "Worked", MyEmail = "243@gmail.com" };

			var _resultMultiCtorAndInline = _sourcePoco.Adapt<MultiCtorAndInlineRecord>();
			var _updateResult = _sourceMultiCtorAndInline.Adapt(_resultMultiCtorAndInline);

			_resultMultiCtorAndInline.MyInt.ShouldBe(1);
			_resultMultiCtorAndInline.MyString.ShouldBe("Hello");
			_resultMultiCtorAndInline.MyEmail.ShouldBe("123@gmail.com");
			_resultMultiCtorAndInline.InitData.ShouldBe("Test");
			_updateResult.MyInt.ShouldBe(2);
			_updateResult.MyString.ShouldBe("Hello World");
			_updateResult.MyEmail.ShouldBe("243@gmail.com");
			_updateResult.InitData.ShouldBe("Worked");
		}


		[TestMethod]
		public void MappingInterfaceToInterface()
		{
			TypeAdapterConfig<IActivityData, IActivityDataExtentions>
				.ForType()
				.Map(dest => dest.TempLength, src => src.Temp.Length);


			var sourceBase = new SampleInterfaceClsBase
			{
				ActivityData = new SampleActivityData
				{
					Data = new SampleActivityParsedData
					{
						Steps = new List<string> { "A", "B", "C" }
					},
					Temp = "Temp data"

				}

			};
			var sourceDerived = new SampleInterfaceClsDerived
			{
				ActivityData = new SampleActivityData
				{
					Data = new SampleActivityParsedData
					{
						Steps = new List<string> { "X", "Y", "Z" }
					},
					Temp = "Update Temp data"

				}

			};

			var sourceExt = new SampleInterfaceClsExtentions
			{
				ActivityData = new SampleActivityDataExtentions
				{
					Data = new SampleActivityParsedData
					{
						Steps = new List<string> { "o", "o", "o" }
					},
					Temp = "Extentions data",
					TempLength = "Extentions data".Length

				}

			};

			var TargetBase = sourceBase.Adapt<SampleInterfaceClsBase>();
			var targetDerived = sourceDerived.Adapt<SampleInterfaceClsDerived>();
			var update = targetDerived.Adapt(TargetBase);

			var targetExtention = sourceExt.Adapt<SampleInterfaceClsExtentions>();


			var updExt = targetDerived.Adapt(targetExtention);

			targetDerived.ShouldNotBeNull();
			targetDerived.ShouldSatisfyAllConditions(
				() => targetDerived.ActivityData.ShouldBe(sourceDerived.ActivityData),
				() => update.ActivityData.ShouldBe(targetDerived.ActivityData),

                ()=> updExt.ActivityData.ShouldBe(targetExtention.ActivityData), 
				() => ((SampleActivityDataExtentions)updExt.ActivityData).Temp.ShouldBe(sourceDerived.ActivityData.Temp),
				() => ((SampleActivityDataExtentions)updExt.ActivityData).TempLength.ShouldBe(sourceDerived.ActivityData.Temp.Length),
				// IActivityData interface and all its derivatives  do not provide access to the Data property for all implementations of the SampleActivityData class,
				// so this property will not be changed by mapping 
				() => ((SampleActivityDataExtentions)updExt.ActivityData).Data.ShouldBe(((SampleActivityDataExtentions)targetExtention.ActivityData).Data)

			);

		}

		/// <summary>
		/// https://github.com/MapsterMapper/Mapster/issues/456
		/// </summary>
		[TestMethod]
		public void WhenRecordReceivedIgnoreCtorParamProcessing()
		{
            TypeAdapterConfig<UserDto456,UserRecord456>.NewConfig()
			 .Ignore(dest => dest.Name);

			TypeAdapterConfig<DtoInside, UserInside>.NewConfig()
				.Ignore(dest => dest.User);

			var userDto = new UserDto456("Amichai");
			var user = new UserRecord456("John");
			var DtoInsider = new DtoInside(userDto);
			var UserInsider = new UserInside(user, new UserRecord456("Skot"));

			var map = userDto.Adapt<UserRecord456>();
			var maptoTarget = userDto.Adapt(user);

			var MapToTargetInsider = DtoInsider.Adapt(UserInsider);

			map.Name.ShouldBeNullOrEmpty(); // Ignore is work set default value
			maptoTarget.Name.ShouldBe("John"); // Ignore is work ignored member save value from Destination
			MapToTargetInsider.User.Name.ShouldBe("John"); // Ignore is work member save value from Destination
			MapToTargetInsider.SecondName.Name.ShouldBe("Skot"); // Unmached member save value from Destination

		}

		[TestMethod]
		public void WhenRecordTypeWorksWithUseDestinationValueAndIgnoreNullValues()
		{

			TypeAdapterConfig<SourceFromTestUseDestValue, TestRecordUseDestValue>
			  .NewConfig()
			  .IgnoreNullValues(true);

			var _source = new SourceFromTestUseDestValue() { X = 300, Y = 200, Name = new StudentNameRecord() { Name = "John" } };
			var result = _source.Adapt<TestRecordUseDestValue>();

			var _sourceFromMapToTarget = new SourceFromTestUseDestValue() { A = 100, X = null, Y = null, Name = null };

			var txt1 = _sourceFromMapToTarget.BuildAdapter().CreateMapExpression<TestRecordUseDestValue>();

			var txt = _sourceFromMapToTarget.BuildAdapter().CreateMapToTargetExpression<TestRecordUseDestValue>();

			var _resultMapToTarget = _sourceFromMapToTarget.Adapt(result);

			result.A.ShouldBe(0);               // default Value - not match
			result.S.ShouldBe("Inside Data");   // is not AutoProperty not mod by source
			result.Y.ShouldBe(200);             // Y is AutoProperty value transmitted from source
			result.Name.Name.ShouldBe("John");  // transmitted from source standart method

			_resultMapToTarget.A.ShouldBe(100);
			_resultMapToTarget.X.ShouldBe(300);            // Ignore NullValues work
			_resultMapToTarget.Y.ShouldBe(200);            // Ignore NullValues work
			_resultMapToTarget.Name.Name.ShouldBe("John"); // Ignore NullValues work

		}

		/// <summary>
		/// https://github.com/MapsterMapper/Mapster/issues/771
		/// https://github.com/MapsterMapper/Mapster/issues/746
		/// </summary>
		[TestMethod]
		public void FixCtorParamMapping()
		{
			var sourceRequestPaymentDto = new PaymentDTO771("MasterCard", "1234", "12/99", "234", 12);
			var sourceRequestOrderDto = new OrderDTO771(Guid.NewGuid(), Guid.NewGuid(), "order123", sourceRequestPaymentDto);
			var db = new Database746(UserID: "256", Password: "123");


			var result = new CreateOrderRequest771(sourceRequestOrderDto).Adapt<CreateOrderCommand771>();
			var resultID = db.Adapt(new Database746());


			result.Order.Payment.CVV.ShouldBe("234");
			resultID.UserID.ShouldBe("256");
		}

		/// <summary>
		/// https://github.com/MapsterMapper/Mapster/issues/842
		/// </summary>
		[TestMethod]
		public void ClassCtorAutomapingWorking()
		{
			var source = new TestRecord() { X = 100 };
			var result = source.Adapt<AutoCtorDestX>();

			result.X.ShouldBe(100);
		}

		/// <summary>
		/// https://github.com/MapsterMapper/Mapster/issues/842
		/// </summary>
		[TestMethod]
		public void ClassCustomCtorWithMapWorking()
		{
			TypeAdapterConfig<TestRecord, AutoCtorDestYx>.NewConfig()
				.Map("y", src => src.X);


			var source = new TestRecord() { X = 100 };
			var result = source.Adapt<AutoCtorDestYx>();

			result.X.ShouldBe(100);
		}

		/// <summary>
		/// https://github.com/MapsterMapper/Mapster/issues/842
		/// </summary>
		[TestMethod]
		public void ClassCustomCtorInsiderUpdateWorking()
		{
			TypeAdapterConfig<TestRecord, AutoCtorDestYx>.NewConfig()
				.Map("y", src => src.X);

			var source = new InsiderData() { X = new TestRecord() { X = 100 } };
			var destination = new InsiderWithCtorDestYx(); // null insider
			source.Adapt(destination);

			destination.X.X.ShouldBe(100);
		}

		/// <summary>
		/// https://github.com/MapsterMapper/Mapster/issues/842
		/// </summary>
		[TestMethod]
		public void ClassUpdateAutoPropertyWitoutSetterWorking()
		{
			var source = new TestRecord() { X = 100 };
			var patch = new TestRecord() { X = 200 };
			var result = source.Adapt<AutoCtorDestX>();

			patch.Adapt(result);

			result.X.ShouldBe(200);
		}

		/// <summary>
		/// https://github.com/MapsterMapper/Mapster/issues/883
		/// </summary>
		[TestMethod]
		public void ClassCtorActivateDefaultValue()
		{
			var source = new Source833
			{
				Value1 = "123",
			};

			Should.NotThrow(() =>
			{
				var target = source.Adapt<Target833>();
				target.Value1.ShouldBe("123");
				target.Value2.ShouldBe(default);
			});
		}

		/// <summary>
		/// https://github.com/MapsterMapper/Mapster/issues/911
		/// </summary>
		[TestMethod]
		public void NotSelfCreationTypeMappingToSelfWithOutError()
		{
			var src = new Uri("https://www.google.com/");
			var srcJ = JsonDocument.Parse("{\"key\": \"value\"}");

			var result = src.Adapt<Uri>();
			var resultJ = srcJ.Adapt<JsonDocument>();

			result.ToString().ShouldBe("https://www.google.com/");
			resultJ.RootElement.GetProperty("key").ToString().ShouldBe("value");
		}


		/// <summary>
		/// Regression: MapToTarget expression for record types should not contain
		/// compiler-generated backing fields (e.g. &lt;Prop&gt;k__BackingField).
		/// Covers the case where source and destination have fully matching properties.
		/// </summary>
		[TestMethod]
		public void MapToTargetRecordShouldNotContainBackingFields()
		{
			var source = new BackingFieldSource { A = 1, B = "hello", C = 42m };
			var destination = new BackingFieldRecord { A = 0, B = "world", C = 0m };

			var mapToTargetExpr = source.BuildAdapter()
				.CreateMapToTargetExpression<BackingFieldRecord>();
			var exprString = GetExpressionDebugView(mapToTargetExpr);
			exprString.ShouldNotContain("k__BackingField");

			var mapExpr = source.BuildAdapter()
				.CreateMapExpression<BackingFieldRecord>();
			var mapExprString = GetExpressionDebugView(mapExpr);
			mapExprString.ShouldNotContain("k__BackingField");

			var result = source.Adapt(destination);
			result.A.ShouldBe(1);
			result.B.ShouldBe("hello");
			result.C.ShouldBe(42m);
			object.ReferenceEquals(result, destination).ShouldBeFalse();
		}

		/// <summary>
		/// Regression: MapToTarget expression for positional record types should not
		/// contain compiler-generated backing fields.
		/// </summary>
		[TestMethod]
		public void MapToTargetPositionalRecordShouldNotContainBackingFields()
		{
			var source = new BackingFieldSource { A = 10, B = "src", C = 99m };
			var destination = new BackingFieldPositionalRecord(0, "dest", 0m);

			var mapToTargetExpr = source.BuildAdapter()
				.CreateMapToTargetExpression<BackingFieldPositionalRecord>();
			var exprString = GetExpressionDebugView(mapToTargetExpr);
			exprString.ShouldNotContain("k__BackingField");

			var result = source.Adapt(destination);
			result.A.ShouldBe(10);
			result.B.ShouldBe("src");
			result.C.ShouldBe(99m);
		}

		/// <summary>
		/// Regression: MapToTarget with partially matching types should not contain
		/// backing fields. The destination record has extra properties not present
		/// in the source, which are restored from the destination instance.
		/// </summary>
		[TestMethod]
		public void MapToTargetPartialMatchRecordShouldNotContainBackingFields()
		{
			var source = new PartialSource { A = 1, B = "hello" };
			var destination = new PartialDestRecord { A = 0, B = "world", Extra1 = "keep", Extra2 = 99 };

			var mapToTargetExpr = source.BuildAdapter()
				.CreateMapToTargetExpression<PartialDestRecord>();
			var exprString = GetExpressionDebugView(mapToTargetExpr);
			exprString.ShouldNotContain("k__BackingField");

			var result = source.Adapt(destination);
			result.A.ShouldBe(1);
			result.B.ShouldBe("hello");
			result.Extra1.ShouldBe("keep");
			result.Extra2.ShouldBe(99);
		}

		/// <summary>
		/// Regression: MapToTarget with IgnoreNullValues and IgnoreCase name matching
		/// should not leak backing fields into the generated expression.
		/// Uses nullable source with partial match.
		/// </summary>
		[TestMethod]
		public void MapToTargetWithIgnoreNullValuesAndIgnoreCaseShouldNotContainBackingFields()
		{
			var config = new TypeAdapterConfig();
			config.Default
				.IgnoreNullValues(true)
				.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);

			var source = new NullablePartialSource { A = 1, B = null };
			var destination = new PartialDestRecord { A = 0, B = "keep", Extra1 = "preserved", Extra2 = 77 };

			var mapToTargetExpr = source.BuildAdapter(config)
				.CreateMapToTargetExpression<PartialDestRecord>();
			var exprString = GetExpressionDebugView(mapToTargetExpr);
			exprString.ShouldNotContain("k__BackingField");

			var result = source.Adapt(destination, config);
			result.A.ShouldBe(1);
			result.B.ShouldBe("keep");    // null ignored
			result.Extra1.ShouldBe("preserved");
			result.Extra2.ShouldBe(77);
		}

		/// <summary>
		/// Regression: MapToTarget with IgnoreNullValues and nullable source
		/// properties should not contain backing fields in the expression.
		/// </summary>
		[TestMethod]
		public void MapToTargetWithIgnoreNullValuesShouldNotContainBackingFields()
		{
			var config = new TypeAdapterConfig();
			config.ForType<NullableSource, BackingFieldRecord>()
				.IgnoreNullValues(true);

			var source = new NullableSource { A = 1, B = null, C = null };
			var destination = new BackingFieldRecord { A = 0, B = "keep", C = 42m };

			var mapToTargetExpr = source.BuildAdapter(config)
				.CreateMapToTargetExpression<BackingFieldRecord>();
			var exprString = GetExpressionDebugView(mapToTargetExpr);
			exprString.ShouldNotContain("k__BackingField");

			var result = source.Adapt(destination, config);
			result.A.ShouldBe(1);
			result.B.ShouldBe("keep");
			result.C.ShouldBe(42m);
		}

		/// <summary>
		/// Regression: MapToTarget with EnableNonPublicMembers should not leak
		/// backing fields into the expression.
		/// </summary>
		[TestMethod]
		public void MapToTargetWithEnableNonPublicMembersShouldNotContainBackingFields()
		{
			var config = new TypeAdapterConfig();
			config.ForType<BackingFieldSource, BackingFieldRecord>()
				.EnableNonPublicMembers(true);

			var source = new BackingFieldSource { A = 5, B = "test", C = 100m };
			var destination = new BackingFieldRecord { A = 0, B = "old", C = 0m };

			var mapToTargetExpr = source.BuildAdapter(config)
				.CreateMapToTargetExpression<BackingFieldRecord>();
			var exprString = GetExpressionDebugView(mapToTargetExpr);
			exprString.ShouldNotContain("k__BackingField");

			var result = source.Adapt(destination, config);
			result.A.ShouldBe(5);
			result.B.ShouldBe("test");
			result.C.ShouldBe(100m);
		}

		/// <summary>
		/// Regression: MapToTarget with Ignore config for a record should not
		/// include backing fields for ignored or non-ignored members.
		/// </summary>
		[TestMethod]
		public void MapToTargetWithIgnoreConfigShouldNotContainBackingFields()
		{
			var config = new TypeAdapterConfig();
			config.ForType<BackingFieldSource, BackingFieldRecord>()
				.Ignore(dest => dest.C);

			var source = new BackingFieldSource { A = 1, B = "hello", C = 42m };
			var destination = new BackingFieldRecord { A = 0, B = "world", C = 99m };

			var mapToTargetExpr = source.BuildAdapter(config)
				.CreateMapToTargetExpression<BackingFieldRecord>();
			var exprString = GetExpressionDebugView(mapToTargetExpr);
			exprString.ShouldNotContain("k__BackingField");

			var result = source.Adapt(destination, config);
			result.A.ShouldBe(1);
			result.B.ShouldBe("hello");
			result.C.ShouldBe(99m);
		}

		/// <summary>
		/// Regression: MapToTarget from a class to a sealed positional record
		/// with partially matching properties should not contain backing fields.
		/// </summary>
		[TestMethod]
		public void MapToTargetSealedPositionalRecordShouldNotContainBackingFields()
		{
			var source = new PartialSource { A = 10, B = "src" };
			var destination = new SealedPartialDestRecord(0, "dest", "extra", 77);

			var mapToTargetExpr = source.BuildAdapter()
				.CreateMapToTargetExpression<SealedPartialDestRecord>();
			var exprString = GetExpressionDebugView(mapToTargetExpr);
			exprString.ShouldNotContain("k__BackingField");

			var result = source.Adapt(destination);
			result.A.ShouldBe(10);
			result.B.ShouldBe("src");
			result.Extra1.ShouldBe("extra");
			result.Extra2.ShouldBe(77);
		}

		/// <summary>
		/// Regression: MapToTarget with settings IgnoreNullValues + IgnoreCase
		/// combined with EnableNonPublicMembers and partial match
		/// should not contain backing fields.
		/// </summary>
		[TestMethod]
		public void MapToTargetWithAllSettingsCombinedShouldNotContainBackingFields()
		{
			var config = new TypeAdapterConfig();
			config.Default
				.IgnoreNullValues(true)
				.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase)
				.EnableNonPublicMembers(true);

			var source = new NullablePartialSource { A = 5, B = "mapped" };
			var destination = new PartialDestRecord { A = 0, B = "old", Extra1 = "stay", Extra2 = 42 };

			var mapToTargetExpr = source.BuildAdapter(config)
				.CreateMapToTargetExpression<PartialDestRecord>();
			var exprString = GetExpressionDebugView(mapToTargetExpr);
			exprString.ShouldNotContain("k__BackingField");

			var result = source.Adapt(destination, config);
			result.A.ShouldBe(5);
			result.B.ShouldBe("mapped");
			result.Extra1.ShouldBe("stay");
			result.Extra2.ShouldBe(42);
		}

		/// <summary>
		/// Regression: MapToTarget with IgnoreNullValues + IgnoreCase for a
		/// positional record with extra ctor params should not contain backing fields.
		/// </summary>
		[TestMethod]
		public void MapToTargetPositionalRecordWithGlobalSettingsShouldNotContainBackingFields()
		{
			var config = new TypeAdapterConfig();
			config.Default
				.IgnoreNullValues(true)
				.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);

			var source = new NullablePartialSource { A = 10, B = null };
			var destination = new SealedPartialDestRecord(0, "dest", "extra", 77);

			var mapToTargetExpr = source.BuildAdapter(config)
				.CreateMapToTargetExpression<SealedPartialDestRecord>();
			var exprString = GetExpressionDebugView(mapToTargetExpr);
			exprString.ShouldNotContain("k__BackingField");

			var result = source.Adapt(destination, config);
			result.A.ShouldBe(10);
			// B is null from source; for positional records, IgnoreNullValues does not
			// apply to constructor parameters, so B becomes null in the new instance.
			result.B.ShouldBeNull();
			result.Extra1.ShouldBe("extra");
			result.Extra2.ShouldBe(77);
		}

		/// <summary>
		/// Regression: IgnoreNonMapped(true) adds ALL non-resolver members (including
		/// compiler-generated backing fields) to Settings.Ignore. Then
		/// RecordIngnoredWithoutConditonRestore selects members in the Ignore list
		/// and restores them from the destination — leaking backing field bindings
		/// into the generated MemberInit expression.
		/// </summary>
		[TestMethod]
		public void MapToTargetWithIgnoreNonMappedShouldNotContainBackingFields()
		{
			var config = new TypeAdapterConfig();
			config.ForType<BackingFieldSource, BackingFieldRecord>()
				.IgnoreNonMapped(true)
				.Map(dest => dest.A, src => src.A)
				.Map(dest => dest.B, src => src.B)
				.Map(dest => dest.C, src => src.C);

			var source = new BackingFieldSource { A = 1, B = "hello", C = 42m };
			var destination = new BackingFieldRecord { A = 0, B = "world", C = 0m };

			// Check the expression does not contain backing field references
			var mapToTargetExpr = source.BuildAdapter(config)
				.CreateMapToTargetExpression<BackingFieldRecord>();
			var exprString = GetExpressionDebugView(mapToTargetExpr);
			exprString.ShouldNotContain("k__BackingField");
		}

		/// <summary>
		/// Regression: IgnoreNonMapped(true) with record MapToTarget should produce
		/// correct mapped values — backing fields must not overwrite property values.
		/// Uses a separate config to avoid side-effects from expression inspection.
		/// </summary>
		[TestMethod]
		public void MapToTargetWithIgnoreNonMappedShouldProduceCorrectValues()
		{
			var config = new TypeAdapterConfig();
			config.ForType<BackingFieldSource, BackingFieldRecord>()
				.IgnoreNonMapped(true)
				.Map(dest => dest.A, src => src.A)
				.Map(dest => dest.B, src => src.B)
				.Map(dest => dest.C, src => src.C);

			var source = new BackingFieldSource { A = 1, B = "hello", C = 42m };
			var destination = new BackingFieldRecord { A = 0, B = "world", C = 0m };

			var result = source.Adapt(destination, config);
			result.A.ShouldBe(1);
			result.B.ShouldBe("hello");
			result.C.ShouldBe(42m);
		}

		/// <summary>
		/// Regression: Full reproduction of user's global settings with IgnoreNonMapped.
		/// IgnoreNullValues(true) + IgnoreNonMapped(true) + IgnoreCase with partial
		/// match and explicit Map resolvers. Expression must not contain backing fields.
		/// </summary>
		[TestMethod]
		public void MapToTargetWithAllUserGlobalSettingsExpressionShouldNotContainBackingFields()
		{
			var config = new TypeAdapterConfig();
			config.Default
				.IgnoreNullValues(true)
				.IgnoreNonMapped(true)
				.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);

			config.ForType<NullablePartialSource, PartialDestRecord>()
				.Map(dest => dest.A, src => src.A)
				.Map(dest => dest.B, src => src.B);

			var source = new NullablePartialSource { A = 5, B = null };

			var mapToTargetExpr = source.BuildAdapter(config)
				.CreateMapToTargetExpression<PartialDestRecord>();
			var exprString = GetExpressionDebugView(mapToTargetExpr);
			exprString.ShouldNotContain("k__BackingField");
		}

		/// <summary>
		/// Regression: Full reproduction of user's global settings with IgnoreNonMapped.
		/// Functional correctness — backing fields must not overwrite property values.
		/// </summary>
		[TestMethod]
		public void MapToTargetWithAllUserGlobalSettingsShouldProduceCorrectValues()
		{
			var config = new TypeAdapterConfig();
			config.Default
				.IgnoreNullValues(true)
				.IgnoreNonMapped(true)
				.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);

			config.ForType<NullablePartialSource, PartialDestRecord>()
				.Map(dest => dest.A, src => src.A)
				.Map(dest => dest.B, src => src.B);

			var source = new NullablePartialSource { A = 5, B = null };
			var destination = new PartialDestRecord { A = 0, B = "keep", Extra1 = "stay", Extra2 = 42 };

			var result = source.Adapt(destination, config);
			result.A.ShouldBe(5);
			result.B.ShouldBe("keep");    // null ignored
			result.Extra1.ShouldBe("stay");
			result.Extra2.ShouldBe(42);
		}

		/// <summary>
		/// Regression: IgnoreNonMapped(true) with a positional record destination.
		/// Backing fields of positional record properties must not leak into the
		/// MapToTarget expression.
		/// </summary>
		[TestMethod]
		public void MapToTargetPositionalRecordWithIgnoreNonMappedShouldNotContainBackingFields()
		{
			var config = new TypeAdapterConfig();
			config.Default
				.IgnoreNullValues(true)
				.IgnoreNonMapped(true)
				.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);

			config.ForType<PartialSource, SealedPartialDestRecord>()
				.Map(dest => dest.A, src => src.A)
				.Map(dest => dest.B, src => src.B);

			var source = new PartialSource { A = 10, B = "src" };
			var destination = new SealedPartialDestRecord(0, "dest", "extra", 77);

			var mapToTargetExpr = source.BuildAdapter(config)
				.CreateMapToTargetExpression<SealedPartialDestRecord>();
			var exprString = GetExpressionDebugView(mapToTargetExpr);
			exprString.ShouldNotContain("k__BackingField");

			// Use a separate config for Adapt to avoid side-effects
			var config2 = new TypeAdapterConfig();
			config2.Default
				.IgnoreNullValues(true)
				.IgnoreNonMapped(true)
				.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);

			config2.ForType<PartialSource, SealedPartialDestRecord>()
				.Map(dest => dest.A, src => src.A)
				.Map(dest => dest.B, src => src.B);

			var result = source.Adapt(destination, config2);
			result.A.ShouldBe(10);
			result.B.ShouldBe("src");
			result.Extra1.ShouldBe("extra");
			result.Extra2.ShouldBe(77);
		}

		#region NowNotWorking

		/// <summary>
		/// https://github.com/MapsterMapper/Mapster/issues/430
		/// </summary>
		[Ignore]
		[TestMethod]
		public void CollectionUpdate()
		{
			List<TestClassPublicCtr> sources = new()
			{
				new(541),
				new(234)
			};
			var destination = new List<TestClassPublicCtr>();
			var _result = sources.Adapt(destination);

			destination.Count.ShouldBe(_result.Count);
		}

		#endregion NowNotWorking

	}


	#region TestClasses

	public class BackingFieldSource
	{
		public int A { get; set; }
		public string B { get; set; }
		public decimal C { get; set; }
	}

	public record BackingFieldRecord
	{
		public int A { get; set; }
		public string B { get; set; }
		public decimal C { get; set; }
	}

	public record BackingFieldPositionalRecord(int A, string B, decimal C);

	public class PartialSource
	{
		public int A { get; set; }
		public string B { get; set; }
	}

	public record PartialDestRecord
	{
		public int A { get; set; }
		public string B { get; set; }
		public string Extra1 { get; set; }
		public int Extra2 { get; set; }
	}

	public sealed record SealedPartialDestRecord(int A, string B, string Extra1, int Extra2);

	public class NullableSource
	{
		public int? A { get; set; }
		public string? B { get; set; }
		public decimal? C { get; set; }
	}

	public class NullablePartialSource
	{
		public int? A { get; set; }
		public string? B { get; set; }
	}

	public class Source833
	{
		public required string Value1 { get; init; }
	}

	public class Target833
	{
		public Target833(string value1, string value2)
		{
			Value1 = value1;
			Value2 = value2;
		}

		public string Value1 { get; }

		public string Value2 { get; }
	}

	public sealed record Database746(
	string Server = "",
	string Name = "",
	string? UserID = null,
	string? Password = null);

	public record CreateOrderRequest771(OrderDTO771 Order);

	public record CreateOrderCommand771(OrderDTO771 Order);


	public record OrderDTO771
	(
	Guid Id,
	Guid CustomerId,
	string OrderName,
	PaymentDTO771 Payment
	);

	public record PaymentDTO771
	(
	string CardName,
	string CardNumber,
	string Expiration,
	string CVV,
	int PaymentMethod
	);

	public class Person553
	{

		public string LastName { get; set; }
		public string FirstMidName { get; set; }
	}

	public class Person554
	{
		public required int ID { get; set; }
		public string LastName { get; set; }
		public string FirstMidName { get; set; }
	}


	public class SourceFromTestUseDestValue
	{
		public int? A { get; set; }
		public int? X { get; set; }
		public int? Y { get; set; }
		public StudentNameRecord Name { get; set; }
	}


	public record TestRecordUseDestValue()
	{
		private string _s = "Inside Data";

		public int A { get; set; }
		public int X { get; set; }

		[UseDestinationValue]
		public int Y { get; }

		[UseDestinationValue]
		public string S { get => _s; }

		[UseDestinationValue]
		public StudentNameRecord Name { get; } = new StudentNameRecord() { Name = "Marta" };
	}

	public record StudentNameRecord
	{
		public string Name { get; set; }
	}

	public record TestRecordY()
	{
		public int X { get; set; }
		public int Y { get; set; }
	}

	public record UserInside(UserRecord456 User, UserRecord456 SecondName);
	public record DtoInside(UserDto456 User);

	public record UserRecord456(string Name);

	public record UserDto456(string Name);

	public interface IActivityDataExtentions : IActivityData
	{
		public int TempLength { get; set; }
	}

	public interface IActivityData : IActivityDataBase
	{
		public string Temp { get; set; }
	}

	public interface IActivityDataBase
	{

	}


	public class SampleInterfaceClsExtentions
	{
		public IActivityDataExtentions? ActivityData { get; set; }

		public SampleInterfaceClsExtentions()
		{

		}

		public SampleInterfaceClsExtentions(IActivityDataExtentions data)
		{
			SetActivityData(data);
		}

		public void SetActivityData(IActivityDataExtentions data)
		{
			ActivityData = data;
		}
	}



	public class SampleInterfaceClsBase
	{
		public IActivityDataBase? ActivityData { get; set; }

		public SampleInterfaceClsBase()
		{

		}

		public SampleInterfaceClsBase(IActivityDataBase data)
		{
			SetActivityData(data);
		}

		public void SetActivityData(IActivityDataBase data)
		{
			ActivityData = data;
		}
	}

	public class SampleInterfaceClsDerived
	{
		public IActivityData? ActivityData { get; set; }

		public SampleInterfaceClsDerived()
		{

		}

		public SampleInterfaceClsDerived(IActivityData data)
		{
			SetActivityData(data);
		}

		public void SetActivityData(IActivityData data)
		{
			ActivityData = data;
		}
	}

	public class SampleActivityDataExtentions : IActivityDataExtentions
	{
		public SampleActivityParsedData Data { get; set; }
		public string Temp { get; set; }
		public int TempLength { get; set; }
	}

	public class SampleActivityData : IActivityData
	{
		public SampleActivityParsedData Data { get; set; }
		public string Temp { get; set; }
	}

	public class SampleActivityParsedData
	{
		public List<string> Steps { get; set; } = new List<string>();
	}



	class MultiCtorAndInlinePoco
	{
		public int MyInt { get; set; }
		public string MyString { get; set; }
		public string MyEmail { get; set; }
		public string InitData { get; set; }
	}

	record MultiCtorAndInlineRecord
	{
		public MultiCtorAndInlineRecord(int myInt)
		{
			MyInt = myInt;
		}

		public MultiCtorAndInlineRecord(int myInt, string myString) : this(myInt)
		{
			MyString = myString;
		}


		public int MyInt { get; private set; }
		public string MyString { get; private set; }
		public string MyEmail { get; set; }
		public string InitData { get; init; }
	}

	record MultiCtorRecord
	{
		public MultiCtorRecord(int myInt)
		{
			MyInt = myInt;
		}

		public MultiCtorRecord(int myInt, string myString) : this(myInt)
		{
			MyString = myString;
		}

		public int MyInt { get; private set; }
		public string MyString { get; private set; }
	}

	class InlinePoco501
	{
		public int MyInt { get; set; }
		public string MyString { get; set; }
	}

	record OnlyInitRecord501
	{
		public int MyInt { get; init; }
		public string MyString { get; init; }
	}

	class PocoWithGuid
	{
		public Guid Id { get; init; }
	}

	class PocoWithId
	{
		public Id Id { get; init; }
	}

	class Id
	{
		private readonly Guid _guid;
		public Id(Guid id) => _guid = id;

		public static implicit operator Id(Guid value) => new(value);
		public static implicit operator Guid(Id value) => value._guid;

		public override string ToString() => _guid.ToString();
	}

	public class FakeRecord
	{
		protected FakeRecord(FakeRecord fake) { }
		public FakeRecord() { }

		public int X { get; set; }
	}

	class UserAccount
	{
		public UserAccount(string id, string email, DateTime created)
		{
			Id = id;
			Email = email;
			Created = created;
		}
		protected UserAccount() { }

		public string Id { get; set; }
		public string? Email { get; set; }
		public DateTime Created { get; set; }
		public DateTime? Modified { get; set; }
	}

	class UpdateUser
	{
		public string? Id { get; set; }
		public string? Email { get; set; }
		public DateTime? Created { get; set; }
		public DateTime? Modified { get; set; }
	}

	class DestinationWithStruct
	{
		public TestStruct TestStruct { get; set; }
	}

	class SourceWithClass
	{
		public SourceWithStruct SourceWithStruct { get; set; }
	}

	class SourceWithStruct
	{
		public TestStruct TestStruct { get; set; }
	}

	struct TestStruct
	{
		public string Property { get; }
		public TestStruct(string property) : this()
		{
			Property = property;
		}
	}

	class TestClassPublicCtr
	{
		public TestClassPublicCtr() { }

		public TestClassPublicCtr(int x)
		{
			X = x;
		}

		public int X { get; set; }
	}

	class TestClassProtectedCtr
	{
		protected TestClassProtectedCtr() { }

		public TestClassProtectedCtr(int x)
		{
			X = x;
		}

		public int X { get; set; }
	}

	class TestClassProtectedCtrPrivateProperty
	{
		protected TestClassProtectedCtrPrivateProperty() { }

		public TestClassProtectedCtrPrivateProperty(int x, string name)
		{
			X = x;
			Name = name;
		}

		public int X { get; private set; }

		public string Name { get; private set; }
	}

	record TestRecord()
	{
		public int X { set; get; }
	}

	record TestRecordPositional(int X);

	record struct TestRecordStruct
	{
		public int X { set; get; }
	}

	/// <summary>
	/// Different Checked Constructor Attribute From Spec
	/// https://learn.microsoft.com/ru-ru/dotnet/csharp/language-reference/proposals/csharp-9.0/records#copy-and-clone-members
	/// </summary>
	sealed record TestSealedRecord()
	{
		public int X { get; set; }
	}

	sealed record TestSealedRecordPositional(int X);

	class AutoCtorDestX
	{
		public AutoCtorDestX(int x)
		{
			X = x;
		}

		public int X { get; set; }
	}

	class AutoCtorDestYx
	{
		public AutoCtorDestYx(int y)
		{
			X = y;
		}

		public int X { get; }
	}

	class InsiderData
	{
		public TestRecord X { set; get; }
	}

	class InsiderWithCtorDestYx
	{
		public AutoCtorDestYx X { set; get; }
	}

	#endregion TestClasses
}
