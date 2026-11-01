import CatalogToolbar from '@/features/catalog/components/CatalogToolbar.jsx';
import CatalogResults from '@/features/catalog/components/CatalogResults.jsx';
import { Link } from 'react-router-dom';
import { Button } from '@/shared/components/ui/Button.jsx';
import { Dialog } from '@/shared/components/ui/Dialog.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';

import ProductFilters from '@/features/catalog/components/ProductFilters.jsx';
import { useCatalog } from '@/features/catalog/hooks/useCatalog.js';

export default function ProductsPage() {
  const catalog = useCatalog();
  const { params, open, setOpen, filterError, query, categories, collections, currency, clear, apply } = catalog;
  const emptyCatalogue = !catalog.data.loading && !catalog.data.error && catalog.data.data?.publishedCount === 0;
  const filters = (
    <>
      <ProductFilters
        key={query}
        params={params}
        categories={categories.data || []}
        collections={collections.data || []}
        apply={apply}
        clear={clear}
        currency={currency}
      />
      {filterError && <p role="alert" className="s-error-text">{filterError}</p>}
    </>
  );

  return (
    <>
      <section className="s-shop-hero" aria-labelledby="shop-title">
        <img
          className="s-shop-hero-bg"
          src={`${import.meta.env.BASE_URL}assets/hero/luxury-welcome-sign-hero.jpg`}
          alt=""
          width="1376"
          height="768"
          loading="eager"
          decoding="async"
        />
        <span className="s-shop-hero-scrim" aria-hidden="true" />
        <div className="s-container s-shop-hero-inner">
          <h1 id="shop-title">The bespoke <em>collection</em></h1>
          <p>Handcrafted acrylic welcome signs, mirror arches, direction signs and event stationery — made for life's most beautiful celebrations.</p>
          <div className="s-shop-hero-actions">
            <Link className="s-button s-button-glass" to="/custom">Design your own <Icon name="arrow" /></Link>
          </div>
        </div>
      </section>

      <div className="s-container s-page s-catalog-page">
        {!emptyCatalogue && <CatalogToolbar catalog={catalog} categories={categories.data || []} />}

        {(categories.error || collections.error) && (
          <div className="s-alert">Some filters are unavailable. <Button variant="text" onClick={() => { categories.reload(); collections.reload(); }}>Retry filters</Button></div>
        )}

        <div className={emptyCatalogue ? 's-catalog-empty' : 's-catalog-layout'}>
          {!emptyCatalogue && <aside className="s-desktop-filters" aria-label="Product filters">
            <h2>Filter products</h2>
            {filters}
          </aside>}
          <CatalogResults catalog={catalog} />
        </div>

        <Dialog open={open} onClose={() => setOpen(false)} title="Product filters" drawer>{filters}</Dialog>
      </div>
    </>
  );
}
