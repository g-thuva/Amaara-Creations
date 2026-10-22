# Asset replacement and photography shot list

The hero's original HTML/CSS design studies are explicitly illustrative. They are not catalogue products, proof of material availability or photographs of customer work. Production catalogue images must come from approved media records, not the acceptance fixture's synthetic image.

Before launch, obtain rights, meaningful alt text and approval for:

- A laptop or everyday-object sticker, close enough to show print edges and scale.
- A car/window decal in context, avoiding identifying number plates.
- A real business packaging label and a material/finish close-up.
- A genuine wedding/event sign and an unobstructed crop of its lettering.
- A personalized gift/detail and an approved workshop/process image.
- Actual examples for every published category/collection and each purchasable product variant.
- Logo master and approved social/share artwork; retain the current mark until approved.

Shoot clean backgrounds in consistent daylight, include both portrait and landscape compositions, and leave copy-safe space. Export responsive WebP/AVIF plus suitable fallbacks, explicit dimensions and a modest byte budget. Avoid customer names, addresses, receipts or unapproved faces in marketing images.

Replacement path: configure the existing CMS hero image and catalogue media APIs; no code change should be needed for real imagery. Verify desktop/mobile crops and alt text. Keep raw uploads private until reviewed. Testimonials and review photos require real records and consent; omit absent content. Never move test fixture products or screenshots into the live catalogue.

Locally served font subsets and their SIL OFL notices are committed under `client/public/fonts`. `scripts/download-storefront-fonts.py` refreshes them only from official Google Fonts sources. They do not depend on a third-party stylesheet at page load.
