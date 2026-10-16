using be.Api.Files;
using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be.Application.Common;
using be.Application.Features.Catalog;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/admin/products")]
[Authorize(Policy = AppPolicies.ManageCatalog)]
public sealed class AdminProductsController(
        ProductManagementService productManagementService,
        ProductVariantService productVariantService,
        ProductMediaService productMediaService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminProductResponse>>> GetProducts([FromQuery] AdminProductQuery query)
    {
        return Ok(await productManagementService.GetProducts(query));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AdminProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductResponse>> GetProduct(int id)
    {
        return Ok(await productManagementService.GetProduct(id));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AdminProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminProductResponse>> CreateProduct(AdminProductRequest request)
    {
        var result = await productManagementService.CreateProduct(request);
        return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AdminProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductResponse>> UpdateProduct(int id, AdminProductRequest request)
    {
        return Ok(await productManagementService.UpdateProduct(id, request));
    }

    [HttpPost("{id:int}/archive")]
    public async Task<IActionResult> ArchiveProduct(int id)
    {
        await productManagementService.ArchiveProduct(id);
        return NoContent();
    }

    [HttpPost("{id:int}/reactivate")]
    public async Task<IActionResult> ReactivateProduct(int id)
    {
        await productManagementService.ReactivateProduct(id);
        return NoContent();
    }

    [HttpGet("{id:int}/variants")]
    public async Task<ActionResult<IReadOnlyList<ProductVariantResponse>>> GetVariants(int id)
    {
        return Ok(await productVariantService.GetVariants(id));
    }

    [HttpPost("{id:int}/variants")]
    public async Task<ActionResult<ProductVariantResponse>> CreateVariant(int id, ProductVariantRequest request)
    {
        var result = await productVariantService.CreateVariant(id, request);
        return CreatedAtAction(nameof(GetVariants), new { id }, result);
    }

    [HttpPut("{productId:int}/variants/{variantId:int}")]
    public async Task<ActionResult<ProductVariantResponse>> UpdateVariant(int productId, int variantId, ProductVariantRequest request)
    {
        return Ok(await productVariantService.UpdateVariant(productId, variantId, request));
    }

    [HttpPost("{productId:int}/variants/{variantId:int}/archive")]
    public async Task<IActionResult> ArchiveVariant(int productId, int variantId)
    {
        await productVariantService.ArchiveVariant(productId, variantId);
        return NoContent();
    }

    [HttpPost("{productId:int}/variants/{variantId:int}/reactivate")]
    public async Task<IActionResult> ReactivateVariant(int productId, int variantId)
    {
        await productVariantService.ReactivateVariant(productId, variantId);
        return NoContent();
    }

    [HttpGet("{id:int}/media")]
    public async Task<ActionResult<IReadOnlyList<ProductMediaResponse>>> GetProductMedia(int id)
    {
        return Ok(await productMediaService.GetProductMedia(id));
    }

    [HttpPost("{id:int}/media")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ProductMediaResponse>> UploadProductMedia(int id, IFormFile file, [FromForm] string? altText, [FromForm] int sortOrder = 0, [FromForm] bool isPrimary = false)
    {
        var result = await productMediaService.UploadProductMedia(id, file.ToMediaUpload(), altText, sortOrder, isPrimary);
        return CreatedAtAction(nameof(GetProductMedia), new { id }, result);
    }

    [HttpPut("{productId:int}/media/{mediaId:int}")]
    public async Task<ActionResult<ProductMediaResponse>> UpdateProductMedia(int productId, int mediaId, ProductMediaUpdateRequest request)
    {
        return Ok(await productMediaService.UpdateProductMedia(productId, mediaId, request));
    }

    [HttpDelete("{productId:int}/media/{mediaId:int}")]
    public async Task<IActionResult> RemoveProductMedia(int productId, int mediaId)
    {
        await productMediaService.RemoveProductMedia(productId, mediaId);
        return NoContent();
    }

    [HttpPost("{id:int}/stock-adjustments")]
    [Authorize(Policy = AppPolicies.ManageInventory)]
    public IActionResult AdjustProductStock(int id, StockAdjustmentRequest request) => Problem(statusCode: 410, title: "Use /admin/inventory/{id}/adjustments with expected stock and a reason.");
}
