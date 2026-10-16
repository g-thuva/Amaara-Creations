export function ProofUploadForm({ message, setMessage, pending, upload }) {
  return (
    <form className="add-product-form" onSubmit={upload}>
      <label>
        Customer-visible proof message
        <textarea
          className="form-control"
          maxLength="1000"
          value={message}
          onChange={(e) => setMessage(e.target.value)}
        />
      </label>
      <label>
        Proof file
        <input
          className="form-control"
          name="file"
          type="file"
          accept="image/png,image/jpeg,image/webp"
          required
        />
      </label>
      <button className="btn btn-primary" disabled={pending}>
        Upload new proof revision
      </button>
    </form>
  );
}
