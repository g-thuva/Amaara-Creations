import React, { useEffect, useState } from "react";
import { adminApi } from "../../services/adminApi";
import "./AdminStyles.css";

const blank = { name: "", slug: "", description: "", sortOrder: 0, isActive: true };

const Collections = () => {
  const [items, setItems] = useState([]);
  const [products, setProducts] = useState([]);
  const [form, setForm] = useState(blank);
  const [editing, setEditing] = useState(null);
  const [selectedProducts, setSelectedProducts] = useState([]);

  const load = async () => {
    setItems(await adminApi.getCollections());
    const productPage = await adminApi.getProducts({ pageSize: 100 });
    setProducts(productPage.items || []);
  };

  useEffect(() => {
    load();
  }, []);

  const save = async (event) => {
    event.preventDefault();
    const payload = { ...form, sortOrder: Number(form.sortOrder) };
    const saved = editing ? await adminApi.updateCollection(editing.id, payload) : await adminApi.createCollection(payload);
    await adminApi.replaceCollectionProducts(saved.id, selectedProducts.map(Number));
    setForm(blank);
    setEditing(null);
    setSelectedProducts([]);
    await load();
  };

  const edit = (collection) => {
    setEditing(collection);
    setForm(collection);
    setSelectedProducts([]);
  };

  const toggle = async (collection) => {
    if (collection.isActive) await adminApi.archiveCollection(collection.id);
    else await adminApi.reactivateCollection(collection.id);
    await load();
  };

  return (
    <div className="admin-page">
      <div className="page-header"><div className="header-content"><div><h2>Collections</h2><p>{items.length} collections</p></div></div></div>
      <div className="products-table-container"><table className="admin-table"><thead><tr><th>Name</th><th>Slug</th><th>Products</th><th>Status</th><th>Actions</th></tr></thead><tbody>{items.map((collection) => (
        <tr key={collection.id}><td>{collection.name}</td><td>{collection.slug}</td><td>{collection.productCount}</td><td><span className={`badge ${collection.isActive ? "badge-success" : "badge-warning"}`}>{collection.isActive ? "Active" : "Archived"}</span></td><td><div className="action-buttons"><button className="btn-edit" onClick={() => edit(collection)}><i className="fa-solid fa-pen" /></button><button className="btn-delete" onClick={() => toggle(collection)}><i className={`fa-solid ${collection.isActive ? "fa-box-archive" : "fa-rotate-left"}`} /></button></div></td></tr>
      ))}</tbody></table></div>
      <div className="card"><div className="card-header"><h3 className="card-title">{editing ? "Edit Collection" : "Create Collection"}</h3></div><form className="add-product-form" onSubmit={save}>
        <div className="form-row"><label className="form-group">Name<input className="form-control" required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></label><label className="form-group">Slug<input className="form-control" value={form.slug || ""} onChange={(e) => setForm({ ...form, slug: e.target.value })} /></label></div>
        <label className="form-group">Description<textarea className="form-control" value={form.description || ""} onChange={(e) => setForm({ ...form, description: e.target.value })} /></label>
        <label className="form-group">Products<select multiple className="form-control" value={selectedProducts.map(String)} onChange={(e) => setSelectedProducts(Array.from(e.target.selectedOptions).map((option) => option.value))}>{products.map((product) => <option key={product.id} value={product.id}>{product.name}</option>)}</select></label>
        <div className="form-row"><label className="form-group">Sort<input className="form-control" type="number" value={form.sortOrder} onChange={(e) => setForm({ ...form, sortOrder: e.target.value })} /></label><label><input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} /> Active</label></div>
        <div className="form-actions"><button type="button" className="btn-cancel" onClick={() => { setForm(blank); setEditing(null); setSelectedProducts([]); }}>Clear</button><button type="submit" className="btn-submit">Save</button></div>
      </form></div>
    </div>
  );
};

export default Collections;
