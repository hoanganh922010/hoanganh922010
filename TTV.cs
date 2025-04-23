using System;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge; // Hoặc Chrome nếu bạn cài chromedriver.exe
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Text;

namespace TTVUploaderApp
{
    public class ChapterAnalysisResult
    {
        public List<string> FinalChapters { get; set; } = new List<string>();
        public int OriginalChapterCount { get; set; } = 0;
        public List<Tuple<string, int>> LongChaptersInfo { get; set; } = new List<Tuple<string, int>>();
        public List<Tuple<string, int>> ShortChaptersInfo { get; set; } = new List<Tuple<string, int>>();
        public int FinalChapterCount => FinalChapters.Count;
    }

    public partial class TTV : Form
    {
        private TextBox? txtNoiDung;
        private Button? btnChonFile;
        private Button? btnTachChuong;
        private ListBox? lstChuong;
        private Button? btnMoFormDang;
        private List<string> processedChapters = new List<string>();

        public TTV()
        {
            try
            {
                InitializeComponentManual();
                try { this.Icon = new Icon("TTVIcon.ico"); }
                catch (Exception ex) { Console.WriteLine($"Lỗi tải icon TTV: {ex.Message}"); }
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
                this.AutoScaleMode = AutoScaleMode.Dpi;

                int currentTop = 10, controlLeft = 10, controlWidth = this.ClientSize.Width - 20;
                if (controlWidth <= 0) controlWidth = 460;

                Label lblNoiDung = new Label() { Text = "Nội dung (từ file .txt):", Top = currentTop, Left = controlLeft, Width = controlWidth, AutoSize = true };
                currentTop += lblNoiDung.Height + 5;
                txtNoiDung = new TextBox() { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 250, Width = controlWidth, Top = currentTop, Left = controlLeft, ReadOnly = false, Font = new Font("Segoe UI", 9F) };
                currentTop += txtNoiDung.Height + 10;

                btnChonFile = new Button() { Text = "1. Chọn File TXT (.txt)...", Top = currentTop, Left = controlLeft, Width = controlWidth, Height = 30 };
                currentTop += btnChonFile.Height + 10;

                Label lblDanhSachChuong = new Label() { Text = "Chương đã tách (sẵn sàng để đăng):", Top = currentTop, Left = controlLeft, Width = controlWidth, AutoSize = true };
                currentTop += lblDanhSachChuong.Height + 5;
                lstChuong = new ListBox() { Width = controlWidth, Height = 150, Top = currentTop, Left = controlLeft, ScrollAlwaysVisible = true, IntegralHeight = false };
                currentTop += lstChuong.Height + 10;

                btnTachChuong = new Button() { Text = "2. Tách chương & Phân tích", Left = controlLeft, Top = currentTop, Width = controlWidth, Height = 30 };
                currentTop += btnTachChuong.Height + 15;

                btnMoFormDang = new Button() { Text = "3. Chuẩn bị Đăng chương...", Left = controlLeft, Top = currentTop, Width = controlWidth, Font = new Font(this.Font, FontStyle.Bold), BackColor = Color.LightGreen, Height = 35, Enabled = false };
                currentTop += btnMoFormDang.Height + 10;

                Controls.Add(lblNoiDung);
                if (txtNoiDung != null) Controls.Add(txtNoiDung);
                if (btnChonFile != null) Controls.Add(btnChonFile);
                Controls.Add(lblDanhSachChuong);
                if (lstChuong != null) Controls.Add(lstChuong);
                if (btnTachChuong != null) Controls.Add(btnTachChuong);
                if (btnMoFormDang != null) Controls.Add(btnMoFormDang);

                if (btnChonFile != null) btnChonFile.Click += BtnChonFile_Click;
                if (btnTachChuong != null) btnTachChuong.Click += BtnTachChuong_Click;
                if (btnMoFormDang != null) btnMoFormDang.Click += BtnMoFormDang_Click;

                this.MinimumSize = new Size(400, 500);
                this.Resize += (s, e) => {
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
                        string fileContent = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                        if (txtNoiDung != null) txtNoiDung.Text = fileContent;
                        if (lstChuong != null) lstChuong.Items.Clear();
                        processedChapters.Clear();
                        if (btnMoFormDang != null) btnMoFormDang.Enabled = false;
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
            if (txtNoiDung == null || string.IsNullOrWhiteSpace(txtNoiDung.Text))
            {
                MessageBox.Show("Nội dung đang trống.", "Thiếu nội dung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetMainFormControlsEnabled(false);
            this.Cursor = Cursors.WaitCursor;
            this.Refresh();

            try
            {
                Task.Run(() =>
                {
                    ChapterAnalysisResult analysisResult;
                    try
                    {
                        analysisResult = TachChuong(txtNoiDung.Text);
                        processedChapters = analysisResult.FinalChapters;
                    }
                    catch (Exception ex)
                    {
                        this.Invoke(new Action(() =>
                        {
                            MessageBox.Show($"Lỗi nghiêm trọng trong quá trình tách chương:\n{ex.Message}\n\n{ex.StackTrace}", "Lỗi Tách Chương", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            SetMainFormControlsEnabled(true);
                            this.Cursor = Cursors.Default;
                        }));
                        return;
                    }

                    this.Invoke(new Action(() =>
                    {
                        try
                        {
                            if (analysisResult.FinalChapterCount > 0)
                            {
                                if (lstChuong != null)
                                {
                                    lstChuong.BeginUpdate();
                                    lstChuong.Items.Clear();
                                    foreach (string chuong in analysisResult.FinalChapters)
                                    {
                                        string displayTitle = chuong.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Chương không tên";
                                        lstChuong.Items.Add(displayTitle.Substring(0, Math.Min(80, displayTitle.Length)) + (displayTitle.Length > 80 ? "..." : ""));
                                    }
                                    lstChuong.EndUpdate();
                                }

                                var summary = new System.Text.StringBuilder();
                                summary.AppendLine($"Tách hoàn tất!");
                                summary.AppendLine($"------------------------------------");
                                summary.AppendLine($"Tổng số chương/phần cuối cùng: {analysisResult.FinalChapterCount}");
                                summary.AppendLine($"Số chương gốc ban đầu: {analysisResult.OriginalChapterCount}");
                                summary.AppendLine($"------------------------------------");

                                if (analysisResult.LongChaptersInfo.Count > 0)
                                {
                                    summary.AppendLine($"Có {analysisResult.LongChaptersInfo.Count} chương gốc dài hơn 38000 ký tự đã được chia:");
                                    foreach (var longInfo in analysisResult.LongChaptersInfo)
                                    {
                                        string originalTitle = longInfo.Item1.Substring(0, Math.Min(60, longInfo.Item1.Length)) + (longInfo.Item1.Length > 60 ? "..." : "");
                                        summary.AppendLine($"- {originalTitle}: chia thành {longInfo.Item2} phần.");
                                    }
                                    summary.AppendLine($"------------------------------------");
                                }
                                else
                                {
                                    summary.AppendLine("Không có chương gốc nào dài hơn 38000 ký tự.");
                                    summary.AppendLine($"------------------------------------");
                                }

                                if (analysisResult.ShortChaptersInfo.Count > 0)
                                {
                                    summary.AppendLine($"Có {analysisResult.ShortChaptersInfo.Count} chương/phần có nội dung dưới 3000 ký tự:");
                                    foreach (var shortInfo in analysisResult.ShortChaptersInfo)
                                    {
                                        string finalTitle = shortInfo.Item1.Substring(0, Math.Min(60, shortInfo.Item1.Length)) + (shortInfo.Item1.Length > 60 ? "..." : "");
                                        summary.AppendLine($"- {finalTitle}: {shortInfo.Item2} ký tự.");
                                    }
                                    summary.AppendLine($"------------------------------------");
                                }
                                else
                                {
                                    summary.AppendLine("Không có chương/phần nào dưới 3000 ký tự.");
                                    summary.AppendLine($"------------------------------------");
                                }

                                MessageBox.Show(summary.ToString(), "Kết Quả Tách Chương Chi Tiết", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                            }
                        }
                        catch (Exception uiEx)
                        {
                            MessageBox.Show($"Lỗi khi cập nhật giao diện sau khi tách chương:\n{uiEx.Message}", "Lỗi UI Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        finally
                        {
                            SetMainFormControlsEnabled(true);
                            this.Cursor = Cursors.Default;
                        }
                    }));
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi bắt đầu tiến trình tách chương: {ex.Message}", "Lỗi Khởi Tạo Task", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetMainFormControlsEnabled(true);
                this.Cursor = Cursors.Default;
            }
        }

        private ChapterAnalysisResult TachChuong(string text)
        {
            List<string> initialChapters = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return new ChapterAnalysisResult();

            var chapterTitleRegex = new Regex(@"^\s*[Cc]hương\s+(\d+)\s*:?\s*(.*)$", RegexOptions.IgnoreCase);
            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            List<string> currentChapterLines = new List<string>();
            bool isInsideChapter = false;
            Match? previousTitleMatch = null;
            bool chapterHadContent = false;


            Action finalizePreviousChapter = () => {
                if (currentChapterLines.Count > 0 && isInsideChapter)
                {
                    string firstLine = currentChapterLines[0];
                    Match originalLineMatch = chapterTitleRegex.Match(firstLine);

                    if (originalLineMatch.Success && originalLineMatch.Groups.Count > 2)
                    {
                        string titleTextGroupValue = originalLineMatch.Groups[2].Value; // Lấy cả khoảng trắng đầu/cuối nếu có


                        int firstLetterIndex = -1;
                        char firstLetter = '\0';
                        for (int k = 0; k < titleTextGroupValue.Length; k++)
                        {
                            if (!char.IsWhiteSpace(titleTextGroupValue[k]))
                            {
                                firstLetterIndex = k;
                                firstLetter = titleTextGroupValue[k];
                                break;
                            }
                        }

                        if (firstLetterIndex != -1 && char.IsLower(firstLetter))
                        {
                            char[] titleChars = titleTextGroupValue.ToCharArray();
                            titleChars[firstLetterIndex] = char.ToUpper(firstLetter);
                            string capitalizedTitleText = new string(titleChars);

                            string prefixPart = firstLine.Substring(0, originalLineMatch.Groups[2].Index);
                            string newFirstLine = prefixPart + capitalizedTitleText;
                            currentChapterLines[0] = newFirstLine;
                        }
                    }
                    initialChapters.Add(string.Join(Environment.NewLine, currentChapterLines).Trim());
                }
            };


            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();
                Match currentTitleMatch = chapterTitleRegex.Match(trimmedLine);

                if (currentTitleMatch.Success)
                {
                    int currentNum = 0;
                    if (!int.TryParse(currentTitleMatch.Groups[1].Value, out currentNum))
                    {
                        if (isInsideChapter) { currentChapterLines.Add(line); chapterHadContent = true; }
                        continue;
                    }

                    string currentTitleText = currentTitleMatch.Groups[2].Value.Trim();
                    bool currentHasText = !string.IsNullOrWhiteSpace(currentTitleText);
                    bool isNewChapter = true; // Giả định là chương mới ban đầu

                    if (previousTitleMatch != null)
                    {
                        int previousNum = 0;
                        int.TryParse(previousTitleMatch.Groups[1].Value, out previousNum);
                        string previousTitleText = previousTitleMatch.Groups[2].Value.Trim();
                        bool previousHasText = !string.IsNullOrWhiteSpace(previousTitleText);

                        if (!chapterHadContent && currentNum == previousNum + 1)
                        {
                            isNewChapter = true;
                        }
                        else if (currentNum == previousNum)
                        {
                            if (currentHasText && !previousHasText && currentChapterLines.Count > 0)
                            {
                                currentChapterLines[0] = line;
                                previousTitleMatch = chapterTitleRegex.Match(line);

                            }
                            isNewChapter = false; 
                        }
                        else if (currentHasText && previousHasText
                                 && currentTitleText.Equals(previousTitleText, StringComparison.OrdinalIgnoreCase)
                                 && chapterHadContent)
                        {
                            currentChapterLines.Add(line);
                            chapterHadContent = true;
                            isNewChapter = false;
                        }
                        else
                        {
                            isNewChapter = true;
                        }
                    }
                    else
                    {
                        isNewChapter = true;
                    }

                    if (isNewChapter)
                    {
                        finalizePreviousChapter();
                        currentChapterLines = new List<string> { line };
                        chapterHadContent = false;
                        isInsideChapter = true;
                        previousTitleMatch = chapterTitleRegex.Match(line);
                    }
                }
                else 
                {
                    if (isInsideChapter)
                    {
                        // if(!string.IsNullOrWhiteSpace(line))
                        // {
                        currentChapterLines.Add(line);
                        if (currentChapterLines.Count > 1) // Chỉ đánh dấu khi có ít nhất 1 dòng sau dòng tiêu đề
                        {
                            chapterHadContent = true;
                        }
                        // }
                    }
                }
            }

            finalizePreviousChapter();

            List<string> finalInitialChapters = new List<string>();
            foreach (string chapter in initialChapters) // Dùng initialChapters đã qua bước viết hoa
            {
                if (string.IsNullOrWhiteSpace(chapter)) continue;

                string[] chapterLines = chapter.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                string firstLine = chapterLines[0];
                Match firstLineMatch = chapterTitleRegex.Match(firstLine.Trim()); // Match để kiểm tra

                if (firstLineMatch.Success)
                {
                    string titleTextPart = firstLineMatch.Groups.Count > 2 ? firstLineMatch.Groups[2].Value.Trim() : "";
                    // Kiểm tra xem có tên chương không VÀ chưa phải là "Vô đề" được thêm tự động
                    if (string.IsNullOrWhiteSpace(titleTextPart) && !firstLine.Contains(": Vô đề"))
                    {

                        string chapterTitleLinePart = firstLine.Split(':')[0].TrimEnd();

                        Match numPartMatch = Regex.Match(firstLine, @"^\s*[Cc]hương\s+\d+");
                        if (numPartMatch.Success)
                            chapterTitleLinePart = numPartMatch.Value;
                        else
                            chapterTitleLinePart = firstLine.TrimEnd(); // Fallback

                        string modifiedChapter = chapterTitleLinePart + ": Vô đề"; // Thêm tiêu đề mặc định
                        if (chapterLines.Length > 1)
                        {
                            modifiedChapter += Environment.NewLine + string.Join(Environment.NewLine, chapterLines.Skip(1));
                        }
                        finalInitialChapters.Add(modifiedChapter);
                    }
                    else
                    {
                        finalInitialChapters.Add(chapter); // Giữ nguyên chương đã có tiêu đề (và đã viết hoa nếu cần)
                    }
                }
                else
                {
                    finalInitialChapters.Add(chapter); // Không khớp định dạng tiêu đề
                }
            }


            if (finalInitialChapters.Count == 0)
            {
                MessageBox.Show("Không thể tách được chương nào từ nội dung đã cung cấp.", "Không Tách Được Chương", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return new ChapterAnalysisResult();
            }

            return ProcessLongChapters(finalInitialChapters, 38000, 3000);
        }

        private ChapterAnalysisResult ProcessLongChapters(List<string> originalChapters, int maxCharCount, int minCharCount)
        {
            ChapterAnalysisResult result = new ChapterAnalysisResult();
            result.OriginalChapterCount = originalChapters.Count;

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
                    result.LongChaptersInfo.Add(Tuple.Create(title, parts));

                    int currentPos = 0;
                    for (int part = 1; part <= parts; part++)
                    {
                        int idealEndPos = Math.Min(currentPos + maxCharCount, content.Length);
                        int endPos = idealEndPos;

                        if (part < parts && endPos < content.Length)
                        {
                            int searchStart = Math.Max(currentPos, idealEndPos - 500);
                            int breakPos = -1;
                            int searchLength = Math.Max(0, idealEndPos - searchStart);
                            int paraBreakPos = content.LastIndexOfAny(paragraphEndings, idealEndPos - 1, searchLength);

                            if (paraBreakPos > currentPos)
                            {
                                breakPos = paraBreakPos + 1;
                                while (breakPos < content.Length && char.IsWhiteSpace(content[breakPos])) breakPos++;
                            }
                            else
                            {
                                int sentenceBreakPos = content.LastIndexOfAny(sentenceEndings, idealEndPos - 1, searchLength);
                                if (sentenceBreakPos > currentPos && (sentenceBreakPos + 1 >= content.Length || char.IsWhiteSpace(content[sentenceBreakPos + 1])))
                                {
                                    breakPos = sentenceBreakPos + 1;
                                    while (breakPos < content.Length && char.IsWhiteSpace(content[breakPos])) breakPos++;
                                }
                            }

                            if (breakPos > currentPos && (content.Length - breakPos) > maxCharCount / 10)
                            {
                                endPos = breakPos;
                            }
                            else
                            {
                                endPos = idealEndPos;
                            }
                        }

                        endPos = Math.Min(endPos, content.Length);
                        if (endPos <= currentPos && currentPos < content.Length)
                        {
                            endPos = content.Length;
                        }

                        string partContent = content.Substring(currentPos, endPos - currentPos).Trim();
                        if (!string.IsNullOrWhiteSpace(partContent))
                        {
                            string newTitle = $"{title} (Phần {part}/{parts})";
                            result.FinalChapters.Add(newTitle + Environment.NewLine + partContent);
                        }
                        currentPos = endPos;
                        if (currentPos >= content.Length && part < parts)
                        {
                            Console.WriteLine($"Cảnh báo: Chia chương '{title}' có thể bị dừng sớm ở phần {part}/{parts}. Vui lòng kiểm tra nội dung.");
                            break;
                        }
                    }
                }
                else
                {
                    result.FinalChapters.Add(chapter);
                }
            }

            foreach (string finalChapter in result.FinalChapters)
            {
                string[] finalParts = finalChapter.Split(new char[] { '\n' }, 2);
                string finalTitle = (finalParts.Length > 0) ? finalParts[0].Trim() : "Chương/Phần không tên";
                string finalContent = (finalParts.Length > 1) ? finalParts[1].Trim() : "";
                int finalContentLength = finalContent.Length;

                if (finalContentLength < minCharCount)
                {
                    result.ShortChaptersInfo.Add(Tuple.Create(finalTitle, finalContentLength));
                }
            }
            return result;
        }

        private void SetMainFormControlsEnabled(bool enabled)
        {
            if (this.InvokeRequired)
            {
                try { this.Invoke(new Action<bool>(SetMainFormControlsEnabled), enabled); }
                catch (ObjectDisposedException) { }
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
                MessageBox.Show("Chưa có nội dung chương nào được tách hoặc danh sách trống. Vui lòng chọn file và tách chương trước.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            FormAutomation formDang = new FormAutomation(this, new List<string>(processedChapters));
            formDang.Show(this);
        }

        public void RemovePostedChapters(int count)
        {
            if (this.InvokeRequired)
            {
                try { this.Invoke(new Action<int>(RemovePostedChapters), count); }
                catch (ObjectDisposedException) { }
                return;
            }

            try
            {
                if (lstChuong != null && count > 0)
                {
                    int itemsToRemove = Math.Min(count, lstChuong.Items.Count);
                    lstChuong.BeginUpdate();
                    for (int i = 0; i < itemsToRemove; i++)
                    {
                        if (lstChuong.Items.Count > 0) lstChuong.Items.RemoveAt(0);
                    }
                    lstChuong.EndUpdate();
                }

                if (processedChapters != null && count > 0)
                {
                    int itemsToRemove = Math.Min(count, processedChapters.Count);
                    if (itemsToRemove > 0)
                    {
                        processedChapters.RemoveRange(0, itemsToRemove);
                        Console.WriteLine($"Đã xóa {itemsToRemove} chương khỏi danh sách processedChapters. Còn lại: {processedChapters.Count}");
                    }
                }
                if (btnMoFormDang != null) btnMoFormDang.Enabled = (processedChapters != null && processedChapters.Count > 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật danh sách chương sau khi đăng: {ex.Message}", "Lỗi Cập Nhật UI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TTV());
        }
    }

    public partial class FormAutomation : Form
    {
        private CheckBox? chkGioiHanSoChuong; private NumericUpDown? numSoChuong; private Button? btnDangChuong; private Button? btnNhapThongTinDangNhap; private Label? lblStatus; private ProgressBar? progressBar;
        private List<string> chaptersToUpload; private TTV? mainAppForm; private IWebDriver? driver; private string storyUploadUrl = ""; private string username = ""; private string password = "";

        public FormAutomation(TTV owner, List<string> chapters)
        {
            mainAppForm = owner;
            chaptersToUpload = chapters ?? new List<string>();
            try
            {
                this.Text = "Đăng truyện TTV - Điều khiển"; this.Size = new Size(450, 400); this.StartPosition = FormStartPosition.CenterParent; this.FormBorderStyle = FormBorderStyle.FixedSingle;
                this.Load += FormAutomation_Load;
                this.FormClosing += FormAutomation_FormClosing;
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

                lblStatus = new Label() { Text = "Trạng thái: Chờ cấu hình...", Top = currentTop, Left = controlLeft, Width = controlWidth, Height = 60, BorderStyle = BorderStyle.Fixed3D, AutoSize = false }; currentTop += lblStatus.Height + 10;

                progressBar = new ProgressBar() { Minimum = 0, Maximum = 100, Value = 0, Top = currentTop, Left = controlLeft, Width = controlWidth, Height = 23, Style = ProgressBarStyle.Blocks }; currentTop += progressBar.Height + 10;

                if (btnNhapThongTinDangNhap != null) Controls.Add(btnNhapThongTinDangNhap);
                if (chkGioiHanSoChuong != null) Controls.Add(chkGioiHanSoChuong);
                if (numSoChuong != null) Controls.Add(numSoChuong);
                if (btnDangChuong != null) Controls.Add(btnDangChuong);
                if (lblStatus != null) Controls.Add(lblStatus);
                if (progressBar != null) Controls.Add(progressBar);

                if (btnNhapThongTinDangNhap != null) btnNhapThongTinDangNhap.Click += BtnNhapThongTinDangNhap_Click;
                if (btnDangChuong != null) btnDangChuong.Click += BtnDangChuong_Click;

                if (lblStatus != null) UpdateStatus($"Sẵn sàng đăng {chaptersToUpload.Count} chương (lô 5).");
                if (progressBar != null)
                {
                    progressBar.Maximum = chaptersToUpload.Count > 0 ? chaptersToUpload.Count : 1;
                    progressBar.Value = 0;
                }
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi tạo UI FormAutomation: {ex.Message}\n{ex.StackTrace}", "Lỗi UI Load", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void UpdateStatus(string message)
        {
            if (lblStatus == null) return;
            if (lblStatus.InvokeRequired)
            {
                try { lblStatus.Invoke(new Action<string>(UpdateStatus), message); }
                catch (ObjectDisposedException) { }
            }
            else lblStatus.Text = $"Trạng thái: {message}";
        }

        private void UpdateProgress(int value)
        {
            if (progressBar == null) return;
            if (progressBar.InvokeRequired)
            {
                try { progressBar.Invoke(new Action<int>(UpdateProgress), value); }
                catch (ObjectDisposedException) { }
            }
            else
            {
                if (progressBar.Maximum <= progressBar.Minimum) progressBar.Maximum = value + 1;
                if (value < progressBar.Minimum) value = progressBar.Minimum;
                if (value > progressBar.Maximum) value = progressBar.Maximum;
                progressBar.Value = value;
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            if (this.InvokeRequired)
            {
                try { this.Invoke(new Action<bool>(SetControlsEnabled), enabled); }
                catch (ObjectDisposedException) { }
            }
            else
            {
                if (btnDangChuong != null) btnDangChuong.Enabled = enabled;
                if (btnNhapThongTinDangNhap != null) btnNhapThongTinDangNhap.Enabled = enabled;
            }
        }

        private void BtnNhapThongTinDangNhap_Click(object? sender, EventArgs e)
        {
            using (FormLogin formLogin = new FormLogin(this, this.storyUploadUrl, this.username, this.password))
            {
                formLogin.ShowDialog(this);
            }
        }

        public void SetLoginInfo(string url, string user, string pass)
        {
            storyUploadUrl = url;
            username = user;
            password = pass;
            if (!string.IsNullOrWhiteSpace(url))
            {
                UpdateStatus($"Đã cập nhật URL. Sẵn sàng đăng {chaptersToUpload.Count} chương.");
            }
            else
            {
                UpdateStatus($"URL đăng chương chưa được nhập.");
            }
        }

        private async void BtnDangChuong_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(storyUploadUrl) || !Uri.TryCreate(storyUploadUrl, UriKind.Absolute, out _))
            {
                MessageBox.Show("Vui lòng nhập URL trang đăng chương hợp lệ của truyện trước.", "Thiếu thông tin URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                BtnNhapThongTinDangNhap_Click(sender, e);
                return;
            }
            if (chaptersToUpload == null || chaptersToUpload.Count == 0)
            {
                MessageBox.Show("Không còn chương nào trong danh sách để đăng!", "Danh sách trống", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetControlsEnabled(false);
            UpdateStatus("Đang khởi động trình duyệt Edge...");
            if (progressBar != null)
            {
                progressBar.Maximum = chaptersToUpload.Count;
                UpdateProgress(0);
            }

            List<string> remainingChapters = new List<string>(chaptersToUpload);
            int totalChaptersAtStart = remainingChapters.Count;
            int totalChaptersPosted = 0;
            bool errorOccurred = false;

            await Task.Run(async () => {
                EdgeDriverService? service = null;
                try
                {
                    var options = new EdgeOptions();
                    options.AddArgument("--disable-gpu");
                    options.AddArgument("--log-level=3");
                    options.AddArgument("--disable-dev-shm-usage");
                    options.AddArgument("--no-sandbox");
                    options.AddArgument("--disable-extensions");
                    options.AddArgument("--start-maximized");

                    service = EdgeDriverService.CreateDefaultService();
                    service.HideCommandPromptWindow = true;

                    using (driver = new EdgeDriver(service, options))
                    {
                        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(90);

                        var standardWait = new WebDriverWait(driver, TimeSpan.FromSeconds(25));
                        var pageLoadWait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
                        var submitWait = new WebDriverWait(driver, TimeSpan.FromSeconds(90));

                        // 1. Mở trang chủ trước
                        string homePageUrl = "https://tangthuvien.net/";
                        UpdateStatus("Đang mở trang chủ TTV (.net)...");
                        try
                        {
                            driver.Navigate().GoToUrl(homePageUrl);
                            pageLoadWait.Until(ExpectedConditions.ElementIsVisible(By.TagName("body")));
                        }
                        catch (WebDriverTimeoutException pageEx) { UpdateStatus($"Lỗi: Timeout ({driver.Manage().Timeouts().PageLoad.TotalSeconds}s) khi tải trang chủ TTV."); MessageBox.Show($"Không thể tải trang chủ TTV sau {driver.Manage().Timeouts().PageLoad.TotalSeconds} giây:\n{homePageUrl}\nLỗi: {pageEx.Message}", "Lỗi Tải Trang Chủ", MessageBoxButtons.OK, MessageBoxIcon.Error); errorOccurred = true; return; }
                        catch (WebDriverException driverEx) { UpdateStatus($"Lỗi WebDriver khi mở trang chủ: {driverEx.Message}"); MessageBox.Show($"Đã xảy ra lỗi khi mở trang chủ TTV:\n{homePageUrl}\nLỗi: {driverEx.Message}", "Lỗi Mở Trang Chủ", MessageBoxButtons.OK, MessageBoxIcon.Error); errorOccurred = true; return; }

                        UpdateStatus("VUI LÒNG ĐĂNG NHẬP vào TTV trong cửa sổ trình duyệt (nếu chưa)...");
                        MessageBox.Show("Trình duyệt Edge đã mở trang chủ TTV...\n\nVui lòng đăng nhập vào tài khoản TTV của bạn.\n\nSau khi đăng nhập xong, ứng dụng sẽ tự động tiếp tục khi bạn nhấn OK trên hộp thoại này.", "Yêu cầu Đăng nhập Thủ công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        UpdateStatus("Đang chờ xác nhận từ bạn...");

                        UpdateStatus($"Đang chuyển đến trang đăng chương: {storyUploadUrl}...");
                        string firstChapterNameSelector = "input[name='chap_name[1]']";
                        try
                        {
                            driver.Navigate().GoToUrl(storyUploadUrl);
                            UpdateStatus("Đang xác nhận trang đăng chương (chờ element)...");
                            standardWait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(firstChapterNameSelector)));
                            UpdateStatus("Truy cập trang đăng chương thành công! Bắt đầu đăng...");
                            await Task.Delay(500);
                        }
                        catch (WebDriverTimeoutException timeEx) { if (driver.Url.Contains(storyUploadUrl)) { UpdateStatus($"Lỗi: Timeout ({standardWait.Timeout.TotalSeconds}s) khi chờ element '{firstChapterNameSelector}' trên trang đăng chương."); MessageBox.Show($"Không thể tìm thấy phần tử '{firstChapterNameSelector}' trên trang đăng chương sau {standardWait.Timeout.TotalSeconds} giây:\n{storyUploadUrl}\n\nLý do có thể:\n- Đăng nhập chưa thành công / Session hết hạn khi chuyển trang.\n- URL đúng nhưng trang tải lỗi hoặc cấu trúc thay đổi.\n\nChi tiết lỗi: {timeEx.Message}", "Lỗi Tìm Phần Tử Trang Đăng Chương", MessageBoxButtons.OK, MessageBoxIcon.Error); } else { UpdateStatus($"Lỗi: Timeout ({driver.Manage().Timeouts().PageLoad.TotalSeconds}s) khi tải trang đăng chương."); MessageBox.Show($"Không thể tải hoàn tất trang đăng chương sau {driver.Manage().Timeouts().PageLoad.TotalSeconds} giây:\n{storyUploadUrl}\nLý do có thể:\n- URL sai.\n- Lỗi mạng / Server TTV.\n- Cần đăng nhập lại nhưng bị lỗi.\n\nChi tiết lỗi: {timeEx.Message}", "Lỗi Tải Trang Đăng Chương", MessageBoxButtons.OK, MessageBoxIcon.Error); } errorOccurred = true; return; }
                        catch (WebDriverException driverEx) { UpdateStatus($"Lỗi WebDriver khi truy cập trang đăng chương: {driverEx.Message}"); MessageBox.Show($"Lỗi khi truy cập trang đăng chương:\n{storyUploadUrl}\n\nChi tiết lỗi: {driverEx.Message}", "Lỗi Truy Cập Trang Đăng Chương", MessageBoxButtons.OK, MessageBoxIcon.Error); errorOccurred = true; return; }
                        catch (Exception ex) { UpdateStatus("Lỗi không xác định khi truy cập trang đăng chương."); MessageBox.Show($"Đã xảy ra lỗi không mong muốn khi truy cập trang đăng chương:\n{storyUploadUrl}\n\nChi tiết lỗi: {ex.Message}", "Lỗi Không Xác Định", MessageBoxButtons.OK, MessageBoxIcon.Error); errorOccurred = true; return; }

                        while (remainingChapters.Count > 0 && !errorOccurred)
                        {
                            int batchSize = Math.Min(5, remainingChapters.Count);
                            int currentBatchNumber = (totalChaptersPosted / 5) + 1;
                            UpdateStatus($"Chuẩn bị đăng lô {currentBatchNumber} ({batchSize} chương). Còn lại: {remainingChapters.Count}...");
                            await Task.Delay(500);

                            for (int i = 0; i < batchSize; i++)
                            {
                                int formIndex = i + 1;
                                string currentChapterData = remainingChapters[i];
                                string[] parts = currentChapterData.Split(new char[] { '\n' }, 2);
                                string originalTitleLine = parts.Length > 0 ? parts[0].Trim() : "";
                                string content = parts.Length > 1 ? parts[1].Trim() : "";
                                string chuongSo = ExtractChapterNumber(originalTitleLine);
                                string tenChuongToSend = ExtractChapterTitleOnly(originalTitleLine);
                                string quyenSo = "1";

                                UpdateStatus($"Đang điền chương {totalChaptersPosted + i + 1}/{totalChaptersAtStart}: {originalTitleLine.Substring(0, Math.Min(60, originalTitleLine.Length))}...");

                                try
                                {
                                    standardWait.Until(ExpectedConditions.ElementToBeClickable(By.Name($"chap_number[{formIndex}]"))).ClearSendKeys(chuongSo);
                                    await Task.Delay(150);
                                    standardWait.Until(ExpectedConditions.ElementToBeClickable(By.Name($"vol[{formIndex}]"))).ClearSendKeys(quyenSo);
                                    await Task.Delay(150);
                                    standardWait.Until(ExpectedConditions.ElementToBeClickable(By.Name($"chap_name[{formIndex}]"))).ClearSendKeys(tenChuongToSend);
                                    await Task.Delay(150);
                                    var noiDungElement = standardWait.Until(ExpectedConditions.ElementIsVisible(By.Name($"introduce[{formIndex}]")));
                                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].value = '';", noiDungElement);
                                    await Task.Delay(100);
                                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input')); arguments[0].dispatchEvent(new Event('change'));", noiDungElement, content);
                                    await Task.Delay(250);

                                    if (i < batchSize - 1 && formIndex < 5)
                                    {
                                        try
                                        {
                                            UpdateStatus($"Thêm form cho chương tiếp theo (index {formIndex + 1})...");
                                            var addBtn = standardWait.Until(ExpectedConditions.ElementToBeClickable(By.Id("add-chap")));
                                            addBtn.Click();
                                            string nextCheckSelector = $"input[name='chap_name[{formIndex + 1}]']";
                                            standardWait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(nextCheckSelector)));
                                            UpdateStatus($"Đã thêm form index {formIndex + 1}.");
                                            await Task.Delay(300);
                                        }
                                        catch (Exception addEx) { UpdateStatus($"Lỗi khi nhấn nút 'Thêm chương' (ID: add-chap): {addEx.Message}"); MessageBox.Show($"Lỗi khi click nút 'Thêm chương' (ID: add-chap) sau khi điền form index {formIndex}.\nKiểm tra lại ID của nút hoặc cấu trúc trang.\n\nLỗi: {addEx.Message}", "Lỗi Thêm Form", MessageBoxButtons.OK, MessageBoxIcon.Error); errorOccurred = true; break; }
                                    }
                                }
                                catch (Exception fillEx) { UpdateStatus($"Lỗi điền thông tin chương {totalChaptersPosted + i + 1}: {fillEx.Message}"); MessageBox.Show($"Lỗi khi điền thông tin cho chương '{originalTitleLine}' (form index {formIndex}).\nKiểm tra lại các selector `name` hoặc cấu trúc trang.\n\nLỗi: {fillEx.Message}", "Lỗi Điền Thông Tin", MessageBoxButtons.OK, MessageBoxIcon.Error); errorOccurred = true; break; }
                            }

                            if (errorOccurred) break;

                            UpdateStatus($"Đang gửi {batchSize} chương (Lô {currentBatchNumber}). Vui lòng chờ...");
                            try
                            {
                                var submitButton = standardWait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("form button[type='submit']")));
                                submitButton.Click();

                                UpdateStatus("Đang chờ xác nhận sau khi gửi...");
                                string themChuongLinkSelector = "a.btn-addchap";
                                try
                                {
                                    submitWait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(themChuongLinkSelector)));
                                    UpdateStatus($"Đã gửi lô {currentBatchNumber} thành công. Chuẩn bị cho lô tiếp theo...");
                                    await Task.Delay(1000);
                                    var themChuongLink = submitWait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector(themChuongLinkSelector)));
                                    themChuongLink.Click();
                                    UpdateStatus("Đang chờ trang đăng chương tải lại...");
                                    standardWait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(firstChapterNameSelector)));
                                    UpdateStatus("Sẵn sàng cho lô tiếp theo.");
                                }
                                catch (Exception waitLinkEx) { UpdateStatus($"Lỗi chờ/nhấn link '{themChuongLinkSelector}' sau khi submit: {waitLinkEx.Message}."); MessageBox.Show($"Đã gửi lô {currentBatchNumber} nhưng không thể tự động quay lại trang thêm chương mới (không tìm thấy link '{themChuongLinkSelector}').\nTiến trình sẽ dừng lại.\n\nLỗi: {waitLinkEx.Message}", "Lỗi Sau Submit", MessageBoxButtons.OK, MessageBoxIcon.Warning); errorOccurred = true; if (mainAppForm != null) mainAppForm.RemovePostedChapters(batchSize); totalChaptersPosted += batchSize; UpdateProgress(totalChaptersPosted); remainingChapters.RemoveRange(0, batchSize); break; }

                                if (mainAppForm != null) mainAppForm.RemovePostedChapters(batchSize);
                                totalChaptersPosted += batchSize;
                                UpdateProgress(totalChaptersPosted);
                                remainingChapters.RemoveRange(0, batchSize);
                                if (remainingChapters.Count > 0) { UpdateStatus($"Chờ 1.5 giây trước khi bắt đầu lô tiếp theo..."); await Task.Delay(1500); }
                            }
                            catch (Exception submitEx) { UpdateStatus($"Lỗi khi gửi lô {currentBatchNumber}: {submitEx.Message}"); MessageBox.Show($"Lỗi khi nhấn nút Submit cho lô {currentBatchNumber}.\nKiểm tra lại selector của nút Submit hoặc lỗi từ phía server.\n\nLỗi: {submitEx.Message}", "Lỗi Submit", MessageBoxButtons.OK, MessageBoxIcon.Error); errorOccurred = true; break; }
                        }

                        if (!errorOccurred)
                        {
                            UpdateStatus($"Hoàn tất! Đã đăng thành công {totalChaptersPosted}/{totalChaptersAtStart} chương.");
                            MessageBox.Show($"Đã đăng thành công {totalChaptersPosted} chương!", "Hoàn Thành", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            UpdateStatus($"Đã dừng do lỗi. {totalChaptersPosted}/{totalChaptersAtStart} chương đã được xử lý trước khi lỗi.");
                        }
                    } 
                    driver = null;
                }
                catch (Exception ex)
                {
                    errorOccurred = true;
                    if (ex.Message.Contains("driver executable file does not exist") || ex.Message.ToLower().Contains("msedgedriver")) { UpdateStatus("Lỗi: Không tìm thấy msedgedriver.exe."); MessageBox.Show("Lỗi khởi tạo trình duyệt Edge:\nKhông tìm thấy tệp 'msedgedriver.exe'.\n\nVui lòng tải phiên bản msedgedriver phù hợp với trình duyệt Edge của bạn và đặt nó vào cùng thư mục với ứng dụng hoặc trong một thư mục thuộc biến môi trường PATH.", "Lỗi Driver", MessageBoxButtons.OK, MessageBoxIcon.Error); } else if (ex is WebDriverException && ex.Message.ToLower().Contains("session not created")) { UpdateStatus("Lỗi: Không thể tạo phiên WebDriver."); MessageBox.Show($"Lỗi khởi tạo trình duyệt Edge:\nKhông thể tạo phiên làm việc WebDriver.\nLý do có thể:\n- Phiên bản msedgedriver không tương thích với trình duyệt Edge hiện tại.\n- Có lỗi với cài đặt trình duyệt Edge.\n\nChi tiết: {ex.Message}", "Lỗi Khởi Tạo Session", MessageBoxButtons.OK, MessageBoxIcon.Error); } else { UpdateStatus($"Lỗi không mong muốn trong quá trình tự động hóa: {ex.Message}"); MessageBox.Show($"Đã xảy ra lỗi không mong muốn:\n{ex.Message}\n\n{ex.StackTrace}", "Lỗi Nghiêm Trọng", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
                finally
                {
                    if (driver != null) { try { driver.Quit(); } catch { } driver = null; }
                    if (service != null) { try { service.Dispose(); } catch { } }
                    SetControlsEnabled(true);
                    string finalStatus = errorOccurred ? $"Đã dừng do lỗi. {totalChaptersPosted}/{totalChaptersAtStart} chương đã xử lý." : $"Hoàn tất. {totalChaptersPosted}/{totalChaptersAtStart} chương đã xử lý.";
                    UpdateStatus(finalStatus);
                    UpdateProgress(totalChaptersPosted);
                }
            });
        }

        private static string ExtractChapterNumber(string titleLine)
        {
            if (string.IsNullOrWhiteSpace(titleLine)) return "1";
            Match match = Regex.Match(titleLine.Trim(), @"^[Cc]hương\s+(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
            match = Regex.Match(titleLine.Trim(), @"\d+");
            if (match.Success)
            {
                return match.Value;
            }
            return "1";
        }

        private static string ExtractChapterTitleOnly(string titleLine)
        {
            if (string.IsNullOrWhiteSpace(titleLine)) return "";

            // Regex tìm tên chương sau "Chương X:" hoặc "Chương X "
            Match match = Regex.Match(titleLine.Trim(), @"^[Cc]hương\s+\d+\s*[:\s]\s*(.*)$", RegexOptions.IgnoreCase);

            if (match.Success && match.Groups.Count > 1 && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                string titlePart = match.Groups[1].Value.Trim();

                if (titlePart.Equals("Vô đề", StringComparison.OrdinalIgnoreCase))
                {
                    return "";
                }
                return titlePart;
            }
            Match voDeMatch = Regex.Match(titleLine.Trim(), @"^[Cc]hương\s+\d+\s*:\s*Vô\s+đề$", RegexOptions.IgnoreCase);
            if (voDeMatch.Success)
            {
                return "";
            }

            return "";
        }


        private void FormAutomation_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try { if (driver != null) { driver.Quit(); driver = null; } } catch (Exception ex) { Console.WriteLine($"Lỗi khi đóng WebDriver: {ex.Message}"); }
        }
    }

    public class FormLogin : Form
    {
        private TextBox? txtUrl;
        private TextBox? txtUsername;
        private TextBox? txtPassword;
        private Button? btnOK;
        private Button? btnCancel;
        private FormAutomation automationForm;

        public FormLogin(FormAutomation ownerForm, string currentUrl, string currentUser, string currentPass)
        {
            automationForm = ownerForm;
            try
            {
                InitializeComponentManual(currentUrl, currentUser, currentPass);
                try { this.Icon = new Icon("TTVIcon.ico"); } catch { }
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi constructor FormLogin: {ex.Message}", "Lỗi Khởi Tạo UI", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void InitializeComponentManual(string currentUrl, string currentUser, string currentPass)
        {
            try
            {
                this.Text = "Đăng truyện TTV - Nhập thông tin";
                this.Size = new Size(450, 220);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.ShowInTaskbar = false;

                int labelWidth = 150, controlLeft = labelWidth + 15, controlWidth = this.ClientSize.Width - controlLeft - 15, currentTop = 15;
                if (controlWidth <= 0) controlWidth = 250;

                Label lblUrl = new Label() { Text = "URL Trang Đăng Chương:", Top = currentTop + 3, Left = 10, Width = labelWidth, AutoSize = false };
                string displayUrl = string.IsNullOrWhiteSpace(currentUrl) ? "https://truyen.tangthuvien.vn/dang-chuong/story/..." : currentUrl;
                txtUrl = new TextBox() { Width = controlWidth, Top = currentTop, Left = controlLeft, Text = displayUrl };
                currentTop += txtUrl.Height + 10;

                Label lblUsername = new Label() { Text = "Tài khoản TTV (tùy chọn):", Top = currentTop + 3, Left = 10, Width = labelWidth, AutoSize = false };
                txtUsername = new TextBox() { Width = controlWidth, Top = currentTop, Left = controlLeft, Text = currentUser };
                currentTop += txtUsername.Height + 10;

                Label lblPassword = new Label() { Text = "Mật khẩu TTV (tùy chọn):", Top = currentTop + 3, Left = 10, Width = labelWidth, AutoSize = false };
                txtPassword = new TextBox() { Width = controlWidth, Top = currentTop, Left = controlLeft, PasswordChar = '*', Text = currentPass };
                currentTop += txtPassword.Height + 20;

                int buttonAreaWidth = 80 + 10 + 80;
                int buttonStartLeft = (this.ClientSize.Width - buttonAreaWidth) / 2;

                btnOK = new Button() { Text = "Lưu", Top = currentTop, Left = buttonStartLeft, Width = 80, DialogResult = DialogResult.OK };
                btnCancel = new Button() { Text = "Hủy", Top = currentTop, Left = btnOK.Right + 10, Width = 80, DialogResult = DialogResult.Cancel };

                this.AcceptButton = btnOK;
                this.CancelButton = btnCancel;

                Controls.Add(lblUrl); if (txtUrl != null) Controls.Add(txtUrl);
                Controls.Add(lblUsername); if (txtUsername != null) Controls.Add(txtUsername);
                Controls.Add(lblPassword); if (txtPassword != null) Controls.Add(txtPassword);
                Controls.Add(btnOK); Controls.Add(btnCancel);

                if (btnOK != null) btnOK.Click += BtnOK_Click;
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi tạo UI FormLogin: {ex.Message}\n{ex.StackTrace}", "Lỗi UI", MessageBoxButtons.OK, MessageBoxIcon.Error); throw; }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            try
            {
                if (txtUrl == null || string.IsNullOrWhiteSpace(txtUrl.Text))
                {
                    MessageBox.Show("Vui lòng nhập URL trang đăng chương.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUrl?.Focus();
                    this.DialogResult = DialogResult.None;
                    return;
                }
                string urlInput = txtUrl.Text.Trim();
                if (!Uri.TryCreate(urlInput, UriKind.Absolute, out Uri? uriResult) || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                {
                    MessageBox.Show("URL không hợp lệ. URL phải bắt đầu bằng http:// hoặc https://", "URL không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUrl?.Focus();
                    this.DialogResult = DialogResult.None;
                    return;
                }
                if (uriResult.Host.ToLower().Contains("tangthuvien") && uriResult.AbsolutePath.Equals("/", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Vui lòng nhập URL cụ thể của trang đăng chương truyện, không phải trang chủ.", "URL Không Cụ Thể", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUrl?.Focus();
                    this.DialogResult = DialogResult.None;
                    return;
                }

                string user = txtUsername?.Text.Trim() ?? "";
                string pass = txtPassword?.Text ?? "";

                automationForm.SetLoginInfo(urlInput, user, pass);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xử lý thông tin đã nhập: {ex.Message}", "Lỗi Lưu Thông Tin", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
            }
        }
    }
    public static class SeleniumExtensions
    {
        public static void ClearSendKeys(this IWebElement element, string text)
        {
            element.Clear();
            element.SendKeys(text);
        }
    }
}
