export interface CmsSection { id: number; sectionKey: string; contentType: string; content: string; sortOrder: number; isActive: boolean }
export interface CmsPage { id: number; title: string; slug: string; summary: string | null; sections: CmsSection[] }
export interface SiteSetting { key: string; value: string; valueType: string; isPublic: boolean }
