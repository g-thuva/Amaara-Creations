import { Link } from 'react-router-dom';
import { Media } from '@/shared/components/ui/Media.jsx';
import { Price } from '@/shared/components/ui/Price.jsx';
import { readCustomSnapshot } from '@/features/custom-design/designModel.js';
export default function OrderItems({ order }) {
  return (
    <div className="s-cart-items">{order.orderItems.map((item) => {
      const snapshot = readCustomSnapshot(item.configurationSnapshotJson);
      return <article className="s-cart-item" key={item.id}>
        <Media src={item.productImageUrl} alt={item.productName} />
        <div><h2>{item.productName}</h2><p>{item.quantity} × <Price value={item.price} currency={order.currency} /></p>
          {snapshot && <dl className="s-inline-spec"><div><dt>Size</dt><dd>{snapshot.Width ?? snapshot.width} × {snapshot.Height ?? snapshot.height} {snapshot.MeasurementUnit ?? snapshot.measurementUnit}</dd></div><div><dt>Material</dt><dd>{snapshot.Material?.Label ?? snapshot.material?.label ?? '—'}</dd></div><div><dt>Finish</dt><dd>{snapshot.Finish?.Label ?? snapshot.finish?.label ?? '—'}</dd></div></dl>}
          {item.customDesignId && <Link className="s-text-link" to={`/account/designs/${item.customDesignId}/proof`}>View proof status</Link>}
        </div><Price value={item.subtotal} currency={order.currency} />
      </article>;
    })}</div>
  );
}
