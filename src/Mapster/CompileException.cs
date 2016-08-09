using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Mapster
{
    public class CompileException : InvalidOperationException
    {
        public CompileException() { }

        public CompileException(CompileArgument argument, Exception innerException) : base(null, innerException)
        {
            this.Argument = argument;
        }

        protected CompileException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public override string Message => 
            "Error while compiling\n" +
            $"source={this.Argument.SourceType}\n" +
            $"destination={this.Argument.DestinationType}\n" +
            $"type={this.Argument.MapType}";

        public CompileArgument Argument { get; }
        public LambdaExpression Expression { get; internal set; }
    }
}
