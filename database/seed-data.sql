USE CreationStoreDb;
GO

-- =========================
-- Seed Roles
-- =========================
INSERT INTO Roles (RoleName)
VALUES
('Admin'),
('Member');
GO

-- =========================
-- Seed Users
-- Lưu ý:
-- PasswordHash hiện tại đang để demo là '111'.
-- Sau này khi làm Auth API sẽ đổi sang hash password thật.
-- =========================
INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, IsActive)
VALUES
('admin', '1', N'Admin Creation Store', 'admin@gmail.com', '0900000001', 1),
('member01', '2', N'Nguyễn Văn Member', 'member01@gmail.com', '0900000002', 1);
GO

-- =========================
-- Seed UserRoles
-- RoleId:
-- 1 = Admin
-- 2 = Member
-- =========================
INSERT INTO UserRoles (UserId, RoleId)
VALUES
(1, 1), -- admin -> Admin
(2, 2); -- member01 -> Member
GO

-- =========================
-- Seed Categories
-- =========================
INSERT INTO Categories (CategoryName, Description)
VALUES
(N'Entertainment', N'Services related to entertainment, movies, music, and streaming platforms'),
(N'Work', N'Software licenses and productivity tools for work'),
(N'Study', N'Learning platforms, study tools, and education services'),
(N'AI World', N'Artificial intelligence tools and smart applications');
GO

-- =========================
-- Seed Products
-- ImageUrl dùng theo folder:
-- src/CreationStore.Blazor/wwwroot/images
-- Ví dụ: wwwroot/images/spotify02.jpg => /images/spotify02.jpg
-- =========================
INSERT INTO Products
(ProductName, Description, Price, IsActive, ImageUrl, ValidityDays, CategoryId)
VALUES
-- =========================
-- CategoryId = 1: Entertainment
-- =========================
(N'Calm Premium 12 Months',
 N'Access to Calm Premium account for 12 months',
 450000, 1, '/images/Calm.png', 365, 1),

(N'Spotify Premium 3 Months',
 N'Access to Spotify Premium for 3 months',
 350000, 1, '/images/spotify02.jpg', 90, 1),

(N'YouTube Premium 6 Months',
 N'Enjoy YouTube Premium without ads for 6 months',
 500000, 1, '/images/logoyoutube.jpg', 180, 1),

(N'Netflix Premium 1 Month',
 N'1 month Netflix Premium access via shared account',
 260000, 1, '/images/netflix02.jpg', 30, 1),

-- =========================
-- CategoryId = 2: Work
-- =========================
(N'Windows 10 Professional - CD Key',
 N'Lifetime license for Windows 10 Professional',
 400000, 1, '/images/windows10_cdkey.png.png', NULL, 2),

(N'Windows 11 Professional - CD Key',
 N'Lifetime license for Windows 11 Professional',
 400000, 1, '/images/windows11_cdkey.png.png', NULL, 2),

(N'Microsoft Office 2019 Professional Plus for Windows',
 N'Permanent license for Microsoft Office 2019 Professional Plus for Windows',
 989000, 1, '/images/office2019_win.png.png', NULL, 2),

(N'Microsoft Office 2019 Home & Business for Mac',
 N'Permanent license for Microsoft Office 2019 Home & Business for Mac',
 999000, 1, '/images/office2019_mac.png.png', NULL, 2),

(N'Grammarly Premium AI 1 Month',
 N'Grammarly Premium with AI writing support for 1 month',
 400000, 1, '/images/grammarly.png', 30, 2),

-- =========================
-- CategoryId = 3: Study
-- =========================
(N'Coursera Course 7 Days',
 N'Access to a Coursera course for 7 days',
 150000, 1, '/images/coursera.png', 7, 3),

(N'Quizlet Plus 1 Year',
 N'1-year subscription to Quizlet Plus',
 700000, 1, '/images/quizlet.jpg', 365, 3),

(N'Duolingo Super 1 Month',
 N'Access to Duolingo Super for 1 month',
 150000, 1, '/images/Duolingo.jpg', 30, 3),

-- =========================
-- CategoryId = 4: AI World
-- =========================
(N'ChatGPT Plus 1 Month',
 N'ChatGPT Plus access for 1 month',
 500000, 1, '/images/chatgpt-1024x623.jpg', 30, 4),

(N'Google AI Pro Gemini 1 Month',
 N'Google Gemini AI Pro access for 1 month',
 300000, 1, '/images/gemini.jpg', 30, 4),

(N'Curiosity AI Pro 14 Days',
 N'Premium access to Curiosity AI for 14 days',
 240000, 1, '/images/curiosity.png', 14, 4),

(N'Super Grok AI 1 Month',
 N'Access to Grok AI for 1 month',
 800000, 1, '/images/Grok.jpg', 30, 4);
GO