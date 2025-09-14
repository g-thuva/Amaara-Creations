import React from "react";
import { useNavigate, useSearchParams } from "react-router-dom";

const products = [
  {
    id: 1,
    name: "Elegant Wedding Sticker",
    price: 250,
    image: "https://images.unsplash.com/photo-1519225421980-715cb0215aed?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80",
    category: "wedding",
    description: "Beautiful gold foil sticker perfect for wedding invitations"
  },
  {
    id: 2,
    name: "Luxury Car Decal",
    price: 300,
    image: "https://images.unsplash.com/photo-1555215695-3004980ad54e?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80",
    category: "car",
    description: "Durable and weather-resistant car decal with premium finish"
  },
  {
    id: 3,
    name: "Floral Monogram",
    price: 199,
    image: "https://images.unsplash.com/photo-1578985545062-69928b1d9587?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80",
    category: "wedding",
    description: "Elegant floral design with custom monogram"
  },
  {
    id: 4,
    name: "Vintage Bumper Sticker",
    price: 179,
    image: "https://images.unsplash.com/photo-1601582589907-f92af5ed9db8?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80",
    category: "car",
    description: "Retro-style bumper sticker with premium adhesive"
  },
  {
    id: 5,
    name: "Gold Foil Accent",
    price: 229,
    image: "https://images.unsplash.com/photo-1624555135871-7bb631f0d8f7?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80",
    category: "wedding",
    description: "Luxurious gold foil sticker for special occasions"
  },
  {
    id: 6,
    name: "Minimalist Decal Set",
    price: 349,
    image: "https://images.unsplash.com/photo-1605000797499-95a51c5269ae?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80",
    category: "car",
    description: "Set of minimalist car decals with premium finish"
  },
];

const Products = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const category = searchParams.get('category');

  // Filter products by category if specified
  const filteredProducts = category
    ? products.filter(product => product.category === category)
    : products;

  // Get unique categories for filter
  const categories = [...new Set(products.map(product => product.category))];

  return (
    <div className="products-page">
      <div className="container">
        <div className="page-header">
          <h1 className="page-title">Our Sticker Collection</h1>
          <p className="page-subtitle">Discover our premium selection of custom stickers</p>
        </div>
        
        <div className="products-container">
          {/* Category Filter */}
          <div className="category-filters">
            <button 
              className={`filter-btn ${!category ? 'active' : ''}`}
              onClick={() => navigate('/products')}
            >
              All Products
            </button>
            {categories.map(cat => (
              <button
                key={cat}
                className={`filter-btn ${category === cat ? 'active' : ''}`}
                onClick={() => navigate(`/products?category=${cat}`)}
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
                      src={product.image} 
                      alt={product.name} 
                      loading="lazy"
                    />
                    <div className="product-overlay">
                      <button className="btn btn-primary">View Details</button>
                    </div>
                  </div>
                  <div className="product-details">
                    <h3 className="product-title">{product.name}</h3>
                    <p className="product-description">{product.description}</p>
                    <div className="product-footer">
                      <span className="product-price">Rs. {product.price}</span>
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
              <p>No products found in this category.</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default Products;

