# OrderService – BTH4 SOA

# 1. Giới thiệu
OrderService là REST API quản lý đơn hàng được xây dựng bằng ASP.NET Core Web API trong bài thực hành Service-Oriented Architecture (SOA).

# 2. Công nghệ sử dụng
- C# và ASP.NET Core Web API (.NET 8)
- Entity Framework Core
- SQLite
- JWT Bearer Authentication
- Swagger / OpenAPI

# 3. Chức năng
- Lấy danh sách và chi tiết đơn hàng.
- Tạo đơn hàng mới (kiểm tra và trừ số lượng tồn kho từ ProductService).
- Cập nhật trạng thái đơn hàng ('pending', 'completed', 'cancelled').
- Xóa đơn hàng.
- Quản lý chi tiết đơn hàng ('order_items'): xem, thêm, cập nhật, xóa mặt hàng.
- Xác thực JWT khi truy cập các API đơn hàng.

# 4. Các API

| Method | Endpoint | Chức năng |
|---|---|---|
| GET | '/orders' | Lấy danh sách đơn hàng |
| GET | '/orders/{id}' | Lấy đơn hàng theo ID |
| POST | '/orders' | Tạo đơn hàng mới (kiểm tra tồn kho) |
| PUT | '/orders/{id}' | Cập nhật trạng thái đơn hàng |
| DELETE | '/orders/{id}' | Xóa đơn hàng |
| GET | '/order_items' | Lấy danh sách chi tiết đơn hàng |
| GET | '/order_items/{id}' | Lấy chi tiết đơn hàng theo ID |
| POST | '/order_items' | Thêm mặt hàng mới |
| PUT | '/order_items/{id}' | Cập nhật mặt hàng |
| DELETE | '/order_items/{id}' | Xóa mặt hàng |

# 5. Cách chạy
1. Khởi chạy service xác thực (BTH2) ở 'http://localhost:5144' và ProductService ở 'http://localhost:5145'.
2. Mở project 'OrderService.csproj' bằng Visual Studio.
3. Khôi phục các NuGet packages.
4. Chạy project bằng profile HTTP đã cấu hình.
5. Truy cập 'http://localhost:5146/swagger' để thử nghiệm API.

# 6. Database
Ứng dụng sử dụng SQLite. Database được tạo tự động khi khởi chạy ứng dụng nếu chưa tồn tại.
