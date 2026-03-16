# GAME DESIGN DOCUMENT: BLOCKFLASH

**Dự án:** BlockFlash (Elemental Puzzle)  
**Tình trạng:** Đã hoàn thiện cơ chế lõi (Core Prototype)  
**Nền tảng:** PC / Mobile (Unity)  
**Ngôn ngữ:** Tiếng Việt

---

## 1. TỔNG QUAN DỰ ÁN (PROJECT OVERVIEW)
*   **Thể loại:** Puzzle (Xếp khối), Casual.
*   **Mô tả ngắn:** Một trò chơi giải đố dựa trên cơ chế xếp khối kinh điển (giống 1010! hoặc Block Blast!), nhưng kết hợp thêm hệ thống **Nguyên tố (Elements)** và **Buff/Skill** để tạo ra chiều sâu chiến thuật và hiệu ứng bùng nổ.
*   **Giá trị cốt lõi (Core Pillars):**
    *   **Dễ chơi nhưng khó giỏi:** Thao tác kéo thả đơn giản nhưng cần tính toán để kích hoạt chuỗi phản ứng.
    *   **Thỏa mãn thị giác (Juiciness):** Hiệu ứng cháy nổ, sét giật và rung màn hình tạo cảm giác hưng phấn.
    *   **Chiến thuật linh hoạt:** Hệ thống Buff cho phép người chơi tùy biến phong cách chơi theo từng ván.

---

## 2. CƠ CHẾ GAME CHÍNH (CORE MECHANICS)
### 2.1. Bàn chơi (The Grid)
*   Kích thước: **8x8 ô**.
*   Trạng thái ô: Trống (Empty), Đang giữ khối (Hover), Đã lấp đầy (Occupied).

### 2.2. Các khối (Polyominos)
*   Game sử dụng các khối đa hình được chia thành **4 cấp độ (Tiers)**:
    *   **Tier 1 (1-2 ô):** Dễ nhất, dùng để lấp chỗ trống.
    *   **Tier 2 (3 ô):** Các khối chữ L nhỏ, thanh thẳng 3 ô.
    *   **Tier 3 (3-4 ô):** Các khối Tetromino kinh điển (hình vuông, chữ Z, chữ T).
    *   **Tier 4 (5 ô):** Pentomino, các khối khó và chiếm diện tích lớn.
*   **Cơ chế Spawn:** Khối được sinh ra theo bộ 3 khối mỗi lượt. Tỉ lệ xuất hiện khối khó (Tier cao) tăng dần theo điểm số của người chơi.

### 2.3. Quy tắc Clear (Xóa dòng)
*   **Clear truyền thống:** Lấp đầy toàn bộ 1 hàng ngang hoặc 1 cột dọc.
*   **Clear đặc biệt (Buff-based):** 
    *   **Seven Cell Clear:** Xóa hàng/cột nếu có ít nhất 7 ô liền nhau được lấp đầy.
    *   **Diagonal Clear:** Xóa theo 2 đường chéo chính của bàn chơi, có thể kết hợp với buff Seven Cell Clear.

---

## 3. HỆ THỐNG NGUYÊN TỐ (ELEMENTAL SYSTEM)
Mỗi ô trong khối có thể mang một thuộc tính nguyên tố, tạo ra các "phản ứng" khi bị xóa.

| Nguyên tố | Hiệu ứng khi bị xóa (On Clear) |
| :--- | :--- |
| **Normal** | Không có hiệu ứng đặc biệt, cộng điểm cơ bản. |
| **Fire** | Kích hoạt hiệu ứng cháy nổ 3x3 sau 1 giây chờ. Gây nổ hàng loạt ô xung quanh. |
| **Lightning** | Phóng sét tới 3 ô ngẫu nhiên đang có khối trên bàn và xóa chúng ngay lập tức. |
| **Ice** | Không biến mất ngay. Khi bị xóa lần 1, lớp băng vỡ ra và ô đó trở thành ô **Normal**. Cần xóa 2 lần. |

---

## 4. HỆ THỐNG BUFF & SKILL
### 4.1. Buff (Cường hóa thụ động)
Khi đạt các mốc điểm nhất định (Milestones), người chơi được chọn 1 trong 3 Buff ngẫu nhiên:
1.  **LightningRateUp:** Tăng tỉ lệ xuất hiện khối nguyên tố Sét.
2.  **FireRateUp:** Tăng tỉ lệ xuất hiện khối nguyên tố Lửa.
3.  **IceRateDown:** Giảm tỉ lệ xuất hiện khối Băng (giảm độ khó).
4.  **DiagonalClear:** Mở khóa khả năng xóa dòng theo đường chéo.
5.  **SevenCellClear:** Cho phép xóa hàng/cột khi đạt 7 ô liên tiếp.
6.  **HardPieceRateDown:** Giảm tỉ lệ xuất hiện các khối Tier 4.
7.  **ScoreMultiplier:** Nhân hệ số điểm số nhận được.
8.  **SkillCooldownReduce:** Giảm thời gian hồi chiêu của kỹ năng chủ động.

### 4.2. Skill (Kỹ năng chủ động)
*   **Swap Skill:** Cho phép người chơi đổi bộ 3 khối hiện tại lấy bộ mới nếu cảm thấy không thể đặt được vào bàn. Cooldown dựa trên số lượt đặt khối.

---

## 5. CHẾ ĐỘ CHƠI (GAME MODES)
### 5.1. Classic Mode (Endless)
*   Chế độ vô tận, cố gắng đạt điểm cao nhất có thể, tương tự Block Blast
*   Hệ thống Buff được kích hoạt liên tục qua các mốc điểm, Skill có thể được dùng.
*   Game kết thúc khi không còn chỗ trống để đặt bất kỳ khối nào trong bộ 3 khối hiện tại.

### 5.2. Level Mode (Missions)
*   Các màn chơi được thiết kế sẵn (Hand-crafted) với các mục tiêu cụ thể, ví dụ:
    *   Xóa tổng 10 hàng và cột.
    *   Kích hoạt nổ Lửa 5 lần.
    *   Phá vỡ 20 ô Băng.
*   Bàn chơi được xếp sẵn các khối ở vị trí hiểm hóc, các khối chờ cũng được xếp sẵn.
*   **Lưu ý:** Hệ thống Buff, Skill bị vô hiệu hóa trong chế độ này để đảm bảo tính thử thách của Level Design.

---

## 6. GIAO DIỆN & TRẢI NGHIỆM NGƯỜI DÙNG (UI/UX)
*   ** HUD (Heads-up Display):**
    *   Điểm số hiện tại.
    *   Nút kỹ năng Swap Blocks.
*   **Visual Juice:**
    *   **Ghost Block:** Hiển thị mờ vị trí khối sẽ đặt xuống để người chơi dễ căn chỉnh.
    *   **Screen Shake:** Rung nhẹ màn hình khi nổ Lửa hoặc xóa nhiều dòng cùng lúc.
    *   **Score Popup:** Hiển thị con số điểm bay lên tại vị trí vừa xóa ô.

---

## 7. ÂM THANH & NHẠC NỀN (AUDIO)
*   **Nhạc nền (BGM):** Sử dụng hệ thống **Procedural Music**. Nhạc nền tự động thay đổi nhạc điệu hoặc cường độ mỗi khi người chơi đạt thêm 500 điểm, tạo cảm giác tiến triển.
*   **Hiệu ứng âm thanh (SFX):**
    *   Tiếng "Swoosh" khi kéo khối.
    *   Tiếng nổ đanh khi xóa dòng (Combo càng cao tiếng càng vang).
    *   Âm thanh đặc trưng cho từng nguyên tố (tiếng điện xẹt, tiếng lửa bùng, tiếng băng vỡ).

---

## 8. CÔNG CỤ PHÁT TRIỂN (TECHNICAL TOOLS)
*   **LevelDataEditor:** Công cụ tùy chỉnh trong Unity Editor để Designer có thể:
    *   Vẽ bàn chơi ban đầu cho các Level.
    *   Thiết lập mục tiêu (Goals) cho từng màn.
    *   Thiết lập các khối chờ cho từng màn.
    *   Chỉ định vị trí các ô nguyên tố cố định.
