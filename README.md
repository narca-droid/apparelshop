# AURA Apparel — ASP.NET Core Online Apparel Store

A full-stack ASP.NET Core 8 MVC apparel e-commerce site with a complete admin
panel: the admin can rename the store, restyle the theme, edit the homepage
hero, manage categories/products/orders, and edit flexible content pages —
all without touching code.

> **Note on this build**: this project was hand-written file-by-file because
> the sandbox it was built in has no .NET SDK installed, so it hasn't been
> compiled here. The code follows standard, well-tested ASP.NET Core 8 / EF
> Core 8 patterns throughout, but **please run a build locally before relying
> on it**, and let me know if anything doesn't compile so it can be fixed.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- No external database needed — uses SQLite (a local `apparelshop.db` file,
  created automatically on first run).

## Getting started

```bash
cd ApparelShop
dotnet restore
dotnet run
```

Then open the URL shown in the console (typically `https://localhost:5001` or
`http://localhost:5000`).

On first run, the app automatically:
- Creates the SQLite database
- Seeds two roles (`Admin`, `Customer`)
- Creates a default admin account
- Seeds 4 categories and 10 sample products with placeholder images
- Seeds default site settings and a few content pages (About Us, Shipping &
  Returns)

### Default admin login

```
Email:    admin@aura-apparel.com
Password: Admin@12345
```

Sign in at `/Account/Login` — the app auto-redirects Admin users to the
admin panel (`/Admin/Dashboard`). Change this password after first login by
adding a "Change Password" flow, or reset it directly via SQL/EF if needed.

## What the admin can control

From the sidebar in `/Admin`:

- **Dashboard** — revenue, order counts, low-stock alerts, recent orders.
- **Products** — full CRUD: name, description, price, sale price, SKU,
  stock, sizes/colors, material, category, main image + gallery images,
  featured/new-arrival/active flags.
- **Categories** — full CRUD: name, slug, description, image, display order,
  active flag. Categories with products can't be deleted (prevents orphaned
  products) — reassign or remove products first.
- **Orders** — view all orders and line items, update order status
  (Pending → Processing → Shipped → Delivered / Cancelled).
- **Content Pages** — create/edit/delete arbitrary HTML pages served at
  `/page/{key}` (About Us, Shipping Policy, Return Policy, etc. — add as
  many as you like).
- **Site & Layout** — the CMS core:
  - Site name, tagline, logo, favicon
  - Theme colors (primary / accent / background) — applied site-wide via CSS
    variables the instant you save
  - Homepage hero banner: headline, subtext, button text/link, background
    image
  - Toggle homepage sections (Featured, New Arrivals) on/off
  - Footer: about text, contact email/phone/address, social links
  - SEO meta title/description

All of this is stored in the database (`SiteSettings` table, a single row)
and rendered live into the public site's layout — there's no redeploy or
code change needed to rebrand the store.

## Storefront features

- Homepage with dynamic hero, category tiles, featured & new-arrival rails
- Shop page with category filter, search, sort, and pagination
- Product detail page with size/color selection and image gallery
- Session-based shopping cart (add/update/remove, live cart count badge)
- Guest checkout that creates a real `Order` record (UAE emirate selector,
  shipping fee, order confirmation page)
- Customer registration/login (Identity-based), admins auto-detected by role
- Content pages (`/page/about-page`, `/page/shipping-policy`, etc.)

## Project structure

```
ApparelShop/
├── Areas/Admin/            # Admin panel (controllers + views, role-gated)
├── Controllers/             # Public storefront controllers
├── Data/                    # DbContext + seed data
├── Models/                  # EF Core entities
├── ViewComponents/          # Dynamic header/footer driven by SiteSettings
├── Views/                   # Storefront Razor views
├── wwwroot/                 # CSS, JS, seed images, uploads
└── Program.cs
```

## Notes & next steps you may want

- **Payments**: checkout currently records orders as "Pending" with no
  payment gateway wired in (UAE cash-on-delivery style). Stripe/PayTabs/
  Telr integration can be added to `CheckoutController`.
- **Image uploads**: stored directly on disk under `wwwroot/uploads`. For
  production/multi-server hosting, swap `SaveUploadAsync` in the admin
  controllers for cloud storage (Azure Blob / S3).
- **Migrations**: the app currently uses `EnsureCreatedAsync()` for
  zero-setup local runs. Before production or any schema change, switch to
  EF Core migrations:
  ```bash
  dotnet ef migrations add Init
  dotnet ef database update
  ```
  and change `SeedData.InitializeAsync` to call `context.Database.MigrateAsync()`
  instead of `EnsureCreatedAsync()`.
- **Product variants**: sizes/colors are simple comma-separated strings for
  simplicity. For per-variant stock tracking, promote these to a proper
  `ProductVariant` table.
