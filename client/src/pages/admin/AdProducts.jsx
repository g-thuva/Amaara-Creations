import React, { useState, useEffect } from "react";
import { FiPlus, FiX, FiEdit, FiTrash2 } from "react-icons/fi";
import { productApi } from "../../services/productApi";
import { uploadApi } from "../../services/uploadApi";
import "./AdminStyles.css";

const AdProducts = () => {
  const [products, setProducts] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showAddForm, setShowAddForm] = useState(false);
  const [editingProduct, setEditingProduct] = useState(null);
  const [formData, setFormData] = useState({
    name: "",
    price: "",
    description: "",
    imageUrl: "",
    stock: "",
    category: "wedding",
    isActive: true
  });
  const [imageFile, setImageFile] = useState(null);

  // Fetch products from API
  useEffect(() => {
    const fetchProducts = async () => {
      setIsLoading(true);
      try {
        const response = await productApi.getProducts({ pageNumber: 1, pageSize: 100 });
        setProducts(response.products || response || []);
      } catch (err) {
        console.error("Error fetching products:", err);
        alert("Failed to load products");
      } finally {
        setIsLoading(false);
      }
    };

    fetchProducts();
  }, []);

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
      setImageFile(file);
      const reader = new FileReader();
      reader.onloadend = () => {
        setFormData(prev => ({
          ...prev,
          imageUrl: reader.result
        }));
      };
      reader.readAsDataURL(file);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!formData.name || !formData.price || !formData.description || formData.stock === "") {
      alert("Please fill in all required fields");
      return;
    }

    try {
      let imageUrl = formData.imageUrl;

      // Upload image if a file is selected
      if (imageFile) {
        const uploadResponse = await uploadApi.uploadProductImage(imageFile);
        imageUrl = `http://localhost:5192${uploadResponse.fileUrl}`;
      }

      const productData = {
        name: formData.name,
        price: parseFloat(formData.price),
        description: formData.description,
        imageUrl: imageUrl,
        category: formData.category,
        stock: parseInt(formData.stock),
        isActive: formData.isActive
      };

      if (editingProduct) {
        // Update existing product
        await productApi.updateProduct(editingProduct.id, productData);
        alert("Product updated successfully!");
      } else {
        // Create new product
        await productApi.createProduct(productData);
        alert("Product added successfully!");
      }

      // Refresh products list
      const response = await productApi.getProducts({ pageNumber: 1, pageSize: 100 });
      setProducts(response.products || response || []);

      // Reset form
      setFormData({
        name: "",
        price: "",
        description: "",
        imageUrl: "",
        stock: "",
        category: "wedding",
        isActive: true
      });
      setImageFile(null);
      setEditingProduct(null);
      setShowAddForm(false);
    } catch (err) {
      const errorMessage = err.response?.data?.message || "Failed to save product";
      alert(errorMessage);
      console.error("Error saving product:", err);
    }
  };

  const handleCancel = () => {
    setShowAddForm(false);
    setEditingProduct(null);
    setFormData({
      name: "",
      price: "",
      description: "",
      imageUrl: "",
      stock: "",
      category: "wedding",
      isActive: true
    });
    setImageFile(null);
  };

  const handleEdit = (product) => {
    setEditingProduct(product);
    setFormData({
      name: product.name,
      price: product.price,
      description: product.description,
      imageUrl: product.imageUrl || "",
      stock: product.stock,
      category: product.category,
      isActive: product.isActive !== false
    });
    setShowAddForm(true);
  };

  const handleDelete = async (id) => {
    if (window.confirm("Are you sure you want to delete this product?")) {
      try {
        await productApi.deleteProduct(id);
        alert("Product deleted successfully!");
        // Refresh products list
        const response = await productApi.getProducts({ pageNumber: 1, pageSize: 100 });
        setProducts(response.products || response || []);
      } catch (err) {
        const errorMessage = err.response?.data?.message || "Failed to delete product";
        alert(errorMessage);
        console.error("Error deleting product:", err);
      }
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

      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <p>Loading products...</p>
        </div>
      ) : (
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
              {products.length === 0 ? (
                <tr>
                  <td colSpan="7" style={{ textAlign: 'center', padding: '2rem' }}>
                    No products found. Add your first product!
                  </td>
                </tr>
              ) : (
                products.map((product) => (
              <tr key={product.id}>
                <td>
                  <img 
                    src={product.imageUrl || product.image || 'https://via.placeholder.com/80'} 
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
                <td>Rs. {product.price?.toLocaleString()}</td>
                <td>{product.stock}</td>
                <td>
                  <span className={`status-badge ${product.stock === 0 ? 'out-of-stock' : 'in-stock'}`}>
                    {product.stock === 0 ? 'Out of Stock' : 'In Stock'}
                  </span>
                </td>
                <td>
                  <div className="action-buttons">
                    <button className="btn-edit" title="Edit" onClick={() => handleEdit(product)}>
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
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Add Product Modal */}
      {showAddForm && (
        <div className="modal-overlay" onClick={handleCancel}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{editingProduct ? 'Edit Product' : 'Add New Product'}</h2>
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
                <label htmlFor="image">Product Image {!editingProduct && '*'}</label>
                <input
                  type="file"
                  id="image"
                  name="image"
                  accept="image/*"
                  onChange={handleImageChange}
                  required={!editingProduct}
                />
                {formData.imageUrl && (
                  <div className="image-preview">
                    <img src={formData.imageUrl} alt="Preview" />
                  </div>
                )}
                {!imageFile && editingProduct && (
                  <small style={{ color: '#666', marginTop: '0.5rem' }}>
                    Current image will be used if no new file is selected
                  </small>
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
                  {editingProduct ? 'Update Product' : 'Add Product'}
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
