import React from "react";
import { useNavigate } from "react-router-dom";

const products = [
  {
    id: 1,
    name: "Wedding Sticker A",
    price: 250,
    image: "https://via.placeholder.com/150",
  },
  {
    id: 2,
    name: "Car Sticker B",
    price: 300,
    image: "https://via.placeholder.com/150",
  },
];

const Products = () => {
  const navigate = useNavigate();

  return (
    <div className="container" style={{ padding: "2rem 0" }}>
      <h2 style={{ fontSize: "2rem", marginBottom: "1rem" }}>All Products</h2>
      <div className="product-grid">
        {products.map((product) => (
          <div
            key={product.id}
            className="product-card"
            style={{ cursor: "pointer" }}
            onClick={() => navigate(`/products/${product.id}`)}
          >
            <img src={product.image} alt={product.name} />
            <h3>{product.name}</h3>
            <p>Rs. {product.price}</p>
          </div>
        ))}
      </div>
    </div>
  );
};

export default Products;
