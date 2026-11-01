using be.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace be.Infrastructure.Persistence.Seed;

public static class CatalogDevelopmentSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Ensure categories exist
        var weddingCat = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "wedding-signs");
        if (weddingCat == null)
        {
            weddingCat = new Category
            {
                Name = "Wedding Signs & Arches",
                Slug = "wedding-signs",
                Description = "Luxury handcrafted welcome signs, mirror arches and entrance boards for weddings",
                SortOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Categories.Add(weddingCat);
            await context.SaveChangesAsync();
        }

        var milestoneCat = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "custom-milestones");
        if (milestoneCat == null)
        {
            milestoneCat = new Category
            {
                Name = "Custom Milestone Boards",
                Slug = "custom-milestones",
                Description = "Personalised boards for engagements, Haldi, Mehndi, Valagappu and birthdays",
                SortOrder = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Categories.Add(milestoneCat);
            await context.SaveChangesAsync();
        }

        var stationeryCat = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "event-stationery");
        if (stationeryCat == null)
        {
            stationeryCat = new Category
            {
                Name = "Event Stationery & Details",
                Slug = "event-stationery",
                Description = "Directional arrows, table numbers, place cards and luxury celebration details",
                SortOrder = 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Categories.Add(stationeryCat);
            await context.SaveChangesAsync();
        }

        // Seed products if catalog has fewer than 5 items
        if (await context.Products.CountAsync() < 5)
        {
            var seedList = new (string Name, string Slug, string ShortDesc, string Desc, decimal Price, int Stock, string Category, int CategoryId, string ImageUrl, bool IsFeatured)[]
            {
                (
                    "Sunset Arch Ceremony Mirror Sign",
                    "sunset-arch-ceremony-sign",
                    "Handcrafted frosted mirror arch welcome sign for outdoor & sunset ceremonies",
                    "Our signature arched mirror welcome sign designed with bespoke white & gold calligraphy, tailored to perfection for modern Swiss & European wedding ceremonies. Features weather-resistant lettering and frosted border finish.",
                    380.00m,
                    10,
                    "wedding",
                    weddingCat.Id,
                    "/uploads/products/2026-11/mirror-arch-sunset.jpg",
                    true
                ),
                (
                    "Traditional Kalyanam Gold Calligraphy Welcome Sign",
                    "traditional-kalyanam-gold-calligraphy-welcome-sign",
                    "Traditional Tamil wedding welcome board with gold foil typography & ceremony details",
                    "Handcrafted wedding welcome sign featuring exquisite gold script typography, Tamil ceremony blessings, and couple names. Suitable for entrance easels and temple wedding halls.",
                    320.00m,
                    8,
                    "wedding",
                    weddingCat.Id,
                    "/uploads/products/2026-11/tamil-wedding-sign.jpg",
                    true
                ),
                (
                    "Auspicious Banana Leaf Ritual Welcome Board with Thali Motif",
                    "banana-leaf-ritual-welcome-board",
                    "Traditional vazhai ilai inspired green welcome sign with auspicious Ganesha & Mangalyam motif",
                    "Honouring sacred South Indian wedding rituals, this unique welcome board features a verdant banana leaf backdrop with handcrafted sacred iconography and gold typography.",
                    290.00m,
                    12,
                    "wedding",
                    weddingCat.Id,
                    "/uploads/products/2026-11/banana-leaf-sign.jpg",
                    true
                ),
                (
                    "Happily Ever After Arched Mirror Sign with Florals",
                    "happily-ever-after-arched-mirror-sign",
                    "Full-length luxury acrylic mirror welcome arch with floral arrangement styling",
                    "Reflective gold-tinted arch mirror with custom precision-cut vinyl lettering. Makes a breathtaking first impression at venue foyers, gardens, and banquet entrances.",
                    420.00m,
                    5,
                    "wedding",
                    weddingCat.Id,
                    "/uploads/products/2026-11/mirror-arch-ceremony.jpg",
                    true
                ),
                (
                    "Kalyanam Floral Crest Reception Board",
                    "kalyanam-floral-crest-reception-board",
                    "Classic white acrylic welcome sign with botanical floral wreath and Ganesha emblem",
                    "Clean, modern white acrylic sign detailed with vibrant floral watercolours and auspicious greetings. Personalised with bride & groom names and date.",
                    280.00m,
                    15,
                    "wedding",
                    weddingCat.Id,
                    "/uploads/products/2026-11/kalyanam-welcome-board.jpg",
                    true
                ),
                (
                    "She Said Yes — Engagement & Proposal Board",
                    "she-said-yes-engagement-sign",
                    "Romantic engagement celebration sign with delicate cursive calligraphy",
                    "Commemorate your unforgettable proposal and engagement party with this chic minimalist acrylic board. Customisable with proposal coordinates and date.",
                    240.00m,
                    7,
                    "custom",
                    milestoneCat.Id,
                    "/uploads/products/2026-11/she-said-yes-sign.jpg",
                    false
                ),
                (
                    "Valagappu & Baby Shower Milestone Ceremony Board",
                    "valagappu-baby-shower-milestone-board",
                    "Traditional maternity milestone celebration welcome board with pastel motifs",
                    "Beautifully designed celebration sign for Valagappu, baby showers, and maternity blessings. Handcrafted with bespoke pastel colour palettes.",
                    220.00m,
                    10,
                    "custom",
                    milestoneCat.Id,
                    "/uploads/products/2026-11/valagappu-ceremony-board.jpg",
                    false
                ),
                (
                    "Personalised Birthday & Milestone Celebration Easel",
                    "personalised-birthday-milestone-easel",
                    "Vibrant festive welcome board for milestone birthdays and anniversaries",
                    "Custom milestone welcome board with bespoke typography, fun accents, and personalised greetings for anniversaries, milestone birthdays, and family events.",
                    195.00m,
                    10,
                    "custom",
                    milestoneCat.Id,
                    "/uploads/products/2026-11/birthday-board.jpg",
                    false
                ),
                (
                    "Wedding Venue Directional Arrow Sign",
                    "wedding-venue-directional-arrow-sign",
                    "Handmade wooden & acrylic wayfinding arrow signs for estate & garden venues",
                    "Guide your guests smoothly through your ceremony, drinks reception, and dinner with charming directional arrow signs. Weatherproof vinyl lettering on premium backing.",
                    85.00m,
                    20,
                    "wedding",
                    stationeryCat.Id,
                    "/uploads/products/2026-11/wedding-direction-arrow.jpg",
                    false
                ),
                (
                    "Table Numbers & Luxury Event Stationery Set",
                    "table-numbers-luxury-event-stationery-set",
                    "Matching frosted acrylic table numbers, place cards and bar menus",
                    "Complete cohesive stationery suite featuring frosted acrylic table numbers with natural wood stands, gold-embossed place cards, and cocktail menus.",
                    150.00m,
                    25,
                    "custom",
                    stationeryCat.Id,
                    "/uploads/products/2026-11/table-numbers-stationery.jpg",
                    false
                )
            };

            foreach (var item in seedList)
            {
                var product = new Product
                {
                    Name = item.Name,
                    Slug = item.Slug,
                    ShortDescription = item.ShortDesc,
                    Description = item.Desc,
                    Price = item.Price,
                    BasePrice = item.Price,
                    Currency = "CHF",
                    Stock = item.Stock,
                    Category = item.Category,
                    CategoryId = item.CategoryId,
                    ImageUrl = item.ImageUrl,
                    IsActive = true,
                    IsFeatured = item.IsFeatured,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.Products.Add(product);
                await context.SaveChangesAsync();

                context.ProductMedia.Add(new ProductMedia
                {
                    ProductId = product.Id,
                    StorageKey = item.ImageUrl.TrimStart('/'),
                    Url = item.ImageUrl,
                    MediaType = "image",
                    AltText = item.Name,
                    OriginalFileName = System.IO.Path.GetFileName(item.ImageUrl),
                    StorageProvider = "local",
                    IsPrimary = true,
                    SortOrder = 0,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
