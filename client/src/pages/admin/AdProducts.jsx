import React, { useState } from "react";
import { FiPlus, FiX, FiEdit, FiTrash2 } from "react-icons/fi";
import "./AdminStyles.css";

const initialProducts = [
  {
    id: 1,
    name: "Elegant Wedding Sticker",
    price: 250,
    image: "https://images.unsplash.com/photo-1519225421980-715cb0215aed?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80",
    category: "wedding",
    description: "Beautiful gold foil sticker perfect for wedding invitations",
    stock: 15
  },
  {
    id: 2,
    name: "Luxury Car Decal",
    price: 300,
    image: "https://images.unsplash.com/photo-1555215695-3004980ad54e?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80",
    category: "car",
    description: "Durable and weather-resistant car decal with premium finish",
    stock: 0
  },
  {
    id: 3,
    name: "Floral Monogram",
    price: 199,
    image: "https://images.unsplash.com/photo-1578985545062-69928b1d9587?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80",
    category: "wedding",
    description: "Elegant floral design with custom monogram",
    stock: 8
  },
  {
    id: 4,
    name: "Vintage Bumper Sticker",
    price: 179,
    image: "https://images.unsplash.com/photo-1601582589907-f92af5ed9db8?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80",
    category: "car",
    description: "Retro-style bumper sticker with premium adhesive",
    stock: 12
  },
  {
    id: 5,
    name: "Gold Foil Accent",
    price: 229,
    image: "https://images.unsplash.com/photo-1624555135871-7bb631f0d8f7?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80",
    category: "wedding",
    description: "Luxurious gold foil sticker for special occasions",
    stock: 5
  },
  {
    id: 6,
    name: "Minimalist Decal Set",
    price: 349,
    image: "https://images.unsplash.com/photo-1605000797499-95a51c5269ae?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80",
    category: "car",
    description: "Set of minimalist car decals with premium finish",
    stock: 20
  },
];

const AdProducts = () => {
  const [products, setProducts] = useState(initialProducts);
  const [showAddForm, setShowAddForm] = useState(false);
  const [formData, setFormData] = useState({
    name: "",
    price: "",
    description: "",
    image: "",
    stock: "",
    category: "wedding"
  });

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleImageChange = (e) => {
    const file = e.target.files[0];
    if (file) {
      const reader = new FileReader();
      reader.onloadend = () => {
        setFormData(prev => ({
          ...prev,
          image: reader.result
        }));
      };
      reader.readAsDataURL(file);
    }
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    
    if (!formData.name || !formData.price || !formData.description || !formData.image || formData.stock === "") {
      alert("Please fill in all fields");
      return;
    }

    const newProduct = {
      id: products.length + 1,
      name: formData.name,
      price: parseFloat(formData.price),
      description: formData.description,
      image: formData.image,
      category: formData.category,
      stock: parseInt(formData.stock),
      isOutOfStock: parseInt(formData.stock) === 0
    };

    setProducts(prev => [...prev, newProduct]);

    setFormData({
      name: "",
      price: "",
      description: "",
      image: "",
      stock: "",
      category: "wedding"
    });

    setShowAddForm(false);
    alert("Product added successfully!");
  };

  const handleCancel = () => {
    setShowAddForm(false);
    setFormData({
      name: "",
      price: "",
      description: "",
      image: "",
      stock: "",
      category: "wedding"
    });
  };

  const handleDelete = (id) => {
    if (window.confirm("Are you sure you want to delete this product?")) {
      setProducts(prev => prev.filter(product => product.id !== id));
      alert("Product deleted successfully!");
    }
  };

  return (
    <div className="admin-page">
      <div className="page-header">
        <div className="header-content">
          <div>
            <h2>Product Management</h2>
            <p>Manage your product catalog</p>
          </div>
          <button 
            className="btn-add-product"
            onClick={() => setShowAddForm(true)}
          >
            <FiPlus /> Add Product
          </button>
        </div>
      </div>

      <div className="products-table-container">
        <table className="admin-table">
          <thead>
            <tr>
              <th>Image</th>
              <th>Product Name</th>
              <th>Category</th>
              <th>Price</th>
              <th>Stock</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {products.map((product) => (
              <tr key={product.id}>
                <td>
                  <img 
                    src={product.image} 
                    alt={product.name}
                    className="product-thumbnail"
                    onError={(e) => {
                      e.target.onerror = null;
                      e.target.src = 'https://via.placeholder.com/80?text=No+Image';
                    }}
                  />
                </td>
                <td>
                  <div className="product-name-cell">
                    <strong>{product.name}</strong>
                    <small>{product.description}</small>
                  </div>
                </td>
                <td>
                  <span className="category-badge">{product.category}</span>
                </td>
                <td>Rs. {product.price}</td>
                <td>{product.stock}</td>
                <td>
                  <span className={`status-badge ${product.stock === 0 ? 'out-of-stock' : 'in-stock'}`}>
                    {product.stock === 0 ? 'Out of Stock' : 'In Stock'}
                  </span>
                </td>
                <td>
                  <div className="action-buttons">
                    <button className="btn-edit" title="Edit">
                      <FiEdit />
                    </button>
                    <button 
                      className="btn-delete" 
                      onClick={() => handleDelete(product.id)}
                      title="Delete"
                    >
                      <FiTrash2 />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Add Product Modal */}
      {showAddForm && (
        <div className="modal-overlay" onClick={handleCancel}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Add New Product</h2>
              <button className="modal-close" onClick={handleCancel}>
                <FiX />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="add-product-form">
              <div className="form-group">
                <label htmlFor="name">Product Name *</label>
                <input
                  type="text"
                  id="name"
                  name="name"
                  value={formData.name}
                  onChange={handleInputChange}
                  required
                  placeholder="Enter product name"
                />
              </div>

              <div className="form-row">
                <div className="form-group">
                  <label htmlFor="price">Price (Rs.) *</label>
                  <input
                    type="number"
                    id="price"
                    name="price"
                    value={formData.price}
                    onChange={handleInputChange}
                    required
                    min="0"
                    step="0.01"
                    placeholder="0.00"
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="stock">Number of Stock *</label>
                  <input
                    type="number"
                    id="stock"
                    name="stock"
                    value={formData.stock}
                    onChange={handleInputChange}
                    required
                    min="0"
                    placeholder="0"
                  />
                </div>
              </div>

              <div className="form-group">
                <label htmlFor="category">Category</label>
                <select
                  id="category"
                  name="category"
                  value={formData.category}
                  onChange={handleInputChange}
                >
                  <option value="wedding">Wedding</option>
                  <option value="car">Car</option>
                  <option value="custom">Custom</option>
                </select>
              </div>

              <div className="form-group">
                <label htmlFor="description">Description *</label>
                <textarea
                  id="description"
                  name="description"
                  value={formData.description}
                  onChange={handleInputChange}
                  required
                  rows="4"
                  placeholder="Enter product description"
                />
              </div>

              <div className="form-group">
                <label htmlFor="image">Product Image *</label>
                <input
                  type="file"
                  id="image"
                  name="image"
                  accept="image/*"
                  onChange={handleImageChange}
                  required
                />
                {formData.image && (
                  <div className="image-preview">
                    <img src={formData.image} alt="Preview" />
                  </div>
                )}
              </div>

              {formData.stock === "0" && (
                <div className="out-of-stock-alert">
                  ⚠️ This product will be displayed as "Out of Stock"
                </div>
              )}

              <div className="form-actions">
                <button type="button" className="btn-cancel" onClick={handleCancel}>
                  Cancel
                </button>
                <button type="submit" className="btn-submit">
                  Add Product
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdProducts;
