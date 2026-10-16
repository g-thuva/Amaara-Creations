export function PrivateAssetsList({ design, pending, download }) {
  return (
    <section className="card card-body">
      <h2>Artwork and generated previews</h2>
      {design.assets.length ? (
        design.assets.map((asset) => (
          <button
            className="btn btn-outline-primary"
            key={asset.id}
            disabled={pending}
            onClick={() => download(asset)}
          >
            Download {asset.assetType}: {asset.originalFileName}
          </button>
        ))
      ) : (
        <p>No artwork supplied.</p>
      )}
    </section>
  );
}
