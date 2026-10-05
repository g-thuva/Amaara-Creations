import { useState } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { useStore } from '../contexts/StoreContext';
import { useResource } from '../hooks/useResource';
import { userApi } from '../services/userApi';
import { uploadApi } from '../services/uploadApi';
import { resolveMediaUrl } from '../services/config';
import { Button, ErrorState, Media, PageHeader, Skeleton } from '../components/storefront/UI';
function ProfileForm({ profile, reload }) {
  const { updateUser } = useAuth();
  const { mutate, pending, notify } = useStore();
  const [uploading, setUploading] = useState(false);
  const save = async e => {
    e.preventDefault(); const data = new FormData(e.currentTarget);
    await mutate(async () => { const updated = await userApi.updateProfile({ name: data.get('name'), phone: data.get('phone') || null, address: data.get('address') || null, avatarUrl: profile.avatarUrl ? resolveMediaUrl(profile.avatarUrl) : null }); updateUser(updated); }, reload, 'Profile saved.');
  };
  const upload = async e => {
    const file = e.target.files?.[0]; if (!file) return;
    if (!['image/jpeg', 'image/png', 'image/webp', 'image/gif'].includes(file.type) || file.size > 5 * 1024 * 1024) { notify('Choose a JPG, PNG, WebP or GIF image smaller than 5 MB.', 'error'); e.target.value = ''; return; }
    setUploading(true);
    await mutate(async () => { const uploaded = await uploadApi.uploadAvatar(file); await userApi.updateAvatar(resolveMediaUrl(uploaded.fileUrl)); }, reload, 'Profile photo updated.');
    setUploading(false); e.target.value = '';
  };
  return <><div className="s-profile-photo"><Media src={profile.avatarUrl} alt={`${profile.name}'s profile photo`}/><div><label className="s-field">Profile photo<input type="file" accept="image/jpeg,image/png,image/webp,image/gif" disabled={pending || uploading} onChange={upload}/></label><p className="s-meta">JPG, PNG, WebP or GIF. Up to 5 MB.</p></div></div><form onSubmit={save} className="s-form"><label>Full name<input name="name" autoComplete="name" required maxLength="200" defaultValue={profile.name}/></label><label>Email<input value={profile.email} type="email" readOnly aria-describedby="email-help"/></label><p id="email-help" className="s-meta">Your sign-in email cannot be changed here.</p><label>Phone<input name="phone" type="tel" autoComplete="tel" maxLength="50" defaultValue={profile.phone || ''}/></label><label>Address<textarea name="address" autoComplete="street-address" maxLength="500" defaultValue={profile.address || ''}/></label><Button type="submit" disabled={pending}>{pending ? 'Saving…' : 'Save profile'}</Button></form></>;
}
export default function Profile() {
  const data = useResource(userApi.getProfile, 'profile');
  return <><PageHeader title="Your profile">Keep your details up to date.</PageHeader>{data.loading ? <Skeleton count={2}/> : data.error ? <ErrorState error={data.error} retry={data.reload}/> : <ProfileForm key={data.data.updatedAt} profile={data.data} reload={data.reload}/>}</>;
}
