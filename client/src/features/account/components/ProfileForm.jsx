import { useToast } from '@/shared/components/feedback/ToastProvider.jsx';
import { useAuthenticatedMutation } from '@/features/auth/hooks/useAuthenticatedMutation.js';
import { useEffect, useState } from 'react';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { accountApi } from '@/features/account/accountApi.js';
import { avatarApi } from '@/features/account/avatarApi.js';
import { Button } from '@/shared/components/ui/Button.jsx';
import { Media } from '@/shared/components/ui/Media.jsx';
import AvatarCropDialog from './AvatarCropDialog.jsx';
export default function ProfileForm({ profile, reload }) {
  const { updateUser, user } = useAuth();
  const { notify } = useToast();
  const { mutate, pending } = useAuthenticatedMutation();
  const [uploading, setUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(null);
  const [dirty, setDirty] = useState(false);
  const [photo, setPhoto] = useState(null);
  useEffect(() => {
    if (!dirty) return undefined;
    const warn = event => { event.preventDefault(); event.returnValue = ''; };
    window.addEventListener('beforeunload', warn);
    return () => window.removeEventListener('beforeunload', warn);
  }, [dirty]);
  const save = async e => {
    e.preventDefault(); const data = new FormData(e.currentTarget);
    await mutate(async () => { const updated = await accountApi.updateProfile({ name: data.get('name'), phone: data.get('phone') || null, address: data.get('address') || null, avatarUrl: profile.avatarUrl || null }); updateUser(updated); setDirty(false); }, reload, 'Profile saved.');
  };
  const upload = async e => {
    const file = e.target.files?.[0]; if (!file) return;
    if (!['image/jpeg', 'image/png', 'image/webp', 'image/gif'].includes(file.type) || file.size > 5 * 1024 * 1024) { notify('Choose a JPG, PNG, WebP or GIF image smaller than 5 MB.', 'error'); e.target.value = ''; return; }
    setPhoto(file); e.target.value = '';
  };
  const savePhoto = async file => {
    setUploading(true);
    setUploadProgress(null);
    try { return await mutate(async () => { const uploaded = await avatarApi.uploadAvatar(file, event => { if (event.total > 0) setUploadProgress(Math.min(100, Math.round(event.loaded / event.total * 100))); }); const avatarUrl = uploaded.fileUrl; await accountApi.updateAvatar(avatarUrl); updateUser({ ...user, avatarUrl }); }, reload, 'Profile photo updated.'); }
    finally { setUploading(false); setUploadProgress(null); }
  };
  return <>
    {photo && <AvatarCropDialog file={photo} onClose={() => setPhoto(null)} onSave={savePhoto} pending={pending || uploading} uploadProgress={uploadProgress} />}

    <section className="s-profile-photo" aria-labelledby="profile-photo-title">
      <div className="s-profile-avatar">
        {profile.avatarUrl ? <Media src={profile.avatarUrl} alt={`${profile.name}'s profile photo`} /> : <span className="s-profile-initials" role="img" aria-label="Profile initials">{(profile.name || "A").split(/\s+/).slice(0, 2).map(word => word[0]).join("").toUpperCase()}</span>}
      </div>
      <div className="s-profile-photo-copy">
        <p className="s-panel-kicker">Personalise your account</p>
        <h2 id="profile-photo-title">Profile photo</h2>
        <p className="s-meta">Add a photo so your account feels familiar when you return.</p>
        <label className="s-upload-control">
          <span>Choose a photo</span>
          <input type="file" accept="image/jpeg,image/png,image/webp,image/gif" disabled={pending || uploading || dirty} onChange={upload} />
        </label>
        <p className="s-meta">JPG, PNG, WebP or GIF · up to 5 MB. {dirty && "Save your profile details before changing the photo."}</p>
      </div>
    </section>

    <form onSubmit={save} onChange={() => setDirty(true)} className="s-form s-profile-form">
      <header className="s-profile-form-header">
        <p className="s-panel-kicker">Personal details</p>
        <h2>Keep your information current</h2>
        <p className="s-meta">These details help us prepare orders and contact you about your Amaara creations.</p>
      </header>
      <div className="s-profile-fields">
        <label>Full name<input name="name" autoComplete="name" required maxLength="200" defaultValue={profile.name} /></label>
        <label>Email<input value={profile.email} type="email" readOnly aria-describedby="email-help" /></label>
        <p id="email-help" className="s-meta">Your sign-in email cannot be changed here.</p>
        <label>Phone<input name="phone" type="tel" autoComplete="tel" maxLength="50" defaultValue={profile.phone || ''} /></label>
        <label>Address<textarea name="address" autoComplete="street-address" maxLength="500" defaultValue={profile.address || ''} /></label>
      </div>
      <div className="s-profile-form-footer">
        <p className="s-meta">{dirty ? 'You have unsaved changes.' : 'Your profile is up to date.'}</p>
        <Button type="submit" disabled={pending || !dirty}>{pending ? 'Saving…' : 'Save profile'}</Button>
      </div>
    </form>
  </>;
}
