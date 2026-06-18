using API.RequestHelpers;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class ProductsController(IUnitOfWork unit) : BaseApiController
{
    [Cached(100000)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetProducts([FromQuery] ProductSpecParams productParams)
    {
        var spec = new ProductSpecification(productParams);

        return await CreatePagedResult(unit.Repository<Product>(), spec,
            productParams.PageIndex, productParams.PageSize);
    }

    [Cached(100000)]
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);

        if (product == null) return NotFound();

        return product;
    }

    [InvalidateCache("api/products|")]
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        unit.Repository<Product>().Add(product);

        if (await unit.Complete())
        {
            return CreatedAtAction("GetProduct", new { id = product.Id }, product);
        }
        ;

        return BadRequest("Problem creating product");
    }

    [InvalidateCache("api/products|")]
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, Product product)
    {
        if (id != product.Id || !ProductExists(id)) return BadRequest("Cannot update this product");

        unit.Repository<Product>().Update(product);

        if (await unit.Complete())
        {
            return NoContent();
        }
        ;

        return BadRequest("Problem updating the product");
    }

    [InvalidateCache("api/products|")]
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);

        if (product == null) return NotFound();

        unit.Repository<Product>().Remove(product);

        if (await unit.Complete())
        {
            return NoContent();
        }
        ;

        return BadRequest("Problem deleting the product");
    }

    [HttpGet("stocks")]
    public async Task<ActionResult<Dictionary<int, int>>> GetStocks([FromQuery] List<int> ids)
    {
        var stocks = new Dictionary<int, int>();
        foreach (var id in ids)
        {
            var product = await unit.Repository<Product>().GetByIdAsync(id);
            stocks[id] = product?.QuantityInStock ?? 0;
        }
        return Ok(stocks);
    }

    [Cached(100000)]
    [HttpGet("{id}/related")]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetRelatedProducts(int id)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);
        if (product == null) return NotFound();

        var spec = new RelatedProductSpecification(id, product.Brand, product.Type);
        var related = await unit.Repository<Product>().ListAsync(spec);

        return Ok(related);
    }

    [Cached(100000)]
    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetBrands()
    {
        var spec = new BrandListSpecification();

        return Ok(await unit.Repository<Product>().ListAsync(spec));
    }

    [Cached(100000)]
    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetTypes()
    {
        var spec = new TypeListSpecification();

        return Ok(await unit.Repository<Product>().ListAsync(spec));
    }

    private bool ProductExists(int id)
    {
        return unit.Repository<Product>().Exists(id);
    }
}