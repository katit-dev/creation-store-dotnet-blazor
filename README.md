# 🛒 Creation Store - Digital Product E-commerce

Creation Store is a digital product e-commerce web application built with **ASP.NET Core Web API, Blazor WebAssembly, SQL Server, and Entity Framework Core**.

The project provides a complete e-commerce workflow including authentication, product browsing, shopping cart, checkout, online payment, order management, and an administration panel.

## 🚀 Technologies

- ASP.NET Core Web API
- Blazor WebAssembly
- C#
- Entity Framework Core
- SQL Server
- JWT Authentication
- Role-based Authorization
- RESTful API
- Swagger / OpenAPI
- Bootstrap
- Git & GitHub

## ✨ Main Features

### 👤 User

- Register, login and logout
- JWT authentication
- Role-based authorization
- View and update profile
- Browse and filter products
- View product details
- Add, update and remove cart items
- Checkout and create orders
- Online payment
- View order history
- View order details
- View payment status
- Cancel eligible orders

### 👨‍💼 Admin

- Admin authentication and logout
- Admin Dashboard
- Revenue statistics
- Product management
- Category management
- User management
- View user details
- Change user roles
- Order management
- View order details
- Complete paid orders
- Cancel eligible orders
- Payment transaction management
- View payments by order
- Admin profile management

## 💳 Payment

The application supports an online payment workflow for customer orders.

Payment flow:

Create Order → Pending Payment → Payment Gateway → Payment Succeeded → Paid → Completed

Payment transactions contain information such as payment method, transaction status, transaction reference, transaction number, response code, bank code, payment date, and paid date.

The system also validates order and payment states to prevent invalid operations, such as completing unpaid orders or cancelling successfully paid orders without refund support.

## 🏗️ Architecture

The project is separated into Backend and Frontend applications.

    CreationStore
    │
    ├── src
    │   ├── CreationStore.API
    │   │   ├── Controllers
    │   │   ├── DTOs
    │   │   ├── Data
    │   │   ├── Models
    │   │   ├── Services
    │   │   │   ├── Interfaces
    │   │   │   └── Implementations
    │   │   └── Helpers
    │   │
    │   └── CreationStore.Blazor
    │       ├── Components
    │       │   ├── Layout
    │       │   └── Pages
    │       ├── DTOs
    │       └── Services
    │
    ├── tests
    │   └── CreationStore.Tests
    │
    └── README.md

### Backend

The ASP.NET Core Web API is responsible for:

- Authentication and authorization
- Business logic
- Database access
- Product and category management
- Cart and order processing
- Payment processing
- User and role management
- Admin management APIs

### Frontend

The Blazor WebAssembly application is responsible for:

- User interface
- Product browsing
- Shopping cart
- Checkout
- Payment UI
- Order management
- User profile
- Admin Dashboard
- Admin management pages
- Application state management

## 🗄️ Main Entities

    User
     │
     ├── UserRole
     │
     └── Order
           │
           ├── OrderItem
           │
           └── PaymentTransaction

    Category
     │
     └── Product

    Cart
     │
     └── CartItem

## 🔐 Security

- JWT-based authentication
- Role-based authorization
- Protected Admin endpoints
- Backend validation
- Client-side validation
- Secure authentication state
- Admin-only management features
- Payment credentials kept on the backend

## 🧪 Testing

The project includes automated API tests for important application scenarios.

Tested areas include:

- Authentication
- Authorization
- Admin User Management
- Admin Order Management
- Payment Management
- Invalid requests
- Unauthorized requests
- Resource not found
- Invalid order and payment states

## 📊 Admin Panel

- Dashboard
- Revenue Statistics
- Product Management
- Category Management
- User Management
- User Details
- Change User Role
- Order Management
- Order Details
- Complete Order
- Cancel Order
- Payment Management
- Payment Details
- Admin Profile
- Logout

## 🛠️ How to Run

### 1. Clone the repository

    git clone <your-repository-url>
    cd creation-store-dotnet-blazor

### 2. Configure SQL Server

Update the database connection string in:

    src/CreationStore.API/appsettings.json

### 3. Restore dependencies

    dotnet restore

### 4. Build the solution

    dotnet build

### 5. Run the Backend

    cd src/CreationStore.API
    dotnet run

### 6. Run the Frontend

Open another terminal:

    cd src/CreationStore.Blazor
    dotnet run

The application URLs depend on the project's launch settings.

## 🎯 Project Goals

This project was developed to practice building a complete full-stack .NET application with:

- RESTful API development
- Layered architecture
- Entity Framework Core
- SQL Server database design
- JWT authentication
- Role-based authorization
- DTO-based API communication
- Blazor WebAssembly
- State management
- E-commerce business logic
- Order and payment processing
- Admin Dashboard development
- Automated API testing
- Git and GitHub workflow

## 📌 Project Status

The core functionality of Creation Store has been implemented, including:

- Authentication
- Authorization
- Product management
- Category management
- Shopping cart
- Checkout
- Orders
- Online payment
- Admin Dashboard
- Admin User Management
- Admin Order Management
- Admin Payment Management
- Admin Profile
- Logout

Further UI/UX improvements and responsive optimization can be added in future iterations.

## 👨‍💻 Author

**Khanh Vy**

Creation Store - Digital E-commerce Platform

Built with **ASP.NET Core Web API + Blazor WebAssembly + SQL Server**.

This project was developed for learning and portfolio purposes.