# Reference research — 22 October 2026

## Method and limits

The execution prompt and all 18 PDF pages were read. The PDF describes seven screenshots, but those image files were not separately supplied. Local before/after evidence uses the real application with disposable acceptance fixtures. It is not a capture of a live customer database.

All reference URLs were attempted with Chrome at 1440, 768 and 390 pixels. The sandbox browser returned `ERR_NETWORK_ACCESS_DENIED` for external navigation; see `evidence/before/observations.json`. The web reader could retrieve several sites' HTML/text. Consequently, mobile layout, imagery composition, interaction behavior, personalization and checkout were **not visually verified**. No third-party assets or code were copied. The following evidence is limited to retrieved page content; adaptation choices are design inferences.

| Reference | Observed content evidence | Adapt / avoid (inference) |
| --- | --- | --- |
| [Zazzle wedding signs](https://www.zazzle.com/c/wedding+signs) | Search, account, gifts/weddings/events and create-your-own navigation appear in fetched content | Adapt a visible design starting point; avoid transferring its broad marketplace menu to a small catalogue |
| [Truly Engaging](https://www.trulyengaging.com/shop/wedding-signs) | Style filters, counted product types, sorting, saved work and file upload are present | Adapt saved work and count-aware filters; avoid filters when there is no inventory |
| [Etsy home](https://www.etsy.com/) and [acrylic signs](https://www.etsy.com/market/acrylic_wedding_sign) | Search, personalizable-item chips, prices, favorites and review counts appear in HTML | Adapt clear product information and search; avoid invented review counts and urgency |
| [iCustomLabel](https://icustomlabel.com/collections/wedding-signs) | Product-family navigation, account/cart and country/currency selection are present | Adapt specific product naming; avoid exposing unsupported currencies and excessive categories |
| [LunaVows](https://lunavows.com/collections/signs-decor) | Web reader unavailable; browser blocked | No verified visual or functional observation. Editorial photography remains an owner brief, not evidence about this site |
| [ANIM8 Sri Lanka](https://www.anim8.lk/) | Local business/product categories, search, steps-to-order and artwork submission links appear | Adapt locally useful ordering guidance; avoid reproducing its many first-level choices or borrowing delivery claims |
| [Made & True](https://madeandtrue.com/) | Collections, sign materials/types, FAQ/gallery and contact links appear | Adapt coherent collection discovery when populated; do not borrow promotional thresholds or turnaround promises |
| [Current Amaara deployment](https://g-thuva.github.io/Amaara-Creations/) | Web reader unavailable; browser blocked | Use checked-out routes and local screenshots; do not claim the deployment matches this branch |

No payment, account creation, artwork submission or purchase was performed on a reference site. Cards, filters and navigation observations do not prove customization, checkout, accessibility or performance quality.

## Primary technical sources

- [WCAG 2.2](https://www.w3.org/TR/WCAG22/): target keyboard operation, text/non-text contrast, focus visibility and recovery, labels, status communication, zoom/reflow and accessible authentication. Automated axe checks cover only part of this target.
- [ASVS 5.0.0](https://asvs.owasp.org/): use as a verification backlog, not certification. See `../security/ASVS_5_CONTROL_MAPPING.md`.
- [OWASP authorization](https://cheatsheetseries.owasp.org/cheatsheets/Authorization_Cheat_Sheet.html): enforce least privilege and object access on every request.
- [OWASP .NET](https://cheatsheetseries.owasp.org/cheatsheets/DotNet_Security_Cheat_Sheet.html), [uploads](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html), [sessions](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html) and [CSRF](https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html): framework-supported identity, bounded/private uploads, session invalidation and explicit defenses for cookie-bearing requests.
- [NIST SP 800-63B-4](https://pages.nist.gov/800-63-4/sp800-63b.html), §3.1.1.2: minimum 15 characters for password-only authentication, allowance of long passphrases, whole-password blocklists and no arbitrary composition rules. Existing sign-in hashes are preserved.
- [Google Web Vitals](https://web.dev/articles/vitals): LCP, CLS and INP require representative measurements; bundle budgets are not field-vitals evidence.

## Sri Lankan privacy law: current primary evidence

The [DPA downloads page](https://www.dpa.gov.lk/guidelines.php) lists the 2025 amendment and a newer commencement order. The [amendment](https://www.dpa.gov.lk/acts/22-2025_E_251104_201549%20(1).pdf) changes commencement to dates appointed by Gazette. [Gazette 2498/16 of 22 July 2026](https://www.dpa.gov.lk/Gazet/2498-16_E.pdf) appoints **1 January 2027** for sections 2 and 3, Part I and Part III. This is narrower than a claim that every provision becomes effective together. The older DPA home-page article mentioning March 2025 is not sufficient current evidence. The March 2025 Gazette download timed out in the web reader.

Inference for release planning: obtain qualified review of applicable provisions, purposes, retention, notices, processor contracts and rights handling before publishing policy copy. This document does not determine the operator's legal obligations or claim compliance.
