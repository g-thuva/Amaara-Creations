import { Link, useParams } from "react-router-dom";
import { useResource } from "@/shared/hooks/useResource.js";
import { customersApi } from "@/features/admin/features/customers/customersApi.js";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { CustomerSummary } from "@/features/admin/features/customers/CustomerSummary.jsx";
import { CustomerAddresses } from "@/features/admin/features/customers/CustomerAddresses.jsx";
import { CustomerActivity } from "@/features/admin/features/customers/CustomerActivity.jsx";
import { PageHeader } from '@/features/admin/shared/components/PageHeader.jsx';
export default function CustomerDetailPage() {
  const { id } = useParams();
  const resource = useResource(
    (signal) => customersApi.getOperations(id, signal),
    id,
  );
  return (
    <div className="admin-page">
      <Link to="/admin/customers">All customers</Link>
      <PageHeader title="Customer details" description="Review the customer’s account, delivery details and recorded activity." />
      <ResourceState resource={resource}>
        {resource.data && (
          <div className="admin-detail-grid"><div className="admin-detail-main">
            <CustomerAddresses data={resource.data} />
            <CustomerActivity data={resource.data} />
          </div><aside className="admin-detail-rail" aria-label="Customer summary"><CustomerSummary data={resource.data} /></aside></div>
        )}
      </ResourceState>
    </div>
  );
}
