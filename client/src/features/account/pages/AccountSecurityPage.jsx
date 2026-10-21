import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';
import AccountSecurityForm from '@/features/account/components/AccountSecurityForm.jsx';
import AuthenticatorSettings from '@/features/account/components/AuthenticatorSettings.jsx';
import SessionList from '@/features/account/components/SessionList.jsx';

export default function AccountSecurityPage() {
  return <><PageHeader title="Account security">Manage your password and sign-in sessions.</PageHeader><AccountSecurityForm /><AuthenticatorSettings /><SessionList /></>;
}
