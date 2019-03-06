using Mapster.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mapster
{
    public class CompileArgument
    {
        public Type SourceType;
        public Type DestinationType;
        public MapType MapType;
        public bool ExplicitMapping;
        public TypeAdapterSettings Settings;
        public CompileContext Context;

        private HashSet<string> _srcNames;
        internal HashSet<string> GetSourceNames()
        {
            return _srcNames ?? (_srcNames = (from it in Settings.Resolvers
                                              where it.SourceMemberName != null
                                              select it.SourceMemberName.Split('.').First()).ToHashSet());
        }

        private HashSet<string> _destNames;
        internal HashSet<string> GetDestinationNames()
        {
            return _destNames ?? (_destNames = (from it in Settings.Resolvers
                                                where it.DestinationMemberName != null
                                                select it.DestinationMemberName.Split('.').First()).ToHashSet());
        }

        private bool _fetchConstructUsing;
        private LambdaExpression _constructUsing;
        internal LambdaExpression GetConstructUsing()
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