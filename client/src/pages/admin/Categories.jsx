import React, { useEffect, useState } from "react";
import { adminApi } from "../../services/adminApi";
import "./AdminStyles.css";

const blank = { name: "", slug: "", description: "", parentCategoryId: "", sortOrder: 0, isActive: true };

const Categories = () => {
  const [items, setItems] = useState([]);
  const [form, setForm] = useState(blank);
  const [editing, setEditing] = useState(null);

  const load = async () => setItems(await adminApi.getCategories());

  useEffect(() => {
    load();
  }, []);

  const save = async (event) => {
    event.preventDefault();
    const payload = { ...form, parentCategoryId: form.parentCategoryId ? Number(form.parentCategoryId) : null, sortOrder: Number(form.sortOrder) };
    if (editing) await adminApi.updateCategory(editing.id, payload);
    else await adminApi.createCategory(payload);
    setForm(blank);
    setEditing(null);
    await load();
  };

  const edit = (category) => {
    setEditing(category);
    setForm({ ...category, parentCategoryId: category.parentCategoryId || "" });
  };

  const toggle = async (category) => {
    if (category.isActive) await adminApi.archiveCategory(category.id);
    else await adminApi.reactivateCategory(category.id);
    await load();
  };

  return (
    <div className="admin-page">
      <div className="page-header"><div className="header-content"><div><h2>Categories</h2><p>{items.length} categories</p></div></div></div>
      <div className="products-table-container">
        <table className="admin-table">
          <thead><tr><th>Name</th><th>Slug</th><th>Parent</th><th>Sort</th><th>Status</th><th>Actions</th></tr></thead>
          <tbody>{items.map((category) => (
            <tr key={category.id}>
              <td>{category.name}</td><td>{category.slug}</td><td>{items.find((item) => item.id === category.parentCategoryId)?.name || ""}</td><td>{category.sortOrder}</td>
              <td><span className={`badge ${category.isActive ? "badge-success" : "badge-warning"}`}>{category.isActive ? "Active" : "Archived"}</span></td>
              <td><div className="action-buttons"><button className="btn-edit" onClick={() => edit(category)}><i className="fa-solid fa-pen" /></button><button className="btn-delete" onClick={() => toggle(category)}><i className={`fa-solid ${category.isActive ? "fa-box-archive" : "fa-rotate-left"}`} /></button></div></td>
            </tr>
          ))}</tbody>
        </table>
      </div>
      <div className="card">
        <div className="card-header"><h3 className="card-title">{editing ? "Edit Category" : "Create Category"}</h3></div>
        <form className="add-product-form" onSubmit={save}>
          <div className="form-row"><label className="form-group">Name<input className="form-control" required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></label><label className="form-group">Slug<input className="form-control" value={form.slug || ""} onChange={(e) => setForm({ ...form, slug: e.target.value })} /></label></div>
          <label className="form-group">Description<textarea className="form-control" value={form.description || ""} onChange={(e) => setForm({ ...form, description: e.target.value })} /></label>
          <div className="form-row"><label className="form-group">Parent<select className="form-control" value={form.parentCategoryId || ""} onChange={(e) => setForm({ ...form, parentCategoryId: e.target.value })}><option value="">None</option>{items.filter((item) => item.id !== editing?.id).map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><label className="form-group">Sort<input className="form-control" type="number" value={form.sortOrder} onChange={(e) => setForm({ ...form, sortOrder: e.target.value })} /></label></div>
          <label><input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} /> Active</label>
          <div className="form-actions"><button type="button" className="btn-cancel" onClick={() => { setForm(blank); setEditing(null); }}>Clear</button><button type="submit" className="btn-submit">Save</button></div>
        </form>
      </div>
    </div>
  );
};

export default Categories;
