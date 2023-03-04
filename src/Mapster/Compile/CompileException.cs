using System;
using System.Linq.Expressions;

namespace Mapster
{
    public class CompileException : InvalidOperationException
    {
        public CompileException(CompileArgument argument, Exception innerException) : base(null, innerException)
        {
            Argument = argument;
        }

        public override string Message => 
            "Error while compiling\n" +
            $"source={Argument.SourceType}\n" +
            $"destination={Argument.DestinationType}\n" +
            $"type={Argument.MapType}";

        public CompileArgument Argument { get; }
        public LambdaExpression? Expression { get; internal set; }
    }
}
