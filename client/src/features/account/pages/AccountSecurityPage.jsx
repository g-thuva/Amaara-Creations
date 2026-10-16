import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';
import AccountSecurityForm from '@/features/account/components/AccountSecurityForm.jsx';

export default function AccountSecurityPage() {
  return <><PageHeader title="Account security">Manage your password and sign-in sessions.</PageHeader><AccountSecurityForm /></>;
}
