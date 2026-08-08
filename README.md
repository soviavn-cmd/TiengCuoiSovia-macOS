# Tiếng Cười Sovia cho macOS

Soundboard nhỏ gọn dành cho livestream và sáng tạo nội dung, với giao diện tương lai lấy cảm hứng từ bảng màu cyan–hồng.

## Tính năng

- 120 nút âm thanh chia thành Effect 1, Effect 2, Music 1 và Music 2.
- Phát/dừng nhanh, chỉnh âm lượng và chế độ luôn nằm trên cùng.
- Chế độ EDIT để đổi tên nút hoặc thay file MP3/WAV/M4A/AAC.
- Tự lưu thiết lập trong thư mục dữ liệu người dùng macOS.
- GitHub Actions tự tạo DMG cho Apple Silicon và Intel.

## Cài đặt

Tải đúng file DMG trong phần **Actions → Artifacts**:

- `TiengCuoiSovia-macOS-arm64.dmg`: Mac Apple Silicon (M1/M2/M3/M4/M5).
- `TiengCuoiSovia-macOS-x64.dmg`: Mac Intel.

Kéo ứng dụng vào thư mục Applications. Bản hiện tại chưa ký bằng Apple Developer ID; lần mở đầu có thể cần nhấp chuột phải vào ứng dụng và chọn **Open**.

## Build

Yêu cầu .NET 8 SDK:

```bash
dotnet restore
dotnet run
```

Ứng dụng dùng `afplay` có sẵn trong macOS để phát âm thanh.
