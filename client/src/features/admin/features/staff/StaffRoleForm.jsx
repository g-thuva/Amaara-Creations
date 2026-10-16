export function StaffRoleForm({ resource, submit, mutation }) {
  return (
    <form className="card card-body add-product-form" onSubmit={submit}>
      <h2>Replace staff role assignment</h2>
      <label>
        Verified account email
        <input className="form-control" type="email" name="email" required />
      </label>
      <fieldset>
        <legend>New roles (leave all clear to remove staff access)</legend>
        {resource.data?.roles.map((role) => (
          <label className="s-check" key={role}>
            <input type="checkbox" name="roles" value={role} /> {role}
          </label>
        ))}
      </fieldset>
      <label>
        Reason
        <textarea
          className="form-control"
          name="reason"
          maxLength="500"
          required
        />
      </label>
      <button className="btn btn-primary" disabled={mutation.pending}>
        Review staff change
      </button>
    </form>
  );
}
