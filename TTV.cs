using System;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge; // Hoặc Chrome nếu bạn cài chromedriver.exe
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers; // <<< CẦN CÀI NUGET PACKAGE 'DotNetSeleniumExtras.WaitHelpers'
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

// Namespace chung
namespace TTVUploaderApp
{
    // --- Class Form Chính (TTV) ---
    public partial class TTV : Form
    {
        // Controls
        private TextBox? txtNoiDung;
        private Button? btnChonFile;
        private Button? btnTachChuong;
        private ListBox? lstChuong;
        private Button? btnMoFormDang;

        // Data
        private List<string> processedChapters = new List<string>();

        public TTV()
        {
            try
            {
                InitializeComponentManual();
                // Đặt Icon cho Form TTV
                try { this.Icon = new Icon("TTVIcon.ico"); } // Cần file TTVIcon.ico trong cùng thư mục chạy
                catch (Exception ex) { Console.WriteLine($"Lỗi tải icon TTV: {ex.Message}"); /* Bỏ qua nếu không có icon */ }
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi nghiêm trọng khi khởi tạo giao diện TTV: {ex.Message}", "Lỗi Khởi Tạo UI Chính", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void InitializeComponentManual()
        {
            try
            {
                this.Text = "Đăng truyện TTV";
                this.Size = new Size(500, 650);
                this.StartPosition = FormStartPosition.CenterScreen;
                this.AutoScaleMode = AutoScaleMode.Dpi; // Hỗ trợ tốt hơn trên màn hình HiDPI

                int currentTop = 10, controlLeft = 10, controlWidth = this.ClientSize.Width - 20;
                if (controlWidth <= 0) controlWidth = 460; // Default width

                // Nội dung Label & TextBox
                Label lblNoiDung = new Label() { Text = "Nội dung (từ file .txt):", Top = currentTop, Left = controlLeft, Width = controlWidth, AutoSize = true };
                currentTop += lblNoiDung.Height + 5;
                txtNoiDung = new TextBox() { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 250, Width = controlWidth, Top = currentTop, Left = controlLeft, ReadOnly = false, Font = new Font("Segoe UI", 9F) }; // Font dễ đọc hơn
                currentTop += txtNoiDung.Height + 10;

                // Nút Chọn File
                btnChonFile = new Button() { Text = "1. Chọn File TXT (.txt)...", Top = currentTop, Left = controlLeft, Width = controlWidth, Height = 30 }; // Tăng chiều cao nút
                currentTop += btnChonFile.Height + 10;

                // Danh sách chương Label & ListBox
                Label lblDanhSachChuong = new Label() { Text = "Chương đã tách:", Top = currentTop, Left = controlLeft, Width = controlWidth, AutoSize = true };
                currentTop += lblDanhSachChuong.Height + 5;
                lstChuong = new ListBox() { Width = controlWidth, Height = 150, Top = currentTop, Left = controlLeft, ScrollAlwaysVisible = true, IntegralHeight = false }; // Cho phép hiển thị dòng cuối không hoàn chỉnh
                currentTop += lstChuong.Height + 10;

                // Nút Tách Chương
                btnTachChuong = new Button() { Text = "2. Tách chương từ Nội dung", Left = controlLeft, Top = currentTop, Width = controlWidth, Height = 30 };
                currentTop += btnTachChuong.Height + 15;

                // Nút Mở Form Đăng
                btnMoFormDang = new Button() { Text = "3. Chuẩn bị Đăng chương...", Left = controlLeft, Top = currentTop, Width = controlWidth, Font = new Font(this.Font, FontStyle.Bold), BackColor = Color.LightGreen, Height = 35 };
                currentTop += btnMoFormDang.Height + 10;

                // Thêm Controls vào Form
                Controls.Add(lblNoiDung);
                if (txtNoiDung != null) Controls.Add(txtNoiDung);
                if (btnChonFile != null) Controls.Add(btnChonFile);
                Controls.Add(lblDanhSachChuong);
                if (lstChuong != null) Controls.Add(lstChuong);
                if (btnTachChuong != null) Controls.Add(btnTachChuong);
                if (btnMoFormDang != null) Controls.Add(btnMoFormDang);

                // Gán sự kiện
                if (btnChonFile != null) btnChonFile.Click += BtnChonFile_Click;
                if (btnTachChuong != null) btnTachChuong.Click += BtnTachChuong_Click;
                if (btnMoFormDang != null) btnMoFormDang.Click += BtnMoFormDang_Click;

                this.MinimumSize = new Size(400, 500); // Kích thước tối thiểu
                this.Resize += (s, e) => { // Xử lý thay đổi kích thước (đơn giản)
                    controlWidth = this.ClientSize.Width - 20;
                    lblNoiDung.Width = controlWidth;
                    if (txtNoiDung != null) txtNoiDung.Width = controlWidth;
                    if (btnChonFile != null) btnChonFile.Width = controlWidth;
                    lblDanhSachChuong.Width = controlWidth;
                    if (lstChuong != null) lstChuong.Width = controlWidth;
                    if (btnTachChuong != null) btnTachChuong.Width = controlWidth;
                    if (btnMoFormDang != null) btnMoFormDang.Width = controlWidth;
                };

            }
            catch (Exception ex) { MessageBox.Show($"Lỗi tạo UI TTV: {ex.Message}\n{ex.StackTrace}", "Lỗi UI", MessageBoxButtons.OK, MessageBoxIcon.Error); throw; }
        }

        private void BtnChonFile_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                openFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "Chọn file văn bản chứa nội dung truyện";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    try
                    {
                        // Nên đọc với UTF-8 để hỗ trợ tiếng Việt tốt nhất
                        string fileContent = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                        if (txtNoiDung != null) txtNoiDung.Text = fileContent;
                        if (lstChuong != null) lstChuong.Items.Clear();
                        processedChapters.Clear(); // Xóa danh sách chương cũ
                        if (btnMoFormDang != null) btnMoFormDang.Enabled = false; // Tắt nút đăng khi chọn file mới
                        MessageBox.Show($"Đã tải file: {Path.GetFileName(filePath)}.", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show($"Lỗi khi đọc file: {ex.Message}", "Lỗi Đọc File", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
            }
        }

        private void BtnTachChuong_Click(object? sender, EventArgs e)
        {
            if (lstChuong != null) lstChuong.Items.Clear();
            processedChapters.Clear();
            if (txtNoiDung == null || string.IsNullOrWhiteSpace(txtNoiDung.Text)) { MessageBox.Show("Nội dung đang trống.", "Thiếu nội dung", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            // Disable controls
            SetMainFormControlsEnabled(false);
            this.Cursor = Cursors.WaitCursor;
            this.Refresh(); // Force UI update

            try
            {
                // Chạy tách chương trên luồng khác để không treo UI
                Task.Run(() =>
                {
                    var chapters = TachChuong(txtNoiDung.Text); // Tách chương
                    processedChapters = chapters; // Lưu kết quả

                    // Cập nhật UI trên luồng chính
                    this.Invoke(new Action(() =>
                    {
                        if (chapters.Count > 0)
                        {
                            if (lstChuong != null)
                            {
                                foreach (string chuong in chapters)
                                {
                                    string displayTitle = chuong.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Chương không tên";
                                    lstChuong.Items.Add(displayTitle.Substring(0, Math.Min(80, displayTitle.Length)) + (displayTitle.Length > 80 ? "..." : ""));
                                }
                            }
                            MessageBox.Show($"Đã tách thành công {chapters.Count} chương.", "Tách Chương Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Không tìm thấy định dạng chương hợp lệ trong nội dung.\nĐịnh dạng mẫu: 'Chương 1: Tên chương'", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        // Bật lại controls sau khi hoàn thành
                        SetMainFormControlsEnabled(true);
                        this.Cursor = Cursors.Default;
                    }));
                });
            }
            catch (Exception ex) // Lỗi đồng bộ (ít khi xảy ra ở đây)
            {
                MessageBox.Show($"Lỗi khi bắt đầu tách chương: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetMainFormControlsEnabled(true);
                this.Cursor = Cursors.Default;
            }
        }

        private void SetMainFormControlsEnabled(bool enabled)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(SetMainFormControlsEnabled), enabled);
                return;
            }
            if (btnChonFile != null) btnChonFile.Enabled = enabled;
            if (btnTachChuong != null) btnTachChuong.Enabled = enabled;
            if (btnMoFormDang != null) btnMoFormDang.Enabled = enabled && (processedChapters != null && processedChapters.Count > 0);
            if (txtNoiDung != null) txtNoiDung.Enabled = enabled;
            if (lstChuong != null) lstChuong.Enabled = enabled;
        }


        private void BtnMoFormDang_Click(object? sender, EventArgs e)
        {
            if (processedChapters == null || processedChapters.Count == 0)
            {
                MessageBox.Show("Chưa có nội dung chương nào được tách. Vui lòng chọn file và tách chương trước.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            FormAutomation formDang = new FormAutomation(this, processedChapters);
            formDang.Show(this);
        }

        // Hàm tách chương gốc (đã cải tiến phần chia chương dài)
        private List<string> TachChuong(string text)
        {
            List<string> chapters = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return chapters;
            var chapterTitleRegex = new Regex(@"^\s*[Cc]hương\s+(\d+)\s*:?\s*(.*)$", RegexOptions.Multiline);
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> currentChapterLines = new List<string>();
            bool foundFirstChapter = false;
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                bool isTitle = chapterTitleRegex.IsMatch(trimmedLine);
                if (isTitle)
                {
                    if (currentChapterLines.Count > 0) chapters.Add(string.Join(Environment.NewLine, currentChapterLines).Trim());
                    currentChapterLines = new List<string> { trimmedLine };
                    foundFirstChapter = true;
                }
                else if (foundFirstChapter)
                {
                    currentChapterLines.Add(line);
                }
            }
            if (currentChapterLines.Count > 0) chapters.Add(string.Join(Environment.NewLine, currentChapterLines).Trim());
            return ProcessLongChapters(chapters, 38000);
        }

        // Hàm xử lý chia nhỏ chương quá dài (cải tiến logic ngắt)
        private List<string> ProcessLongChapters(List<string> originalChapters, int maxCharCount)
        {
            List<string> processedResult = new List<string>();
            char[] sentenceEndings = new char[] { '.', '!', '?' };
            char[] paragraphEndings = new char[] { '\n', '\r' };
            foreach (string chapter in originalChapters)
            {
                string[] chapterParts = chapter.Split(new char[] { '\n' }, 2);
                string title = (chapterParts.Length > 0) ? chapterParts[0].Trim() : "Chương không tên";
                string content = (chapterParts.Length > 1) ? chapterParts[1] : "";
                int charCount = content.Length;
                if (charCount > maxCharCount)
                {
                    int parts = (int)Math.Ceiling((double)charCount / maxCharCount);
                    int currentPos = 0;
                    for (int part = 1; part <= parts; part++)
                    {
                        int idealEndPos = Math.Min(currentPos + maxCharCount, content.Length);
                        int endPos = idealEndPos;
                        if (part < parts && endPos < content.Length)
                        {
                            int searchStart = Math.Max(currentPos, idealEndPos - 500);
                            int breakPos = -1;
                            int paraBreakPos = content.LastIndexOfAny(paragraphEndings, idealEndPos - 1, idealEndPos - searchStart);
                            if (paraBreakPos > currentPos) breakPos = paraBreakPos;
                            else
                            {
                                int sentenceBreakPos = content.LastIndexOfAny(sentenceEndings, idealEndPos - 1, idealEndPos - searchStart);
                                if (sentenceBreakPos > currentPos && sentenceBreakPos + 1 < content.Length && char.IsWhiteSpace(content[sentenceBreakPos + 1])) breakPos = sentenceBreakPos + 1;
                            }
                            if (breakPos > currentPos && (content.Length - breakPos) > maxCharCount / 10) endPos = breakPos;
                        }
                        string partContent = content.Substring(currentPos, endPos - currentPos).Trim();
                        if (!string.IsNullOrWhiteSpace(partContent))
                        {
                            string newTitle = $"{title} (Phần {part}/{parts})";
                            processedResult.Add(newTitle + Environment.NewLine + partContent);
                        }
                        currentPos = endPos;
                        if (currentPos >= content.Length && part < parts) { Console.WriteLine($"Cảnh báo: Chia chương '{title}' dừng sớm."); break; }
                    }
                }
                else { processedResult.Add(chapter); }
            }
            return processedResult;
        }

        // Hàm được gọi từ FormAutomation để xóa chương đã đăng khỏi ListBox và danh sách
        public void RemovePostedChapters(int count)
        {
            if (this.InvokeRequired) { try { this.Invoke(new Action<int>(RemovePostedChapters), count); } catch (ObjectDisposedException) { } return; }
            try
            {
                if (lstChuong != null && count > 0)
                {
                    int itemsToRemove = Math.Min(count, lstChuong.Items.Count);
                    for (int i = 0; i < itemsToRemove; i++) if (lstChuong.Items.Count > 0) lstChuong.Items.RemoveAt(0);
                }
                if (processedChapters != null && count > 0)
                {
                    int itemsToRemove = Math.Min(count, processedChapters.Count);
                    if (itemsToRemove > 0) processedChapters.RemoveRange(0, itemsToRemove);
                }
                if (btnMoFormDang != null) btnMoFormDang.Enabled = (processedChapters != null && processedChapters.Count > 0);
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi xóa chương đã đăng: {ex.Message}", "Lỗi Cập Nhật", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        // Entry point
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TTV());
        }
    } // --- Kết thúc class TTV ---

    // =======================================================================

    // --- Class FormAutomation (Khởi tạo UI trong sự kiện Form_Load) ---
    public partial class FormAutomation : Form
    {
        // Controls
        private CheckBox? chkGioiHanSoChuong; private NumericUpDown? numSoChuong; private Button? btnDangChuong; private Button? btnNhapThongTinDangNhap; private Label? lblStatus; private ProgressBar? progressBar;
        // Data & References
        private List<string> chaptersToUpload; private TTV? mainAppForm; private IWebDriver? driver; private string storyUploadUrl = ""; private string username = ""; private string password = "";
        // URLs (loginUrl không còn dùng để điều hướng ban đầu)
        // private string loginUrl = "https://tangthuvien.vn/account/login";

        public FormAutomation(TTV owner, List<string> chapters)
        {
            mainAppForm = owner; chaptersToUpload = chapters ?? new List<string>();
            try
            {
                this.Text = "Đăng truyện TTV - Điều khiển"; this.Size = new Size(450, 400); this.StartPosition = FormStartPosition.CenterParent; this.FormBorderStyle = FormBorderStyle.FixedSingle;
                this.Load += FormAutomation_Load; this.FormClosing += FormAutomation_FormClosing;
                try { this.Icon = new Icon("TTVIcon.ico"); } catch { }
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi constructor FormAutomation: {ex.Message}", "Lỗi Khởi Tạo", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void FormAutomation_Load(object? sender, EventArgs e)
        {
            try
            {
                int currentTop = 15, controlLeft = 15, controlWidth = this.ClientSize.Width - 30; if (controlWidth <= 0) controlWidth = 400;
                btnNhapThongTinDangNhap = new Button() { Text = "1. Nhập URL Truyện Cần Đăng", Top = currentTop, Left = controlLeft, Width = controlWidth, Height = 30 }; currentTop += btnNhapThongTinDangNhap.Height + 15;
                chkGioiHanSoChuong = new CheckBox() { Text = "Tự động đăng 5 chương/lần", Top = currentTop + 3, Left = controlLeft, AutoSize = true, Checked = true, Enabled = false };
                numSoChuong = new NumericUpDown() { Top = currentTop, Left = chkGioiHanSoChuong.Right + 5, Value = 5, Width = 60, Minimum = 1, Maximum = 5, Enabled = false }; currentTop += numSoChuong.Height + 15;
                btnDangChuong = new Button() { Text = "2. Bắt đầu Đăng (Yêu cầu Đăng nhập Thủ công)", Left = controlLeft, Top = currentTop, Width = controlWidth, Font = new Font(this.Font, FontStyle.Bold), BackColor = Color.PaleGreen, Height = 35 }; currentTop += btnDangChuong.Height + 20;
                lblStatus = new Label() { Text = "Trạng thái: Chờ cấu hình...", Top = currentTop, Left = controlLeft, Width = controlWidth, Height = 60, BorderStyle = BorderStyle.Fixed3D }; currentTop += lblStatus.Height + 10;
                progressBar = new ProgressBar() { Minimum = 0, Maximum = 100, Value = 0, Top = currentTop, Left = controlLeft, Width = controlWidth, Height = 23, Style = ProgressBarStyle.Blocks }; currentTop += progressBar.Height + 10;
                if (btnNhapThongTinDangNhap != null) Controls.Add(btnNhapThongTinDangNhap); if (chkGioiHanSoChuong != null) Controls.Add(chkGioiHanSoChuong); if (numSoChuong != null) Controls.Add(numSoChuong); if (btnDangChuong != null) Controls.Add(btnDangChuong); if (lblStatus != null) Controls.Add(lblStatus); if (progressBar != null) Controls.Add(progressBar);
                if (btnNhapThongTinDangNhap != null) btnNhapThongTinDangNhap.Click += BtnNhapThongTinDangNhap_Click; if (btnDangChuong != null) btnDangChuong.Click += BtnDangChuong_Click;
                if (lblStatus != null) UpdateStatus($"Sẵn sàng đăng {chaptersToUpload.Count} chương (lô 5).");
                if (progressBar != null) { progressBar.Maximum = chaptersToUpload.Count > 0 ? chaptersToUpload.Count : 1; progressBar.Value = 0; }
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi tạo UI FormAutomation: {ex.Message}\n{ex.StackTrace}", "Lỗi UI Load", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void UpdateStatus(string message)
        {
            if (lblStatus == null) return;
            if (lblStatus.InvokeRequired) { try { lblStatus.Invoke(new Action<string>(UpdateStatus), message); } catch (ObjectDisposedException) { } }
            else lblStatus.Text = $"Trạng thái: {message}";
        }

        private void UpdateProgress(int value)
        {
            if (progressBar == null) return;
            if (progressBar.InvokeRequired) { try { progressBar.Invoke(new Action<int>(UpdateProgress), value); } catch (ObjectDisposedException) { } }
            else
            {
                if (value < progressBar.Minimum) value = progressBar.Minimum;
                if (progressBar.Maximum <= progressBar.Minimum) progressBar.Maximum = value + 1;
                if (value > progressBar.Maximum) value = progressBar.Maximum;
                progressBar.Value = value;
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            if (this.InvokeRequired) { try { this.Invoke(new Action<bool>(SetControlsEnabled), enabled); } catch (ObjectDisposedException) { } }
            else { if (btnDangChuong != null) btnDangChuong.Enabled = enabled; if (btnNhapThongTinDangNhap != null) btnNhapThongTinDangNhap.Enabled = enabled; }
        }

        private void BtnNhapThongTinDangNhap_Click(object? sender, EventArgs e)
        {
            using (FormLogin formLogin = new FormLogin(this, this.storyUploadUrl, this.username, this.password)) { formLogin.ShowDialog(this); }
        }

        public void SetLoginInfo(string url, string user, string pass)
        {
            storyUploadUrl = url; username = user; password = pass;
            if (!string.IsNullOrWhiteSpace(url)) UpdateStatus($"Đã cập nhật URL. Sẵn sàng đăng {chaptersToUpload.Count} chương.");
            else UpdateStatus($"URL đăng chương chưa được nhập.");
        }

        // --- Hàm xử lý đăng chương Selenium (Mở trang chủ trước) ---
        private async void BtnDangChuong_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(storyUploadUrl) || !Uri.TryCreate(storyUploadUrl, UriKind.Absolute, out _))
            {
                MessageBox.Show("Vui lòng nhập URL trang đăng chương hợp lệ của truyện.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                BtnNhapThongTinDangNhap_Click(sender, e); return;
            }
            if (chaptersToUpload == null || chaptersToUpload.Count == 0)
            {
                MessageBox.Show("Không còn chương nào để đăng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information); return;
            }
            SetControlsEnabled(false); UpdateStatus("Đang khởi động trình duyệt Edge...");
            if (progressBar != null) { progressBar.Maximum = chaptersToUpload.Count; UpdateProgress(0); }
            List<string> remainingChapters = new List<string>(chaptersToUpload);
            int totalChaptersAtStart = remainingChapters.Count; int totalChaptersPosted = 0; bool errorOccurred = false;

            await Task.Run(async () => {
                try
                {
                    var options = new EdgeOptions(); // options.AddArgument("--headless");
                    options.AddArgument("--disable-gpu"); options.AddArgument("--log-level=3"); options.AddArgument("--disable-dev-shm-usage"); options.AddArgument("--no-sandbox");
                    using (driver = new EdgeDriver(options))
                    {
                        var standardWait = new WebDriverWait(driver, TimeSpan.FromSeconds(25)); var pageLoadWait = new WebDriverWait(driver, TimeSpan.FromSeconds(60)); var submitWait = new WebDriverWait(driver, TimeSpan.FromSeconds(90));
                        // Mở trang chủ TTV
                        string homePageUrl = "https://tangthuvien.net/";
                        UpdateStatus("Đang mở trang chủ TTV..."); driver.Navigate().GoToUrl(homePageUrl);
                        try { pageLoadWait.Until(ExpectedConditions.ElementIsVisible(By.TagName("body"))); }
                        catch (WebDriverTimeoutException) { UpdateStatus("Không thể tải trang chủ TTV."); MessageBox.Show($"Không thể tải trang chủ TTV:\n{homePageUrl}", "Lỗi Tải Trang", MessageBoxButtons.OK, MessageBoxIcon.Error); errorOccurred = true; return; }
                        // Yêu cầu đăng nhập thủ công
                        UpdateStatus("VUI LÒNG ĐĂNG NHẬP vào TTV trong cửa sổ trình duyệt (nếu chưa)...");
                        MessageBox.Show("Trình duyệt Edge đã mở trang chủ TTV...\n\nVui lòng đăng nhập vào tài khoản TTV của bạn.\n\nSau khi đăng nhập xong, ứng dụng sẽ tự động tiếp tục.", "Yêu cầu Đăng nhập Thủ công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        // Truy cập URL đăng chương cụ thể
                        UpdateStatus("Đang thử truy cập trang đăng chương của truyện...");
                        string firstChapterNameSelector = "input[name='chap_name[1]']"; // <<< KIỂM TRA SELECTOR NÀY
                        try
                        {
                            driver.Navigate().GoToUrl(storyUploadUrl); UpdateStatus("Đang xác nhận trang đăng chương...");
                            standardWait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(firstChapterNameSelector))); UpdateStatus("Truy cập trang đăng chương thành công! Bắt đầu đăng...");
                        }
                        catch (Exception ex) when (ex is WebDriverTimeoutException || ex is NoSuchElementException || ex is WebDriverException)
                        {
                            UpdateStatus("Lỗi: Không thể truy cập/xác nhận trang đăng chương."); MessageBox.Show($"Không thể xác nhận trang đăng chương:\n{storyUploadUrl}\nLý do: Chưa đăng nhập / Sai URL / Web thay đổi (selector '{firstChapterNameSelector}' sai?).\nLỗi: {ex.Message}", "Lỗi Truy Cập Trang Đăng Chương", MessageBoxButtons.OK, MessageBoxIcon.Error); errorOccurred = true; return;
                        }
                        // Vòng lặp đăng chương
                        while (remainingChapters.Count > 0 && !errorOccurred)
                        {
                            int batchSize = Math.Min(5, remainingChapters.Count); int currentBatchNumber = (totalChaptersPosted / 5) + 1;
                            UpdateStatus($"Chuẩn bị đăng lô {currentBatchNumber} ({batchSize} chương). Còn lại: {remainingChapters.Count}...");
                            // Điền form cho lô
                            for (int i = 0; i < batchSize; i++)
                            {
                                int formIndex = i + 1; string currentChapterData = remainingChapters[i]; string[] parts = currentChapterData.Split(new char[] { '\n' }, 2); string originalTitleLine = parts.Length > 0 ? parts[0] : ""; string content = parts.Length > 1 ? parts[1].Trim() : "";
                                string chuongSo = ExtractChapterNumber(originalTitleLine); string tenChuongToSend = ExtractChapterTitleOnly(originalTitleLine); if (string.IsNullOrWhiteSpace(tenChuongToSend)) { tenChuongToSend = $"Chương {chuongSo}"; UpdateStatus($"Cảnh báo: Dùng tên mặc định '{tenChuongToSend}'."); }
                                string quyenSo = "1";
                                UpdateStatus($"Đang điền chương {totalChaptersPosted + i + 1}/{totalChaptersAtStart}: {tenChuongToSend.Substring(0, Math.Min(50, tenChuongToSend.Length))}...");
                                try
                                { // <<< KIỂM TRA CÁC SELECTOR BÊN DƯỚI >>>
                                    standardWait.Until(ExpectedConditions.ElementToBeClickable(By.Name($"chap_number[{formIndex}]"))).ClearSendKeys(chuongSo); await Task.Delay(100);
                                    standardWait.Until(ExpectedConditions.ElementToBeClickable(By.Name($"vol[{formIndex}]"))).ClearSendKeys(quyenSo); await Task.Delay(100);
                                    standardWait.Until(ExpectedConditions.ElementToBeClickable(By.Name($"chap_name[{formIndex}]"))).ClearSendKeys(tenChuongToSend); await Task.Delay(100);
                                    var noiDungElement = standardWait.Until(ExpectedConditions.ElementIsVisible(By.Name($"introduce[{formIndex}]")));
                                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input')); arguments[0].dispatchEvent(new Event('change'));", noiDungElement, content); await Task.Delay(150);
                                    // Thêm form nếu cần
                                    if (i < batchSize - 1 && formIndex < 5)
                                    {
                                        try
                                        {
                                            UpdateStatus($"Thêm form index {formIndex + 1}..."); var addBtn = standardWait.Until(ExpectedConditions.ElementToBeClickable(By.Id("add-chap"))); addBtn.Click(); // <<< KIỂM TRA ID add-chap
                                            string nextCheck = $"input[name='chap_name[{formIndex + 1}]']"; standardWait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(nextCheck))); UpdateStatus($"Đã thêm form index {formIndex + 1}."); await Task.Delay(250);
                                        }
                                        catch (Exception addEx) { UpdateStatus($"Lỗi nhấn 'Thêm chương': {addEx.Message}"); MessageBox.Show($"Lỗi click nút 'Thêm chương' (ID: add-chap) sau index {formIndex}:\n{addEx.Message}", "Lỗi Thêm Form", MessageBoxButtons.OK, MessageBoxIcon.Error); errorOccurred = true; break; } // Thoát for
                                    }
                                }
                                catch (Exception fillEx) { UpdateStatus($"Lỗi điền chương {totalChaptersPosted + i + 1}: {fillEx.Message}"); MessageBox.Show($"Lỗi điền chương '{tenChuongToSend}' (index {formIndex}):\n{fillEx.Message}\nKiểm tra Selectors.", "Lỗi Điền Thông Tin", MessageBoxButtons.OK, MessageBoxIcon.Error); errorOccurred = true; break; } // Thoát for
                            } // Kết thúc for điền batch
                            if (errorOccurred) break; // Thoát while nếu lỗi trong for
                            // Submit lô
                            UpdateStatus($"Đang gửi {batchSize} chương (Lô {currentBatchNumber})...");
                            try
                            { // <<< KIỂM TRA SELECTOR NÚT SUBMIT >>>
                                var submitButton = standardWait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("button.btn-default[type='submit']"))); submitButton.Click();
                                // Chờ và quay lại trang thêm chương
                                UpdateStatus("Đang chờ chuyển hướng..."); string themChuongLinkSelector = "a.btn-addchap"; // <<< KIỂM TRA SELECTOR NÀY
                                try
                                {
                                    submitWait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(themChuongLinkSelector))); UpdateStatus("Đã về DS. Nhấn Thêm chương..."); await Task.Delay(700);
                                    var themChuongLink = submitWait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector(themChuongLinkSelector))); themChuongLink.Click();
                                    UpdateStatus("Đang chờ trang đăng tải lại..."); standardWait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(firstChapterNameSelector))); UpdateStatus("Sẵn sàng cho lô tiếp.");
                                }
                                catch (Exception waitLinkEx) { UpdateStatus($"Lỗi chờ/nhấn link '{themChuongLinkSelector}': {waitLinkEx.Message}."); MessageBox.Show($"Lỗi chờ/click link 'Thêm chương' ({themChuongLinkSelector}) sau lô {currentBatchNumber}.\nKiểm tra quy trình TTV.\nLỗi: {waitLinkEx.Message}", "Lỗi Sau Submit", MessageBoxButtons.OK, MessageBoxIcon.Warning); errorOccurred = true; break; } // Thoát while
                                // Cập nhật UI chính
                                if (mainAppForm != null) mainAppForm.RemovePostedChapters(batchSize);
                                // Cập nhật trạng thái
                                UpdateStatus($"Đã xử lý lô {currentBatchNumber} ({batchSize} chương)."); remainingChapters.RemoveRange(0, batchSize); totalChaptersPosted += batchSize; UpdateProgress(totalChaptersPosted);
                                if (remainingChapters.Count > 0) { UpdateStatus($"Chờ 1.5 giây..."); await Task.Delay(1500); }
                            }
                            catch (Exception submitEx) { UpdateStatus($"Lỗi gửi lô {currentBatchNumber}: {submitEx.Message}"); MessageBox.Show($"Lỗi submit lô {currentBatchNumber}:\n{submitEx.Message}\nKiểm tra selector nút Submit.", "Lỗi Submit", MessageBoxButtons.OK, MessageBoxIcon.Error); errorOccurred = true; break; } // Thoát while
                        } // Kết thúc while đăng chương
                        // Hoàn thành
                        if (!errorOccurred) { UpdateStatus($"Hoàn tất! Đã đăng {totalChaptersPosted}/{totalChaptersAtStart} chương."); MessageBox.Show($"Đã đăng thành công {totalChaptersPosted} chương!", "Hoàn thành", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                        else { UpdateStatus($"Đã dừng do lỗi. {totalChaptersPosted}/{totalChaptersAtStart} chương đã xử lý."); }
                    } // Kết thúc using driver
                }
                catch (Exception ex)
                { // Lỗi chung
                    errorOccurred = true;
                    if (ex.Message.Contains("driver executable file does not exist") || ex.Message.ToLower().Contains("msedgedriver")) { UpdateStatus("Lỗi: Không tìm thấy msedgedriver.exe."); MessageBox.Show("Lỗi khởi tạo Edge:\nKhông tìm thấy 'msedgedriver.exe'.\nVui lòng tải đúng phiên bản và đặt vào thư mục chạy.", "Lỗi Driver", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    else { UpdateStatus($"Lỗi không mong muốn: {ex.Message}"); MessageBox.Show($"Lỗi không mong muốn:\n{ex.Message}\n{ex.StackTrace}", "Lỗi Nghiêm Trọng", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
                finally
                { // Dọn dẹp cuối cùng
                    SetControlsEnabled(true);
                    string finalStatus = errorOccurred ? $"Đã dừng do lỗi. {totalChaptersPosted}/{totalChaptersAtStart} chương đã xử lý." : $"Hoàn tất. {totalChaptersPosted}/{totalChaptersAtStart} chương đã xử lý.";
                    UpdateStatus(finalStatus); UpdateProgress(totalChaptersPosted);
                    if (driver != null) { try { driver.Quit(); } catch { } driver = null; }
                }
            }); // Kết thúc Task.Run
        }

        private static string ExtractChapterNumber(string titleLine)
        {
            if (string.IsNullOrWhiteSpace(titleLine)) return "1";
            Match match = Regex.Match(titleLine.Trim(), @"^[Cc]hương\s+(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1) return match.Groups[1].Value;
            match = Regex.Match(titleLine.Trim(), @"\d+");
            if (match.Success) return match.Value; return "1";
        }
        private static string ExtractChapterTitleOnly(string titleLine)
        {
            if (string.IsNullOrWhiteSpace(titleLine)) return "";
            Match match = Regex.Match(titleLine.Trim(), @"^[Cc]hương\s+\d+\s*[:\s]\s*(.*)$", RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1 && !string.IsNullOrWhiteSpace(match.Groups[1].Value)) return match.Groups[1].Value.Trim();
            return titleLine.Trim();
        }
        private void FormAutomation_FormClosing(object? sender, FormClosingEventArgs e) { try { driver?.Quit(); } catch { } }

        // Helper extension method cho Clear + SendKeys
    } // --- Kết thúc class FormAutomation ---

    // --- Class FormLogin (Đã có URL mặc định) ---
    public class FormLogin : Form
    {
        private TextBox? txtUrl; private TextBox? txtUsername; private TextBox? txtPassword; private Button? btnOK; private Button? btnCancel; private FormAutomation automationForm;
        public FormLogin(FormAutomation ownerForm, string currentUrl, string currentUser, string currentPass)
        {
            automationForm = ownerForm; try { InitializeComponentManual(currentUrl, currentUser, currentPass); try { this.Icon = new Icon("TTVIcon.ico"); } catch { } } catch (Exception ex) { MessageBox.Show($"Lỗi constructor FormLogin: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        private void InitializeComponentManual(string currentUrl, string currentUser, string currentPass)
        {
            try
            {
                this.Text = "Đăng truyện TTV - Nhập thông tin"; this.Size = new Size(450, 220); this.StartPosition = FormStartPosition.CenterParent; this.FormBorderStyle = FormBorderStyle.FixedDialog; this.MaximizeBox = false; this.MinimizeBox = false; this.ShowInTaskbar = false;
                int labelWidth = 150, controlLeft = labelWidth + 15, controlWidth = this.ClientSize.Width - controlLeft - 15, currentTop = 15;
                Label lblUrl = new Label() { Text = "URL Đăng Chương Truyện:", Top = currentTop + 3, Left = 10, Width = labelWidth };
                string displayUrl = string.IsNullOrWhiteSpace(currentUrl) ? "https://tangthuvien.net/dang-chuong/story/38650" : currentUrl; // Mặc định URL
                txtUrl = new TextBox() { Width = controlWidth, Top = currentTop, Left = controlLeft, Text = displayUrl }; currentTop += txtUrl.Height + 10;
                Label lblUsername = new Label() { Text = "Tài khoản TTV (tùy chọn):", Top = currentTop + 3, Left = 10, Width = labelWidth };
                txtUsername = new TextBox() { Width = controlWidth, Top = currentTop, Left = controlLeft, Text = currentUser }; currentTop += txtUsername.Height + 10;
                Label lblPassword = new Label() { Text = "Mật khẩu TTV (tùy chọn):", Top = currentTop + 3, Left = 10, Width = labelWidth };
                txtPassword = new TextBox() { Width = controlWidth, Top = currentTop, Left = controlLeft, PasswordChar = '*', Text = currentPass }; currentTop += txtPassword.Height + 20;
                int buttonAreaWidth = 80 + 10 + 80, buttonStartLeft = (this.ClientSize.Width - buttonAreaWidth) / 2;
                btnOK = new Button() { Text = "Lưu", Top = currentTop, Left = buttonStartLeft, Width = 80, DialogResult = DialogResult.OK };
                btnCancel = new Button() { Text = "Hủy", Top = currentTop, Left = btnOK.Right + 10, Width = 80, DialogResult = DialogResult.Cancel };
                this.AcceptButton = btnOK; this.CancelButton = btnCancel;
                Controls.Add(lblUrl); if (txtUrl != null) Controls.Add(txtUrl); Controls.Add(lblUsername); if (txtUsername != null) Controls.Add(txtUsername); Controls.Add(lblPassword); if (txtPassword != null) Controls.Add(txtPassword); Controls.Add(btnOK); Controls.Add(btnCancel);
                if (btnOK != null) btnOK.Click += BtnOK_Click;
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi tạo UI FormLogin: {ex.Message}\n{ex.StackTrace}", "Lỗi UI", MessageBoxButtons.OK, MessageBoxIcon.Error); throw; }
        }
        private void BtnOK_Click(object? sender, EventArgs e)
        {
            try
            {
                if (txtUrl == null || string.IsNullOrWhiteSpace(txtUrl.Text)) { MessageBox.Show("Vui lòng nhập URL.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtUrl?.Focus(); this.DialogResult = DialogResult.None; return; }
                if (!Uri.TryCreate(txtUrl.Text.Trim(), UriKind.Absolute, out Uri? uriResult) || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)) { MessageBox.Show("URL không hợp lệ (phải là http/https).", "URL không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtUrl?.Focus(); this.DialogResult = DialogResult.None; return; }
                if (uriResult.Host.Equals("tangthuvien.net", StringComparison.OrdinalIgnoreCase) && uriResult.AbsolutePath.Equals("/", StringComparison.OrdinalIgnoreCase)) { MessageBox.Show("Vui lòng nhập URL cụ thể của trang đăng chương.", "URL Không Cụ Thể", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtUrl?.Focus(); this.DialogResult = DialogResult.None; return; }
                string user = txtUsername?.Text.Trim() ?? ""; string pass = txtPassword?.Text ?? "";
                automationForm.SetLoginInfo(txtUrl.Text.Trim(), user, pass);
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi xử lý Lưu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); this.DialogResult = DialogResult.None; }
        }
    } // --- Kết thúc Class FormLogin ---

    // --- Extension Methods ---
    public static class SeleniumExtensions
    {
        // Helper extension method cho Clear + SendKeys
        public static void ClearSendKeys(this IWebElement element, string text)
        {
            element.Clear();
            element.SendKeys(text);
        }
    }

} // --- Kết thúc namespace TTVUploaderApp ---