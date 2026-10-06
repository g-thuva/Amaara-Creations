import React, { useEffect, useState } from "react";
import { adminApi } from "../../services/adminApi";
import "./AdminStyles.css";
import AppIcon from "../../components/AppIcon";

const blankPage = {
  title: "",
  slug: "",
  summary: "",
  isPublished: false,
  sections: [{ sectionKey: "main", contentType: "text", content: "", sortOrder: 0, isActive: true }],
};

const Content = () => {
  const [pages, setPages] = useState([]);
  const [form, setForm] = useState(blankPage);
  const [editing, setEditing] = useState(null);

  const load = async () => setPages(await adminApi.getPages());

  useEffect(() => {
    load();
  }, []);

  const save = async (event) => {
    event.preventDefault();
    if (editing) await adminApi.updatePage(editing.id, form);
    else await adminApi.createPage(form);
    setForm(blankPage);
    setEditing(null);
    await load();
  };

  const edit = (page) => {
    setEditing(page);
    setForm({ ...page, sections: page.sections.length ? page.sections : blankPage.sections });
  };

  const updateSection = (index, field, value) => {
    setForm({
      ...form,
      sections: form.sections.map((section, current) => current === index ? { ...section, [field]: value } : section),
    });
  };

  return (
    <div className="admin-page">
      <div className="page-header"><div className="header-content"><div><h2>Content</h2><p>{pages.length} pages</p></div></div></div>
      <div className="products-table-container"><table className="admin-table"><thead><tr><th>Title</th><th>Slug</th><th>Status</th><th>Updated</th><th>Actions</th></tr></thead><tbody>{pages.map((page) => (
        <tr key={page.id}><td>{page.title}</td><td>{page.slug}</td><td><span className={`badge ${page.isPublished ? "badge-success" : "badge-warning"}`}>{page.isPublished ? "Published" : "Draft"}</span></td><td>{new Date(page.updatedAt).toLocaleDateString()}</td><td><button className="btn-edit" onClick={() => edit(page)}><AppIcon name="edit" /></button></td></tr>
      ))}</tbody></table></div>
      <div className="card"><div className="card-header"><h3 className="card-title">{editing ? "Edit Page" : "Create Page"}</h3></div><form className="add-product-form" onSubmit={save}>
        <div className="form-row"><label className="form-group">Title<input className="form-control" required value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} /></label><label className="form-group">Slug<input className="form-control" value={form.slug || ""} onChange={(e) => setForm({ ...form, slug: e.target.value })} /></label></div>
        <label className="form-group">Summary<input className="form-control" value={form.summary || ""} onChange={(e) => setForm({ ...form, summary: e.target.value })} /></label>
        {form.sections.map((section, index) => (
          <div className="card" key={index}>
            <div className="card-body">
              <div className="form-row"><input className="form-control" value={section.sectionKey} onChange={(e) => updateSection(index, "sectionKey", e.target.value)} /><input className="form-control" value={section.contentType} onChange={(e) => updateSection(index, "contentType", e.target.value)} /></div>
              <textarea className="form-control" value={section.content} onChange={(e) => updateSection(index, "content", e.target.value)} />
            </div>
          </div>
        ))}
        <button type="button" className="btn btn-outline-primary" onClick={() => setForm({ ...form, sections: [...form.sections, { sectionKey: "", contentType: "text", content: "", sortOrder: form.sections.length, isActive: true }] })}>Add Section</button>
        <label><input type="checkbox" checked={form.isPublished} onChange={(e) => setForm({ ...form, isPublished: e.target.checked })} /> Published</label>
        <div className="form-actions"><button type="button" className="btn-cancel" onClick={() => { setForm(blankPage); setEditing(null); }}>Clear</button><button type="submit" className="btn-submit">Save</button></div>
      </form></div>
    </div>
  );
};

export default Content;
