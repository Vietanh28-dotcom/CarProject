# Báo cáo tích hợp bên ngoài — CarProject

Phiên bản: cập nhật 2026-08-05
Tác giả: [Auto-generated]

## Mục đích
Tổng hợp tất cả các tích hợp/dịch vụ bên ngoài đã tích hợp vào ứng dụng `CarProject`, vị trí code liên quan, cách cấu hình, cách kiểm thử và các khuyến nghị bảo mật/triển khai.

---

## Tổng quan tích hợp
- Sepay (QR payment + webhook)
- VietQr (tạo QR data-uri)
- SMTP (gửi email)
- Google OAuth (đăng nhập)
- Serilog (logging)
- JWT cookie auth (JWT lưu trong cookie)
- Minimal APIs (ajax endpoints phục vụ client)
- Cấu hình DB / EF Core + migrations

---

## Chi tiết từng tích hợp

**1) Sepay (QR / webhook)**
- Mục đích: tạo mã QR/chi tiết chuyển khoản để khách chuyển khoản (hiển thị QR), nhận webhook xác nhận thanh toán và lưu `SepayTransactionId` vào đơn đặt cọc.
- File cấu hình: [CarProject/CarProject/appsettings.json](CarProject/CarProject/appsettings.json#L1-L80)
- Service & DI: `SepayService` được cấu hình tại [CarProject/CarProject/Program.cs](CarProject/CarProject/Program.cs#L60-L76)
- Implement: [CarProject/CarProject/Services/SepayService.cs](CarProject/CarProject/Services/SepayService.cs)
- Nơi sử dụng: trả `showQr`/`qrUrl` tại
  - [CarProject/CarProject/Pages/Orders/DepositForm.cshtml.cs](CarProject/CarProject/Pages/Orders/DepositForm.cshtml.cs#L420-L480)
  - [CarProject/CarProject/Pages/Orders/Cart/Checkout.cshtml.cs](CarProject/CarProject/Pages/Orders/Cart/Checkout.cshtml.cs#L320-L360)
  - [CarProject/CarProject/Pages/TestDrive.cshtml.cs](CarProject/CarProject/Pages/TestDrive.cshtml.cs#L200-L240)
- Migration liên quan: [CarProject/CarProject/Migrations/20260804044651_FixSepayTransactionIdType.cs](CarProject/CarProject/Migrations/20260804044651_FixSepayTransactionIdType.cs)
- Kiểm thử: cần domain/public HTTPS để Sepay webhook hoạt động (hoặc dùng ngrok). Quy trình kiểm thử mô tả ở phần "Hướng dẫn kiểm thử".

**2) VietQr (QR generator)**
- Mục đích: sinh payload chuẩn VNPAY/Napas, tạo PNG và trả data URI `data:image/png;base64,...` cho frontend.
- Implement: [CarProject/CarProject/Services/VietQr.cs](CarProject/CarProject/Services/VietQr.cs#L1-L120)
- Sử dụng: cùng chỗ Sepay để set `qrUrl`.

**3) SMTP / Email**
- Mục đích: gửi email thông báo (đăng ký, đặt cọc, thông báo admin).
- Cấu hình: `Smtp` section trong [CarProject/CarProject/appsettings.json](CarProject/CarProject/appsettings.json#L1-L80)
- Service: [CarProject/CarProject/Services/EmailService.cs](CarProject/CarProject/Services/EmailService.cs)

**4) Google OAuth**
- Mục đích: đăng nhập bằng Google nếu bật trong config.
- Cấu hình & đăng ký: [CarProject/CarProject/Program.cs](CarProject/CarProject/Program.cs#L80-L120)

**5) Serilog**
- Mục đích: logging ra console và file (Logs/app-.log).
- Cấu hình: [CarProject/CarProject/Program.cs](CarProject/CarProject/Program.cs#L1-L40)
- Activity log service: [CarProject/CarProject/Services/ActivityLogService.cs](CarProject/CarProject/Services/ActivityLogService.cs)

**6) JWT cookie auth**
- Mục đích: xác thực người dùng bằng JWT lưu trong cookie (`MyLxCarJwt`) thay vì Bearer header.
- Helpers: [CarProject/CarProject/Services/JwtCookieExtensions.cs](CarProject/CarProject/Services/JwtCookieExtensions.cs)
- Middleware đọc token và gán `HttpContext.User`: [CarProject/CarProject/Program.cs](CarProject/CarProject/Program.cs#L250-L320)
- Lưu ý: API yêu cầu cookie để hoạt động; các tool thử nghiệm phải set cookie JWT.

**7) Minimal APIs & AJAX endpoints**
- Nhiều endpoint tiện ích được định nghĩa trong `Program.cs` (login JSON, avatar upload, cart APIs, admin APIs...). Xem: [CarProject/CarProject/Program.cs](CarProject/CarProject/Program.cs#L300-L700)

**8) EF Core & Migrations**
- DbContext và migrations nằm trong thư mục `Migrations/` (nhiều file). Kiểm tra đặc biệt cột `SepayTransactionId` đã được migrate thành `bigint`.

---

## Hướng dẫn kiểm thử nhanh (manual)
1. Chạy ứng dụng local:
```powershell
dotnet run --project CarProject/CarProject/CarProject.csproj
```
2. Mở trình duyệt tới `https://localhost:7185`.
3. Đăng nhập test user: `user` / `user123` (seeded trong `DbInitializer`).
4. Thêm 1 xe vào giỏ hàng (từ trang details), vào `Giỏ hàng` → `Thanh toán`.
5. Chọn phương thức thanh toán `Chuyển khoản` (nếu có) — frontend sẽ gọi endpoint và nhận JSON chứa `showQr`/`qrUrl`.
6. Nếu AJAX nhận được HTML (redirect login), console sẽ hiển thị lỗi do middleware trước đây; đã cập nhật middleware để trả `401` JSON cho AJAX. Nếu vẫn trả HTML, kiểm tra cookie JWT.
7. Để kiểm thử webhook Sepay: triển khai public HTTPS hoặc sử dụng ngrok, cấu hình `Sepay:WebhookBaseUrl` và gửi test webhook.

---

## Checklist triển khai (Production)
- [ ] Đặt `Sepay:ApiKey` và `Sepay:WebhookSecret` trên biến môi trường hoặc config bảo mật.
- [ ] Xác nhận `Sepay:WebhookBaseUrl` là domain công khai HTTPS và endpoint webhook đang chạy.
- [ ] Chạy migrations (`dotnet ef database update`) để đảm bảo các cột cần thiết (`SepayTransactionId`) tồn tại.
- [ ] Cập nhật SMTP cấu hình (host, user, password) cho môi trường.
- [ ] Cập nhật Google OAuth clientId/clientSecret nếu dùng.
- [ ] Kiểm tra và cập nhật các package có cảnh báo bảo mật (xem build warnings: `Azure.Identity`, `Microsoft.Data.SqlClient`).
- [ ] Cấu hình log rotation / quyền truy cập thư mục `Logs`.

---

## Vấn đề đã biết / đề xuất
- Middleware AJAX: đã sửa trả `401` JSON cho các request AJAX chưa auth; giữ làm tiêu chuẩn cho client-side.
- DB model vs schema: nếu model có trường `[NotMapped]` (ví dụ `LichHenLaiThu.MaGiaoDich`) — kiểm tra kỹ nếu muốn lưu dữ liệu giao dịch ở bảng khác.
- Sepay webhook cần test trên môi trường public (ngrok / staging).
- Cập nhật dependencies có advisory.

---

## Tệp tham khảo chính (code)
- `CarProject/CarProject/appsettings.json` — cấu hình Sepay, Smtp, ConnectionStrings
- `CarProject/CarProject/Program.cs` — DI, middleware, minimal APIs
- `CarProject/CarProject/Services/SepayService.cs` — Sepay integration
- `CarProject/CarProject/Services/VietQr.cs` — QR generator
- `CarProject/CarProject/Pages/Orders/DepositForm.cshtml.cs` — deposit (single) handler
- `CarProject/CarProject/Pages/Orders/Cart/Checkout.cshtml.cs` — checkout (cart) handler

---

Nếu bạn muốn tôi xuất file này ra PDF hoặc thêm log thực tế (console + response JSON từ bài test đặt cọc), chọn 1 trong các tuỳ chọn:
- Tạo PDF từ Markdown và đặt tại `docs/integration-report.pdf`.
- Chạy test thực tế: tự động add-to-cart + checkout bằng `user/user123`, thu JSON trả về và đính kèm vào báo cáo.

