using BCrypt.Net;
using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace FinancyApplication
{
    public enum UserRole { User, Admin }

    public class User
    {
        protected Data data = new Data();
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVerified { get; set; } = false;
        public string ResetToken { get; set; }
        public DateTime CreatedAt { get; set; }

        public User() { }
        public User(string username, string email, string password)
        {
            if (data.EmailExists(email))
                throw new Exception("This email is already registered! Please login instead.");

            Username = username;
            Email = email;
            CreatedAt = DateTime.Now;

            if (data.GetUserCount() == 0)
                this.Role = UserRole.Admin;
            else
                this.Role = UserRole.User;

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            this.UserID = data.InsertUser(this, hashedPassword);

            UserProfile newProfile = new UserProfile
            {
                UserID = this.UserID,
                FirstName = "",
                LastName = "",
                PhoneNumber = "",
                AvatarUrl = "default_avatar.png",
                PreferredCurrency = "USD"
            };
            data.InsertProfile(newProfile);

            string code = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            data.UpdateResetToken(email, code);

            EmailService emailService = new EmailService();
            emailService.SendVerificationCode(email, username, code);
        }

        public bool Login(string email, string password)
        {
            try
            {
                if (data.ValidateLogin(email, password))
                {
                    User loggedInUser = data.GetUserByEmail(email);
                    Application.Current.Properties["CurrentUser"] = loggedInUser;
                    return true;
                }
                MessageBox.Show("Invalid credentials.");
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Login error: " + ex.Message);
                return false;
            }
        }

        public void Logout()
        {
            Application.Current.Properties["CurrentUser"] = null;
            MessageBox.Show("You have been logged out successfully.");
        }

        public void ResetPassword(string email, string recipientName)
        {
            try
            {
                string token = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
                data.UpdateResetToken(email, token);

                EmailService emailService = new EmailService();
                bool sent = emailService.SendResetToken(email, recipientName, token);

                if (sent)
                    MessageBox.Show("A reset code has been sent to " + email);
                else
                    MessageBox.Show("Could not send email. Please try again.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Reset password error: " + ex.Message);
            }
        }

        public void Deactivate()
        {
            try
            {
                this.IsActive = false;
                data.UpdateUserStatus(this.UserID, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Deactivate error: " + ex.Message);
            }
        }

        public void UpdateRole(User callingUser, UserRole newRole)
        {
            if (callingUser.Role != UserRole.Admin)
            {
                MessageBox.Show("Access denied. Only admins can change user roles.");
                return;
            }

            try
            {
                this.Role = newRole;
                data.UpdateUserRole(this.UserID, newRole.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("UpdateRole error: " + ex.Message);
            }
        }

        public bool VerifyAccount(string code)
        {
            try
            {
                string storedToken = data.GetResetToken(this.Email);

                if (string.IsNullOrEmpty(storedToken))
                {
                    MessageBox.Show("No verification code found.");
                    return false;
                }

                if (storedToken == code)
                {
                    this.IsVerified = true;
                    data.UpdateVerificationStatus(this.UserID, true);
                    data.UpdateResetToken(this.Email, "");
                    MessageBox.Show("Account verified successfully!");
                    return true;
                }
                else
                {
                    MessageBox.Show("Invalid verification code.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("VerifyAccount error: " + ex.Message);
                return false;
            }
        }

        public override string ToString() => $"{Username} ({Role})";
    }

    public class Admin : User
    {
        public Admin() : base()
        {
            this.Role = UserRole.Admin;
        }

        public List<User> GetAllUsers()
        {
            try
            {
                return data.GetAllUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetAllUsers error: " + ex.Message);
                return new List<User>();
            }
        }

        public List<Transaction> ViewAllTransactions()
        {
            try
            {
                return data.GetAllTransactions();
            }
            catch (Exception ex)
            {
                MessageBox.Show("ViewAllTransactions error: " + ex.Message);
                return new List<Transaction>();
            }
        }

        public void DeactivateUser(int userId)
        {
            try
            {
                data.UpdateUserStatus(userId, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("DeactivateUser error: " + ex.Message);
            }
        }

        public void DeleteUser(int userId)
        {
            try
            {
                data.DeleteUser(userId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("DeleteUser error: " + ex.Message);
            }
        }

        public void ResetUserPassword(int userId)
        {
            try
            {
                string newPassword = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
                data.UpdateUserPassword(userId, hashedPassword);
                MessageBox.Show("Password has been reset. New temporary password: " + newPassword);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ResetUserPassword error: " + ex.Message);
            }
        }

        public void ManageDefaultCategories()
        {
            try
            {
                var defaults = data.GetDefaultCategories();
                string list = "Current System Defaults:\n";
                foreach (var cat in defaults)
                    list += "- " + cat.Name + " (" + cat.Type + ")\n";
                MessageBox.Show(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        public void GenerateReport()
        {
            try
            {
                int totalUsers = data.GetUserCount();
                int activeUsers = data.GetActiveUserCount();
                int inactiveUsers = totalUsers - activeUsers;
                int totalAccounts = data.GetTotalAccountCount();
                int totalTransactions = data.GetTotalTransactionCount();

                string reportContent =
                    "================================================" + "\n" +
                    "           FINANCY SYSTEM REPORT                " + "\n" +
                    "================================================" + "\n" +
                    "Generated:         " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" +
                    "Generated by:      " + this.Username + "\n" +
                    "------------------------------------------------" + "\n" +
                    "USER STATISTICS" + "\n" +
                    "------------------------------------------------" + "\n" +
                    "Total Users:       " + totalUsers + "\n" +
                    "Active Users:      " + activeUsers + "\n" +
                    "Inactive Users:    " + inactiveUsers + "\n" +
                    "------------------------------------------------" + "\n" +
                    "SYSTEM STATISTICS" + "\n" +
                    "------------------------------------------------" + "\n" +
                    "Total Accounts:    " + totalAccounts + "\n" +
                    "Total Transactions:" + totalTransactions + "\n" +
                    "================================================" + "\n";

                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    "FinancyReport_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt"
                );

                File.WriteAllText(path, reportContent);
                MessageBox.Show("Report saved to Downloads: " + path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("GenerateReport error: " + ex.Message);
            }
        }
    }

    public class UserProfile
    {
        private Data data = new Data();
        public int ProfileID { get; set; }
        public int UserID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string AvatarUrl { get; set; }
        public string PreferredCurrency { get; set; }
        public bool NotifGoalReminders { get; set; }

        public void Save()
        {
            try
            {
                data.UpdateProfile(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save profile error: " + ex.Message);
            }
        }

        public string GetFullName() => $"{FirstName} {LastName}";

        public void UploadAvatar(string path)
        {
            if (File.Exists(path))
            {
                this.AvatarUrl = path;
                this.Save();
            }
            else
            {
                MessageBox.Show("The image file was not found.");
            }
        }
    }

    public class UserReport
    {
        private Data data = new Data();
        private int userID;
        private string username;

        public UserReport(int userId, string username)
        {
            this.userID = userId;
            this.username = username;
        }

        private List<Transaction> GetMonthlyTransactions(int month, int year)
        {
            return data.GetTransactionsByUser(this.userID, month, year);
        }

        public void GeneratePDF(int month, int year)
        {
            try
            {
                GlobalFontSettings.FontResolver = new WindowsFontResolver();

                List<Transaction> transactions = GetMonthlyTransactions(month, year);
                decimal totalIncome = transactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
                decimal totalExpense = transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);
                decimal net = totalIncome - totalExpense;

                string monthName = new DateTime(year, month, 1).ToString("MMMM yyyy");
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    $"FinancyReport_{username}_{year}_{month:00}.pdf"
                );

                PdfSharp.Pdf.PdfDocument document = new PdfSharp.Pdf.PdfDocument();
                document.Info.Title = $"Financy Report {monthName}";

                PdfSharp.Pdf.PdfPage page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;

                XGraphics gfx = XGraphics.FromPdfPage(page);

                XFont titleFont = new XFont("Arial", 18, XFontStyleEx.Bold);
                XFont boldFont = new XFont("Arial", 11, XFontStyleEx.Bold);
                XFont normalFont = new XFont("Arial", 10, XFontStyleEx.Regular);

                XColor green = XColor.FromArgb(0, 184, 148);
                XColor red = XColor.FromArgb(239, 68, 68);
                XColor dark = XColor.FromArgb(31, 41, 55);
                XColor muted = XColor.FromArgb(100, 116, 139);
                XColor headerBg = XColor.FromArgb(46, 125, 50);

                double margin = 40;
                double y = margin;
                double pageWidth = page.Width.Point - margin * 2;

                gfx.DrawString("Financy — Monthly Report", titleFont,
                    new XSolidBrush(green), new XRect(margin, y, pageWidth, 30), XStringFormats.TopLeft);
                y += 30;

                gfx.DrawString($"User: {username}    Period: {monthName}", normalFont,
                    new XSolidBrush(muted), new XRect(margin, y, pageWidth, 20), XStringFormats.TopLeft);
                y += 25;

                gfx.DrawLine(new XPen(XColor.FromArgb(200, 200, 200)), margin, y, margin + pageWidth, y);
                y += 15;

                gfx.DrawString("Summary", boldFont, new XSolidBrush(dark),
                    new XRect(margin, y, pageWidth, 20), XStringFormats.TopLeft);
                y += 20;
                gfx.DrawString($"Total Income:    €{totalIncome:N2}", normalFont, new XSolidBrush(green),
                    new XRect(margin, y, pageWidth, 18), XStringFormats.TopLeft);
                y += 18;
                gfx.DrawString($"Total Expenses:  €{totalExpense:N2}", normalFont, new XSolidBrush(red),
                    new XRect(margin, y, pageWidth, 18), XStringFormats.TopLeft);
                y += 18;
                gfx.DrawString($"Net Balance:     €{net:N2}", boldFont,
                    new XSolidBrush(net >= 0 ? green : red),
                    new XRect(margin, y, pageWidth, 18), XStringFormats.TopLeft);
                y += 28;

                gfx.DrawLine(new XPen(XColor.FromArgb(200, 200, 200)), margin, y, margin + pageWidth, y);
                y += 15;

                gfx.DrawString("Transactions", boldFont, new XSolidBrush(dark),
                    new XRect(margin, y, pageWidth, 20), XStringFormats.TopLeft);
                y += 22;

                double col1 = margin, col2 = margin + 80, col3 = margin + 280, col4 = margin + 380;

                gfx.DrawRectangle(new XSolidBrush(headerBg), margin, y, pageWidth, 18);
                gfx.DrawString("Date", boldFont, XBrushes.White, new XRect(col1 + 4, y + 2, 75, 16), XStringFormats.TopLeft);
                gfx.DrawString("Description", boldFont, XBrushes.White, new XRect(col2 + 4, y + 2, 195, 16), XStringFormats.TopLeft);
                gfx.DrawString("Type", boldFont, XBrushes.White, new XRect(col3 + 4, y + 2, 95, 16), XStringFormats.TopLeft);
                gfx.DrawString("Amount", boldFont, XBrushes.White, new XRect(col4 + 4, y + 2, 80, 16), XStringFormats.TopLeft);
                y += 20;

                bool alternate = false;
                foreach (Transaction t in transactions)
                {
                    if (y > page.Height.Point - 60)
                    {
                        page = document.AddPage();
                        page.Size = PdfSharp.PageSize.A4;
                        gfx = XGraphics.FromPdfPage(page);
                        y = margin;
                    }

                    XColor rowBg = alternate
                        ? XColor.FromArgb(245, 245, 245)
                        : XColor.FromArgb(255, 255, 255);
                    gfx.DrawRectangle(new XSolidBrush(rowBg), margin, y, pageWidth, 16);

                    XColor amtColor = t.Type == "Expense" ? red : green;

                    gfx.DrawString(t.Date.ToString("yyyy-MM-dd"), normalFont, new XSolidBrush(dark),
                        new XRect(col1 + 4, y + 1, 75, 14), XStringFormats.TopLeft);
                    gfx.DrawString(
                        string.IsNullOrWhiteSpace(t.Description) ? "—" : t.Description,
                        normalFont, new XSolidBrush(dark),
                        new XRect(col2 + 4, y + 1, 195, 14), XStringFormats.TopLeft);
                    gfx.DrawString(t.Type, normalFont, new XSolidBrush(dark),
                        new XRect(col3 + 4, y + 1, 95, 14), XStringFormats.TopLeft);
                    gfx.DrawString($"€{t.Amount:N2}", normalFont, new XSolidBrush(amtColor),
                        new XRect(col4 + 4, y + 1, 80, 14), XStringFormats.TopLeft);

                    y += 17;
                    alternate = !alternate;
                }

                document.Save(path);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path)
                { UseShellExecute = true });

                MessageBox.Show("PDF report saved to Downloads:\n" + path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("GeneratePDF error: " + ex.Message);
            }
        }

        public void GenerateExcel(int month, int year)
        {
            try
            {
                List<Transaction> transactions = GetMonthlyTransactions(month, year);
                decimal totalIncome = transactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
                decimal totalExpense = transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);
                decimal net = totalIncome - totalExpense;

                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    $"FinancyReport_{username}_{year}_{month:00}.xlsx"
                );

                using (XLWorkbook workbook = new XLWorkbook())
                {
                    IXLWorksheet ws = workbook.Worksheets.Add("Monthly Report");

                    ws.Cell("A1").Value = "Financy Monthly Report";
                    ws.Cell("A1").Style.Font.Bold = true;
                    ws.Cell("A1").Style.Font.FontSize = 16;
                    ws.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#2e7d32");

                    ws.Cell("A2").Value = $"User: {username}";
                    ws.Cell("A3").Value = $"Period: {new DateTime(year, month, 1):MMMM yyyy}";

                    ws.Cell("A5").Value = "Summary";
                    ws.Cell("A5").Style.Font.Bold = true;
                    ws.Cell("A6").Value = "Total Income";
                    ws.Cell("B6").Value = totalIncome;
                    ws.Cell("A7").Value = "Total Expenses";
                    ws.Cell("B7").Value = totalExpense;
                    ws.Cell("A8").Value = "Net Balance";
                    ws.Cell("B8").Value = net;
                    ws.Cell("B8").Style.Font.Bold = true;

                    ws.Cell("A10").Value = "Date";
                    ws.Cell("B10").Value = "Description";
                    ws.Cell("C10").Value = "Type";
                    ws.Cell("D10").Value = "Amount";

                    IXLRange headerRange = ws.Range("A10:D10");
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2e7d32");
                    headerRange.Style.Font.FontColor = XLColor.White;

                    int row = 11;
                    foreach (Transaction t in transactions)
                    {
                        ws.Cell(row, 1).Value = t.Date.ToString("yyyy-MM-dd");
                        ws.Cell(row, 2).Value = t.Description;
                        ws.Cell(row, 3).Value = t.Type;
                        ws.Cell(row, 4).Value = t.Amount;

                        string bg = t.Type == "Income" ? "#e8f5e9" : "#ffebee";
                        ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromHtml(bg);
                        row++;
                    }

                    ws.Columns().AdjustToContents();
                    workbook.SaveAs(path);
                }

                MessageBox.Show("Excel report saved to Downloads:\n" + path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("GenerateExcel error: " + ex.Message);
            }
        }
    }

    public class WindowsFontResolver : IFontResolver
    {
        public string DefaultFontName => "Arial";

        public byte[] GetFont(string faceName)
        {
            string fontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            string file = faceName switch
            {
                "Arial#Bold" => Path.Combine(fontsFolder, "arialbd.ttf"),
                "Arial#Italic" => Path.Combine(fontsFolder, "ariali.ttf"),
                _ => Path.Combine(fontsFolder, "arial.ttf")
            };
            return File.ReadAllBytes(file);
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (familyName.Equals("Arial", StringComparison.OrdinalIgnoreCase))
            {
                if (isBold) return new FontResolverInfo("Arial#Bold");
                if (isItalic) return new FontResolverInfo("Arial#Italic");
                return new FontResolverInfo("Arial#Regular");
            }
            return new FontResolverInfo("Arial#Regular");
        }
    }
}