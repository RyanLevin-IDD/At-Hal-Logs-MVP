using AutomationHoistinger;
using Microsoft.VisualBasic.ApplicationServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
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
        public async Task RunAutomationAsync()
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

                //For each domain
                for (int i = 0; i < domains.Length; i++)
                {
                    await Task.Delay(1000);
                    await ProcessDomainLogs(_driver, domains[i], wait);
                }
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
                try { var e = drv.FindElement(By.Id("email-input")); return e.Displayed ? e : null; } catch {
                    txtLogs.Text += Environment.NewLine + "Login: Could not find: email-input";
                    return null; }
            });
            var passwordInput = wait.Until(drv =>
            {
                try { var e = drv.FindElement(By.Id("password-input")); return e.Displayed ? e : null; } catch {
                    txtLogs.Text += Environment.NewLine + "Login: Could not find: password-input";
                    return null; }
            });
            var loginButton = wait.Until(drv =>
            {
                try { var e = drv.FindElement(By.CssSelector("button[type='submit']")); return e.Displayed && e.Enabled ? e : null; } catch {
                    txtLogs.Text += Environment.NewLine + "Login: Could not find: submit button";
                    return null; }
            });

            //Fill form and click login
            emailInput.Clear();
            passwordInput.Clear();
            emailInput.SendKeys(email);
            passwordInput.SendKeys(password);
            Thread.Sleep(1000);
            loginButton.Click();

        }
        private void NavigateToLogs(ChromeDriver driver)
        {
            try
            {
                WaitForDomReady(driver);
                //Click on "accese logs" tab
                var acceseLogsTab = WaitForElementById(driver, Globals.acceseLogsTab_Id);
                ClickElement(acceseLogsTab);
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                txtLogs.Text += Environment.NewLine + $"Error navigating to logs: {ex.Message}";
                throw;
            }
        }
        private async Task<bool> NavigateToWebsitesAsync(string domain, ChromeDriver driver, WebDriverWait wait)
        {
            txtLogs.Text += Environment.NewLine + $"=== NavigateToWebsitesAsync CALLED for domain: {domain} ===";
            txtLogs.Text += Environment.NewLine + $"Current browser URL: {driver.Url}";
            bool insideDashboard = false;
            // Wait for page to be fully loaded
            WaitForLoadingDone(driver);
            while (!insideDashboard)
            {
                //First try the direct link
                driver.Navigate().GoToUrl(Globals.BASE_URL + $"/{domain}/analytics");
                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                WaitForLoadingDone(driver);
                await Task.Delay(4000);
                var currentUrl = driver.Url;

                //Inside the dashboard
                if (currentUrl.StartsWith($"https://hpanel.hostinger.com/websites/{domain}/analytics", StringComparison.OrdinalIgnoreCase))
                {
                    insideDashboard = true;
                    return true;
                }
                //Click on the first option after search
                else if (currentUrl == "https://hpanel.hostinger.com/websites")
                {
                    WaitForLoadingDone(driver);
                    wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                    await Task.Delay(1000);
                    var searchField = wait.Until(d => d.FindElement(By.CssSelector("input[data-qa='h-form-field-input']")));

                    Actions actions = new Actions(driver);
                    actions
                        .Click(searchField)
                        .SendKeys(domain)
                        .SendKeys(Keys.Enter)
                        .Perform();

                    //Wait for page to load
                    WaitForPageLoad(driver, wait);
                    await Task.Delay(4000);
                    bool domainsFound = await WaitForDomainsList(driver, TimeSpan.FromMinutes(2));

                    if (domainsFound)
                    {
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
                        // No domains with that name found
                        return false;
                    }
                }
            }
            return false;
        }

        //Fetch all logs from a single page
        private List<Dictionary<string, string>> ScrapeAccessLogs(ChromeDriver driver, bool step1Delay = false)
        {
            var allRows = new List<Dictionary<string, string>>();
            try
            {
                // Find all rows
                var rows = driver.FindElements(By.CssSelector(".access-logs__table-row"));

                foreach (var row in rows)
                {
                    if (step1Delay) { Thread.Sleep(300); }
                    var cells = row.FindElements(By.CssSelector(".access-logs__table-item"));
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", row);
                    var rowData = new Dictionary<string, string>();

                    // Map headers to data
                    rowData["Time"] = cells[0].Text.Trim();
                    rowData["IP Address"] = cells[1].Text.Trim();
                    rowData["Request"] = cells[2].Text.Trim();
                    rowData["Device"] = cells[3].Text.Trim();
                    rowData["Country"] = cells[4].Text.Trim();
                    rowData["Size (bytes)"] = cells[5].Text.Trim();
                    rowData["Response time (ms)"] = cells[6].Text.Trim();

                    allRows.Add(rowData);
                }
            }
            catch (Exception ex)
            {
                txtLogs.Text += Environment.NewLine + $"Error scraping logs: {ex.Message}";
            }
            return allRows;
        }
        private List<Dictionary<string, string>> ScrapeAccessLogsWithFilter(ChromeDriver driver, Dictionary<string, string>? firstRow = null)
        {
            var allRows = new List<Dictionary<string, string>>();

            try
            {
                // Parse reference time if provided
                DateTime? referenceTime = null;
                if (firstRow != null && firstRow.ContainsKey("Time"))
                {
                    if (DateTime.TryParse(firstRow["Time"], out var parsedTime))
                        referenceTime = parsedTime;
                }

                // Find all rows
                var rows = driver.FindElements(By.CssSelector(".access-logs__table-row"));

                foreach (var row in rows)
                {
                    var cells = row.FindElements(By.CssSelector(".access-logs__table-item"));
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", row);

                    var rowData = new Dictionary<string, string>
                    {
                        ["Time"] = cells[0].Text.Trim(),
                        ["IP Address"] = cells[1].Text.Trim(),
                        ["Request"] = cells[2].Text.Trim(),
                        ["Device"] = cells[3].Text.Trim(),
                        ["Country"] = cells[4].Text.Trim(),
                        ["Size (bytes)"] = cells[5].Text.Trim(),
                        ["Response time (ms)"] = cells[6].Text.Trim()
                    };

                    //only add if Time >= firstRow["Time"]
                    if (referenceTime.HasValue)
                    {
                        if (DateTime.TryParse(rowData["Time"], out var currentRowTime))
                        {
                            if (currentRowTime >= referenceTime.Value)
                            {
                                allRows.Add(rowData);
                            }
                                
                        }
                    }
                    else
                    {
                        allRows.Add(rowData);
                    }
                }
            }
            catch (Exception ex)
            {
                txtLogs.Text += Environment.NewLine + $"Error scraping logs: {ex.Message} , Step 2";
            }

            return allRows;
        }

        //Fetch all logs for the domain and send them to the sheet
        private async Task ProcessDomainLogs(ChromeDriver driver, string domain, WebDriverWait wait)
        {
            //Make sure domain is available
            bool websiteDahsboardReady = await NavigateToWebsitesAsync(domain, driver, wait);
            if (!websiteDahsboardReady)
                return;
            WaitForLoadingDone(driver);
            NavigateToLogs(driver);

            var timeFilterElement = GetTimeSelection(driver);
            timeFilterElement.Click();

            bool logsFound = await WaitForLogsList(driver, TimeSpan.FromMinutes(1));
            if (!logsFound)
                return;
            ApplyFilters(driver); //Filter by time
            int totalPages = CalculateLogPages(driver);
            string domainName = ExtractDomainName(domain);
            //add steps helpers
            bool step1Delay = false;
            bool step2 = false;
            //helper task to scrape all pages
            async Task<List<Dictionary<string, string>>> ScrapeAllPagesAsync(bool step1Delay,bool step2, Dictionary<string, string>? firstRow)
            {
                List<Dictionary<string, string>> logs = new List<Dictionary<string, string>>();
                for (int page = 1; page <= totalPages; page++)
                {
                    wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                    await Task.Delay(1000);
                    wait.Until(d => d.FindElement(By.CssSelector(".access-logs__table-row")));
                    if (step2)
                    {
                        var pageLogs = ScrapeAccessLogsWithFilter(driver, firstRow);
                        logs.AddRange(pageLogs);
                    }
                    else
                    {
                        var pageLogs = ScrapeAccessLogs(driver, step1Delay);
                        logs.AddRange(pageLogs);
                    }
                    

                    if (page < totalPages)
                    {
                        var nextPageButton = WaitForElement(driver, Globals.nextPageButton_Xpath);
                        nextPageButton.Click();
                        WaitForElement(driver, Globals.tableElement_Xpath);
                    }
                }
                return logs;
            }

            //Scrape first time
            List<Dictionary<string, string>> allLogs = await ScrapeAllPagesAsync(step1Delay,step2, null);

            // Compare totals
            var resultsCounterElement = WaitForElement(driver, Globals.resultsCounterElement_Xpath);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", resultsCounterElement);
            string text = resultsCounterElement.Text;
            string resultsInHoistinger = text.Split(new string[] { "of" }, StringSplitOptions.None).Last().Trim();
            int rowsInHoistinger = int.Parse(resultsInHoistinger);
            int rowsInResults = allLogs.Count;


            //Step 1 withouth delay - try 3 times
            int maxRetries = 3;
            int retryCount = 0;
            while (rowsInHoistinger != rowsInResults && retryCount < maxRetries)
            {
                await Task.Delay(2000); // Wait 2 seconds before checking again

                // Refresh and retry
                var timeFilterElement_r = GetTimeSelection(driver);
                timeFilterElement_r.Click();
                WaitForElement(driver, Globals.tableElement_Xpath);
                await Task.Delay(1500);
                allLogs = await ScrapeAllPagesAsync(step1Delay, step2 , null);
                rowsInResults = allLogs.Count;

                retryCount++;
            }
            //Step 1 with delay - try 3 times
            if (rowsInHoistinger != rowsInResults)
            {
                retryCount = 0;
                step1Delay = true;
                txtLogs.Text += Environment.NewLine + "Step1(no delay): Logs didnt match after trying 3 times, moving to step 1 with delay";
                await SendInfoLogsToSheetAPI("Step1(no delay): Logs didnt match after trying 3 times, moving to step 1 with delay");
                while (rowsInHoistinger != rowsInResults && retryCount < maxRetries)
                {
                    await Task.Delay(2000);
                    // Refresh and retry
                    var timeFilterElement_r = GetTimeSelection(driver);
                    timeFilterElement_r.Click();
                    WaitForElement(driver, Globals.tableElement_Xpath);
                    await Task.Delay(1500);

                    allLogs = await ScrapeAllPagesAsync(step1Delay, step2, null);
                    rowsInResults = allLogs.Count;

                    retryCount++;
                }
                if(rowsInHoistinger != rowsInResults)
                {
                    txtLogs.Text += Environment.NewLine + "Step1(delay): Logs didnt match after trying 3 times with delay, moving to step 2: 6h filter";
                    await SendInfoLogsToSheetAPI("Step1(delay): Logs didnt match after trying 3 times with delay, moving to step 2: 6h filter");
                }
                else
                {
                    txtLogs.Text += Environment.NewLine + "Step1(delay):Successe, logs are matching, sending results to sheet";
                    await SendInfoLogsToSheetAPI("Step1(delay):Successe, logs are matching, sending results to sheet");
                } 

            }
            // Step 2 try the 6h filter
            if (rowsInHoistinger != rowsInResults)
            {
                step2 = true;  
                //click the 6h filter first
                SetTimeFilterForMissingLog(driver);
                WaitForElement(driver, Globals.tableElement_Xpath);
                await Task.Delay(1500);
                var firstRow = allLogs.First();
                var firstResultsCount = allLogs.Count();
                var firstLogs = allLogs;
                allLogs = await ScrapeAllPagesAsync(step1Delay, step2, firstRow);
                rowsInResults = allLogs.Count;
                if(firstResultsCount != rowsInResults)
                {
                    txtLogs.Text += Environment.NewLine + "Step2(6h filter): logs count is the same as the 1h, sending results to sheet";
                    await SendInfoLogsToSheetAPI("Step2(6h filter): logs count is the same as the 1h, sending results to sheet");
                    allLogs = firstLogs;
                }
                else
                {
                    txtLogs.Text += Environment.NewLine + "Step2(6h filter): counted diffrent number of results vs the 1h filter, sending results to sheet";
                    await SendInfoLogsToSheetAPI("Step2(6h filter): counted diffrent number of results vs the 1h filter, sending results to sheet");
                }
            }

            // Send to Sheet
            if (rowsInResults > 0)
            {
                await SendResultsToSheetAPI(allLogs, domainName);
            }
        }

        //Check for missing logs
        private async Task CheckForMissingLogs(ChromeDriver driver, Dictionary<string, string> lastLog, Dictionary<string, string> firstLog, string domain)
        {
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
            //Set filters
            SetTimeFilterForMissingLog(driver);
            //ScrapeLogs
            WaitForPageLoad(driver, wait);
            //step1 find starting page
            int totalPages = CalculateLogPages(driver);
            int startingPageIndex = 1;
            for (int page = 1; page <= totalPages; page++)
            {
                bool isStartingPage = IsStartingPage(driver, lastLog["Time"]);
                if (isStartingPage)
                {
                    //done
                    startingPageIndex = page;
                    break;

                }
                else
                {
                    var nextPageButton = WaitForElement(driver, Globals.nextPageButton_Xpath);
                    nextPageButton.Click();
                    WaitForElement(driver, Globals.tableElement_Xpath);
                }
            }

            //Find the lastLog and look for everything between it and the startLog  
            bool lastLogFound = false;
            bool firstLogFound = false;
            List<string> allMissingLogs = new List<string>();

            for (int page = startingPageIndex; page <= totalPages && !firstLogFound; page++)
            {
                WaitForPageLoad(driver, wait);
                bool logsFound = await WaitForLogsList(driver, TimeSpan.FromMinutes(1));
                await Task.Delay(1000);
                string[] pageMissingLogs = CollectMissingLogsFromPage(driver, lastLog, firstLog, ref lastLogFound, ref firstLogFound);
                allMissingLogs.AddRange(pageMissingLogs);

                if (!firstLogFound && page < totalPages)
                {
                    var nextPageButton = WaitForElement(driver, Globals.nextPageButton_Xpath);
                    nextPageButton.Click();
                    WaitForElement(driver, Globals.tableElement_Xpath);
                }
            }
            //Send logs to sheet
            List<Dictionary<string, string>> missingLogsDict = allMissingLogs
            .Select(json => JsonConvert.DeserializeObject<Dictionary<string, string>>(json))
            .ToList();

            await SendMissingLogsInfoToSheet(missingLogsDict, domain);
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
                txtAPI.Text,
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            var responseContent = await response.Content.ReadAsStringAsync();
           txtLogs.Text += Environment.NewLine + responseContent;

            // Parse JSON response
            var responseObj = JsonConvert.DeserializeObject<JObject>(responseContent);

            var previousLastLog = responseObj["previousLastLog"]?.ToObject<Dictionary<string, string>>();
            var currentFirstLog = responseObj["currentFirstLog"]?.ToObject<Dictionary<string, string>>();

            if (previousLastLog != null && currentFirstLog!= null)
            {
                await CheckForMissingLogs(_driver, previousLastLog, currentFirstLog, domain);
            }
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
                txtAPI.Text,
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            var responseContent = await response.Content.ReadAsStringAsync();

            txtLogs.Text += Environment.NewLine + "Info Log sent, response:";
            txtLogs.Text += Environment.NewLine + responseContent;
        }

        //Helpers
        private async Task SendMissingLogsInfoToSheet(List<Dictionary<string, string>> missingLogs, string domain)
        {
            //string message = "No Discrepancy detected";
            if (missingLogs == null || missingLogs.Count == 0)
            {
                return; // Nothing to send
            }

            else
            {
                string message = "";
                int numberOfLogs = missingLogs.Count;
                string firstLogTime = missingLogs.First()["Time"];
                string firstLogIp = missingLogs.First()["IP Address"];
                string firstLogRequest = missingLogs.First()["Request"];
                string lastLogTime = missingLogs.Last()["Time"];
                string lastLogIp = missingLogs.Last()["IP Address"];
                string lastLogRequest = missingLogs.Last()["Request"];

                message = $"Discrepancy detected\n" +
                                 $"Number of logs missed: {numberOfLogs}\n" +
                                 $"First missing log- time: {firstLogTime}\n" +
                                 $" IP: {firstLogIp}\n" +
                                 $" Request: {firstLogRequest}\n" +
                                 "--------------\n" +
                                 $"Last missing log time: {lastLogTime}\n" +
                                 $" IP: {lastLogIp}\n" +
                                 $" Request: {lastLogRequest}\n" +
                                 "--------------\n" +
                                 $"Domain: {domain}";
                await SendInfoLogsToSheetAPI(message);
            }


        }
        private string[] CollectMissingLogsFromPage(ChromeDriver driver, Dictionary<string, string> lastLog, Dictionary<string, string> firstLog, ref bool lastLogFound, ref bool firstLogFound)
        {
            List<string> missingLogs = new List<string>();

            // Get all rows on the current page
            var rows = driver.FindElements(By.CssSelector(".access-logs__table-row"));

            foreach (var row in rows)
            {
                var cells = row.FindElements(By.CssSelector(".access-logs__table-item"));
                var rowData = new Dictionary<string, string>
                {
                    ["Time"] = cells[0].Text.Trim(),
                    ["IP Address"] = cells[1].Text.Trim(),
                    ["Request"] = cells[2].Text.Trim(),
                    ["Device"] = cells[3].Text.Trim(),
                    ["Country"] = cells[4].Text.Trim(),
                    ["Size (bytes)"] = cells[5].Text.Trim(),
                    ["Response time (ms)"] = cells[6].Text.Trim()
                };

                // Step 2: Find LastLog anchor
                if (!lastLogFound)
                {
                    if (DictionariesEqual(rowData, lastLog))
                    {
                        lastLogFound = true; //start saving from next row
                    }
                    continue; //skip until LastLog is found
                }

                // Step 3: Save logs until FirstLog
                if (DictionariesEqual(rowData, firstLog))
                {
                    firstLogFound = true;
                    break; //done collecting
                }

                missingLogs.Add(JsonConvert.SerializeObject(rowData));
            }

            return missingLogs.ToArray();
        }
        private bool DictionariesEqual(Dictionary<string, string> a, Dictionary<string, string> b)
        {
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;

            foreach (var key in a.Keys)
            {
                if (!b.ContainsKey(key)) return false;
                if (a[key] != b[key]) return false;
            }

            return true;
        }
        private bool IsStartingPage(ChromeDriver driver, string lastLogTimeStamp)
        {
            try
            {
                //Find all rows on the current page
                var rows = driver.FindElements(By.CssSelector(".access-logs__table-row"));

                if (rows.Count == 0)
                {
                    return false; //No rows on page
                }

                //Get the last row on this page
                var lastRow = rows[rows.Count - 1];
                var cells = lastRow.FindElements(By.CssSelector(".access-logs__table-item"));

                if (cells.Count == 0)
                {
                    return false;
                }

                // Get the timestamp from the first cell (Time column)
                string lastRowTimeStamp = cells[0].Text.Trim();

                // Parse both timestamps to DateTime for comparison
                DateTime lastLogTime = DateTime.Parse(lastLogTimeStamp);
                DateTime lastRowTime = DateTime.Parse(lastRowTimeStamp);

                //Return true if the last row on this page is newer than the LastLog
                //
                return lastRowTime >= lastLogTime;
            }
            catch (Exception ex)
            {
                txtLogs.Text += Environment.NewLine + $"Error checking starting page when handling missing logs: {ex.Message}";
                return false;
            }
        }
        private void SetTimeFilterForMissingLog(ChromeDriver driver)
        {
            IWebElement timeFilterElement = null;
            string? selectedValue = SelectBoxFilterByTime.SelectedItem?.ToString();
            switch (selectedValue)
            {
                case "Last 1h":
                    timeFilterElement = WaitForElement(driver, Globals.filterByLast6H_Xpath);
                    break;
                case "Last 6h":
                    timeFilterElement = WaitForElement(driver, Globals.filterByLast24H_Xpath);
                    break;
                case "Last 24h":
                    timeFilterElement = WaitForElement(driver, Globals.filterByLast7D_Xpath);
                    break;
                case "Last 7d":
                    //empty logs
                    break;
                default:
                    timeFilterElement = WaitForElement(driver, Globals.filterByLast7D_Xpath);
                    break;
            }
            timeFilterElement.Click();
        }

        //Captcha
        private void CheckForCaptcha(ChromeDriver driver)
        {
            if (CaptchaDetecter(driver))
            {
                CaptchaSolver(driver);
            }

        }
        /*private bool CaptchaDetecter(ChromeDriver driver)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                IWebElement verificationElement = wait.Until(drv =>
                {
                    try
                    {
                        var el = drv.FindElement(By.Id("uMtSJ0"));
                        return el.Displayed ? el : null; // only return if visible
                    }
                    catch (NoSuchElementException)
                    {
                        return null; // not found yet, retry
                    }
                });

                if (verificationElement != null)
                {
                    txtLogs.Text += Environment.NewLine + "Captcha detected - attempting to solve";
                    return true;
                }
                return false;
            }
            catch (WebDriverTimeoutException)
            {
                txtLogs.Text += Environment.NewLine + " Could not find the captcha element";
                return false; // timeout without detection
            }
        }*/
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
        private string ExtractDomainName(string domain)
        {
            int lastDot = domain.LastIndexOf('.');
            string domainName = lastDot > 0 ? domain.Substring(0, lastDot) : domain;
            return domainName;
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
                                bool verified = await WaitForManualVerificationAsync(_driver, TimeSpan.FromMinutes(15));

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
                            bool verified = await WaitForManualVerificationAsync(driver, TimeSpan.FromMinutes(15));

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
                catch (WebDriverException) {
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
            WebDriverWait wait = new WebDriverWait(driver,TimeSpan.FromMinutes(5));
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
                        return true;
                    }
                }
                catch (WebDriverException) { }

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
            var sortByTimeButton = WaitForElement(driver, Globals.logsTableTimeFilter_Xpath);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", sortByTimeButton);
            sortByTimeButton.Click();
            var maxResultsSelectionBox = WaitForElement(driver, Globals.resultsPerPageButton_Xpath);
            maxResultsSelectionBox.Click();
            var maxResults100 = WaitForElement(driver, Globals.maxResults100_Xpath);
            maxResults100.Click();
        }
        private int CalculateLogPages(ChromeDriver driver)
        {
            var resultsCounterElement = WaitForElement(driver, Globals.resultsCounterElement_Xpath);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", resultsCounterElement);
            string text = resultsCounterElement.Text;
            string lastNumber = text.Split(new string[] { "of" }, StringSplitOptions.None).Last().Trim();

            //convert to int
            int total = int.Parse(lastNumber);
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
        private IWebElement WaitForElementById(IWebDriver drv, string id)
        {
            WebDriverWait wait = new WebDriverWait(drv, TimeSpan.FromSeconds(Globals.FINDING_ELEMENT_TIMEOUT));
            var element = wait.Until(drv =>
            {
                try
                {
                    var e = drv.FindElement(By.Id(id));
                    return e.Displayed ? e : null;
                }
                catch
                {
                    return null;
                }
            });

            return element;
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
                var response = await client.GetAsync(txtAPI.Text);

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
            txtAPI.Text = Config.Get("SheetAPI");
        }
        private async void btnStart_Click(object sender, EventArgs e)
        {
            //Update the timer
            if (_timeManager != null)
            {
                await _timeManager.RunAutomationManually();
            }
        }
        private void SaveSetting()
        {
            // Save current settings
            Config.Set("Email", txtEmail.Text);
            Config.Set("Password", txtPassword.Text);
            Config.Set("TimeFilter", SelectBoxFilterByTime.SelectedItem?.ToString() ?? "");
            Config.Set("ChromeProfile", txtProfile.Text);
            Config.Set("SheetAPI", txtAPI.Text);
            Config.SetBool("TimerEnabled", chkEnableTimer.Checked);
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
    }
}
