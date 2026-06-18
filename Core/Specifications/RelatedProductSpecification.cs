using Core.Entities;

namespace Core.Specifications;

public class RelatedProductSpecification : BaseSpecification<Product>
{
    public RelatedProductSpecification(int productId, string brand, string type, int take = 5)
        : base(x => x.Id != productId && (x.Brand == brand || x.Type == type))
    {
        ApplyPaging(0, take);
    }
}
