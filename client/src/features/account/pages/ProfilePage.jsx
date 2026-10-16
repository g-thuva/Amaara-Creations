import { useResource } from '@/shared/hooks/useResource.js';
import { accountApi } from '@/features/account/accountApi.js';
import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';
import ProfileForm from '@/features/account/components/ProfileForm.jsx';
export default function ProfilePage() {
  const data = useResource(accountApi.getProfile, 'profile');
  return <><PageHeader title="Your profile">Keep your details up to date.</PageHeader>{data.loading ? <Skeleton count={2}/> : data.error ? <ErrorState error={data.error} retry={data.reload}/> : <ProfileForm key={data.data.updatedAt} profile={data.data} reload={data.reload}/>}</>;
}
