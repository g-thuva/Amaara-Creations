import { Link } from "react-router-dom";
export function TopProductsList({ topProducts }) {
  return (
    <div className="dashboard-card">
      <div className="card-header">
        <h3>Top Selling Products</h3>
        <Link to="/admin/products" className="view-all">
          View All
        </Link>
      </div>
      <div className="product-list">
        {topProducts.length === 0 ? (
          <div style={{ padding: "2rem", textAlign: "center" }}>
            <p>No products yet</p>
          </div>
        ) : (
          topProducts.slice(0, 5).map((product, index) => (
            <div key={index} className="product-item">
              <div className="product-info">
                <h4>{product.productName}</h4>
                <div className="product-meta">
                  <span>{product.quantitySold} sales</span>
                </div>
              </div>
              <div className="product-chart">
                <div
                  className="chart-bar"
                  style={{
                    width: `${(product.quantitySold / (topProducts[0]?.quantitySold || 1)) * 100}%`,
                  }}
                ></div>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
