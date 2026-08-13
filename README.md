🛍️ CreationStore – Digital E-Commerce Platform

A full-stack e-commerce web application for selling digital products, built with ASP.NET Core Web API and Blazor.

The project focuses on a complete shopping workflow, including authentication, product browsing, cart management, order processing, online payment, and an administrator management panel.

📌 About The Project

CreationStore is a digital e-commerce platform where customers can:

🔐 Register and log in

👤 Manage their profile

🛒 Add digital products to the cart

📦 Create and track orders

💳 Make online payments

📋 View order history and payment status

Administrators can manage the store through a dedicated Admin Panel:

📊 View dashboard statistics

📦 Manage products

🗂️ Manage categories

👥 Manage users and roles

🧾 Manage orders

💳 View payment transactions

👤 Manage administrator profile

🚪 Securely log out

✨ Main Features

👤 Customer

User registration and login

Authentication and authorization

Role-based access

View and update profile information

Browse products

View product details

Add/remove products from cart

Update cart quantities

Checkout

Create orders

Online payment

View order history

View payment status

🛡️ Administrator

Admin authentication

Admin dashboard

Revenue statistics

Product management

Category management

User management

User role management

Order management

Order detail view

Complete paid orders

Cancel eligible orders

Payment transaction management

Admin profile

Admin logout

💳 Payment Workflow

The application supports an online payment workflow using payment transaction data such as:

Payment method

Transaction status

Transaction reference

Transaction number

Response code

Bank code

Payment date

Paid date

Typical order flow:

Create Order
     │
     ▼
Pending Payment
     │
     ├──────────────► Payment Failed
     │
     ▼
Payment Succeeded
     │
     ▼
Paid
     │
     ▼
Completed

Orders can also be cancelled when they are in an eligible state.

Note: Payment cancellation/refund rules are handled separately from order cancellation. A successfully paid order should not be treated as refunded unless a real refund flow is implemented.

🏗️ Project Architecture

The project is separated into two main applications:

CreationStore
│
├── src
│   ├── CreationStore.API
│   │   ├── Controllers
│   │   ├── DTOs
│   │   ├── Services
│   │   ├── Models
│   │   ├── Data
│   │   └── Helpers
│   │
│   └── CreationStore.Blazor
│       ├── Components
│       ├── DTOs
│       ├── Services
│       └── Layout
│
└── tests
    └── CreationStore.Tests

Backend – CreationStore.API

Responsible for:

REST API endpoints

Authentication and authorization

Business logic

Database access

Entity Framework Core

Product, category, user and order operations

Payment transaction processing

Admin management APIs

Frontend – CreationStore.Blazor

Responsible for:

User interface

Customer pages

Admin pages

Form validation

State management

API communication

Authentication state

Admin dashboard

Order and payment management UI

Tests – CreationStore.Tests

Contains automated API tests for important application scenarios such as authentication and admin operations.

🧩 Admin Module

The Admin Panel provides a separate interface for store management.

Admin Panel
│
├── Dashboard
│   ├── Revenue statistics
│   ├── Store overview
│   └── Date filtering
│
├── Products
│   └── Product management
│
├── Categories
│   └── Category management
│
├── Orders
│   ├── Order list
│   ├── Order detail
│   ├── Complete order
│   └── Cancel order
│
├── Users
│   ├── User list
│   ├── User detail
│   └── Change user role
│
├── Payments
│   ├── Payment list
│   ├── Payment detail
│   └── Payment transactions
│
└── Profile
    ├── View profile
    ├── Edit profile
    └── Logout

Admin APIs are protected with role-based authorization so that administrator functions are not available to normal users.

🛠️ Technologies Used

Backend

⚙️ C#

🟣 ASP.NET Core Web API

🗄️ Entity Framework Core

🗃️ Microsoft SQL Server

🔐 Authentication & Role-based Authorization

📦 RESTful API

💳 Online Payment Integration

Frontend

🔵 Blazor

🎨 HTML / CSS

🧩 Bootstrap

⚡ C#

Development Tools

💻 Visual Studio / VS Code

🌿 Git

🐙 GitHub

🧪 Automated API Testing

🗂️ Important API Areas

The backend is organized around several main modules:

/api
│
├── authentication
├── products
├── categories
├── cart
├── orders
├── payments
│
└── admin
    ├── dashboard
    ├── products
    ├── categories
    ├── users
    ├── orders
    └── payments

Examples of administrator resources:

GET    /api/admin/users
GET    /api/admin/users/{userId}
PUT    /api/admin/users/{userId}/role

GET    /api/admin/orders
GET    /api/admin/orders/{orderId}

GET    /api/admin/payments
GET    /api/admin/payments/{paymentTransactionId}
GET    /api/admin/payments/order/{orderId}

🗄️ Main Business Entities

The application is centered around common e-commerce entities:

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

This structure allows the system to manage users, products, shopping carts, orders and payment transactions as connected business data.

🚀 How to Run

1. Clone the repository

git clone <your-repository-url>
cd creation-store-dotnet-blazor

2. Configure the database

Create/configure the SQL Server database connection in the backend configuration.

Check:

src/CreationStore.API/appsettings.json

or the appropriate environment-specific configuration file.

3. Configure payment settings

Configure the required payment gateway settings in the backend configuration before testing the online payment flow.

Do not commit real API keys, secrets or private credentials to GitHub.

4. Restore dependencies

dotnet restore

5. Build the solution

dotnet build

6. Run the API

From the API project:

dotnet run

7. Run the Blazor frontend

From the Blazor project:

dotnet run

The exact localhost URLs depend on the launch settings of the solution.

🧪 Testing

Before deployment, test the main application flows:

Customer

Register

Login

View profile

Edit profile

Browse products

Add product to cart

Checkout

Payment success

Payment failure

View orders

Logout

Administrator

Admin login

Redirect to Admin Dashboard

View dashboard statistics

Manage products

Manage categories

View users

Change user role

View orders

Complete paid order

Cancel eligible order

View payment transactions

View/edit admin profile

Admin logout

🎯 Learning & Development Goals

This project was developed to practice building a complete modern web application with:

✅ Layered application architecture

✅ REST API development

✅ Entity Framework Core

✅ SQL Server database design

✅ Authentication and authorization

✅ Role-based access control

✅ DTO-based API communication

✅ State management in Blazor

✅ E-commerce business workflow

✅ Order and payment processing

✅ Admin dashboard development

✅ Automated API testing

✅ Git and GitHub workflow

📸 Screenshots

Suggested screenshots for the repository:

docs/
├── home.png
├── product-detail.png
├── cart.png
├── checkout.png
├── payment.png
├── admin-dashboard.png
├── admin-orders.png
├── admin-users.png
└── admin-payments.png

Add screenshots here to showcase the project UI on GitHub.

🔒 Security Notes

For production deployment:

Never commit passwords or API secrets.

Store sensitive configuration using environment variables or a secure secret manager.

Use HTTPS.

Validate all user input on the backend.

Protect administrator APIs with role-based authorization.

Do not expose private payment credentials in the frontend.

📁 Repository Structure

creation-store-dotnet-blazor/
│
├── src/
│   ├── CreationStore.API/
│   │   ├── Controllers/
│   │   ├── Data/
│   │   ├── DTOs/
│   │   ├── Helpers/
│   │   ├── Models/
│   │   └── Services/
│   │
│   └── CreationStore.Blazor/
│       ├── Components/
│       ├── DTOs/
│       ├── Services/
│       └── wwwroot/
│
├── tests/
│   └── CreationStore.Tests/
│
└── README.md

👨‍💻 Project

CreationStore – Digital E-Commerce Platform

Built as a full-stack web development project using ASP.NET Core Web API + Blazor + SQL Server.

📬 Contact

For questions or collaboration, please contact the project author through the contact information provided on the GitHub profile.

© 2026 CreationStore