# GAME DESIGN DOCUMENT: BLOCKFLASH

**Dự án:** BlockFlash
**Thể loại:** Puzzle / Strategy / Casual  
**Phiên bản:** 1.0  
**Ngôn ngữ thiết kế:** Tiếng Anh 
**Nền tảng:** PC / Mobile (Unity)

---

## 1. TỔNG QUAN (OVERVIEW)

### 1.1. Mục tiêu sản phẩm
Tạo ra một phiên bản cải tiến cho dòng game xếp khối Block Blast bằng cách đưa vào các yếu tố chiến thuật: **Quản lý tài nguyên, Hệ thống Nguyên tố, Kĩ năng và Dự báo tương lai.**

### 1.2. Đối tượng người chơi
*   **Người chơi Casual:** Thích cảm giác nổ combo và dọn dẹp bàn cờ nhanh chóng.
*   **Người chơi Puzzle Hardcore:** Muốn giải các bài toán logic phức tạp trong Level Mode với tài nguyên hữu hạn (Túi khối cố định, số lượt đi giới hạn).

---

## 🕹️ 2. VÒNG LẶP GAME (GAME LOOP)

1.  **Nhận quân bài:** Hệ thống nạp 3 khối chờ vào slot (**Rolling Refill** - nạp ngay khi có ô trống nếu đang ở Level Mode).
2.  **Phân tích & Dự đoán (Level Mode):** Người chơi xem bảng **Deck Summary** (tổng số khối còn lại) và ô **Next Piece** (khối sắp nạp) để lên kế hoạch.
3.  **Hành động:** 
    *   **Đặt khối:** Khớp khối vào bàn cờ để xóa dòng.
    *   **Bỏ khối (Discard) (Level Mode):** Đánh đổi 2 lượt đi (-2 Moves) để lấy khối mới từ túi nếu cần.
4.  **Hiệu ứng:** Kích hoạt các nguyên tố (**Fire**, **Lightning**, **Ice**) để dọn dẹp bàn cờ diện rộng hoặc tạo chuỗi combo.
5.  **Phát triển:** Nhận Buff và tiến xa nhất có thể (Endless Mode), hoặc Hoàn thành mục tiêu của mỗi màn chơi (Level Mode).

---

## 🧱 3. CƠ CHẾ CHÍNH (CORE MECHANICS)

### 3.1. Bàn chơi (The Grid)
*   Kích thước 8x8 ô vuông đồng nhất.
*   Mỗi ô có thể mang: Trạng thái (Trống/Đầy) và thuộc tính Nguyên tố (Element).
*   Độ phân giải hiển thị: Các ô nguyên tố có màu sắc, icon  đặc trưng để dễ nhận diện chiến thuật.

### 3.2. Quy tắc Đặt khối & Xóa dòng
*   **Xóa hàng ngang/dọc:** Khi lấp đầy 8 ô bất kể nguyên tố.
*   **Combo:** Xóa hàng liên tiếp trong các lượt đi, hoặc xóa nhiều hàng cùng lúc sẽ tăng Combo, từ đó tăng hệ số nhân điểm (Multiplier).
*   **Diagonal Clear (Buff):** Xóa theo các đường chéo (8 ô, hoặc 7 ô nếu có Buff).
*   **7-Cell Clear (Buff):** Giảm độ khó, chỉ cần lấp đầy 7/8 ô trong hàng để kích nổ.
*   **Tính điểm:** Score = (Số ô xóa được x 5) x Hệ số Combo.

### 3.3. Hệ thống tính điểm & Combo (Scoring System)
*   **Công thức:** `Score = (CellClear x 5) x ComboMultiplier`
*   **CellClear:** Mỗi ô đơn lẻ bị xóa được tính +5 điểm (Ví dụ: 1 hàng 8 ô = 40 điểm cơ bản).
*   **ComboMultiplier:** Bắt đầu từ x1.0 và tăng thêm 0.1 cho mỗi cấp Combo (x1.0 -> x1.1 -> x1.2...).
*   **Cơ chế tăng Combo:** 
    *   Tăng +1 Combo cho mỗi hàng/cột (8 ô) được xóa.
    *   Tăng +1 Combo cho mỗi chuỗi 7+ ô được xóa (nếu có Buff 7-Cell hoặc Diagonal).
    *   Combo tích lũy khi xóa nhiều hàng cùng lúc HOẶC xóa hàng liên tiếp trong các lượt đi kế tiếp.
*   **Cơ chế Reset:** Combo sẽ quay về 0 nếu người chơi kết thúc lượt đặt khối (bao gồm cả các hiệu ứng nổ dây chuyền) mà không xóa được hàng/cột nào.

### 3.4. Rolling Refill (Nạp khối liên tục) trong Level Mode
*   Khác với game truyền thống và Endless Mode (phải dùng hết 3 khối mới đổi bộ mới), hệ thống sẽ nạp 1 khối mới từ túi (Pool) ngay khi 1 trong 3 slot chờ bị trống.
*   Tính năng này tối ưu hóa sự lựa chọn của người chơi tại mọi thời điểm, cho phép "xả" khối rác nhanh để chờ khối quan trọng.

---

## 🔥 4. HỆ THỐNG NGUYÊN TỐ (ELEMENTAL SYSTEM)

Mỗi nguyên tố đại diện cho một lối chơi và giải pháp khác nhau:

### 4.1. Fire (Lửa) - Sức mạnh bùng nổ diện rộng
*   **Hiệu ứng:** Khi dòng chứa ô Lửa bị xóa, nó kích hoạt vụ nổ 3x3 quanh ô đó.
*   **Tâm lý:** Giúp người chơi cảm thấy thỏa mãn khi dọn dẹp được các cụm khối "rác" tích tụ lâu ngày, tạo ra các khoảng trống lớn tức thì.

### 4.2. Lightning (Sét) - Giải cứu ô kẹt
*   **Hiệu ứng:** Khi dòng chứa ô Sét bị xóa, nó phóng sét phá hủy ngẫu nhiên 3 ô đang có khối trên bàn cờ.
*   **Tâm lý:** Là "vị cứu tinh" khi người chơi lỡ tay để lại những lỗ hổng đơn lẻ không thể lấp bằng khối to.

### 4.3. Ice (Băng) - Trở ngại đa tầng
*   **Hiệu ứng:** Ô băng cần bị xóa 2 lần. Lần 1: Lớp băng vỡ ra (ô trở lại trạng thái Normal nhưng vẫn ở vị trí cũ). Lần 2: Ô biến mất hoàn toàn.
*   **Tâm lý:** Tạo ra các "vùng cấm" tạm thời, buộc người chơi phải tập trung xóa hàng tại cùng một vị trí 2 lần liên tục.
*   **Hướng phát triển:** Có thể cải tiến cách các nguyên tố tương tác với nhau trong tương lai, thêm các nguyên tố mới.
--- 

## 🧩 5. CHẾ ĐỘ CHƠI CẤP ĐỘ (LEVEL MODE)

### 5.1. Quản lý túi khối (Deck Management) và Giới hạn lượt đi (Move Limit)
*   **Spawn Pool:** Trong Level Mode, danh sách khối là cố định, mỗi màn chơi có 1 danh sách khác nhau.
*   **Deck View (Summary):** Hiển thị danh sách khối và số lượng chính xác còn lại trong túi (Ví dụ: "Còn 2 mảnh chữ L, 1 mảnh 1x1").
*   **Next Piece Preview (Up Next):** Hiển thị khối thứ 4 (khối sắp được nạp vào slot trống) dưới dạng hình ảnh mờ (Alpha 50%).
*   **Move Limit:** Số lượt đi tối đa cho mỗi màn chơi.

### 5.2. Chức năng Bỏ khối (Discard System)
*   **Thao tác:** Nút Discard -> Chọn 1 khối chờ -> Xóa bỏ -> Nạp khối tiếp theo từ túi (khối tiếp theo từ túi là khối thứ 4 trong danh sách khối chờ).
*   **Strategic Cost:** 
    *   Mỗi lần bỏ khối trừ trực tiếp **2 lượt đi (Moves)**.
    *   **Decision Making:** Người chơi phải cân nhắc: "Mình nên bỏ khối CHỮ L này để lấy khối 1x1 chuẩn bị cho mục tiêu, dù mất 2 lượt đi, hay đặt nó vào vị trí xấu và chỉ mất 1 lượt?".
*   **Kết thúc:** Kết thúc khi hoàn thành mục tiêu đặt ra của từng màn chơi.
---

## 📈 6. CHẾ ĐỘ VÔ TẬN (ENDLESS MODE)
    Là chế độ cải tiến từ Block Blast truyền thống
*   **Độ khó lũy tiến:** Khối khó (Tier 4 như Pentomino) xuất hiện nhiều hơn khi điểm số tăng cao.
*   **Hệ thống Buff:** Mỗi mốc điểm nhất định (score = 50, 100, 200, 300, 400, 500...) sẽ hiện 3 tấm thẻ Buff ngẫu nhiên để người chơi "Xây dựng Build" riêng (Tăng tỷ lệ nguyên tố, Mở khóa đường chéo, Nhân điểm số...).
*   **Kỹ năng Swap:** Đổi toàn bộ 3 khối hiện tại lấy bộ mới hoàn toàn (có cooldown dựa trên lượt đặt khối).
*   **Kết thúc:** Kết thúc khi người chơi không thể đặt thêm khối nào nữa.
---

## 🖥️ 7. GIAO DIỆN NGƯỜI DÙNG (UI/HUD ARCHITECTURE)

### 7.1. Màn hình Gameplay (HUD)
*   **Top Bar:** Điểm số, Mục tiêu (Goals), Số lượt đi còn lại (Moves).
*   **Center:** Bàn cờ 8x8 với hiệu ứng Grid và Element rực rỡ.
*   **Dashboard:** 
    *   Bộ 3 khối chờ tại trung tâm dưới.
    *   Ô **UP NEXT** nằm phía bên cạnh bộ 3 khối.
    *   Nút **Discard** tích hợp bộ đếm lượt còn lại ngay trên nút.
*   **Summary Panel:** Bật/Tắt để xem toàn bộ danh sách khối còn lại trong túi.

---

## ✨ 8. HIỆU ỨNG THỊ GIÁC & ÂM THANH (JUICE & AUDIO)

### 8.1. Visual Effects (VFX / Polish)
*   **Screen Shake:** Rung màn hình khi có vụ nổ Lửa hoặc Combo lớn.
*   **Particle Burst:** Tia điện vàng rực rỡ khi Sét giật; Hiệu ứng vỡ nát khi Băng bị phá.
*   **Ghost Block:** Hiển thị vị trí mờ của khối khi người chơi đang kéo rà trên bàn cờ.
*   **Popup Score:** Điểm số bay lên có màu vàng kim cho các pha Combo xuất sắc.
*   **Tweening:** Hoạt ảnh phóng to/thu nhỏ mềm mại khi nhặt khối và đặt khối.

### 8.2. Audio Direction
*   **Procedural Music:** Nhạc nền tự động tăng cường độ (Drum & Bass) khi người chơi sắp hoàn thành mục tiêu hoặc đạt điểm cao kỷ lục.
*   **SFX Layers:** Tiếng "thịch" đầm chắc khi đặt khối thành công; Tiếng "bùm" vang dền từ vụ nổ Lửa.

---

## 🛠️ 9. CÔNG CỤ PHÁT TRIỂN (TECHNICAL ARCHITECTURE)

### 9.1. LevelData (ScriptableObject)
Lưu trữ toàn bộ định nghĩa một màn chơi độc lập:
*   Mục tiêu (Score / Clear / Trigger Elements).
*   Địa hình ban đầu (Ma trận 8x8).
*   Danh sách túi khối (Spawn Pool) chi tiết đến từng ô nguyên tố.
*   Thông số lượt đi & giới hạn nổ/bỏ khối.

### 9.2. LevelDataEditor (Unity Tooling)
*   Giao diện thiết kế trực quan ngay trong Unity Inspector.
*   Tính năng vẽ địa hình bằng cách click chuột, tự động Serialize dữ liệu xuống ScriptableObject.
*   Hỗ trợ thiết kế Element cho từng cell của từng khối trong Pool DRAW.
