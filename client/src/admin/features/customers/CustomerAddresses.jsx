export function CustomerAddresses({ data }) {
  return (
    <>
      <h2>Saved addresses</h2>
      {data.addresses.length ? (
        data.addresses.map((a) => (
          <address className="card card-body" key={a.id}>
            <strong>
              {a.label}
              {a.isDefault && " · Default"}
            </strong>
            <p>
              {a.recipientName}
              <br />
              {a.addressLine1} {a.addressLine2}
              <br />
              {a.city} {a.districtOrProvince} {a.postalCode}
              <br />
              {a.country}
              <br />
              {a.phone}
            </p>
          </address>
        ))
      ) : (
        <p>No saved addresses.</p>
      )}
    </>
  );
}
