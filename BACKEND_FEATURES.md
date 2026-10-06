# 🚀 Backend Features Required for Amaara Creations

Based on comprehensive frontend analysis, here are all the features that need to be implemented in the .NET 8 + SQL Server backend.

---

## 📋 **Table of Contents**

1. [Authentication & Authorization](#1-authentication--authorization)
2. [Product Management](#2-product-management)
3. [Cart Management](#3-cart-management)
4. [Order Management](#4-order-management)
5. [Wishlist Management](#5-wishlist-management)
6. [Review Management](#6-review-management)
7. [Custom Product Builder](#7-custom-product-builder)
8. [User Profile Management](#8-user-profile-management)
9. [Admin Dashboard](#9-admin-dashboard)
10. [Admin Features](#10-admin-features)

---

## 🔐 **1. Authentication & Authorization**

### **Features:**
- ✅ User Registration
- ✅ User Login
- ✅ User Logout
- ✅ Password Validation
- ✅ JWT Token Authentication
- ✅ Role-Based Authorization (Admin/Customer)
- ✅ Forgot Password (UI exists)
- ✅ Change Password (UI exists)

### **API Endpoints Needed:**
```
POST   /api/auth/register          - Register new user
POST   /api/auth/login             - Login user (returns JWT token)
POST   /api/auth/logout             - Logout user
POST   /api/auth/refresh-token     - Refresh JWT token
GET    /api/auth/me                 - Get current user info
POST   /api/auth/forgot-password   - Request password reset
POST   /api/auth/reset-password    - Reset password with token
POST   /api/auth/change-password   - Change password (authenticated)
```

### **Data Models:**
- **User** (Id, Name, Email, PasswordHash, Phone, Address, Role, CreatedAt, UpdatedAt)
- **UserRole** (Customer, Admin)

---

## 🛍️ **2. Product Management**

### **Features:**
- ✅ List all products
- ✅ Get product by ID
- ✅ Search products
- ✅ Filter by category (Wedding, Car, Custom)
- ✅ Admin: Add product
- ✅ Admin: Edit product
- ✅ Admin: Delete product
- ✅ Admin: View all products with stock status
- ✅ Product stock management
- ✅ Out of stock indicator

### **API Endpoints Needed:**
```
GET    /api/products                - Get all products (with pagination, search, filter)
GET    /api/products/{id}           - Get product by ID
GET    /api/products?category={cat} - Filter by category
GET    /api/products?search={term}  - Search products
POST   /api/products                - Create product (Admin only)
PUT    /api/products/{id}           - Update product (Admin only)
DELETE /api/products/{id}            - Delete product (Admin only)
```

### **Data Models:**
- **Product** (Id, Name, Price, Description, ImageUrl, Category, Stock, IsOutOfStock, CreatedAt, UpdatedAt)

---

## 🛒 **3. Cart Management**

### **Features:**
- ✅ Add item to cart
- ✅ View cart items
- ✅ Update quantity
- ✅ Remove item from cart
- ✅ Clear cart
- ✅ Calculate total
- ✅ Cart persistence (user-specific)

### **API Endpoints Needed:**
```
GET    /api/cart                    - Get user's cart
POST   /api/cart                    - Add item to cart
PUT    /api/cart/{itemId}           - Update cart item quantity
DELETE /api/cart/{itemId}           - Remove item from cart
DELETE /api/cart                    - Clear entire cart
POST   /api/cart/checkout           - Proceed to checkout (creates order)
```

### **Data Models:**
- **CartItem** (Id, UserId, ProductId, Quantity, CreatedAt, UpdatedAt)

---

## 📦 **4. Order Management**

### **Features:**
- ✅ Create order from cart (Checkout)
- ✅ View user's orders
- ✅ View order details
- ✅ Order status tracking (Pending, Processing, Shipped, Delivered, Cancelled)
- ✅ Admin: View all orders
- ✅ Admin: Update order status
- ✅ Order items list
- ✅ Order total calculation

### **API Endpoints Needed:**
```
GET    /api/orders                   - Get user's orders
GET    /api/orders/{id}              - Get order details
POST   /api/orders                   - Create order (checkout)
PUT    /api/orders/{id}/status       - Update order status (Admin only)
GET    /api/admin/orders             - Get all orders (Admin only)
GET    /api/admin/orders/{id}        - Get order details (Admin only)
```

### **Data Models:**
- **Order** (Id, OrderNumber, UserId, OrderDate, Total, Status, ShippingAddress, CreatedAt, UpdatedAt)
- **OrderItem** (Id, OrderId, ProductId, Quantity, Price, Subtotal)

---

## ❤️ **5. Wishlist Management**

### **Features:**
- ✅ Add product to wishlist
- ✅ View wishlist
- ✅ Remove from wishlist
- ✅ Add wishlist item to cart
- ✅ Wishlist persistence

### **API Endpoints Needed:**
```
GET    /api/wishlist                 - Get user's wishlist
POST   /api/wishlist                 - Add product to wishlist
DELETE /api/wishlist/{productId}     - Remove from wishlist
POST   /api/wishlist/{productId}/cart - Add wishlist item to cart
```

### **Data Models:**
- **WishlistItem** (Id, UserId, ProductId, CreatedAt)

---

## ⭐ **6. Review Management**

### **Features:**
- ✅ Add product review
- ✅ View product reviews
- ✅ Update own review
- ✅ Delete own review
- ✅ Star rating (1-5)
- ✅ Review comments
- ✅ Admin: View all reviews
- ✅ Admin: Delete any review

### **API Endpoints Needed:**
```
GET    /api/products/{id}/reviews    - Get product reviews
POST   /api/products/{id}/reviews    - Add review (authenticated)
PUT    /api/reviews/{id}              - Update review (owner only)
DELETE /api/reviews/{id}              - Delete review (owner/admin)
GET    /api/admin/reviews            - Get all reviews (Admin only)
DELETE /api/admin/reviews/{id}       - Delete review (Admin only)
```

### **Data Models:**
- **Review** (Id, ProductId, UserId, Rating, Comment, CreatedAt, UpdatedAt)

---

## 🎨 **7. Custom Product Builder**

### **Features:**
- ✅ Create custom product design
- ✅ Save custom design
- ✅ Add custom product to cart
- ✅ Custom product specifications (Text, Font, Width, Height)
- ✅ Price calculation based on area (Width × Height × Rate)
- ✅ Live preview (frontend only)
- ✅ Order custom product

### **API Endpoints Needed:**
```
POST   /api/custom-products           - Create custom product
GET    /api/custom-products           - Get user's custom products
GET    /api/custom-products/{id}      - Get custom product details
POST   /api/custom-products/{id}/cart - Add custom product to cart
POST   /api/custom-products/{id}/order - Order custom product directly
```

### **Data Models:**
- **CustomProduct** (Id, UserId, Text, Font, Width, Height, Area, RatePerSqCm, TotalCost, Status, CreatedAt, UpdatedAt)

---

## 👤 **8. User Profile Management**

### **Features:**
- ✅ View profile
- ✅ Edit profile (Name, Phone, Address)
- ✅ View orders
- ✅ View wishlist
- ✅ Change password
- ✅ Profile avatar

### **API Endpoints Needed:**
```
GET    /api/users/profile             - Get user profile
PUT    /api/users/profile             - Update user profile
POST   /api/users/change-password    - Change password
GET    /api/users/profile/avatar      - Get user avatar
POST   /api/users/profile/avatar      - Upload user avatar
```

### **Data Models:**
- **User** (extended with: AvatarUrl, Phone, Address)

---

## 📊 **9. Admin Dashboard**

### **Features:**
- ✅ Total Revenue statistics
- ✅ Total Orders count
- ✅ Total Customers count
- ✅ Average Rating
- ✅ Recent Orders list
- ✅ Top Selling Products
- ✅ Revenue trends
- ✅ Order status breakdown

### **API Endpoints Needed:**
```
GET    /api/admin/dashboard           - Get dashboard statistics
GET    /api/admin/dashboard/revenue  - Get revenue statistics
GET    /api/admin/dashboard/orders   - Get order statistics
GET    /api/admin/dashboard/products  - Get product statistics
```

---

## 🔧 **10. Admin Features**

### **10.1 Admin Products Management**
- ✅ View all products in table format
- ✅ Add new product
- ✅ Edit existing product
- ✅ Delete product
- ✅ View stock status
- ✅ Filter by category
- ✅ Search products

**API Endpoints:**
```
GET    /api/admin/products            - Get all products (admin view)
POST   /api/admin/products            - Create product
PUT    /api/admin/products/{id}      - Update product
DELETE /api/admin/products/{id}      - Delete product
```

### **10.2 Admin Orders Management**
- ✅ View all orders
- ✅ View order details
- ✅ Update order status
- ✅ Filter orders by status
- ✅ Search orders
- ✅ View customer information

**API Endpoints:**
```
GET    /api/admin/orders              - Get all orders
GET    /api/admin/orders/{id}         - Get order details
PUT    /api/admin/orders/{id}         - Update order
PUT    /api/admin/orders/{id}/status  - Update order status
```

### **10.3 Admin Customers Management**
- ✅ View all customers
- ✅ View customer details
- ✅ View customer orders
- ✅ Customer statistics
- ✅ Search customers

**API Endpoints:**
```
GET    /api/admin/customers           - Get all customers
GET    /api/admin/customers/{id}      - Get customer details
GET    /api/admin/customers/{id}/orders - Get customer orders
GET    /api/admin/customers/{id}/stats - Get customer statistics
```

### **10.4 Admin Reviews Management**
- ✅ View all reviews
- ✅ Delete review
- ✅ Filter reviews
- ✅ Review moderation
- ✅ Review statistics

**API Endpoints:**
```
GET    /api/admin/reviews             - Get all reviews
DELETE /api/admin/reviews/{id}        - Delete review
GET    /api/admin/reviews/stats       - Get review statistics
```

---

## 📸 **Additional Features**

### **Image Upload**
- ✅ Product image upload
- ✅ Custom product image generation/save
- ✅ User avatar upload
- ✅ Image validation
- ✅ Image storage (File system or Azure Blob Storage)

**API Endpoints:**
```
POST   /api/upload/product-image     - Upload product image
POST   /api/upload/avatar            - Upload user avatar
```

---

## 🗄️ **Database Schema Summary**

### **Core Tables:**
1. **Users** - User accounts
2. **Products** - Product catalog
3. **CartItems** - Shopping cart
4. **Orders** - Order headers
5. **OrderItems** - Order line items
6. **WishlistItems** - User wishlists
7. **Reviews** - Product reviews
8. **CustomProducts** - Custom product designs

### **Additional Tables:**
- **Categories** (optional - for better category management)
- **OrderStatusHistory** (optional - for tracking order status changes)

---

## 🔄 **Data Flow**

### **Customer Flow:**
1. Register → Login → Browse Products → Add to Cart/Wishlist → Checkout → Order → Review

### **Admin Flow:**
1. Login → Dashboard → Manage Products/Orders/Customers/Reviews

---

## 📝 **Priority Implementation Order**

### **Foundation: Core Features (Must Have)**
1. ✅ Authentication & Authorization
2. ✅ Product Management (CRUD)
3. ✅ Cart Management
4. ✅ Order Management (Create & View)

### **Authentication: Enhanced Features (Should Have)**
5. ✅ Review Management
6. ✅ Wishlist Management
7. ✅ User Profile Management
8. ✅ Custom Product Builder

### **Catalogue Media: Admin Features (Important)**
9. ✅ Admin Dashboard
10. ✅ Admin Products Management
11. ✅ Admin Orders Management
12. ✅ Admin Customers Management
13. ✅ Admin Reviews Management

### **Storefront: Additional Features (Nice to Have)**
14. ✅ Image Upload
15. ✅ Email Notifications
16. ✅ Search & Filtering
17. ✅ Pagination

---

## 🔒 **Security Considerations**

1. **JWT Token Authentication** - Secure token-based auth
2. **Password Hashing** - Use ASP.NET Core Identity
3. **Role-Based Authorization** - Admin vs Customer
4. **Input Validation** - Server-side validation
5. **SQL Injection Prevention** - EF Core handles this
6. **XSS Protection** - Input sanitization
7. **CORS Configuration** - Already configured

---

## 📦 **Required NuGet Packages**

- ✅ `Microsoft.EntityFrameworkCore.SqlServer` - Already added
- ✅ `Microsoft.EntityFrameworkCore.Tools` - Already added
- ⚠️ `Microsoft.AspNetCore.Identity.EntityFrameworkCore` - For authentication
- ⚠️ `Microsoft.AspNetCore.Authentication.JwtBearer` - For JWT tokens
- ⚠️ `AutoMapper` - For object mapping (optional)
- ⚠️ `FluentValidation` - For validation (optional)
- ⚠️ `Swashbuckle.AspNetCore` - Already added

---

## 🎯 **Summary**

**Total API Endpoints Needed: ~45-50 endpoints**

**Main Controllers Required:**
1. `AuthController` - Authentication
2. `ProductsController` - Product management
3. `CartController` - Cart operations
4. `OrdersController` - Order management
5. `WishlistController` - Wishlist operations
6. `ReviewsController` - Review management
7. `CustomProductsController` - Custom products
8. `UsersController` - User profile
9. `AdminController` - Admin dashboard & management
10. `UploadController` - Image uploads

**Total Data Models: ~8-10 entities**

This comprehensive list covers all features visible in your frontend. Each feature has clear requirements and can be implemented systematically.

