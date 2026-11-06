namespace be.Application.Features.Cms;

public sealed record StoreSettingDefinition(string Key, string Label, string Group, string Description, string Format = "text", int MaxLength = 500);
public static class StoreSettingCatalog
{
    // Every entry has an existing storefront consumer; infrastructure stays in deployment configuration.
    public static IReadOnlyList<StoreSettingDefinition> All { get; } = Array.AsReadOnly(new[] {
        new StoreSettingDefinition("site.name", "Store name", "General", "Storefront navigation, home link and footer identity.", MaxLength: 120),
        new StoreSettingDefinition("contact.email", "Contact email", "Contact", "Contact page and footer email link.", "email", 254),
        new StoreSettingDefinition("contact.phone", "Contact phone", "Contact", "Contact page and footer phone link.", "tel", 80),
        new StoreSettingDefinition("contact.address", "Contact address", "Contact", "Contact page and footer address."),
        new StoreSettingDefinition("contact.hours", "Opening hours", "Contact", "Contact page business hours."),
        new StoreSettingDefinition("contact.whatsapp", "WhatsApp link", "Contact", "Contact page messaging link.", "url"),
        new StoreSettingDefinition("social.instagram", "Instagram", "Social", "Contact page and footer social link.", "url"),
        new StoreSettingDefinition("social.tiktok", "TikTok", "Social", "Contact page and footer social link.", "url"),
        new StoreSettingDefinition("social.facebook", "Facebook", "Social", "Contact page social link.", "url"),
        new StoreSettingDefinition("announcement.text", "Announcement", "Storefront", "Announcement displayed above the storefront navigation."),
        new StoreSettingDefinition("announcement.url", "Announcement link", "Storefront", "Optional destination for the announcement.", "link"),
        new StoreSettingDefinition("delivery.note", "Delivery note", "Storefront", "Home page trust information and product delivery note."),
        new StoreSettingDefinition("business.notice", "Business notice", "Storefront", "Home page trust information."),
        new StoreSettingDefinition("custom.notice", "Custom order notice", "Storefront", "Home page trust information."),
        new StoreSettingDefinition("policy.privacy.url", "Privacy policy link", "Policies", "Footer privacy link.", "link"),
        new StoreSettingDefinition("policy.terms.url", "Terms link", "Policies", "Footer terms link.", "link"),
    });
}
