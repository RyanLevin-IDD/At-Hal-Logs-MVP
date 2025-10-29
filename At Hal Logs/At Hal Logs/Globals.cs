using System.Configuration;

namespace AutomationHoistinger
{
    public class Globals
    {
        public static string LOGIN_URL = "https://auth.hostinger.com/login";
        public static string BASE_URL = "https://hpanel.hostinger.com/websites";
        public static string CHROME_PATH = "C:\\Users\\";
        public static string CHROME_PATH2 = "\\AppData\\Local\\Google\\Chrome\\Application\\chrome.exe";
        public static int MAX_LOGS_PER_PAGE = 100; //Only effects calculating how many pages
        public static int FINDING_ELEMENT_TIMEOUT = 90; //How long to wait before Timeout if element is not found
        public static string DEFAULT_TIME_FILTER = "Last 1h";
        public static int BASE_RUNTIME_INTERVAL = 55; //Base interval between each run
        public static int BASE_RUNTIME_OFFSET = 2; //Offset for run

        //Elements Xpaths
        //Navigation
        public static string analyticsButton_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[1]/div[3]/div/aside/div/div/div[3]/div/div/nav/ul/li[5]/div/div/div/button";
        public static string wevsitesButton_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[1]/div[2]/div/div[2]/div/div/nav/ul/li[2]/div/div/div/button";
        public static string acceseLogsTab_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[2]/div[2]/div/div[2]/main/section/div/div[2]/div[1]/ul/li[2]/div/div";
        public static string searchField_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[2]/div[2]/div/div[2]/div[2]/div/div/div[1]/div/div[1]/div[1]/div[2]/div/div/input";
        public static string tableElement_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[2]/div[2]/div/div[2]/main/section/div/div[3]/div/div[2]";
        public static string homeButton_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[1]/div[2]/div/div[2]/div/div/nav/ul/li[1]/div/div/div/a";
        public static string dashboardButton_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[2]/div[2]/div/div[2]/div[5]/div/div[2]/div/div[2]/span[2]/button";
        public static string maxResults100_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[2]/div[2]/div/div[2]/main/section/div/div[3]/div/div[3]/div[1]/select/option[6]";
        public static string resultsPerPageButton_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[2]/div[2]/div/div[2]/main/section/div/div[3]/div/div[3]/div[1]/select";
        public static string resultsCounterElement_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[2]/div[2]/div/div[2]/main/section/div/div[3]/div/div[3]/div[2]/div[1]";
        public static string nextPageButton_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[2]/div[2]/div/div[2]/main/section/div/div[3]/div/div[3]/div[2]/div[3]";
        public static string logsTableTimeFilter_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[2]/div[2]/div/div[2]/main/section/div/div[3]/div/div[2]/div[1]/div[1]/span[2]";
        public static string cpatchaBox_Xpath = "/html/body//div/div/div[1]/div/label/span[2]";

        //Filter By x hours buttons
        public static string filterByLast1H_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[2]/div[2]/div/div[2]/main/section/div/div[1]/div/div/div[1]/div[3]/span/div[1]";
        public static string filterByLast6H_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[2]/div[2]/div/div[2]/main/section/div/div[1]/div/div/div[1]/div[3]/span/div[2]";
        public static string filterByLast24H_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[2]/div[2]/div/div[2]/main/section/div/div[1]/div/div/div[1]/div[3]/span/div[3]";
        public static string filterByLast7D_Xpath = "/html/body/div[1]/div[1]/div/div/div/div[2]/div[2]/div/div[2]/main/section/div/div[1]/div/div/div[1]/div[3]/span/div[4]";

        //Elements data-qa
        public static string analyticsButton_dataQa = "navigate-hostingDashboard-access_logs";
        public static string dashboardButton_dataQa = "hpanel_tracking-websites-dashboard_button";
        public static string wevsitesButton_dataQa = "hp-menu__item-wrapper hp-menu__item-link";
        public static string searchField_dataQa = "h-form-field-input";
        public static string sususpiciousElements_cssSelector = "h1.form-title";
        public static string acceseLogsTab_Id = "hpanel_tracking-accesslogs_tab";
    }
}
