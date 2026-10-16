export function ProductionTransitionForm({ data, pending, production }) {
  return (
    <section className="card card-body">
      <h2>Production transition</h2>
      {data.availableProductionTransitions?.length ? (
        <form className="add-product-form" onSubmit={production}>
          <label>
            Next production state
            <select className="form-control" name="status">
              {data.availableProductionTransitions.map((s) => (
                <option key={s}>{s}</option>
              ))}
            </select>
          </label>
          <label>
            Production note
            <textarea
              className="form-control"
              name="note"
              maxLength="500"
              required
            />
          </label>
          <button className="btn btn-primary" disabled={pending}>
            Update production
          </button>
        </form>
      ) : (
        <p>
          No transition available. Review order, proof and production state.
        </p>
      )}
    </section>
  );
}
