import CatalogToolbar from '@/features/catalog/components/CatalogToolbar.jsx';
import CatalogResults from '@/features/catalog/components/CatalogResults.jsx';
import { Link } from 'react-router-dom';
import { Button } from '@/shared/components/ui/Button.jsx';
import { Dialog } from '@/shared/components/ui/Dialog.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';

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
      <div className="s-page-band">
        <div className="s-container">
          <nav className="s-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><Icon name="chevronRight" /><span aria-current="page">Shop</span></nav>
          <PageHeader eyebrow="The Amaara shop" title="Stickers, decals and printed details">
            Browse the products currently published by Amaara Creations.
          </PageHeader>
        </div>
      </div>

      <div className="s-container s-page s-catalog-page">
        {!emptyCatalogue && <CatalogToolbar catalog={catalog} />}

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
