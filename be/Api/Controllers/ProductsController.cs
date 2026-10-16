using be.Application.Features.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/products")]
public class ProductsController(ProductQueries service) : ControllerBase
{
    // GET: api/v1/products
    [HttpGet]
    public async Task<ActionResult<ProductListResponse>> GetProducts(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? categoryId = null,
        [FromQuery] int? collectionId = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] bool? inStock = null,
        [FromQuery] bool? featured = null,
        [FromQuery] string sort = "newest")
    {
        return Ok(await service.GetProducts(category, search, pageNumber, pageSize, categoryId, collectionId, minPrice, maxPrice, inStock, featured, sort));
    }

    // GET: api/v1/products/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponse>> GetProduct(int id)
    {
        return Ok(await service.GetProduct(id));
    }
}
