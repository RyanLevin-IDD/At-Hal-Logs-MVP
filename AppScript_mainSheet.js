const SUPPORTED_PARAMS_SHEET_NAME = "supported-parameters-config";
function doPost(e) { // POST request - receives JSON logs
  try {
    const data = JSON.parse(e.postData.contents);

    // Process async
    if (data.results && data.domain) {
      main(data);

      // Send response immediately
      return ContentService
        .createTextOutput(JSON.stringify({ status: "OK" }))
        .setMimeType(ContentService.MimeType.JSON);
    } 

    // Default response
    return ContentService
      .createTextOutput(JSON.stringify({ status: "received" }))
      .setMimeType(ContentService.MimeType.JSON);

  } catch (err) {
    return ContentService
      .createTextOutput(JSON.stringify({ status: "error", message: err.message }))
      .setMimeType(ContentService.MimeType.JSON);
  }
}


// Run the process of reciving data and writing to sheet
function main(data){
  //Prepare
  var tab = getOrCreateTab(data.domain);
  var supportedHeaders = getSupportedHeaders();
  tab = ensureHeadersSimple(tab, supportedHeaders);

  //Transform
  data.results = normalizeTimestamps(data.results);
  var updatedResults = addDevRequestToResults(data.results);
  updatedResults = addSupportedParameters(updatedResults,supportedHeaders, tab);
  
  //Write
  writeResultsToSheet(updatedResults, tab);
}

/*
-Gets: results (data object)
-Do: Normalize the timestamps to be in a single line
- Return: updated resutls
*/
function normalizeTimestamps(results) {
  return results.map(r => {
    if (r.Date && r.Time) {
      r.Time = `${r.Date.trim()} ${r.Time.trim()}`;
    } else if (r.Date) {
      r.Time = r.Date.trim();
    } else if (r.Time) {
      r.Time = r.Time.trim();
    } else {
      r.Time = "";
    }
    return r;
  });
}

/*
-Gets: 
  - tab(sheet object for domain tab)
  - supportedHeaders (List of string headers from the config tab)
-Do: Ensure the tab have all the headers we need
-Return: tab (updated sheet object)
*/
function ensureHeadersSimple(tab, supportedHeaders) {
  const baseHeaders = [
    "Time",
    "IP Address",
    "Request",
    "Device",
    "Country",
    "Size (bytes)",
    "Response time (ms)"
  ];

  const finalHeaders = [...baseHeaders,"Dev Request",...supportedHeaders];
  const row1 = tab.getRange(1, 1, 1, tab.getMaxColumns()).getValues()[0];

  const hasHeaders = row1.some(v => v && v.toString().trim() !== "");
  if (hasHeaders) return tab;

  tab.getRange(1, 1, 1, finalHeaders.length).setValues([finalHeaders]);
  return tab;
}

/*
-Gets: results (data object)
-Do: Add devrequest function to all rows in results
-Return: updated results object
*/
function addDevRequestToResults(results) {
  const formula = '=LOWER(INDIRECT("C" & ROW()))';
  return results.map(row => ({
    ...row,
    "Dev Request": formula
  }));
}

/*
-Gets:
  - results (data object)
  - headersList (list of strings for the supported parameter headers)
  - tab (sheet object)
-Do: Add sheet function that extract parameter to all supprted headers in the list
-Return: updated results object
*/
function addSupportedParameters(results, headersList, tab) {
  const updated = [];
  
  const startColIndex = headersList.length;
  
  const startRow = tab.getLastRow() + 1;

  for (let i = 0; i < results.length; i++) {
    const rowObj = { ...results[i] };
    const rowNumber = i + startRow;

    for (let j = 0; j < headersList.length; j++) {
      const headerName = headersList[j];
      const colIndex = 9 + j;

      const formula =
        `=IFERROR(REGEXEXTRACT(H${rowNumber},"[?&]" & INDIRECT(ADDRESS(1,${colIndex})) & "=([^& ]+)"), "")`;

      rowObj[headerName] = formula;
    }

    updated.push(rowObj);
  }

  return updated;
}

/*
-Gets:
  -results (data object)
  - tab (sheet object)
-Do: Write results to the tab
-Return: OK
*/
function writeResultsToSheet(results, tab) {
  if (!results || results.length === 0) return;

  const headers = Object.keys(results[0]);
  if (tab.getLastRow() === 0) {
    tab.appendRow(headers);
  }

  const values = results.map(obj =>
    headers.map(h => {
      let val = obj[h] !== undefined ? obj[h] : "";
      if (typeof val === "string") {
        val = val.replace(/\r?\n/g, " "); 
      }
      return val;
    })
  );
  tab.getRange(tab.getLastRow() + 1, 1, values.length, headers.length).setValues(values);
}

/*
-Gets: tabName (string name of the tab)
-Do: Validate tab with tabName exists, if not create it
-Return: tab (sheet object)
*/
function getOrCreateTab(tabName) {
  const ss = SpreadsheetApp.getActive();
  let tab = ss.getSheetByName(tabName);
  if (!tab) {tab = ss.insertSheet(tabName);}
  return tab;
}

/*
-Do: fetch the headers from the config tab
-Return: headers (string list of supported headers)
*/
function getSupportedHeaders() {
    const ss = SpreadsheetApp.getActiveSpreadsheet();
    const sheet = ss.getSheetByName(SUPPORTED_PARAMS_SHEET_NAME);
    
    if (!sheet) {
        throw new Error(`Sheet "${SUPPORTED_PARAMS_SHEET_NAME}" not found`);
    }
    
    const headers = sheet.getRange(1, 1, 1, sheet.getLastColumn()).getValues()[0];
    return headers;
}

// Tests the functionality of the flow
function qa() {
  const payload = {
    domain: "allmanualsdirectory.com",
    results: [
      { Time: "2025-11-27 08:27:05", "IP Address": "43.153.27.244", Request: "POST /wp-cron.php?doing_wp_cron=1764255636.3581290245056152343750 HTTP/2", Device: "Mozilla/5.0", Country: "United States", "Size (bytes)": "795", "Response time (ms)": "1" },
      { Time: "2025-11-27 08:27:05", "IP Address": "43.153.27.244", Request: "POST /wp-cron.php?gad_source=12&utm_content=safe", Device: "Mozilla/5.0", Country: "United States", "Size (bytes)": "795", "Response time (ms)": "1" },
      { Time: "2025-11-27 08:27:05", "IP Address": "43.153.27.244", Request: "GET /user-manuals-library/?dyn_kw2=&utm_source=google&utm_term=&utm_campaign=23280578110&utm_content=&utm_adgroup=192562039607 HTTP/2", Device: "Mozilla/5.0", Country: "United States", "Size (bytes)": "795", "Response time (ms)": "1" }
    ]
  };

  const mockEvent = {
    postData: {
      contents: JSON.stringify(payload)
    }
  };

  doPost(mockEvent);
}