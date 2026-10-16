import { Link, useParams } from "react-router-dom";
import { useResource } from "../../../hooks/useResource.js";
import { customersApi } from "./customersApi.js";
import { ResourceState } from "../../shared/components/ResourceState.jsx";
import { CustomerSummary } from "./CustomerSummary.jsx";
import { CustomerAddresses } from "./CustomerAddresses.jsx";
import { CustomerActivity } from "./CustomerActivity.jsx";
export default function CustomerDetailPage() {
  const { id } = useParams();
  const resource = useResource(
    (signal) => customersApi.getOperations(id, signal),
    id,
  );
  return (
    <div className="admin-page">
      <Link to="/admin/customers">All customers</Link>
      <h1>Customer details</h1>
      <ResourceState resource={resource}>
        {resource.data && (
          <>
            <CustomerSummary data={resource.data} />
            <CustomerAddresses data={resource.data} />
            <CustomerActivity data={resource.data} />
          </>
        )}
      </ResourceState>
    </div>
  );
}
