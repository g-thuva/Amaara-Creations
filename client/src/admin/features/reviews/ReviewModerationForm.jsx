export function ReviewModerationForm({ row, pending, review }) {
  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        const f = new FormData(e.currentTarget);
        review(
          { id: row.id, rowVersion: row.rowVersion, status: f.get("status") },
          f.get("reason"),
        );
      }}
    >
      <label>
        New state
        <select className="form-control" name="status">
          {["Published", "Hidden", "Deleted"].map((s) => (
            <option key={s}>{s}</option>
          ))}
        </select>
      </label>
      <label>
        Reason
        <textarea
          className="form-control"
          name="reason"
          required
          maxLength="500"
        />
      </label>
      <button className="btn btn-outline-primary" disabled={pending}>
        Review moderation for {row.id}
      </button>
    </form>
  );
}
