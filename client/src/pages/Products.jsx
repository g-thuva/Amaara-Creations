import React, { useState, useEffect } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { productApi } from "../services/productApi";
import "./Products.css";

const Products = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [products, setProducts] = useState([]);
  const [filteredProducts, setFilteredProducts] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [searchTerm, setSearchTerm] = useState("");
  
  const category = searchParams.get('category');
  const search = searchParams.get('search');

  // Fetch products from API
  useEffect(() => {
    const fetchProducts = async () => {
      setIsLoading(true);
      setError("");
      try {
        const params = {
          category: category || undefined,
          search: search || undefined,
          pageNumber: 1,
          pageSize: 100, // Get all products for now
        };
        
        const response = await productApi.getProducts(params);
        const productList = response.products || response;
        setProducts(productList);
        setFilteredProducts(productList);
      } catch (err) {
        console.error("Error fetching products:", err);
        setError("Failed to load products. Please try again later.");
      } finally {
        setIsLoading(false);
      }
    };

    fetchProducts();
  }, [category, search]);

  // Get unique categories for filter
  const categories = [...new Set(products.map(product => product.category).filter(Boolean))];

  const handleCategoryFilter = (cat) => {
    if (cat) {
      navigate(`/products?category=${cat}`);
    } else {
      navigate('/products');
    }
  };

  const handleSearch = (e) => {
    e.preventDefault();
    if (searchTerm.trim()) {
      navigate(`/products?search=${encodeURIComponent(searchTerm)}`);
    } else {
      navigate('/products');
    }
  };

  if (isLoading) {
    return (
      <div className="products-page">
        <div className="container">
          <div style={{ textAlign: 'center', padding: '3rem' }}>
            <p>Loading products...</p>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="products-page">
        <div className="container">
          <div style={{ textAlign: 'center', padding: '3rem', color: 'red' }}>
            <p>{error}</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="products-page">
      <div className="container">
        <div className="page-header">
          <h1 className="page-title">Our Sticker Collection</h1>
          <p className="page-subtitle">Discover our premium selection of custom stickers</p>
        </div>
        
        <div className="products-container">
          {/* Search Bar */}
          <form onSubmit={handleSearch} style={{ marginBottom: '2rem' }}>
            <div style={{ display: 'flex', gap: '0.5rem', maxWidth: '500px', margin: '0 auto' }}>
              <input
                type="text"
                placeholder="Search products..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                style={{ flex: 1, padding: '0.75rem', borderRadius: '4px', border: '1px solid #ddd' }}
              />
              <button type="submit" className="btn btn-primary">Search</button>
            </div>
          </form>

          {/* Category Filter */}
          <div className="category-filters">
            <button 
              className={`filter-btn ${!category ? 'active' : ''}`}
              onClick={() => handleCategoryFilter(null)}
            >
              All Products
            </button>
            {categories.map(cat => (
              <button
                key={cat}
                className={`filter-btn ${category === cat ? 'active' : ''}`}
                onClick={() => handleCategoryFilter(cat)}
              >
                {cat.charAt(0).toUpperCase() + cat.slice(1)}
              </button>
            ))}
          </div>
          
          {/* Product Grid */}
          {filteredProducts.length > 0 ? (
            <div className="product-grid">
              {filteredProducts.map((product) => (
                <div
                  key={product.id}
                  className="product-card"
                  onClick={() => navigate(`/products/${product.id}`)}
                >
                  <div className="product-image">
                    <img 
                      src={product.imageUrl || product.image || 'https://via.placeholder.com/400'} 
                      alt={product.name} 
                      loading="lazy"
                      onError={(e) => {
                        e.target.onerror = null;
                        e.target.src = 'https://via.placeholder.com/400?text=No+Image';
                      }}
                    />
                    {product.stock === 0 && (
                      <div className="out-of-stock-badge">Out of Stock</div>
                    )}
                    <div className="product-overlay">
                      <button className="btn btn-primary">View Details</button>
                    </div>
                  </div>
                  <div className="product-details">
                    <h3 className="product-title">{product.name}</h3>
                    <p className="product-description">{product.description}</p>
                    <div className="product-footer">
                      <span className="product-price">Rs. {product.price?.toLocaleString()}</span>
                      <span className={`product-category ${product.category}`}>
                        {product.category}
                      </span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="no-results">
              <p>No products found{category ? ` in ${category} category` : ""}.</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default Products;

