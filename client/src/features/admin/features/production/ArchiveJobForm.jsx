export function ArchiveJobForm({ design, pending, setArchive }) {
  return (
    <form
      className="card card-body add-product-form"
      onSubmit={(e) => {
        e.preventDefault();
        setArchive({
          status: "Archived",
          note: new FormData(e.currentTarget).get("note"),
          rowVersion: design.rowVersion,
        });
      }}
    >
      <h2>Archive fulfilled job</h2>
      <label>
        Archive reason
        <textarea
          className="form-control"
          name="note"
          required
          maxLength="500"
        />
      </label>
      <button className="btn btn-outline-primary" disabled={pending}>
        Review archive
      </button>
    </form>
  );
}
