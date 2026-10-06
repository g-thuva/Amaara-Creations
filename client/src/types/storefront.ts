export interface ProductMedia { id: number; url: string; altText: string | null; width: number | null; height: number | null; isPrimary: boolean }
export interface ProductVariant { id: number; name: string; priceOverride: number | null; stockQuantity: number }
export interface StoreProduct { id: number; slug: string; name: string; description: string; shortDescription: string | null; price: number; basePrice: number; category: string; categoryId: number | null; imageUrl: string; stock: number; isActive: boolean; isFeatured: boolean; isOutOfStock: boolean; media: ProductMedia[]; variants: ProductVariant[]; createdAt: string; updatedAt: string }
export interface ProductList { products: StoreProduct[]; totalCount: number; pageNumber: number; pageSize: number; totalPages: number }
export interface Category { id: number; name: string; slug: string; description: string | null; imageUrl: string | null; parentCategoryId: number | null }
export interface Collection { id: number; name: string; slug: string; description: string | null; imageUrl?: string | null; productCount: number }
export interface CmsSection { id: number; sectionKey: string; contentType: string; content: string; sortOrder: number; isActive: boolean }
export interface CmsPage { id: number; title: string; slug: string; summary: string | null; sections: CmsSection[] }
export interface SiteSetting { key: string; value: string; valueType: string; isPublic: boolean }
