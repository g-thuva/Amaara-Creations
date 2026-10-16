import { useSettings } from "@/features/admin/features/settings/useSettings.js";
import { SettingsList } from "@/features/admin/features/settings/SettingsList.jsx";
import { SettingForm } from "@/features/admin/features/settings/SettingForm.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
export default function SettingsPage() {
  const feature = useSettings();
  return (
    <div className="admin-page">
      <div className="page-header">
        <div className="header-content">
          <div>
            <h2>Site Settings</h2>
            <p>{feature.items.length} settings</p>
          </div>
        </div>
      </div>
      <MutationFeedback {...feature.mutation} />
      <ResourceState resource={feature.resource}>
        <SettingsList {...feature} />
      </ResourceState>
      <SettingForm {...feature} pending={feature.mutation.pending} />
    </div>
  );
}
