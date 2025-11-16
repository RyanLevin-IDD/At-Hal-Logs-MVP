using AutomationHoistinger;
using Microsoft.VisualBasic.ApplicationServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Keys = OpenQA.Selenium.Keys;
namespace At_Hal_Logs
{
    public partial class AppWindow : Form
    {
        private TimeManager _timeManager;
        private ChromeDriver _driver = null;
        private CancellationTokenSource _cancellationTokenSource;
        public System.Windows.Forms.ComboBox TimeFilterComboBox => this.SelectBoxFilterByTime;

        public AppWindow()
        {
            InitializeComponent();
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            SelectBoxFilterByTime.SelectedIndexChanged += (s, e) =>
            {
                if (_timeManager != null && _timeManager.isTimerRunning)
                {
                    _timeManager.ResetNextRunBasedOnSelection();
                }
            };

        }

        //Main Run
        public async Task RunAutomationAsync(CancellationToken token)
        {
            // Save current user settings
            SaveSetting();
            doneBox.Text = "";
            try
            {
                //SetUp reuse existing driver or create new one
                if (_driver == null || !IsBrowserStillOpen(_driver))
                {
                    //Browser is closed or dont exist -> create new one
                    _driver = CreateChromeDriver();
                    PrepareBrowser(_driver);

                    // Navigate to base URL for new browser
                    _driver.Navigate().GoToUrl(Globals.LOGIN_URL);

                    // Wait for captcha resolution and page to be ready (login or dashboard)
                    bool pageReady = await WaitForCaptchaOrLoginAsync(_driver, TimeSpan.FromMinutes(3));

                    if (!pageReady)
                    {
                        throw new Exception("Page failed to load after captcha handling");
                    }
                }
                else
                {
                    //Browser is still open just refresh to check login status
                    WebDriverWait wait1 = new WebDriverWait(_driver, TimeSpan.FromSeconds(Globals.FINDING_ELEMENT_TIMEOUT));
                    WaitForPageLoad(_driver, wait1);
                    _driver.Navigate().GoToUrl(Globals.LOGIN_URL);
                    bool pageReady = await WaitForCaptchaOrLoginAsync(_driver, TimeSpan.FromMinutes(3));
                    if (!pageReady)
                    {
                        throw new Exception("Page failed to load after captcha handling");
                    }
                }
                WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(Globals.FINDING_ELEMENT_TIMEOUT));
                var domains = await GetDomainsFromSheetAPI();

                //Get THE STARING HOUR TIMEFRAME
                string dateYesterday = getTimeFrame();


                //For each domain
                for (int i = 0; i < domains.Length; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        txtLogs.Text += Environment.NewLine + "Run stopped by user.";
                        doneBox.Text = "Run Stopped";
                        return;
                    }
                    await Task.Delay(1000);
                    await ProcessDomainLogs(_driver, domains[i], wait, token, dateYesterday);
                }
            }
            catch (OperationCanceledException)
            {
                txtLogs.Text += Environment.NewLine + "Run cancelled safely.";
            }
            catch (Exception ex)
            {
                await SendInfoLogsToSheetAPI(ex.Message);
                txtLogs.Text += Environment.NewLine + $"ERROR: {ex.Message}\n{ex.StackTrace}";
            }
            finally
            {
                //Browser stays open for reuse
                doneBox.Text = "Run Finished";
                if (chkEnableTimer.Checked)
                {
                    _timeManager.Start();
                }
            }
        }

        //Automation Actions
        private void Login(WebDriverWait wait, ChromeDriver driver)
        {
            //Set Credenditals
            string email = txtEmail.Text;
            string password = txtPassword.Text;
            //Wait for login fields
            var emailInput = wait.Until(drv =>
            {
                try { var e = drv.FindElement(By.Id("email-input")); return e.Displayed ? e : null; }
                catch
                {
                    txtLogs.Text += Environment.NewLine + "Login: Could not find: email-input";
                    return null;
                }
            });
            var passwordInput = wait.Until(drv =>
            {
                try { var e = drv.FindElement(By.Id("password-input")); return e.Displayed ? e : null; }
                catch
                {
                    txtLogs.Text += Environment.NewLine + "Login: Could not find: password-input";
                    return null;
                }
            });
            var loginButton = wait.Until(drv =>
            {
                try { var e = drv.FindElement(By.CssSelector("button[type='submit']")); return e.Displayed && e.Enabled ? e : null; }
                catch
                {
                    txtLogs.Text += Environment.NewLine + "Login: Could not find: submit button";
                    return null;
                }
            });

            //Fill form and click login
            emailInput.Clear();
            passwordInput.Clear();
            emailInput.SendKeys(email);
            passwordInput.SendKeys(password);
            Thread.Sleep(1000);
            loginButton.Click();

        }
        private bool NavigateToLogs(ChromeDriver driver)
        {
            try
            {
                WaitForDomReady(driver);
                //Click on "accese logs" tab
                var acceseLogsTab = WaitForElementById(driver, Globals.acceseLogsTab_Id);
                if(acceseLogsTab == null)
                {
                    return false;
                }
                ClickElement(acceseLogsTab);
                Thread.Sleep(1000);
                return true;
            }
            catch (Exception ex)
            {
                txtLogs.Text += Environment.NewLine + $"Error navigating to logs: {ex.Message}";
                return false;
            }
        }
        private async Task<bool> NavigateToWebsitesAsync(string domain, ChromeDriver driver, WebDriverWait wait, CancellationToken token)
        {
            txtLogs.Text += Environment.NewLine + $"=== NavigateToWebsitesAsync CALLED for domain: {domain} ===";
            txtLogs.Text += Environment.NewLine + $"Current browser URL: {driver.Url}";
            bool insideDashboard = false;
            // Wait for page to be fully loaded
            WaitForLoadingDone(driver);
            while (!insideDashboard)
            {
                token.ThrowIfCancellationRequested();
                //First try the direct link
                driver.Navigate().GoToUrl(Globals.BASE_URL + $"/{domain}/analytics");
                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                WaitForLoadingDone(driver);
                await Task.Delay(4000, token);
                var currentUrl = driver.Url;

                //Inside the dashboard
                if (currentUrl.StartsWith($"https://hpanel.hostinger.com/websites/{domain}/analytics", StringComparison.OrdinalIgnoreCase))
                {
                    txtLogs.Text += Environment.NewLine + $"Reached {domain} Dashboard";
                    insideDashboard = true;
                    return true;
                }
                //Click on the first option after search
                else if (currentUrl == "https://hpanel.hostinger.com/websites")
                {
                    WaitForLoadingDone(driver);
                    wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                    await Task.Delay(1000, token);
                    txtLogs.Text += Environment.NewLine + "Searching for the domain";
                    var searchField = wait.Until(d => d.FindElement(By.CssSelector("input[data-qa='h-form-field-input']")));

                    Actions actions = new Actions(driver);
                    actions
                        .Click(searchField)
                        .SendKeys(domain)
                        .SendKeys(Keys.Enter)
                        .Perform();

                    //Wait for page to load
                    WaitForPageLoad(driver, wait);
                    await Task.Delay(4000, token);
                    bool domainsFound = await WaitForDomainsList(driver, TimeSpan.FromMinutes(2));

                    if (domainsFound)
                    {
                        txtLogs.Text += Environment.NewLine + "Domain found!";
                        var dashboardButton = WaitForElementByDataQa(driver, Globals.dashboardButton_dataQa);
                        dashboardButton.Click();
                        wait.Until(d =>
                        {
                            try
                            {
                                var loaderImage = d.FindElement(By.CssSelector("img.animation-loader__outline"));
                                return loaderImage.Displayed; // still visible -> keep waiting
                            }
                            catch (NoSuchElementException)
                            {
                                return false; //not in DOM yet, keep waiting
                            }
                            catch (StaleElementReferenceException)
                            {
                                return false; //replaced/removed -> keep waiting
                            }
                        });
                    }
                    else
                    {
                        // No domains with that name found -> exit admin user and try again
                        var exit_btn = WaitForElementByDataQa(driver, Globals.exit_admin_account_btn_dataQa);
                        exit_btn.Click();
                        bool pageReady = await WaitForCaptchaOrLoginAsync(_driver, TimeSpan.FromMinutes(3));
                        if (!pageReady)
                        {
                            throw new Exception("Page failed to load after captcha handling");
                        }
                    }
                }
            }
            return false;
        }

        //Fetch all logs from a single page
        private (List<Dictionary<string, string>> Rows, bool IsDone) ScrapeAccessLogs(ChromeDriver driver, string dateYesterday, int startIndex, bool firstRun)
        {
            var allRows = new List<Dictionary<string, string>>();
            int _startIndex = 0;
            bool isDone = false;
            try
                {
                    var rows = driver.FindElements(By.CssSelector(".access-logs__table-row"));
                    string todayString = DateTime.Today.ToString("yyyy-MM-dd");

                    if (firstRun)
                    {
                        if (startIndex == 0)
                        {
                            _startIndex = rows.Count - 1; //Start from the buttom
                        }
                        else
                        {
                            _startIndex = startIndex - 1; //start from the last index
                        }
                        // Go up (newer rows below)
                        for (int i = _startIndex; i >= 0; i--)
                        {
                            Thread.Sleep(100);
                            var row = rows[i];
                            var cells = row.FindElements(By.CssSelector(".access-logs__table-item"));
                            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", row);

                            string timeText = cells[0].Text.Trim();
                            if (timeText.Length < 10)
                                continue;

                            string rowDate = timeText.Substring(0, 10);

                            // Stop when reaching today's date
                            if (rowDate == todayString)
                            {
                                isDone = true;
                                break;
                            }

                            var rowData = new Dictionary<string, string>
                            {
                                ["Time"] = timeText,
                                ["IP Address"] = cells[1].Text.Trim(),
                                ["Request"] = cells[2].Text.Trim(),
                                ["Device"] = cells[3].Text.Trim(),
                                ["Country"] = cells[4].Text.Trim(),
                                ["Size (bytes)"] = cells[5].Text.Trim(),
                                ["Response time (ms)"] = cells[6].Text.Trim()
                            };

                            allRows.Add(rowData);
                        }
                    }
                    else
                    {

                        // Go upward (from bottom to top)
                        for (int i = rows.Count - 1; i >= 0; i--)
                        {
                            Thread.Sleep(100);
                            var row = rows[i];
                            var cells = row.FindElements(By.CssSelector(".access-logs__table-item"));
                            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", row);

                            string timeText = cells[0].Text.Trim();
                            if (timeText.Length < 10)
                                continue;

                            string rowDate = timeText.Substring(0, 10);

                            // Stop when reaching today's date
                            if (rowDate == todayString)
                            {
                                isDone = true;
                                break;
                            }

                            var rowData = new Dictionary<string, string>
                            {
                                ["Time"] = timeText,
                                ["IP Address"] = cells[1].Text.Trim(),
                                ["Request"] = cells[2].Text.Trim(),
                                ["Device"] = cells[3].Text.Trim(),
                                ["Country"] = cells[4].Text.Trim(),
                                ["Size (bytes)"] = cells[5].Text.Trim(),
                                ["Response time (ms)"] = cells[6].Text.Trim()
                            };

                            allRows.Add(rowData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    txtLogs.Text += Environment.NewLine + $"Error scraping logs: {ex.Message}";
                }

            return (allRows, isDone);
        }

        private (bool Found, int StartIndex, bool NoNewLogs) FindStartingRow(ChromeDriver driver, int daysAgoToAnalyze)
        {
            bool found = false;
            int startIndex = -1;
            bool noNewLogs = false;
            try
            {
                string dateToFind = DateTime.Now.AddDays(daysAgoToAnalyze).ToString("yyyy-MM-dd");
                var rows = driver.FindElements(By.CssSelector(".access-logs__table-row"));

                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var cells = row.FindElements(By.CssSelector(".access-logs__table-item"));

                    if (cells.Count == 0)
                        continue;

                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", row);

                    string timeText = cells[0].Text.Trim();

                    // If the time column contains dateYesterday
                    if (timeText.Contains(dateToFind))
                    {
                        found = true;
                        startIndex = i;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                txtLogs.Text += Environment.NewLine + $"Error finding starting row: {ex.Message}";
            }

            return (found, startIndex, noNewLogs);
        }
        //Fetch all logs for the domain and send them to the sheet
        private async Task ProcessDomainLogs(ChromeDriver driver, string domain, WebDriverWait wait, CancellationToken token, string dateYesterday)
        {
            //Make sure domain is available
            bool websiteDahsboardReady = await NavigateToWebsitesAsync(domain, driver, wait, token);
            if (!websiteDahsboardReady || token.IsCancellationRequested)
                return;
            WaitForLoadingDone(driver);
            bool acceseLogsPressed = NavigateToLogs(driver);
            if (!acceseLogsPressed)
            {
                //Send logs and continue to next domain
                SendInfoLogsToSheetAPI($"Could not find access logs tab for domain:{domain}\n" +
                    $"Continue to next domain");
                return;
            }
            //Press 24h
            var timeFilterElement = GetTimeSelection(driver);
            timeFilterElement.Click();

            bool logsFound = await WaitForLogsList(driver, TimeSpan.FromMinutes(1));
            if (!logsFound) 
            {
                txtLogs.Text += Environment.NewLine + "Logs not found after 1 minute";
                return;
            }
            ApplyFilters(driver);
            int totalPages = CalculateLogPages(driver, domain);
            string domainName = domain;
            //Find the starting row page
            var (found, startIndex, pageNumber, isNewLogs) = await FindStartingRowAsync(driver, wait, totalPages, dateYesterday, token, domain);

            //Scrape
            List <Dictionary<string, string>> allLogs = await ScrapeLogsFromPageAsync(driver,wait,dateYesterday,pageNumber,startIndex,token);
            int rowsInResults = allLogs.Count;
            txtLogs.Text += Environment.NewLine + "Done analyzing logs, Sending to sheet";
            // Send to Sheet
            if (rowsInResults > 0)
            {
                await SendResultsToSheetAPI(allLogs, domainName);
            }
        }



        private async Task<(bool Found, int StartIndex, int PageNumber, bool NewLogs)> FindStartingRowAsync(ChromeDriver driver, WebDriverWait wait, int totalPages, string dateToFind, CancellationToken token, string domain)
        {
            bool found = false;
            int startIndex = -1;
            int pageNumber = -1;
            bool newLogs = true;
            txtLogs.Text += Environment.NewLine + "Looking for the starting row";
            for (int page = 1; page <= totalPages; page++)
            {
                token.ThrowIfCancellationRequested();

                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                await Task.Delay(1000, token);
                wait.Until(d => d.FindElement(By.CssSelector(".access-logs__table-row")));
                //check missing logs last run
                var lastRowFromSheet = await GetLastRowFromSheet(domain);
                int daysAgoToAnalyze = -2; //yesterday
                if (lastRowFromSheet != null)
                {
                    daysAgoToAnalyze = calculateHowManyDays(lastRowFromSheet);
                }
                
                var (startingRowFound, startIdx, isNewLogs) = FindStartingRow(driver, daysAgoToAnalyze);
                if (startingRowFound)
                {
                    found = true;
                    startIndex = startIdx;
                    pageNumber = page;
                    newLogs = isNewLogs;
                    txtLogs.Text += Environment.NewLine + "Starting row found!";
                    break;
                }

                // Move to next page if not found
                if (page < totalPages)
                {
                    var nextPageButton = WaitForElement(driver, Globals.nextPageButton_Xpath);
                    nextPageButton.Click();
                    WaitForElement(driver, Globals.tableElement_Xpath);
                }
                else
                {
                    txtLogs.Text += Environment.NewLine + "Reached end page and starting row not found";
                }
            }

            return (found, startIndex, pageNumber, newLogs);
        }
        private int calculateHowManyDays(Dictionary<string, string> lastRowInSheet)
        {
            // Extract text
            if (!lastRowInSheet.TryGetValue("Time", out string timeString))
                return -6;

            // Parse the date from sheet
            if (!DateTime.TryParse(timeString, out DateTime rowDate))
                return -6;

            // Compare dates only
            int daysAgo = (DateTime.Now.Date - rowDate.Date).Days;

            // Valid only if 2–6 days ago
            if (daysAgo >= 2 && daysAgo <= 6)
                return -daysAgo;

            // Anything else -> default -6
            return -6;
        }
        private async Task<List<Dictionary<string, string>>> ScrapeLogsFromPageAsync(ChromeDriver driver, WebDriverWait wait, string dateYesterday, int startPage, int startIndex, CancellationToken token)
        {
            List<Dictionary<string, string>> logs = new List<Dictionary<string, string>>();
            bool isDone = false;
            bool firstRun = true;
            for (int page = startPage; page > 0; page--)
            {
                token.ThrowIfCancellationRequested();

                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                await Task.Delay(1000, token);
                wait.Until(d => d.FindElement(By.CssSelector(".access-logs__table-row")));

                var (pageLogs, doneFlag) = ScrapeAccessLogs(driver, dateYesterday, startIndex, firstRun);
                logs.AddRange(pageLogs);
                isDone = doneFlag;
                firstRun = false;

                if (isDone)
                    break;

                if (page > 1)
                {
                    var nextPageButton = WaitForElement(driver, Globals.prevPageButton_Xpath);
                    nextPageButton.Click();
                    WaitForElement(driver, Globals.tableElement_Xpath);
                }

                // after first page, always start from buttom
                startIndex = 0;
            }

            return logs;
        }
        //API
        private async Task SendResultsToSheetAPI(List<Dictionary<string, string>> results, string domain)
        {
            var client = new HttpClient();

            var payload = new
            {
                domain = domain,
                results = results
            };

            var json = JsonConvert.SerializeObject(payload);

            var response = await client.PostAsync(
                txtAPI_Dev.Text,
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            var responseContent = await response.Content.ReadAsStringAsync();
            txtLogs.Text += Environment.NewLine + responseContent;

            // Parse JSON response
            var responseObj = JsonConvert.DeserializeObject<JObject>(responseContent);

            var previousLastLog = responseObj["previousLastLog"]?.ToObject<Dictionary<string, string>>();
            var currentFirstLog = responseObj["currentFirstLog"]?.ToObject<Dictionary<string, string>>();

        }
        private async Task SendInfoLogsToSheetAPI(string log)
        {
            var client = new HttpClient();
            var payload = new
            {
                machineName = Environment.MachineName,
                logs = log
            };
            var json = JsonConvert.SerializeObject(payload);
            var response = await client.PostAsync(
                txtAPI_Dev.Text,
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            var responseContent = await response.Content.ReadAsStringAsync();

            txtLogs.Text += Environment.NewLine + "Info Log sent, response:";
            txtLogs.Text += Environment.NewLine + responseContent;
        }
        private async Task GetLastRowFromSeet(string domain)
        {

        }
        //Helpers
        private bool DictionariesEqual(Dictionary<string, string> a, Dictionary<string, string> b)
        {
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;

            foreach (var key in a.Keys)
            {
                if (!b.ContainsKey(key)) return false;

                var valueA = a[key]?.Trim().Replace("\r", "") ?? "";
                var valueB = b[key]?.Trim().Replace("\r", "") ?? "";

                if (valueA != valueB) return false;
            }

            return true;
        }


        //Captcha
        private void CheckForCaptcha(ChromeDriver driver)
        {
            if (CaptchaDetecter(driver))
            {
                CaptchaSolver(driver);
            }

        } 
        private bool CaptchaDetecter(ChromeDriver driver)
        {
            try
            {
                var js = (IJavaScriptExecutor)driver;

                // Check what Selenium actually sees
                string pageSource = driver.PageSource;

                if (pageSource.Contains("cf-turnstile-response"))
                {
                    txtLogs.Text += Environment.NewLine + "[CAPTCHA DEBUG] Input found in page source";
                    return true;
                }

                // Also try to find the container div by ID
                try
                {
                    var container = driver.FindElement(By.Id("oplP4"));
                    txtLogs.Text += Environment.NewLine + $"[CAPTCHA DEBUG] Container found, displayed={container.Displayed}";
                    return true;
                }
                catch (NoSuchElementException)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                txtLogs.Text += Environment.NewLine + $"[CAPTCHA DEBUG] Exception: {ex.GetType().Name} - {ex.Message}";
                return false;
            }
        }
        private void CaptchaSolver(ChromeDriver driver)
        {
            Actions actions = new Actions(driver);

            //Click on a blank area of the page
            var body = driver.FindElement(By.TagName("body"));
            actions.MoveToElement(body, 10, 10) //
                   .Click()
                   .Perform();

            //Press Tab once
            actions = new Actions(driver);
            actions.SendKeys(Keys.Tab).Perform();

            //Press Space
            actions = new Actions(driver);
            actions.SendKeys(Keys.Space).Perform();
            txtLogs.Text += Environment.NewLine + "Captcha solved";
            Thread.Sleep(1000);
        }

        //Browser actions
        private void WaitForDomReady(IWebDriver driver)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(d =>
                ((IJavaScriptExecutor)d)
                    .ExecuteScript("return document.readyState").ToString() == "complete"
            );
        }
        private async Task<bool> WaitForCaptchaOrLoginAsync(ChromeDriver driver, TimeSpan timeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopwatch.Elapsed < timeout)
            {
                try
                {
                    string currentUrl = driver.Url;
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    // Exit condition: Already logged in (dashboard loaded)
                    if (currentUrl.StartsWith("https://hpanel.hostinger.com", StringComparison.OrdinalIgnoreCase))
                    {
                        txtLogs.Text += Environment.NewLine + "Loggsed in and ready";
                        return true;
                    }
                    bool loginFormFound = false;
                    //Login page loaded (email/password fields visible) -> login
                    try
                    {
                        //Send log and wait 15 min for manual input, if not close browser
                        if (currentUrl.StartsWith("https://auth.hostinger.com/v1/", StringComparison.OrdinalIgnoreCase))
                        {
                            if (IsEmailBlocked(driver))
                            {
                                await SendInfoLogsToSheetAPI("Manual email verification needed. Please log in to Hostinger on the machine before restarting the app.");

                                txtLogs.Text += Environment.NewLine + "Waiting up to 15 minutes for user to complete verification";

                                // Wait for up to 15 minutes or until login succeeds
                                bool verified = false;
                                verified = await WaitForManualVerificationAsync(_driver, TimeSpan.FromMinutes(15));

                                if (verified)
                                {
                                    txtLogs.Text += Environment.NewLine + "Verification completed, continuing automation";
                                }
                                else
                                {
                                    throw new Exception("Manual verification timed out");
                                }
                            }
                        }
                        var emailInput = driver.FindElement(By.Id("email-input"));
                        var passwordInput = driver.FindElement(By.Id("password-input"));
                        var loginButton = driver.FindElement(By.CssSelector("button[type='submit']"));

                        if (emailInput.Displayed && passwordInput.Displayed && loginButton.Displayed && loginButton.Enabled)
                        {
                            loginFormFound = true;
                            txtLogs.Text += Environment.NewLine + Environment.NewLine + "Login form detected -> login";
                            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
                            Login(wait, driver);
                            Thread.Sleep(1000);
                            WaitForPageLoad(driver, wait);
                        }
                        if (IsEmailBlocked(driver))
                        {
                            await SendInfoLogsToSheetAPI("Manual email verification needed. Please log in to Hostinger on the machine before restarting the app.");

                            txtLogs.Text += Environment.NewLine + "Waiting up to 15 minutes for user to complete verification";

                            // Wait for up to 15 minutes or until login succeeds
                            bool verified = false;
                            verified = await WaitForManualVerificationAsync(driver, TimeSpan.FromMinutes(15));

                            if (verified)
                            {
                                txtLogs.Text += Environment.NewLine + "Verification completed, continuing automation";
                            }
                            else
                            {
                                throw new Exception("Manual verification timed out");
                            }
                        }
                    }
                    catch (NoSuchElementException)
                    {
                        // Login elements not found yet, continue checking
                    }
                    if (!loginFormFound)
                    {
                        CheckForCaptcha(driver);
                    }
                }
                catch (WebDriverException ex)
                {
                    txtLogs.Text += Environment.NewLine + $"No Verification needed";
                }

                // Check every 2 seconds
                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            txtLogs.Text += Environment.NewLine + "Timeout waiting for captcha resolution or login page";
            return false; // timed out
        }
        private async Task<bool> WaitForDomainsList(ChromeDriver driver, TimeSpan timeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopwatch.Elapsed < timeout)
            {
                try
                {
                    string currentUrl = driver.Url;

                    //Exit condition1:  1 or more results
                    if (IsElementVisible(driver, Globals.dashboardButton_dataQa))
                    {
                        return true;
                    }
                    //Exit condition: no results
                    else if (IsElementVisableByCss(driver, "h3[data-msgid='v2.nothing.found']"))
                    {

                        return false;
                    }
                }
                catch (WebDriverException)
                {
                    txtLogs.Text += Environment.NewLine + "Timedout waiting fot the domain lists";
                }

                await Task.Delay(TimeSpan.FromSeconds(1)); // check every 15 seconds
            }

            return false; // timed out
        }
        private async Task<bool> WaitForLogsList(ChromeDriver driver, TimeSpan timeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopwatch.Elapsed < timeout)
            {
                try
                {
                    // First check if "no logs" message exists
                    var noLogsMessage = driver.FindElements(By.CssSelector("p[data-msgid='There are no logs collected yet']"));
                    if (noLogsMessage.Count > 0 && noLogsMessage[0].Displayed)
                    {
                        txtLogs.Text += Environment.NewLine + "No logs found message displayed";
                        return false; // Exit condition: no logs message found
                    }

                    // Check if table container exists
                    var tableContainer = driver.FindElements(By.CssSelector(".access-logs__table"));
                    if (tableContainer.Count > 0)
                    {
                        // Now check for rows
                        var logsRows = driver.FindElements(By.CssSelector(".access-logs__table-row"));

                        if (logsRows.Count > 0)
                        {
                            return true; // Exit condition: logs exist
                        }
                    }
                }
                catch (WebDriverException ex)
                {
                    txtLogs.Text += Environment.NewLine + $"WebDriver exception: {ex.Message}";
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            return false; // Timeout
        }
        private bool WaitForLoadingDone(ChromeDriver driver)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromMinutes(5));
            try
            {

                return wait.Until(drv =>
                {
                    try
                    {
                        var loader = drv.FindElement(By.CssSelector("img.animation-loader__outline"));
                        // If still visible, keep waiting
                        return !loader.Displayed;
                    }
                    catch (NoSuchElementException)
                    {
                        // Loader not in DOM -> loading finished
                        Thread.Sleep(500);
                        return true;
                    }
                    catch (StaleElementReferenceException)
                    {
                        // Loader removed/replaced -> loading finished
                        Thread.Sleep(500);
                        return true;
                    }
                });
            }
            catch (WebDriverTimeoutException)
            {
                // Loader still visible after timeout
                txtLogs.Text += Environment.NewLine + "Loader still visable after timeout";
                return false;
            }
        }//Wait until the spinning loading circle is gone
        private void ClickElement(IWebElement element)
        {
            element.Click();
        }
        private async Task<bool> WaitForManualVerificationAsync(ChromeDriver driver, TimeSpan timeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await Task.Delay(5);
            while (stopwatch.Elapsed < timeout)
            {
                try
                {
                    string currentUrl = driver.Url;

                    // If user finishes login
                    if (currentUrl.StartsWith("https://hpanel.hostinger.com", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    else
                    {
                        CheckForCaptcha(driver);
                    }
                }
                catch (WebDriverException) {}

                await Task.Delay(TimeSpan.FromSeconds(15)); // check every 15 seconds
            }

            return false; // timed out
        }
        private bool IsEmailBlocked(ChromeDriver driver)
        {
            bool suspiciousElement = WaitForElementByCssClass(driver, Globals.sususpiciousElements_cssSelector, "Suspicious Login Detected");

            return suspiciousElement;
        }
        private void ApplyFilters(ChromeDriver driver)
        {
            //Sort logs from oldest to newest and select max resuls per page
            /*var sortByTimeButton = WaitForElement(driver, Globals.logsTableTimeFilter_Xpath);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", sortByTimeButton);
            sortByTimeButton.Click();*/
            txtLogs.Text += Environment.NewLine + "Trying to set max results 100";
            var maxResultsSelectionBox = WaitForElement(driver, Globals.resultsPerPageButton_Xpath);
            maxResultsSelectionBox.Click();
            var maxResults100 = WaitForElement(driver, Globals.maxResults100_Xpath);
            maxResults100.Click();
            txtLogs.Text += Environment.NewLine + "Max results set";
        }
        private int CalculateLogPages(ChromeDriver driver, string domainName)
        {
            var resultsCounterElement = WaitForElement(driver, Globals.resultsCounterElement_Xpath);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", resultsCounterElement);
            string text = resultsCounterElement.Text;
            string lastNumber = text.Split(new string[] { "of" }, StringSplitOptions.None).Last().Trim();

            //convert to int
            int total = int.Parse(lastNumber);
            //Check if more than 10000
            if (total == 10000)
            {

                SendInfoLogsToSheetAPI($"10,000 or more logs detected for domain: {domainName}, please monitor results for the day");
            }
            int totalPages = (int)Math.Ceiling((double)total / Globals.MAX_LOGS_PER_PAGE);
            return totalPages;
        }
        private bool IsElementVisible(IWebDriver drv, string dataQa)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(drv, TimeSpan.FromSeconds(10));
                return wait.Until(d =>
                {
                    try
                    {
                        var e = d.FindElement(By.CssSelector($"[data-qa='{dataQa}']"));
                        return e.Displayed;
                    }
                    catch
                    {
                        return false;
                    }
                });
            }
            catch (WebDriverTimeoutException)
            {
                txtLogs.Text += Environment.NewLine + "TimedOut looking for element with dataQA: " + dataQa;
                return false;
            }
            catch
            {
                return false;
            }
        }
        private bool IsElementVisableByCss(IWebDriver drv, string cssClass)
        {
            try
            {
                var element = drv.FindElement(By.CssSelector(cssClass));
                return element.Displayed;
            }
            catch
            {
                return false;
            }
        }
        private IWebElement WaitForElement(IWebDriver drv, string xpath)
        {
            WebDriverWait wait = new WebDriverWait(drv, TimeSpan.FromSeconds(Globals.FINDING_ELEMENT_TIMEOUT));

            var element = wait.Until(drv =>
            {
                try
                {
                    var e = drv.FindElement(By.XPath(xpath));
                    return e.Displayed ? e : null;
                }
                catch
                {
                    return null;
                }
            });

            return element;
        }
        private IWebElement WaitForElementById(ChromeDriver drv, string id)
        {
            WebDriverWait wait = new WebDriverWait(drv, TimeSpan.FromSeconds(Globals.FINDING_ELEMENT_TIMEOUT));

            try
            {
                // Primary attempt - normal wait
                return wait.Until(d =>
                {
                    try
                    {
                        var e = d.FindElement(By.Id(id));
                        return e.Displayed ? e : null;
                    }
                    catch
                    {
                        return null;
                    }
                });
            }
            catch (WebDriverTimeoutException)
            {
                // Timeout caught -> go to fallback
                return RobustElementRecovery(drv);
            }
        }
        private IWebElement RobustElementRecovery(ChromeDriver drv)
        {
            try
            {
                txtLogs.Text += Environment.NewLine + "Timeout finding Accese logs tab: Starting robust method";
                //Hard refresh
                txtLogs.Text += Environment.NewLine + "-Refreshing Page-";
                drv.Navigate().Refresh();
                
                bool ready = WaitForLoadingDone(drv);
                Thread.Sleep(2000);
                txtLogs.Text += Environment.NewLine + "-Trying by ID...-";
                var byId = TryFind(drv, By.Id(Globals.acceseLogsTab_Id));
                if (byId != null) return byId;
                txtLogs.Text += Environment.NewLine + "-Trying by data-qa...-";
                var byDataQa = TryFind(drv, By.CssSelector($"[data-qa='{Globals.acceseLogsTab_Id}']"));
                if (byDataQa != null) return byDataQa;
                txtLogs.Text += Environment.NewLine + "-Trying by Xpath...-";
                var byXpath = TryFind(drv, By.XPath(Globals.acceseLogsTab_Xpath));
                if (byXpath != null) return byXpath;
                txtLogs.Text += Environment.NewLine + "-Trying by Text...-";
                var byText = TryFind(drv, By.XPath($"//*[contains(text(), '{Globals.acceseLogsTab_Text}')]"));
                if (byText != null) return byText;

                // If all failed -> log + return null
                txtLogs.Text += Environment.NewLine + $"Robust recovery failed to find Accese logs tab";
                return null;
            }
            catch (Exception ex)
            {
                txtLogs.Text += Environment.NewLine +$"Robust recovery error: {ex.Message}";
                return null;
            }
        }
        private IWebElement TryFind(ChromeDriver drv, By selector)
        {
            try
            {
                var e = drv.FindElement(selector);
                return e.Displayed ? e : null;
            }
            catch
            {
                return null;
            }
        }
        private IWebElement WaitForElementByDataQa(IWebDriver drv, string dataQa)
        {
            WebDriverWait wait = new WebDriverWait(drv, TimeSpan.FromSeconds(Globals.FINDING_ELEMENT_TIMEOUT));

            var element = wait.Until(drv =>
            {
                try
                {
                    var e = drv.FindElement(By.CssSelector($"[data-qa='{dataQa}']"));
                    return e.Displayed ? e : null;
                }
                catch
                {
                    return null;
                }
            });

            return element;
        }
        private async Task QuickCheckMissingLogsAsync(ChromeDriver driver, string domain, int index)
        {
            // Fetch last row from sheet
            var lastRow = await GetLastRowFromSheet(domain);
            if (lastRow == null)
            {
                txtLogs.Text += Environment.NewLine + "No previous logs found in sheet.";
                return;
            }

            try
            {
                // Get all rows on the page
                var rows = driver.FindElements(By.CssSelector(".access-logs__table-row"));
                int prevIndex = index - 1;

                if (prevIndex < 0 || prevIndex >= rows.Count)
                {
                    txtLogs.Text += Environment.NewLine + "Invalid index to check previous row.";
                    return;
                }

                var row = rows[prevIndex];
                var cells = row.FindElements(By.CssSelector(".access-logs__table-item"));
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", row);

                // Map current row to dictionary
                var currentRow = new Dictionary<string, string>
                {
                    ["Time"] = cells[0].Text.Trim(),
                    ["IP Address"] = cells[1].Text.Trim(),
                    ["Request"] = cells[2].Text.Trim(),
                    ["Device"] = cells[3].Text.Trim(),
                    ["Country"] = cells[4].Text.Trim(),
                    ["Size (bytes)"] = cells[5].Text.Trim(),
                    ["Response time (ms)"] = cells[6].Text.Trim()
                };

                // Use your helper to compare dictionaries
                bool isMatch = DictionariesEqual(currentRow, lastRow);

                if (isMatch)
                    txtLogs.Text += Environment.NewLine + "Previous row matches last row from sheet";
                else
                {
                    txtLogs.Text += Environment.NewLine + "Previous row does NOT match last row from sheet, sending email";
                    await SendInfoLogsToSheetAPI("Previous row does NOT match last row from sheet\n" +
                    $@"Previous row time from sheet: {lastRow["Time"]}" + "\n" +
                    $@"Row time found in Hoistinger: {currentRow["Time"]}" + "\n" +
                    $@"Domain name: {domain}"
                    );
                }

            }
            catch (Exception ex)
            {
                txtLogs.Text += Environment.NewLine + $"Error checking previous row: {ex.Message}";
            }
        }
        private bool WaitForElementByCssClass(IWebDriver drv, string cssClass, string textToSearch)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(drv, TimeSpan.FromSeconds(5));

                bool elementFound = wait.Until(d =>
                {
                    try
                    {
                        var e = d.FindElement(By.Id("2fa-code-form"));
                        return e.Displayed; // Return true when found and visible
                    }
                    catch (NoSuchElementException)
                    {
                        return false;
                    }
                });

                return elementFound;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }
        private bool IsBrowserStillOpen(ChromeDriver driver)
        {
            try
            {
                var _ = driver.WindowHandles;
                return true;
            }
            catch
            {
                return false;
            }
        }
        private void WaitForPageLoad(ChromeDriver drv, WebDriverWait wait)
        {
            wait.Until(drv =>
                        ((IJavaScriptExecutor)drv).ExecuteScript("return document.readyState").Equals("complete")
                    );
        }


        //SetUp
        private ChromeDriver CreateChromeDriver()
        {
            var options = new ChromeOptions();
            string username = Environment.UserName;
            username = username.ToLower();
            var userDataDir = $@"C:\Users\{username}\AppData\Local\Google\Chrome\User Data";
            string profileDir = txtProfile.Text;
            options.AddArgument("--start-maximized");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-plugins");
            options.AddArgument("--disable-images");
            options.AddArgument($"--user-data-dir={userDataDir}");
            options.AddArgument($"--profile-directory={profileDir}");

            //Hide the CMD window
            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            return new ChromeDriver(service, options);
        }
        private void PrepareBrowser(ChromeDriver driver)
        {
            try
            {
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

                js.ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined});");
                js.ExecuteScript(@"
                    Object.defineProperty(navigator, 'plugins', { get: () => [1,2,3,4,5] });
                    Object.defineProperty(navigator, 'languages', { get: () => ['en-US','en','he'] });
                    window.chrome = { runtime: {} };
                ");
            }
            catch (Exception ex)
            {
                throw new Exception("PrepareBrowser error: " + ex.Message);
            }
        }
        private async Task<string[]> GetDomainsFromSheetAPI()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(txtAPI_Client.Text);

                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

                if (parsed.TryGetValue("domains", out object? value))
                {
                    var domainsArray = JsonConvert.DeserializeObject<string[]>(value.ToString());
                    return domainsArray;
                }

                return Array.Empty<string>(); //fallback empty
            }
        }
        private async Task<Dictionary<string, string>?> GetLastRowFromSheet(string domain)
        {
            using var client = new HttpClient();

            string url = $"{txtAPI_Dev.Text}?domain={Uri.EscapeDataString(domain)}";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();

            var jsonObj = JsonConvert.DeserializeObject<JObject>(responseContent);
            if (jsonObj == null || jsonObj["lastLog"] == null)
                return null;

            var lastRowObj = jsonObj["lastLog"] as JObject;
            if (lastRowObj == null)
                return null;

            var lastRow = lastRowObj.Properties()
                                     .ToDictionary(p => p.Name, p => p.Value.ToString());

            if (lastRow.ContainsKey("Time") && DateTime.TryParse(lastRow["Time"], out DateTime parsedTime))
            {
                lastRow["Time"] = parsedTime.ToString("yyyy-MM-dd\nHH:mm:ss");
            }

            return lastRow;
        }
        private IWebElement GetTimeSelection(ChromeDriver driver)
        {
            string? selectedValue = SelectBoxFilterByTime.SelectedItem?.ToString();
            if (selectedValue == null)
            {
                selectedValue = Globals.DEFAULT_TIME_FILTER;
            }

            switch (selectedValue)
            {
                case "Last 1h":
                    return WaitForElement(driver, Globals.filterByLast1H_Xpath);
                case "Last 6h":
                    return WaitForElement(driver, Globals.filterByLast6H_Xpath);
                case "Last 24h":
                    return WaitForElement(driver, Globals.filterByLast24H_Xpath);
                case "Last 7d":
                    return WaitForElement(driver, Globals.filterByLast7D_Xpath);
                default:
                    return WaitForElement(driver, Globals.filterByLast1H_Xpath);
            }
        }


        //Crash handlers
        private void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            //SendFailSafeEmail("Please restart the app on device ....");
            txtLogs.Text += Environment.NewLine + "An unexpected error occurred. Need to debug";
        }
        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            //SendFailSafeEmail("Please restart the app on device ....");
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            //Clean up browser when app closes
            if (_driver != null)
            {
                try
                {
                    _driver.Quit();
                    _driver.Dispose();
                }
                catch { }
            }

            if (e.CloseReason != CloseReason.UserClosing)
            {
                // App crashed, system shutdown
                //SendFailSafeEmail(new Exception("App closed unexpectedly. Reason: " + e.CloseReason));
            }

            base.OnFormClosing(e);
        }


        //UI
        private void SelectBoxFilterByTime_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedValue = SelectBoxFilterByTime.SelectedItem.ToString();

            TimeSpan newInterval = selectedValue switch
            {
                "Last 1h" => TimeSpan.FromHours(1),
                "Last 6h" => TimeSpan.FromHours(6),
                "Last 24h" => TimeSpan.FromHours(24),
                "Last 7d" => TimeSpan.FromDays(7),
                _ => TimeSpan.FromHours(1)
            };
            _timeManager.isTimerRunning = chkEnableTimer.Checked;
            //_timeManager.SetInterval(newInterval); // update UI
        }
        private void save_btn_Click(object sender, EventArgs e)
        {
            SaveSetting();
            MessageBox.Show("Settings saved successfully!", "Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AppWindow_Load(object sender, EventArgs e)
        {
            // Initialize the timer
            _timeManager = new TimeManager(this);
            chkEnableTimer.Checked = Config.GetBool("TimerEnabled");
            _timeManager.isTimerRunning = chkEnableTimer.Checked;
            if (chkEnableTimer.Checked) { txtTimerRunner.Text = "Auto Run ON"; }
            else { txtTimerRunner.Text = "Auto Run OFF"; }

            //Load last used fields to the UI
            txtEmail.Text = Config.Get("Email");
            txtPassword.Text = Config.Get("Password");
            SelectBoxFilterByTime.SelectedItem = Config.Get("TimeFilter");
            txtProfile.Text = Config.Get("ChromeProfile");
            txtAPI_Dev.Text = Config.Get("SheetAPIDev");
            txtAPI_Client.Text = Config.Get("SheetAPIClient");
        }
        private async void btnStart_Click(object sender, EventArgs e)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            //Update the timer
            if (_timeManager != null)
            {
                await _timeManager.RunAutomationManually(_cancellationTokenSource.Token);
            }
        }
        private void SaveSetting()
        {
            // Save current settings
            Config.Set("Email", txtEmail.Text);
            Config.Set("Password", txtPassword.Text);
            Config.Set("TimeFilter", SelectBoxFilterByTime.SelectedItem?.ToString() ?? "");
            Config.Set("ChromeProfile", txtProfile.Text);
            Config.Set("SheetAPIDev", txtAPI_Dev.Text);
            Config.SetBool("TimerEnabled", chkEnableTimer.Checked);
            Config.Set("SheetAPIClient", txtAPI_Client.Text);
        }
        private void chkEnableTimer_CheckedChanged(object sender, EventArgs e)
        {
            if (chkEnableTimer.Checked)
            {
                // Timer enabled
                _timeManager.Start();
                txtTimerRunner.Text = "Auto Run ON";
            }
            else
            {
                // Timer disabled
                _timeManager.Stop();
                txtTimerRunner.Text = "Auto Run OFF";
            }
        }

        private void stopRun_btn_Click(object sender, EventArgs e)
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                txtLogs.Text += Environment.NewLine + "Stop signal sent...";
            }
            else
            {
                txtLogs.Text += Environment.NewLine + "No active run to stop.";
            }
        }

        public CancellationTokenSource CancellationTokenSource
        {
            get
            {
                if (_cancellationTokenSource == null)
                    _cancellationTokenSource = new CancellationTokenSource();
                return _cancellationTokenSource;
            }
        }

        private string getTimeFrame()
        {
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return yesterday;
        }
    }
}
