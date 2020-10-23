using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Mapster.Utils;

namespace Mapster
{
    public class CompileArgument
    {
        public Type SourceType { get; set; }
        public Type DestinationType { get; set; }
        public MapType MapType { get; set; }
        public bool ExplicitMapping { get; set; }
        public TypeAdapterSettings Settings { get; set; }
        public CompileContext Context { get; set; }
        public bool UseDestinationValue { get; set; }

        private HashSet<string>? _srcNames;
        internal HashSet<string> GetSourceNames()
        {
            return _srcNames ??= (from it in Settings.Resolvers
                where it.SourceMemberName != null
                select it.SourceMemberName!.Split('.').First()).ToHashSet();
        }

        private HashSet<string>? _destNames;
        internal HashSet<string> GetDestinationNames()
        {
            return _destNames ??= (from it in Settings.Resolvers
                where it.DestinationMemberName != null
                select it.DestinationMemberName.Split('.').First()).ToHashSet();
        }

        private bool _fetchConstructUsing;
        private LambdaExpression? _constructUsing;
        internal LambdaExpression? GetConstructUsing()
        {
            if (!_fetchConstructUsing)
            {
                _constructUsing = Settings.ConstructUsingFactory?.Invoke(this);
                _fetchConstructUsing = true;
            }
            return _constructUsing;
        }
    }
}