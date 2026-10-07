# Product Database Design

> Phạm vi: thiết kế database và tài liệu business rule cho Product / Multi-site / Pricing / Flash Sale.  
> Chưa triển khai API, migration hoặc thay đổi DB thật.

---

## 1. Business Rule Matrix

| Business rule | DB constraint / schema | Backend validation | Trạng thái / ghi chú |
|---|---|---|---|
| Không hard delete | Dùng `status` / archive thay vì xóa record nghiệp vụ | Backend chuyển trạng thái thay vì `DELETE` | Đã chốt theo requirement |
| Các cột non-PK nullable | Chỉ PK bắt buộc `NOT NULL` theo CMS convention | Kiểm tra field bắt buộc tại thời điểm publish/activate | Đã chốt theo convention |
| Product lifecycle | Enum Product: `draft / published / archived` | Validate transition trạng thái | Đã chốt |
| Điều kiện publish Product | Không ép bằng `NOT NULL` vì draft có thể thiếu dữ liệu | Đề xuất check: `name` + `ProductSite.slug` + >= 1 SKU + ảnh primary + giá hợp lệ | Cần lead chốt danh sách chính xác |
| SKU không được trùng | `UNIQUE(sku_code)` hoặc `UNIQUE(product_id, sku_code)` | Check duplicate trước create/update | Cần lead chốt scope |
| Slug Product unique theo website | `UNIQUE(site_id, slug)` trên `ProductSite` | Check trước create/update | Đã chốt theo requirement |
| Một Product chỉ có một config trên một Site | `UNIQUE(product_id, site_id)` trên `ProductSite` | Check duplicate | Đã chốt |
| Product thuộc một Category chính | `Product.category_id` FK -> `Category.id` | Kiểm tra Category tồn tại và hợp lệ khi gán | Đã chốt: Category 1-N Product |
| SkinConcern tách riêng và N-N với Product | `ProductSkinConcern` + `UNIQUE(product_id, skin_concern_id)` | Check duplicate | Đã chốt |
| Ảnh đại diện của Product | `ProductImage.is_primary` | Khi publish phải có ít nhất 1 primary image | Giữ `is_primary`; exact-one cần lead nếu muốn |
| Không link SKU sai Product/Site | FK riêng chưa đủ để chứng minh ownership chéo bảng | Check `sku.product_id == product_site.product_id` | Đã chốt rule |
| Một SKU chỉ có một config giá trên một ProductSite | `UNIQUE(product_site_id, sku_id)` | Check duplicate | Đã chốt |
| Giá dùng numeric, không float | `regular_price`, `sale_price`, `flash_price` dùng `numeric` | - | Đã chốt |
| Giá không âm | `CHECK(price >= 0)` khi có giá | Validate trước save | Đã chốt |
| Pricing ở Website + SKU | `ProductSiteSku` chứa giá theo `product_site_id + sku_id` | Resolve đúng Site + SKU khi hiển thị | Đã chốt |
| Flash Sale áp dụng ở SKU-level | `FlashSaleItem.sku_id` FK -> `ProductSku.id` | Xử lý flash theo SKU | Đã chốt |
| Một SKU không lặp trong cùng campaign | `UNIQUE(campaign_id, sku_id)` | Check duplicate | Đã chốt |
| Flash price < regular price của cùng SKU trên cùng Site | Không phù hợp simple `CHECK` cross-table | Lookup `ProductSiteSku` theo campaign site + SKU rồi so sánh | Đã chốt |
| Flash Sale chỉ áp dụng SKU có bán trên Site của campaign | FK riêng không đủ | Phải tồn tại `ProductSiteSku` tương ứng Site + SKU | Đã chốt |
| Campaign `end_at > start_at` | `CHECK(end_at > start_at)` khi cả hai có giá trị | Validate trước save | Đã chốt |
| Campaign overlap cùng Site + SKU | Không đơn giản bằng `CHECK` thông thường | Backend kiểm tra time interval theo policy | Cần lead chốt policy |
| Discount % không lưu dư thừa | Không tạo `discount_percent` | Tính từ regular/sale/flash price khi cần | Đã chốt |
| Chỉ hiển thị Product published trên Site active | Không phù hợp `CHECK` cross-table | Query filter Product/ProductSite status + `cms_sites.is_active` | Đã chốt |
| Featured / Best Seller | Chỉ tạo boolean nếu là manual CMS flag | Nếu calculated thì query/tính từ dữ liệu | Cần lead chốt |
| NULL / empty / inherited override | Schema cho phép NULL | Backend phải có semantics thống nhất nếu có override | Cần lead chốt semantics |
| Không dùng sold count demo làm sales thật | Không tạo transactional `sold_count` từ dữ liệu demo | Không dùng demo count cho business logic | Đã chốt |

---

## 2. Table Definitions

### Convention dùng chung

- PK dùng `uuid`.
- Chỉ PK `NOT NULL` theo quy ước CMS hiện tại.
- Các cột non-PK cho phép `NULL`.
- Timestamp dùng `timestamptz`.
- FK ưu tiên `delete: restrict`, `update: cascade`.
- Giá dùng `numeric`, không dùng `float`.

### 2.1 Product

**Mục đích:** Lưu thông tin sản phẩm gốc dùng chung toàn hệ thống. Mỗi Product thuộc một Category chính.

| Field | Data type | Nullable | Default | PK/FK / Ghi chú |
|---|---|---:|---|---|
| `id` | `uuid` | No | - | PK |
| `category_id` | `uuid` | Yes | `NULL` | FK -> `Category.id` |
| `name` | `varchar(255)` | Yes | `NULL` | Tên sản phẩm |
| `short_name` | `varchar(255)` | Yes | `NULL` | Tên ngắn / subtitle |
| `description` | `text` | Yes | `NULL` | Mô tả |
| `ingredients` | `text / jsonb` | Yes | `NULL` | Chưa chốt data type - cần lead xác nhận |
| `benefits` | `text / jsonb` | Yes | `NULL` | Chưa chốt data type - cần lead xác nhận |
| `usage_instructions` | `text / jsonb` | Yes | `NULL` | Chưa chốt data type - cần lead xác nhận |
| `status` | `enum` | Yes | `draft` | `draft / published / archived` |
| `created_at` | `timestamptz` | Yes | `NULL` | Audit |
| `updated_at` | `timestamptz` | Yes | `NULL` | Audit |

**Relations**
- Category 1-N Product.
- Product 1-N ProductSku.
- Product 1-N ProductImage.
- Product N-N SkinConcern qua `ProductSkinConcern`.
- Product N-N `cms_sites` qua `ProductSite`.

**FK rule**
- `category_id -> Category.id`
- `delete: restrict`
- `update: cascade`

**Publish required fields đề xuất**
- `Product.name`
- `ProductSite.slug`
- ít nhất 1 `ProductSku`
- ít nhất 1 ảnh primary
- ít nhất 1 giá hợp lệ

> Cần lead xác nhận danh sách bắt buộc cuối cùng.

### 2.2 ProductSku

**Mục đích:** Lưu biến thể/SKU của Product.

| Field | Data type | Nullable | Default | PK/FK / Ghi chú |
|---|---|---:|---|---|
| `id` | `uuid` | No | - | PK |
| `product_id` | `uuid` | Yes | `NULL` | FK -> `Product.id` |
| `sku_code` | `varchar(100)` | Yes | `NULL` | Unique scope cần lead chốt |
| `volume` | `numeric(10,2)` | Yes | `NULL` | Ví dụ `30` |
| `unit` | `varchar(32)` | Yes | `NULL` | Ví dụ `ml`, `g` |
| `packaging` | `varchar(255)` | Yes | `NULL` | Quy cách đóng gói |
| `status` | `enum` | Yes | `active` (đề xuất) | `active / archived` |
| `created_at` | `timestamptz` | Yes | `NULL` | Audit |
| `updated_at` | `timestamptz` | Yes | `NULL` | Audit |

**Unique cần lead chốt**
- Cách 1: `UNIQUE(sku_code)` — SKU duy nhất toàn hệ thống.
- Cách 2: `UNIQUE(product_id, sku_code)` — SKU chỉ cần duy nhất trong một Product.

**FK rule**
- `product_id -> Product.id`
- `delete: restrict`
- `update: cascade`

### 2.3 Category

**Mục đích:** Lưu loại sản phẩm chính, ví dụ: **Tinh chất, Toner, Mặt nạ**.

| Field | Data type | Nullable | Default | PK/FK / Ghi chú |
|---|---|---:|---|---|
| `id` | `uuid` | No | - | PK |
| `name` | `varchar(160)` | Yes | `NULL` | Tên category |
| `code` | `varchar(100)` | Yes | `NULL` | Optional: identifier nội bộ |
| `slug` | `varchar(160)` | Yes | `NULL` | Optional: khi category có URL riêng |
| `status` | `enum` | Yes | `active` (đề xuất) | `active / archived` |
| `created_at` | `timestamptz` | Yes | `NULL` | Audit |
| `updated_at` | `timestamptz` | Yes | `NULL` | Audit |

**Relation**
- Category 1-N Product.
- Không dùng bảng `ProductCategory` trong thiết kế hiện tại.

**Điểm cần lead xác nhận**
- Có cần `code` không?
- Có cần `slug` cho Category không?

### 2.4 SkinConcern

**Mục đích:** Taxonomy riêng cho vấn đề/nhu cầu da.

| Field | Data type | Nullable | Default | PK/FK / Ghi chú |
|---|---|---:|---|---|
| `id` | `uuid` | No | - | PK |
| `name` | `varchar(160)` | Yes | `NULL` | Tên vấn đề da |
| `code` | `varchar(100)` | Yes | `NULL` | Optional |
| `status` | `enum` | Yes | `active` (đề xuất) | `active / archived` |
| `created_at` | `timestamptz` | Yes | `NULL` | Audit |
| `updated_at` | `timestamptz` | Yes | `NULL` | Audit |

**Relation**
- Product N-N SkinConcern qua `ProductSkinConcern`.

### 2.5 ProductSkinConcern

**Mục đích:** Bảng nối Product N-N SkinConcern.

| Field | Data type | Nullable | Default | PK/FK / Ghi chú |
|---|---|---:|---|---|
| `id` | `uuid` | No | - | PK |
| `product_id` | `uuid` | Yes | `NULL` | FK -> `Product.id` |
| `skin_concern_id` | `uuid` | Yes | `NULL` | FK -> `SkinConcern.id` |
| `created_at` | `timestamptz` | Yes | `NULL` | Audit |

**Unique**
- `UNIQUE(product_id, skin_concern_id)`

**FK rule**
- `delete: restrict`
- `update: cascade`

### 2.6 ProductImage

**Mục đích:** Lưu ảnh đại diện và gallery của Product.

| Field | Data type | Nullable | Default | PK/FK / Ghi chú |
|---|---|---:|---|---|
| `id` | `uuid` | No | - | PK |
| `product_id` | `uuid` | Yes | `NULL` | FK -> `Product.id` |
| `image_url` | `text` | Yes | `NULL` | URL ảnh |
| `alt_text` | `varchar(255)` | Yes | `NULL` | SEO/accessibility |
| `sort_order` | `integer` | Yes | `NULL` | Thứ tự gallery |
| `is_primary` | `boolean` | Yes | `false` (đề xuất) | Đánh dấu ảnh đại diện |
| `status` | `enum` | Yes | `active` (đề xuất) | `active / archived` nếu giữ lifecycle ảnh |
| `created_at` | `timestamptz` | Yes | `NULL` | Audit |
| `updated_at` | `timestamptz` | Yes | `NULL` | Audit |

**Index**
- `(product_id, sort_order)`

**FK rule**
- `product_id -> Product.id`
- `delete: restrict`
- `update: cascade`

**Điểm cần lead xác nhận**
- ProductImage có cần `status` riêng không?
- Gallery dùng chung theo Product hay cần per-site?

### 2.7 ProductSite

**Mục đích:** Liên kết Product với `cms_sites` và lưu cấu hình publish / SEO / hiển thị theo từng website.

| Field | Data type | Nullable | Default | PK/FK / Ghi chú |
|---|---|---:|---|---|
| `id` | `uuid` | No | - | PK |
| `product_id` | `uuid` | Yes | `NULL` | FK -> `Product.id` |
| `site_id` | `uuid` | Yes | `NULL` | FK -> `cms_sites.id` |
| `slug` | `varchar(160)` | Yes | `NULL` | Unique trong site |
| `publish_status` | `enum` | Yes | `draft` | `draft / published / archived` |
| `seo_title` | `varchar(255)` | Yes | `NULL` | SEO theo site |
| `seo_description` | `text` | Yes | `NULL` | SEO theo site |
| `display_label` | `TBD` | Yes | `NULL` | `varchar` / `jsonb` / bảng riêng |
| `is_featured` | `boolean?` | Yes | `false?` | Chỉ giữ nếu CMS manual |
| `is_best_seller` | `boolean?` | Yes | `false?` | Chỉ giữ nếu CMS manual |
| `sort_order` | `integer` | Yes | `NULL` | Thứ tự hiển thị |
| `published_at` | `timestamptz` | Yes | `NULL` | Ngày publish |
| `created_at` | `timestamptz` | Yes | `NULL` | Audit |
| `updated_at` | `timestamptz` | Yes | `NULL` | Audit |

**Unique**
- `UNIQUE(product_id, site_id)`
- `UNIQUE(site_id, slug)`

**Index**
- `(site_id, publish_status)`

**FK rule**
- `product_id -> Product.id`
- `site_id -> cms_sites.id`
- `delete: restrict`
- `update: cascade`

### 2.8 ProductSiteSku

**Mục đích:** Cấu hình SKU và giá theo từng website.

| Field | Data type | Nullable | Default | PK/FK / Ghi chú |
|---|---|---:|---|---|
| `id` | `uuid` | No | - | PK |
| `product_site_id` | `uuid` | Yes | `NULL` | FK -> `ProductSite.id` |
| `sku_id` | `uuid` | Yes | `NULL` | FK -> `ProductSku.id` |
| `regular_price` | `numeric(18,2)` | Yes | `NULL` | Giá thường |
| `sale_price` | `numeric(18,2)` | Yes | `NULL` | Giá khuyến mãi |
| `status` | `enum` | Yes | `active` (đề xuất) | `active / inactive` nếu cần tắt riêng SKU trên site |
| `created_at` | `timestamptz` | Yes | `NULL` | Audit |
| `updated_at` | `timestamptz` | Yes | `NULL` | Audit |

**Unique**
- `UNIQUE(product_site_id, sku_id)`

**Index**
- `(product_site_id, status)`
- `(sku_id)`

**Check**
- `regular_price >= 0` khi có giá
- `sale_price >= 0` khi có giá

**Backend validation**
- `sku.product_id == product_site.product_id`

**FK rule**
- `delete: restrict`
- `update: cascade`

### 2.9 FlashSaleCampaign

**Mục đích:** Lưu chiến dịch Flash Sale.

| Field | Data type | Nullable | Default | PK/FK / Ghi chú |
|---|---|---:|---|---|
| `id` | `uuid` | No | - | PK |
| `site_id` | `uuid` | Yes | `NULL` | FK -> `cms_sites.id` |
| `name` | `varchar(255)` | Yes | `NULL` | Tên campaign |
| `start_at` | `timestamptz` | Yes | `NULL` | Bắt đầu |
| `end_at` | `timestamptz` | Yes | `NULL` | Kết thúc |
| `status` | `enum` | Yes | `draft` (đề xuất) | `draft / active / archived` |
| `created_at` | `timestamptz` | Yes | `NULL` | Audit |
| `updated_at` | `timestamptz` | Yes | `NULL` | Audit |

**Check**
- `end_at > start_at` khi cả hai có giá trị.

**Index**
- `(site_id, start_at, end_at)`

**FK rule**
- `site_id -> cms_sites.id`
- `delete: restrict`
- `update: cascade`

**Điểm cần lead xác nhận**
- 1 Campaign thuộc 1 Site hay có campaign cross-site?

### 2.10 FlashSaleItem

**Mục đích:** Lưu SKU tham gia Flash Sale. Flash Sale đã chốt ở SKU-level.

| Field | Data type | Nullable | Default | PK/FK / Ghi chú |
|---|---|---:|---|---|
| `id` | `uuid` | No | - | PK |
| `campaign_id` | `uuid` | Yes | `NULL` | FK -> `FlashSaleCampaign.id` |
| `sku_id` | `uuid` | Yes | `NULL` | FK -> `ProductSku.id` |
| `flash_price` | `numeric(18,2)` | Yes | `NULL` | Giá flash |
| `sort_order` | `integer` | Yes | `NULL` | Thứ tự hiển thị |
| `status` | `enum` | Yes | `active` (đề xuất) | `active / inactive` nếu giữ lifecycle item |
| `created_at` | `timestamptz` | Yes | `NULL` | Audit |
| `updated_at` | `timestamptz` | Yes | `NULL` | Audit |

**Unique**
- `UNIQUE(campaign_id, sku_id)`

**Check**
- `flash_price >= 0`

**Backend validation**
- `flash_price < regular_price` của cùng SKU trên cùng Site.
- SKU phải thực sự có `ProductSiteSku` trên Site của campaign.

**FK rule**
- `delete: restrict`
- `update: cascade`

---

## 3. Website Field Mapping

| Field trên Pax Moly | Bảng / cột dự kiến | Ghi chú |
|---|---|---|
| Tên sản phẩm | `Product.name` | Global Product |
| Tên ngắn / subtitle | `Product.short_name` | Global Product |
| Mô tả | `Product.description` | Global Product |
| Thành phần | `Product.ingredients` | `text/jsonb` cần chốt |
| Công dụng | `Product.benefits` | `text/jsonb` cần chốt |
| Hướng dẫn sử dụng | `Product.usage_instructions` | `text/jsonb` cần chốt |
| Danh mục | `Product.category_id -> Category.name` | Category 1-N Product |
| Vấn đề da | `SkinConcern.name` qua `ProductSkinConcern` | N-N |
| SKU code | `ProductSku.sku_code` | SKU-level |
| Dung tích | `ProductSku.volume` | SKU-level |
| Đơn vị | `ProductSku.unit` | SKU-level |
| Quy cách đóng gói | `ProductSku.packaging` | SKU-level |
| Ảnh đại diện | `ProductImage` với `is_primary = true` | Product-level |
| Gallery | `ProductImage.image_url` theo `sort_order` | Hiện global theo Product |
| Alt text | `ProductImage.alt_text` | SEO/accessibility |
| Slug Product | `ProductSite.slug` | Per-site |
| Publish status | `ProductSite.publish_status` | Per-site |
| SEO title | `ProductSite.seo_title` | Per-site |
| SEO description | `ProductSite.seo_description` | Per-site |
| Display label | `ProductSite.display_label` | Cần chốt type/cơ chế |
| Featured | `ProductSite.is_featured` nếu manual | Nếu calculated thì không lưu field |
| Best Seller | `ProductSite.is_best_seller` nếu manual | Nếu sales-derived thì không lưu field |
| Giá thường | `ProductSiteSku.regular_price` | Site + SKU |
| Giá khuyến mãi | `ProductSiteSku.sale_price` | Site + SKU |
| Discount % | Không lưu | Tính từ giá |
| Flash Sale campaign | `FlashSaleCampaign` | Theo site |
| Flash Sale price | `FlashSaleItem.flash_price` | SKU-level |
| Sold count demo | Không map vào dữ liệu sales thật | Theo requirement |
| Review / rating | Ngoài scope | Không thiết kế |

---

## 4. Example Data - 1 Product + 2 SKU + 2 Website

### Category

| Key | Value |
|---|---|
| `CAT01` | `name=Tinh chất; status=active` |

### Product

| Key | Value |
|---|---|
| `P001` | `category=CAT01; name=Niacinamide 15% + Zinc 5% Serum; status=published` |

### ProductSku

| Key | Value |
|---|---|
| `SKU01` | `product=P001; sku_code=NIAC-30; volume=30; unit=ml` |
| `SKU02` | `product=P001; sku_code=NIAC-50; volume=50; unit=ml` |

### cms_sites

| Key | Value |
|---|---|
| `SITE01` | `Pax Moly VN` |
| `SITE02` | `Partner Site` |

### ProductSite

| Key | Value |
|---|---|
| `PS01` | `product=P001; site=SITE01; slug=niacinamide-serum; status=published` |
| `PS02` | `product=P001; site=SITE02; slug=serum-niacinamide-15; status=published` |

### ProductSiteSku

| Key | Value |
|---|---|
| `PS01 + SKU01` | `regular=459000; sale=389000` |
| `PS01 + SKU02` | `regular=599000; sale=529000` |
| `PS02 + SKU01` | `regular=469000; sale=399000` |
| `PS02 + SKU02` | `regular=619000; sale=549000` |

### Flash Sale example

| Key | Value |
|---|---|
| Campaign | `site=SITE01; name=Flash Sale 10.10` |
| Item | `sku=SKU01; flash_price=350000` |
| Validation | `350000 < regular_price 459000 => hợp lệ` |

---

## 5. Assumptions / Lead Decisions

| Vấn đề | Đề xuất hiện tại | Câu hỏi cần chốt | Trạng thái |
|---|---|---|---|
| Content fields | `ingredients / benefits / usage_instructions` | `text` hay `jsonb`? | Cần lead xác nhận |
| Publish requirements | Đề xuất `name + slug + >=1 SKU + primary image + valid price` | Xác nhận danh sách bắt buộc | Cần lead xác nhận |
| SKU unique | Đề xuất `UNIQUE(sku_code)` | Global hay per-product? | Cần lead xác nhận |
| Category cardinality | Một Product thuộc một Category chính; Category 1-N Product | - | Đã chốt |
| Category implementation | `Product.category_id -> Category.id`; không dùng `ProductCategory` | - | Đã chốt |
| Category code | Chỉ giữ nếu cần identifier nội bộ | Có cần không? | Cần lead xác nhận |
| Category slug | Chỉ giữ nếu category có URL riêng | Có cần không? | Cần lead xác nhận |
| ProductImage status | Đề xuất `active/archived` | Có cần lifecycle riêng không? | Cần lead xác nhận |
| Gallery scope | Hiện dùng chung theo Product | Có cần gallery per-site không? | Cần lead xác nhận |
| Display label | Chưa chốt | Một label / nhiều label / bảng riêng? | Cần lead xác nhận |
| Featured | Giữ boolean nếu CMS manual | Manual hay calculated? | Cần lead xác nhận |
| Best Seller | Giữ boolean nếu CMS manual | Manual hay sales-derived? | Cần lead xác nhận |
| ProductSiteSku status | Đề xuất `active/inactive` | Có cần tắt riêng SKU trên site không? | Cần lead xác nhận |
| Campaign ownership | Đề xuất 1 Campaign thuộc 1 Site | Có campaign cross-site không? | Cần lead xác nhận |
| FlashSaleItem status | Đề xuất `active/inactive` | Có cần lifecycle item riêng không? | Cần lead xác nhận |
| Campaign overlap | Đề xuất không cho cùng Site + SKU overlap | Policy cuối cùng là gì? | Cần lead xác nhận |
| Per-site content override semantics | Hiện chưa thiết kế content override theo website | Nếu sau này có override: cần chốt `NULL = inherit`, empty string = explicit blank | Cần lead xác nhận |
| SkinConcern | Tách riêng Category; N-N qua `ProductSkinConcern` | - | Đã chốt |
| Pricing | Website + SKU qua `ProductSiteSku` | - | Đã chốt |
| Flash Sale scope | SKU-level | - | Đã chốt |

---
