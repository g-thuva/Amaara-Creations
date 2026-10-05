import React, { useEffect, useState } from "react";
import { adminApi } from "../../services/adminApi";
import "./AdminStyles.css";

const blank = { key: "", value: "", valueType: "text", isPublic: true, description: "" };

const Settings = () => {
  const [items, setItems] = useState([]);
  const [form, setForm] = useState(blank);

  const load = async () => setItems(await adminApi.getSettings());

  useEffect(() => {
    load();
  }, []);

  const save = async (event) => {
    event.preventDefault();
    await adminApi.saveSetting(form);
    setForm(blank);
    await load();
  };

  return (
    <div className="admin-page">
      <div className="page-header"><div className="header-content"><div><h2>Site Settings</h2><p>{items.length} settings</p></div></div></div>
      <div className="products-table-container"><table className="admin-table"><thead><tr><th>Key</th><th>Value</th><th>Type</th><th>Public</th><th>Actions</th></tr></thead><tbody>{items.map((item) => (
        <tr key={item.id}><td>{item.key}</td><td>{item.value}</td><td>{item.valueType}</td><td>{item.isPublic ? "Yes" : "No"}</td><td><button className="btn-edit" onClick={() => setForm(item)}><i className="fa-solid fa-pen" /></button></td></tr>
      ))}</tbody></table></div>
      <div className="card"><div className="card-header"><h3 className="card-title">Edit Setting</h3></div><form className="add-product-form" onSubmit={save}>
        <div className="form-row"><label className="form-group">Key<input className="form-control" required value={form.key} onChange={(e) => setForm({ ...form, key: e.target.value })} /></label><label className="form-group">Type<input className="form-control" value={form.valueType} onChange={(e) => setForm({ ...form, valueType: e.target.value })} /></label></div>
        <label className="form-group">Value<textarea className="form-control" required value={form.value} onChange={(e) => setForm({ ...form, value: e.target.value })} /></label>
        <label className="form-group">Description<input className="form-control" value={form.description || ""} onChange={(e) => setForm({ ...form, description: e.target.value })} /></label>
        <label><input type="checkbox" checked={form.isPublic} onChange={(e) => setForm({ ...form, isPublic: e.target.checked })} /> Public</label>
        <div className="form-actions"><button type="button" className="btn-cancel" onClick={() => setForm(blank)}>Clear</button><button type="submit" className="btn-submit">Save</button></div>
      </form></div>
    </div>
  );
};

export default Settings;
