namespace Mapster.Models
{
    public class AdapterModel<TSource, TDestination>
    {
        public FieldModel[] Fields;
        public PropertyModel<TSource, TDestination>[] Properties;
    }
}
