# Storefront Revision 2 — Brand evidence audit

> **Instagram audit: PENDING — no references supplied.**
>
> To complete this audit, add owner-approved reference files under `docs/brand-reference/instagram/`, `docs/brand-reference/logo/`, and `docs/brand-reference/products/`. Include a profile screenshot, 9–12 recent grid posts, 3–6 representative product photographs, vector or high-resolution logo files, and any known colour or typography notes. Do not include account credentials or private customer material.

## Evidence available for this pass

No `docs/brand-reference/` directory or owner-provided Instagram, logo, or product reference files were present on 6 October 2026. Instagram was not scraped, embedded, or queried through an unofficial API.

The visual evidence base for Revision 2 is therefore the repository's original Amaara interface documented in the revision brief:

- navy family `#0f2d4a`, `#1a365d`, and `#2c5282`;
- decorative sky accents `#4299e1` and `#90cdf4`;
- Inter body typography and Poppins headings;
- pill calls to action, rounded cards, soft navy-tinted shadows, image-led merchandising, and a navy footer;
- a brand focus on custom stickers, wedding stickers, car decals, labels, and personalised printed details.

## Decisions adopted

- Restore the navy-and-sky palette through scoped CSS variables in `storefront.css`.
- Use Poppins for headings and Inter for body and UI text, with Sinhala and Tamil fallbacks.
- Restore an image-led CMS carousel, image category cards, rounded product cards, hover lift, and the four-column navy footer.
- Use the darker `#2b6cb0` for interactive sky treatments. Keep `#4299e1` decorative.
- Use only media supplied through Catalogue Media catalogue/CMS storage. Provide CSS/SVG brand fallbacks where media is absent.
- Show Instagram only when a valid HTTPS URL is present in the public `social.instagram` setting.

## Decisions deferred

- Exact logo geometry, safe area, minimum size, and dark/light variants remain pending an owner logo file.
- Photography crop, lighting, props, and post-production rules remain pending product and Instagram references.
- Caption language mix, emoji use, and detailed voice guidance remain pending visible owner-approved account references.
- The optional warm highlight token is not used because available evidence does not support it.

## Later audit procedure

When references are supplied, record sampled hex values, photography traits, graphic typography, visible product categories, tone, logo variants, and recurring motifs. Compare each finding with the current tokens and document any adopted or rejected adjustment. Owner media must be uploaded through the media library or committed as an approved local asset; it must never be hot-linked from Instagram.

