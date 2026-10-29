using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Linq;
using Serilog;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using CarProject.Services;
using CarProject.Data;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("Logs/app-.log", rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// Global handlers to capture otherwise-silent crashes when running under debugger/IDE
AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    try
    {
        var ex = e.ExceptionObject as Exception;
        var msg = $"UNHANDLED: {ex?.GetType().Name}: {ex?.Message}\n{ex?.StackTrace}";
        var path = Path.Combine(Path.GetTempPath(), "app_unhandled.log");
        System.IO.File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}\n\n");
        Log.Fatal(ex, "Unhandled exception");
    }
    catch { }
};

TaskScheduler.UnobservedTaskException += (s, e) =>
{
    try
    {
        var ex = e.Exception;
        var path = Path.Combine(Path.GetTempPath(), "app_unobserved_task.log");
        System.IO.File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
        Log.Error(ex, "Unobserved task exception");
    }
    catch { }
};

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddServiceDefaults();

    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorPages();

    builder.Services.AddDistributedMemoryCache();

    // Allow larger uploads from browsers / IDE-run profiles (set to 100 MB)
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB
        options.ValueLengthLimit = int.MaxValue;
        options.ValueCountLimit = int.MaxValue;
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<CarProject.Services.IActivityLogService, CarProject.Services.ActivityLogService>();
    builder.Services.AddScoped<CarProject.Services.ICartService, CarProject.Services.CartService>();
    builder.Services.AddScoped<CarProject.Services.IJwtService, CarProject.Services.JwtService>();
    builder.Services.AddScoped<CarProject.Services.IPasswordService, CarProject.Services.PasswordService>();
    builder.Services.Configure<CarProject.Services.SmtpSettings>(builder.Configuration.GetSection("Smtp"));
    builder.Services.AddScoped<CarProject.Services.IEmailService, CarProject.Services.EmailService>();
    builder.Services.AddScoped<CarProject.Services.INotificationService, CarProject.Services.NotificationService>();
    builder.Services.Configure<CarProject.Services.SepaySettings>(builder.Configuration.GetSection("Sepay"));
    builder.Services.AddScoped<CarProject.Services.ISepayService, CarProject.Services.SepayService>();
    builder.Services.AddHttpClient<CarProject.Services.ISepayService, CarProject.Services.SepayService>();
    builder.Services.AddScoped<CarProject.Services.IRevenueService, CarProject.Services.RevenueService>();
    builder.Services.AddScoped<CarProject.Services.ISaleService, CarProject.Services.SaleService>();

    // JWT config
    var jwtKey = builder.Configuration["Jwt:Key"] ?? "CarProjectSuperSecretKey2024@MustBe32CharsLong!";
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CarProject";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CarProject";

    // Google OAuth (only if configured)
    var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
    var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    var useGoogle = !string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret);

    var authBuilder = builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = useGoogle ? GoogleDefaults.AuthenticationScheme
            : Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

    if (useGoogle)
    {
        authBuilder.AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.CallbackPath = "/Account/GoogleCallback";
            options.SaveTokens = true;
        });
    }

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connectionString))
    {
        builder.Services.AddDbContext<CarProject.Data.AppDbContext>(options =>
            options.UseSqlServer(connectionString));
    }

    builder.Host.UseSerilog();

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(2);
        // Increase max request body size to allow large JSON payloads or large uploads
        options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100 MB
    });

    var app = builder.Build();

    // Seed database on startup
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<CarProject.Data.AppDbContext>();
            context.Database.Migrate();
            CarProject.Data.DbInitializer.SeedData(context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error seeding database");
        }
    }

    // Force UTF-8 charset on all text responses
    app.Use(async (context, next) =>
    {
        context.Response.OnStarting(() =>
        {
            var ct = context.Response.ContentType;
            if (ct != null && ct.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                && ct.IndexOf("charset", StringComparison.OrdinalIgnoreCase) < 0)
            {
                context.Response.ContentType = ct + "; charset=utf-8";
            }
            return Task.CompletedTask;
        });
        await next();
    });

    app.MapDefaultEndpoints();

    if (app.Environment.IsDevelopment())
    {
        // Show detailed exceptions when running from Visual Studio / development profile
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseRouting();

    // Request logging middleware
    app.Use(async (context, next) =>
    {
        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;
        Log.Information("--> {Method} {Path}", method, path);
        await next(context);
        sw.Stop();
        Log.Information("<-- {Method} {Path} => {StatusCode} ({Elapsed}ms)", method, path, context.Response.StatusCode, sw.ElapsedMilliseconds);
    });

    // JWT cookie auth middleware (replaces session)
    app.Use(async (context, next) =>
    {
        var token = context.GetJwtFromCookie();
        if (!string.IsNullOrEmpty(token))
        {
            var jwt = context.RequestServices.GetRequiredService<CarProject.Services.IJwtService>();
            var principal = jwt.ValidateToken(token);
            if (principal != null)
                context.User = principal;
        }
        await next(context);
    });

    app.UseAuthentication();
    app.UseAuthorization();

    // Authorization middleware: phân quyền theo đường dẫn
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value ?? "";
        var role = context.User.GetJwtRole();
        var isLoggedIn = context.User.IsJwtLoggedIn();

        // /Admin/* -> chỉ Admin
        if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
        {
            if (!isLoggedIn)
            {
                var isAjax = (context.Request.Headers.ContainsKey("X-Requested-With") && context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                             || (context.Request.Headers.ContainsKey("Accept") && context.Request.Headers["Accept"].Any(a => a != null && a.Contains("application/json")));
                if (isAjax)
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { error = "Unauthorized" }));
                    return;
                }
                context.Response.Redirect("/Account/Login");
                return;
            }
            if (role != "Admin")
            {
                context.Response.StatusCode = 403;
                return;
            }
        }

        // /Notifications/*, /Orders/*, /Profile, /TestDrive, /QuanLy/* -> cần đăng nhập
        if (((path.StartsWith("/Notifications", StringComparison.OrdinalIgnoreCase)) ||
             path.StartsWith("/Orders", StringComparison.OrdinalIgnoreCase) ||
             path.Equals("/Profile", StringComparison.OrdinalIgnoreCase) ||
             path.Equals("/TestDrive", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("/TestDrive", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("/QuanLy", StringComparison.OrdinalIgnoreCase))
            && !isLoggedIn)
        {
            var isAjax = (context.Request.Headers.ContainsKey("X-Requested-With") && context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                         || (context.Request.Headers.ContainsKey("Accept") && context.Request.Headers["Accept"].Any(a => a != null && a.Contains("application/json")));
            if (isAjax)
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { error = "Unauthorized" }));
                return;
            }
            context.Response.Redirect("/Account/Login");
            return;
        }

        await next(context);
    });

    // Prevent browser caching of avatar images
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.Value?.StartsWith("/uploads/avatars") == true && !context.Response.HasStarted)
        {
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
        }
        await next();
    });

    app.MapStaticAssets();
    // Serve runtime-uploaded files (e.g. /uploads/admin/*) that are NOT in the
    // MapStaticAssets build-time manifest (production publish only serves manifest assets)
    app.UseStaticFiles();
    // Minimal API for avatar upload (bypasses Razor Pages pipeline entirely)
    app.MapPost("/api/upload-avatar", async (HttpContext ctx, IWebHostEnvironment env) =>
    {
        try
        {
            var userId = ctx.User.GetJwtUserName();
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            string body;
            using (var reader = new StreamReader(ctx.Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            var payload = JsonSerializer.Deserialize<JsonElement>(body);
            var dataUrl = payload.GetProperty("avatarBase64").GetString();
            if (string.IsNullOrEmpty(dataUrl))
                return Results.BadRequest(new { error = "Missing image data" });

            var commaIdx = dataUrl.IndexOf(',');
            if (commaIdx < 0)
                return Results.BadRequest(new { error = "Invalid data URL" });

            var base64 = dataUrl.Substring(commaIdx + 1);
            var bytes = Convert.FromBase64String(base64);

            var webRoot = env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "wwwroot");
            webRoot = Path.GetFullPath(webRoot);
            var uploadsDir = Path.Combine(webRoot, "uploads", "avatars");
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            var fileName = $"{userId}.jpg";
            var filePath = Path.Combine(uploadsDir, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, bytes);

            var db = ctx.RequestServices.GetRequiredService<CarProject.Data.AppDbContext>();
            var user = await db.TaiKhoan.FirstOrDefaultAsync(t => t.TenDangNhap == userId);
            if (user != null)
            {
                user.AvatarUrl = $"/uploads/avatars/{fileName}";
                await db.SaveChangesAsync();
                var jwtSvc = ctx.RequestServices.GetRequiredService<CarProject.Services.IJwtService>();
                ctx.SetJwtCookie(jwtSvc.GenerateToken(user));
            }

            var log = ctx.RequestServices.GetRequiredService<CarProject.Services.IActivityLogService>();
            await log.LogAsync("Cập nhật ảnh đại diện");

            return Results.Ok(new { success = true, url = $"/uploads/avatars/{fileName}" });
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(Path.GetTempPath(), "avatar_error.log");
            System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
            return Results.Ok(new { success = false, error = ex.Message });
        }
    });

    // Minimal API for admin avatar upload (session-independent, username from body)
    app.MapPost("/api/upload-avatar-admin", async (HttpContext ctx, IWebHostEnvironment env) =>
    {
        try
        {
            string body;
            using (var reader = new StreamReader(ctx.Request.Body))
                body = await reader.ReadToEndAsync();

            var payload = JsonSerializer.Deserialize<JsonElement>(body);
            var dataUrl = payload.GetProperty("avatarBase64").GetString();
            var userName = payload.GetProperty("userName").GetString();

            if (string.IsNullOrEmpty(dataUrl) || string.IsNullOrEmpty(userName))
                return Results.BadRequest(new { success = false, error = "Missing data" });

            // Verify user exists
            var db = ctx.RequestServices.GetRequiredService<CarProject.Data.AppDbContext>();
            var user = await db.TaiKhoan.FindAsync(userName);
            if (user == null)
                return Results.Ok(new { success = false, error = "User not found" });

            var commaIdx = dataUrl.IndexOf(',');
            if (commaIdx < 0)
                return Results.BadRequest(new { success = false, error = "Invalid data URL" });

            var base64 = dataUrl.Substring(commaIdx + 1);
            var bytes = Convert.FromBase64String(base64);

            var webRoot = env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "wwwroot");
            webRoot = Path.GetFullPath(webRoot);
            var uploadsDir = Path.Combine(webRoot, "uploads", "avatars");
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            var fileName = $"{userName}.jpg";
            var filePath = Path.Combine(uploadsDir, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, bytes);

            user.AvatarUrl = $"/uploads/avatars/{fileName}";
            await db.SaveChangesAsync();

            // Regenerate JWT if active
            if (ctx.User.IsJwtLoggedIn())
            {
                var jwtSvc = ctx.RequestServices.GetRequiredService<CarProject.Services.IJwtService>();
                ctx.SetJwtCookie(jwtSvc.GenerateToken(user));
            }

            var log = ctx.RequestServices.GetRequiredService<CarProject.Services.IActivityLogService>();
            await log.LogAsync("Cập nhật ảnh đại diện (admin)");

            return Results.Ok(new { success = true, url = user.AvatarUrl });
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(Path.GetTempPath(), "avatar_admin_error.log");
            System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
            return Results.Ok(new { success = false, error = ex.Message });
        }
    });

    // Manager: accept a test-drive booking
    app.MapPost("/api/testdrive/accept", async (HttpContext ctx) =>
    {
        var role = ctx.User.GetJwtRole();
        if (string.IsNullOrEmpty(role) || !(role == "Admin" || role == "Quản Lý"))
            return Results.Unauthorized();

        var db = ctx.RequestServices.GetRequiredService<CarProject.Data.AppDbContext>();
        var notif = ctx.RequestServices.GetRequiredService<CarProject.Services.INotificationService>();

        try
        {
            var payload = await ctx.Request.ReadFromJsonAsync<JsonElement>();
            if (!payload.TryGetProperty("maLichHen", out var idProp)) return Results.BadRequest(new { success = false, error = "Missing maLichHen" });
            var ma = idProp.GetInt32();
            var lich = await db.LichHenLaiThu.FindAsync(ma);
            if (lich == null) return Results.Ok(new { success = false, error = "Booking not found" });

            lich.TrangThai = "Đã chấp nhận";
            await db.SaveChangesAsync();

            // Notify user
            var title = "Yêu cầu lái thử đã được chấp nhận";
            var content = $"Đơn lái thử của bạn (ID {lich.MaLichHen}) đã được showroom chấp nhận. Vui lòng đến theo lịch.";
            await notif.SendAsync(lich.MaKhachHang, title, content, "/Profile");

            return Results.Ok(new { success = true });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error accepting test drive");
            return Results.Ok(new { success = false, error = ex.Message });
        }
    });

    // Manager: mark test-drive as completed and update showroom stats
    app.MapPost("/api/testdrive/complete", async (HttpContext ctx) =>
    {
        var role = ctx.User.GetJwtRole();
        if (string.IsNullOrEmpty(role) || !(role == "Admin" || role == "Quản Lý"))
            return Results.Unauthorized();

        var db = ctx.RequestServices.GetRequiredService<CarProject.Data.AppDbContext>();
        var notif = ctx.RequestServices.GetRequiredService<CarProject.Services.INotificationService>();

        try
        {
            var payload = await ctx.Request.ReadFromJsonAsync<JsonElement>();
            if (!payload.TryGetProperty("maLichHen", out var idProp)) return Results.BadRequest(new { success = false, error = "Missing maLichHen" });
            var ma = idProp.GetInt32();
            var lich = await db.LichHenLaiThu.FindAsync(ma);
            if (lich == null) return Results.Ok(new { success = false, error = "Booking not found" });

            lich.TrangThai = "Hoàn thành";

            // Update monthly showroom statistics
            var ky = DateTime.Now.ToString("yyyy-MM");
            var thongKe = await db.ThongKeTongHop_Boss.FirstOrDefaultAsync(t => t.KyBaoCao == ky && t.MaChiNhanh == lich.MaChiNhanh);
            if (thongKe == null)
            {
                thongKe = new CarProject.Models.ThongKeTongHop_Boss
                {
                    KyBaoCao = ky,
                    MaChiNhanh = lich.MaChiNhanh,
                    TongDoanhThu = 0,
                    TongTienCocThuVe = 0,
                    TongSoXeDaBan = 0,
                    SoDonCocBiHuy = 0,
                    TongLuotXemWeb = 0,
                    TongLuotLaiThu = 1,
                    MaDongXeBanChayNhat = lich.MaDong
                };
                db.ThongKeTongHop_Boss.Add(thongKe);
            }
            else
            {
                thongKe.TongLuotLaiThu += 1;
            }

            await db.SaveChangesAsync();

            // Notify user
            var title = "Lịch lái thử đã hoàn thành";
            var content = $"Đơn lái thử (ID {lich.MaLichHen}) đã được đánh dấu hoàn thành. Cảm ơn bạn đã tham gia.";
            await notif.SendAsync(lich.MaKhachHang, title, content, "/Profile");

            return Results.Ok(new { success = true });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error completing test drive");
            return Results.Ok(new { success = false, error = ex.Message });
        }
    });

    // Minimal API for JWT login (JSON body: { tenDangNhap, matKhau })
    app.MapPost("/api/login", async (HttpContext ctx, CarProject.Services.IJwtService jwt) =>
    {
        string body;
        using (var reader = new StreamReader(ctx.Request.Body))
            body = await reader.ReadToEndAsync();

        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        var tenDangNhap = payload.GetProperty("tenDangNhap").GetString() ?? "";
        var matKhau = payload.GetProperty("matKhau").GetString() ?? "";

        if (string.IsNullOrEmpty(tenDangNhap) || string.IsNullOrEmpty(matKhau))
            return Results.BadRequest(new { error = "Missing credentials" });

        var db = ctx.RequestServices.GetRequiredService<CarProject.Data.AppDbContext>();
        var user = await db.TaiKhoan.FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

        if (user == null || (user.TrangThai != "Active" && user.TrangThai != "Hoạt động"))
            return Results.Unauthorized();

        var passwordService = ctx.RequestServices.GetRequiredService<CarProject.Services.IPasswordService>();
        if (!passwordService.Verify(matKhau, user.MatKhau ?? ""))
            return Results.Unauthorized();

        var token = jwt.GenerateToken(user);

        return Results.Ok(new
        {
            token,
            expiresIn = 1800,
            user = new
            {
                tenDangNhap = user.TenDangNhap,
                tenHienThi = user.TenHienThi ?? user.TenDangNhap,
                vaiTro = user.VaiTro,
                avatarUrl = user.AvatarUrl ?? ""
            }
        });
    });

    // Minimal API for cart operations
    app.MapGet("/api/cart/count", async (HttpContext ctx, ICartService cart) =>
    {
        var userName = ctx.User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName))
            return Results.Ok(new { count = 0 });
        var count = await cart.GetCartCountAsync();
        return Results.Ok(new { count });
    });

    app.MapGet("/api/notifications/count", async (HttpContext ctx, CarProject.Services.INotificationService notif) =>
    {
        var userName = ctx.User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName))
            return Results.Ok(new { count = 0 });
        var count = await notif.GetUnreadCountAsync(userName);
        return Results.Ok(new { count });
    });

    app.MapPost("/api/cart/add", async (HttpContext ctx, ICartService cart, AppDbContext db) =>
    {
        var userName = ctx.User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName))
            return Results.Ok(new { success = false, error = "Vui lòng đăng nhập" });

        string body;
        using (var reader = new StreamReader(ctx.Request.Body))
            body = await reader.ReadToEndAsync();

        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        var item = JsonSerializer.Deserialize<CartItem>(body) ?? new CartItem();

        var phienBan = await db.PhienBanXe.FindAsync(item.MaPhienBan);
        if (phienBan == null)
            return Results.Ok(new { success = false, error = "Xe không tồn tại" });

        var cartItems = await cart.GetCartAsync();
        var existingQty = cartItems.Where(c => c.MaPhienBan == item.MaPhienBan).Sum(c => c.SoLuong);

        var cartReservedQty = await db.GioHang
            .Where(g => g.MaTaiKhoan != userName && g.MaPhienBan == item.MaPhienBan)
            .SumAsync(g => (int?)g.SoLuong) ?? 0;
        var depositReservedQty = await db.DonDatCocChiTiet
            .Where(c => c.MaPhienBan == item.MaPhienBan
                && c.DonDatCoc != null
                && c.DonDatCoc!.TrangThaiThanhToan == "Đã thanh toán"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã hủy"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã huỷ"
                && c.DonDatCoc!.TrangThaiDonHang != "Từ chối")
            .SumAsync(c => (int?)c.SoLuong) ?? 0;

        var reservedByOthers = cartReservedQty + depositReservedQty;
        var available = Math.Max(0, phienBan.SoLuongTrongKho - reservedByOthers);
        if (available <= 0)
        {
            return Results.Ok(new { success = false, error = "Phiên bản này đã có người đặt cọc hoặc đang có người khác thêm vào giỏ hàng. Hiện không còn xe trống." });
        }

        if (existingQty + 1 > available)
            return Results.Ok(new { success = false, error = $"Phiên bản này đã có người đặt cọc hoặc đang có người khác thêm vào giỏ hàng. Hiện còn {available} xe chưa bị đặt cọc." });

        await cart.AddToCartAsync(item);
        return Results.Ok(new { success = true });
    });

    app.MapDelete("/api/cart/remove/{id:int}", async (int id, ICartService cart) =>
    {
        await cart.RemoveFromCartAsync(id);
        return Results.Ok(new { success = true });
    });

    // Inline admin editor API endpoints
    app.MapPost("/api/admin/upload", async (HttpContext ctx, IWebHostEnvironment env) =>
    {
        var file = ctx.Request.Form.Files.FirstOrDefault();
        if (file == null) return Results.BadRequest(new { error = "No file" });
        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "admin");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(uploadsDir, fileName);
        using (var stream = new FileStream(path, FileMode.Create))
            await file.CopyToAsync(stream);
        return Results.Ok(new { url = $"/uploads/admin/{fileName}" });
    });

    app.MapPost("/api/admin/dongxe/images/upload", async (HttpContext ctx, IWebHostEnvironment env) =>
    {
        var files = ctx.Request.Form.Files;
        if (files == null || files.Count == 0)
            return Results.BadRequest(new { error = "No files" });
        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "admin");
        Directory.CreateDirectory(uploadsDir);
        var urls = new List<string>();
        foreach (var file in files)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(uploadsDir, fileName);
            using (var stream = new FileStream(path, FileMode.Create))
                await file.CopyToAsync(stream);
            urls.Add($"/uploads/admin/{fileName}");
        }
        return Results.Ok(new { success = true, urls });
    });

    app.MapPost("/api/admin/banner/save", async (HttpContext ctx, AppDbContext db) =>
    {
        try
        {
            var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
            var duongDanAnh = body.TryGetProperty("duongDanAnh", out var prop) ? prop.GetString() : null;
            if (string.IsNullOrWhiteSpace(duongDanAnh))
                return Results.BadRequest(new { success = false, error = "Vui lòng cung cấp đường dẫn ảnh" });

            var userName = ctx.User.GetJwtUserName();
            if (string.IsNullOrEmpty(userName) || !await db.TaiKhoan.AnyAsync(t => t.TenDangNhap == userName))
                userName = await db.TaiKhoan.Where(t => t.VaiTro == "Admin").Select(t => t.TenDangNhap).FirstOrDefaultAsync() ?? "admin";
            var maxThuTu = await db.QuangCaoBanner.AnyAsync()
                ? await db.QuangCaoBanner.MaxAsync(b => b.ThuTuHienThi)
                : 0;
            var banner = new CarProject.Models.QuangCaoBanner
            {
                DuongDanAnh = duongDanAnh,
                DuongDanLienKet = "",
                ThuTuHienThi = maxThuTu + 1,
                MaQuanLyCapNhat = userName,
                TrangThaiKichHoat = true
            };
            db.QuangCaoBanner.Add(banner);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(Path.GetTempPath(), "banner_save_error.log");
            System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
            return Results.Ok(new { success = false, error = ex.Message });
        }
    });

    app.MapPost("/api/admin/banner/delete", async (HttpContext ctx, AppDbContext db) =>
    {
        try
        {
            var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
            var maBanner = body.TryGetProperty("maBanner", out var p) ? p.GetInt32() : 0;
            var banner = await db.QuangCaoBanner.FirstOrDefaultAsync(b => b.MaBanner == maBanner);
            if (banner == null)
                return Results.BadRequest(new { success = false, error = "Không tìm thấy banner" });
            db.QuangCaoBanner.Remove(banner);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(Path.GetTempPath(), "banner_delete_error.log");
            System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
            return Results.Ok(new { success = false, error = ex.Message });
        }
    });

    app.MapGet("/api/admin/dongxe/{id}", async (int id, AppDbContext db) =>
    {
        var dongXe = await db.DongXe.Include(d => d.HangXe).Include(d => d.PhienBanXes)
            .FirstOrDefaultAsync(d => d.MaDong == id);
        if (dongXe == null) return Results.NotFound();
        var hx = dongXe.HangXe;
        var images = await db.HinhAnhXe
            .Where(h => h.MaDong == id)
            .OrderBy(h => h.ThuTu).ThenBy(h => h.MaHinhAnh)
            .Select(h => new { h.MaHinhAnh, h.DuongDanAnh, h.LaChinh, h.ThuTu })
            .ToListAsync();
        return Results.Ok(new
        {
            dongXe.MaDong,
            dongXe.TenDong,
            dongXe.KieuDang,
            dongXe.MaHang,
            dongXe.DuongDanAnh,
            images,
            brand = hx != null ? new { hx.MaHang, hx.TenHang, hx.QuocGia, hx.DuongDanLogo } : null,
            versions = dongXe.PhienBanXes?.Select(v => new
            {
                v.MaPhienBan,
                v.TenPhienBan,
                v.GiaNiemYet,
                v.MauSac,
                v.DongCo,
                v.HopSo,
                v.LoaiNhietLieu,
                v.SoLuongTrongKho,
                v.DuongDanAnh,
                v.TrangThai,
                v.MaKhuyenMai
            }).ToList()
        });
    });

    app.MapPost("/api/admin/hangxe/save", async (HttpContext ctx, AppDbContext db) =>
    {
        var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
        var maHang = body.GetProperty("maHang").GetInt32();
        var hangXe = await db.HangXe.FindAsync(maHang);
        if (hangXe == null) return Results.NotFound();
        if (body.TryGetProperty("tenHang", out var th)) hangXe.TenHang = th.GetString() ?? hangXe.TenHang;
        if (body.TryGetProperty("quocGia", out var qg)) hangXe.QuocGia = qg.GetString() ?? hangXe.QuocGia;
        if (body.TryGetProperty("duongDanLogo", out var ddl))
            hangXe.DuongDanLogo = ddl.ValueKind == JsonValueKind.Null || string.IsNullOrEmpty(ddl.GetString()) ? "" : ddl.GetString();
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    });

    app.MapPost("/api/admin/hangxe/delete", async (HttpContext ctx, AppDbContext db) =>
    {
        var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
        if (!body.TryGetProperty("maHang", out var mh)) return Results.BadRequest(new { success = false, error = "Thiếu mã hãng xe" });
        var maHang = mh.GetInt32();
        var hangXe = await db.HangXe.FindAsync(maHang);
        if (hangXe == null) return Results.NotFound(new { success = false, error = "Không tìm thấy hãng xe" });
        var tenHang = hangXe.TenHang;
        var hasDongXe = await db.DongXe.AnyAsync(d => d.MaHang == maHang);
        if (hasDongXe)
            return Results.BadRequest(new { success = false, error = $"Không thể xoá \"{tenHang}\" vì có dòng xe thuộc hãng này. Vui lòng xoá các dòng xe liên quan trước." });
        try
        {
            db.HangXe.Remove(hangXe);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(Path.GetTempPath(), "hangxe_delete_error.log");
            System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
            return Results.Ok(new { success = false, error = ex.Message });
        }
    });

    app.MapPost("/api/admin/dongxe/save", async (HttpContext ctx, AppDbContext db) =>
    {
        var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
        var maDong = body.GetProperty("maDong").GetInt32();
        var dongXe = await db.DongXe.FindAsync(maDong);
        if (dongXe == null) return Results.NotFound();
        if (body.TryGetProperty("tenDong", out var td)) dongXe.TenDong = td.GetString() ?? dongXe.TenDong;
        if (body.TryGetProperty("maHang", out var mh)) dongXe.MaHang = mh.GetInt32();
        if (body.TryGetProperty("kieuDang", out var kd)) dongXe.KieuDang = kd.GetString() ?? dongXe.KieuDang;
        if (body.TryGetProperty("duongDanAnh", out var dda))
            dongXe.DuongDanAnh = dda.ValueKind == JsonValueKind.Null || string.IsNullOrEmpty(dda.GetString()) ? null : dda.GetString();
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    });

    app.MapPost("/api/admin/dongxe/images/save", async (HttpContext ctx, AppDbContext db) =>
    {
        try
        {
            var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
            var maDong = body.GetProperty("maDong").GetInt32();
            if (!await db.DongXe.AnyAsync(d => d.MaDong == maDong))
                return Results.BadRequest(new { success = false, error = "Không tìm thấy dòng xe" });

            var images = new List<CarProject.Models.HinhAnhXe>();
            if (body.TryGetProperty("images", out var imgArr) && imgArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var it in imgArr.EnumerateArray())
                {
                    var url = it.TryGetProperty("duongDanAnh", out var u) ? u.GetString() : null;
                    if (string.IsNullOrWhiteSpace(url)) continue;
                    var isChinh = it.TryGetProperty("laChinh", out var lc) && lc.GetBoolean();
                    images.Add(new CarProject.Models.HinhAnhXe
                    {
                        MaDong = maDong,
                        DuongDanAnh = url,
                        LaChinh = isChinh,
                        ThuTu = images.Count
                    });
                }
            }

            if (images.Any() && !images.Any(i => i.LaChinh))
                images[0].LaChinh = true;

            var existing = await db.HinhAnhXe.Where(h => h.MaDong == maDong).ToListAsync();
            db.HinhAnhXe.RemoveRange(existing);
            foreach (var img in images) db.HinhAnhXe.Add(img);

            var dongXe = await db.DongXe.FindAsync(maDong);
            if (dongXe != null)
                dongXe.DuongDanAnh = images.FirstOrDefault(i => i.LaChinh)?.DuongDanAnh ?? dongXe.DuongDanAnh;

            await db.SaveChangesAsync();
            var saved = await db.HinhAnhXe.Where(h => h.MaDong == maDong)
                .OrderBy(h => h.ThuTu).ThenBy(h => h.MaHinhAnh)
                .Select(h => new { h.MaHinhAnh, h.DuongDanAnh, h.LaChinh, h.ThuTu })
                .ToListAsync();
            return Results.Ok(new { success = true, count = images.Count, images = saved });
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(Path.GetTempPath(), "dongxe_images_save_error.log");
            System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
            return Results.Ok(new { success = false, error = ex.Message });
        }
    });

    app.MapPost("/api/admin/dongxe/images/setmain", async (HttpContext ctx, AppDbContext db) =>
    {
        try
        {
            var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
            var maHinhAnh = body.GetProperty("maHinhAnh").GetInt32();
            var hinhAnh = await db.HinhAnhXe.FindAsync(maHinhAnh);
            if (hinhAnh == null) return Results.BadRequest(new { success = false, error = "Không tìm thấy ảnh" });
            await db.HinhAnhXe.Where(h => h.MaDong == hinhAnh.MaDong).ExecuteUpdateAsync(s => s.SetProperty(h => h.LaChinh, false));
            hinhAnh.LaChinh = true;
            var dongXe = await db.DongXe.FindAsync(hinhAnh.MaDong);
            if (dongXe != null) dongXe.DuongDanAnh = hinhAnh.DuongDanAnh;
            await db.SaveChangesAsync();
            var saved = await db.HinhAnhXe.Where(h => h.MaDong == hinhAnh.MaDong)
                .OrderBy(h => h.ThuTu).ThenBy(h => h.MaHinhAnh)
                .Select(h => new { h.MaHinhAnh, h.DuongDanAnh, h.LaChinh, h.ThuTu })
                .ToListAsync();
            return Results.Ok(new { success = true, images = saved });
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(Path.GetTempPath(), "dongxe_images_setmain_error.log");
            System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
            return Results.Ok(new { success = false, error = ex.Message });
        }
    });

    app.MapPost("/api/admin/dongxe/images/delete", async (HttpContext ctx, AppDbContext db) =>
    {
        try
        {
            var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
            var maHinhAnh = body.GetProperty("maHinhAnh").GetInt32();
            var hinhAnh = await db.HinhAnhXe.FindAsync(maHinhAnh);
            if (hinhAnh == null) return Results.BadRequest(new { success = false, error = "Không tìm thấy ảnh" });
            var maDong = hinhAnh.MaDong;
            var wasMain = hinhAnh.LaChinh;
            db.HinhAnhXe.Remove(hinhAnh);
            await db.SaveChangesAsync();
            if (wasMain)
            {
                var next = await db.HinhAnhXe.Where(h => h.MaDong == maDong).OrderBy(h => h.ThuTu).FirstOrDefaultAsync();
                if (next != null) next.LaChinh = true;
                var dongXe = await db.DongXe.FindAsync(maDong);
                if (dongXe != null) dongXe.DuongDanAnh = next?.DuongDanAnh ?? dongXe.DuongDanAnh;
                await db.SaveChangesAsync();
            }
            var remaining = await db.HinhAnhXe.Where(h => h.MaDong == maDong)
                .OrderBy(h => h.ThuTu).ThenBy(h => h.MaHinhAnh)
                .Select(h => new { h.MaHinhAnh, h.DuongDanAnh, h.LaChinh, h.ThuTu })
                .ToListAsync();
            return Results.Ok(new { success = true, images = remaining });
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(Path.GetTempPath(), "dongxe_images_delete_error.log");
            System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
            return Results.Ok(new { success = false, error = ex.Message });
        }
    });

    app.MapPost("/api/admin/phienban/save", async (HttpContext ctx, AppDbContext db) =>
    {
        var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
        var maPhienBan = body.GetProperty("maPhienBan").GetInt32();
        var pb = await db.PhienBanXe.FindAsync(maPhienBan);
        if (pb == null) return Results.NotFound();
        if (body.TryGetProperty("tenPhienBan", out var tp)) pb.TenPhienBan = tp.GetString() ?? pb.TenPhienBan;
        if (body.TryGetProperty("giaNiemYet", out var gn)) pb.GiaNiemYet = gn.GetInt64();
        if (body.TryGetProperty("mauSac", out var ms)) pb.MauSac = ms.GetString() ?? pb.MauSac;
        if (body.TryGetProperty("dongCo", out var dc)) pb.DongCo = dc.GetString() ?? pb.DongCo;
        if (body.TryGetProperty("hopSo", out var hs)) pb.HopSo = hs.GetString() ?? pb.HopSo;
        if (body.TryGetProperty("loaiNhietLieu", out var lnl)) pb.LoaiNhietLieu = lnl.GetString() ?? pb.LoaiNhietLieu;
        if (body.TryGetProperty("soLuongTrongKho", out var slk)) pb.SoLuongTrongKho = slk.GetInt32();
        if (body.TryGetProperty("trangThai", out var tt)) pb.TrangThai = tt.GetString() ?? pb.TrangThai;
        if (body.TryGetProperty("maKhuyenMai", out var km))
            pb.MaKhuyenMai = km.ValueKind == JsonValueKind.Null || string.IsNullOrEmpty(km.GetString()) ? "" : km.GetString();
        if (body.TryGetProperty("duongDanAnh", out var dda))
            pb.DuongDanAnh = dda.ValueKind == JsonValueKind.Null || string.IsNullOrEmpty(dda.GetString()) ? "" : dda.GetString();
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    });

    app.MapGet("/api/image-proxy", async (string url, HttpContext ctx) =>
    {
        if (string.IsNullOrWhiteSpace(url)) return Results.BadRequest();
        try
        {
            using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
            http.Timeout = TimeSpan.FromSeconds(15);
            var resp = await http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return Results.StatusCode((int)resp.StatusCode);
            var contentType = resp.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
            var stream = await resp.Content.ReadAsStreamAsync();
            return Results.Stream(stream, contentType);
        }
        catch { return Results.StatusCode(502); }
    });

    app.MapGet("/api/check-payment", async (CarProject.Data.AppDbContext db, int? maDonCoc, string? maGiaoDich) =>
    {
        if (maDonCoc.HasValue)
        {
            var don = await db.DonDatCoc.FindAsync(maDonCoc.Value);
            return Results.Json(new { paid = don?.TrangThaiThanhToan == "Đã thanh toán" });
        }

        if (!string.IsNullOrEmpty(maGiaoDich))
        {
            var lich = await db.LichHenLaiThu
                .FirstOrDefaultAsync(l => l.YKienKhachHang.Contains(maGiaoDich));
            var paid = lich != null && lich.YKienKhachHang.Contains("| Paid");
            return Results.Json(new { paid });
        }

        return Results.BadRequest(new { success = false, error = "Missing maDonCoc or maGiaoDich" });
    });

    app.MapPost("/api/sepay-webhook", async (HttpContext ctx, CarProject.Services.ISepayService sepay, CarProject.Data.AppDbContext db, CarProject.Services.IActivityLogService log) =>
    {
        try
        {
            using var reader = new StreamReader(ctx.Request.Body);
            var body = await reader.ReadToEndAsync();

            var signature = ctx.Request.Headers["X-SePay-Signature"].FirstOrDefault() ?? "";
            var timestamp = ctx.Request.Headers["X-SePay-Timestamp"].FirstOrDefault() ?? "";
            if (!sepay.VerifyWebhook(body, signature, timestamp))
                return Results.Json(new { success = false, message = "Invalid signature" }, statusCode: 400);

            var data = System.Text.Json.JsonSerializer.Deserialize<CarProject.Services.SepayWebhookData>(body);
            if (data == null || data.id <= 0)
                return Results.Ok(new { success = true });

            var maGiaoDich = sepay.ExtractTransactionCode(data.content ?? "");
            if (string.IsNullOrEmpty(maGiaoDich))
                return Results.Ok(new { success = true });

            var notifSvc = ctx.RequestServices.GetRequiredService<CarProject.Services.INotificationService>();
            var donCoc = await db.DonDatCoc
                .Include(d => d.ChiTiets)
                .FirstOrDefaultAsync(d => d.MaGiaoDich == maGiaoDich);
            if (donCoc != null)
            {
                donCoc.SepayTransactionId = data.id;
                var wasPaid = donCoc.TrangThaiThanhToan == "Đã thanh toán";
                donCoc.TrangThaiThanhToan = "Đã thanh toán";
                if (string.IsNullOrEmpty(donCoc.TrangThaiDonHang))
                    donCoc.TrangThaiDonHang = "Chờ xác nhận";

                // Cộng doanh thu cọc vào ThongKeTongHop_Boss theo showroom nguồn từng chi tiết
                // (chỉ khi đây là lần đầu thanh toán / chưa tính doanh thu)
                if (!wasPaid || !donCoc.DaTinhDoanhThu)
                {
                    var revenueSvc = ctx.RequestServices.GetRequiredService<CarProject.Services.IRevenueService>();
                    await revenueSvc.AllocateDepositRevenueAsync(donCoc);
                }

                await db.SaveChangesAsync();

                // Lần đầu thanh toán (chuyển từ chưa thanh toán sang đã thanh toán):
                // báo khách + Admin + Quản lý showroom có xe trong đơn
                if (!wasPaid)
                {
                    var hnCn = donCoc.MaChiNhanh == null ? null
                        : await db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaChiNhanh == donCoc.MaChiNhanh);
                    var hnName = hnCn?.TenChiNhanh ?? donCoc.MaChiNhanh ?? "";

                    var xeTextList = new List<string>();
                    foreach (var g in donCoc.ChiTiets.GroupBy(c => c.MaChiNhanh))
                    {
                        var gCn = await db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaChiNhanh == g.Key);
                        xeTextList.Add($"- {g.Sum(x => x.SoLuong)} xe của {(gCn?.TenChiNhanh ?? g.Key ?? "")}"
);}
                    var listText = string.Join("\n", xeTextList);
                    var totalXe = donCoc.ChiTiets.Sum(c => c.SoLuong);

                    if (!string.IsNullOrEmpty(donCoc.MaKhachHang))
                    {
                        await notifSvc.SendAsync(donCoc.MaKhachHang, "Thanh toán thành công",
                            $"Thanh toán cọc cho đơn #{donCoc.MaDonCoc} đã hoàn tất. {totalXe} xe sẽ được xử lý theo showroom {hnName}.", "/Orders/DepositResult?maDonCoc=" + donCoc.MaDonCoc);
                    }

                    // Báo Admin + Quản lý showroom có xe khi có đơn cọc online vừa thanh toán
                    await notifSvc.SendToRoleAsync("Admin", "Đơn đặt cọc mới - đã thanh toán",
                        $"Khách {donCoc.HoTen} đã đặt cọc {totalXe} xe (đơn #{donCoc.MaDonCoc}). Vui lòng vào xử lý.",
                        "/Admin/DonCoc/Index");

                    foreach (var g in donCoc.ChiTiets.GroupBy(c => c.MaChiNhanh))
                    {
                        var gCn = await db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaChiNhanh == g.Key);
                        var ql = gCn?.MaQuanLy;
                        if (!string.IsNullOrWhiteSpace(ql))
                        {
                            var gName = gCn?.TenChiNhanh ?? (g.Key ?? "");
                            await notifSvc.SendAsync(ql, "Đơn đặt cọc mới - đã thanh toán",
                                $"Khách {donCoc.HoTen} đã thanh toán {g.Sum(x => x.SoLuong)} xe phân bổ tại {gName}. Vui lòng tiếp nhận trong đơn #{donCoc.MaDonCoc}.",
                                $"/QuanLy/DonCoc?highlight={donCoc.MaDonCoc}");
                        }
                    }
                }

                await log.LogAsync($"Webhook Sepay nhận thanh toán đơn cọc #{donCoc.MaDonCoc}, số tiền {data.transferAmount}");
                return Results.Ok(new { success = true });
            }

            var lichHen = await db.LichHenLaiThu
                .FirstOrDefaultAsync(l => l.MaGiaoDich == maGiaoDich || l.YKienKhachHang.Contains(maGiaoDich));
            if (lichHen != null)
            {
                lichHen.SepayTransactionId = data.id;
                lichHen.MaGiaoDich = maGiaoDich;
                if (!lichHen.YKienKhachHang.Contains("| Paid"))
                    lichHen.YKienKhachHang = lichHen.YKienKhachHang.Trim() + " | Paid";
                await db.SaveChangesAsync();

                if (!string.IsNullOrEmpty(lichHen.MaKhachHang))
                {
                    await notifSvc.SendAsync(lichHen.MaKhachHang, "Thanh toán đặt cọc lái thử thành công",
                        $"Yêu cầu lái thử đã được ghi nhận và thanh toán. Showroom sẽ liên hệ bạn sớm.", "/TestDrive");
                }

                await log.LogAsync($"Webhook Sepay nhận thanh toán lịch lái thử #{lichHen.MaLichHen}, mã giao dịch {maGiaoDich}");
                return Results.Ok(new { success = true });
            }

            return Results.Ok(new { success = true });
        }
        catch (Exception ex)
        {
            var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Sepay webhook error");
            return Results.Json(new { success = false, message = "Internal error" }, statusCode: 500);
        }
    });

    app.MapRazorPages();
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
