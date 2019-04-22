using System;
using System.Linq.Expressions;

namespace Mapster
{
    public class CompileException : InvalidOperationException
    {
        public CompileException(CompileArgument argument, Exception innerException) : base(null, innerException)
        {
            this.Argument = argument;
        }

        public override string Message => 
            "Error while compiling\n" +
            $"source={this.Argument.SourceType}\n" +
            $"destination={this.Argument.DestinationType}\n" +
            $"type={this.Argument.MapType}";

        public CompileArgument Argument { get; }
        public LambdaExpression? Expression { get; internal set; }
    }
}
