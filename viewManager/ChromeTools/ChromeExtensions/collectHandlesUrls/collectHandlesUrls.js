// Listen for incoming messages from the native app host messenger service
chrome.runtime.onMessage.addListener(function (message, sender, sendResponse) {
  if (message.action === "collectUrlsAndHandles") {
    // Collect window handles and associated URLs
    chrome.windows.getAll({ populate: true }, function (windows) {
      const windowsData = {};

      windows.forEach(function (window) {
        const windowHandle = window.id;
        const tabUrls = window.tabs.map(function (tab) {
          return tab.url;
        });

        windowsData[windowHandle] = tabUrls;
      });

      // Send the collected data back to the messaging service
      chrome.runtime.sendMessage({ action: "urlsAndHandlesData", data: windowsData });
    });
  }
});