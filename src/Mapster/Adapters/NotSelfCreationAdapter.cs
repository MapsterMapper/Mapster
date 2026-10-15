namespace Mapster.Adapters
{
    /// <summary>
    /// Immitation behavior in 7.4.0 for Types that cannot be instantiated from itselves
    /// Example:  Uri,  JsonDocument
    /// </summary>
    internal class NotSelfCreationAdapter : PrimitiveAdapter
    {
        protected override int Score => -150;

        protected override bool CanMap(PreCompileArgument arg)
        {
            return !arg.ExplicitMapping && arg.SourceType == arg.DestinationType 
                && arg.DestinationType.IsNotSelfCreation(arg.DestinationType.GetFieldsAndProperties(arg)); 
        }
    }
}
