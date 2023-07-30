// Initialize the handlesToUrls object to store window handles and their corresponding tabs
self.handlesToUrls = {};
self.scheduleAnUpdate = false;
self.updateInterval = 1000; //milliseconds
self.lastUpdateTime = Date.now();
self.port = null; // Variable to store the port for communication with the Native Messaging Host

// Function to update handlesToUrls object and send it to the native messaging host
function updateHandlesToUrls() {
    // Collect window handles and associated URLs
    chrome.windows.getAll({ populate: true }, function (windows) {
        windows.forEach(function (window) {
            const windowHandle = window.id;
            const tabUrls = window.tabs.map(function (tab) {
                return tab.url;
            });

            handlesToUrls[windowHandle] = tabUrls;
        });
    });

    // Send the updated handlesToUrls object to the native messaging host
    sendMessageToNativeHost(handlesToUrls);
    scheduleAnUpdate = false;
    lastUpdateTime = Date.now();

}

function checkForActivation() {
    // Update the lastUpdateTime with the current time when scheduleAnUpdate is true
    if (scheduleAnUpdate) {
        const currentTime = Date.now();
        const timeDifference = currentTime - lastUpdateTime;

        if (timeDifference >= updateInterval) {
            updateHandlesToUrls();
        }
    }
}

function scheduleAnUpdateNow() {
    scheduleAnUpdate = true;
}

// Function to send a message to the native messaging host
function sendMessageToNativeHost(handlesToUrls) {
    // Use the chrome.runtime.sendNativeMessage() method to send the message to the host
    console.log("Sending data as reply...");
    port.postMessage(
        { action: "urlsAndHandlesData", data: handlesToUrls }
    );
    console.log("data reply sent...");
}

// Event listeners to track changes in URLs, tabs, and windows


// Function to connect to the Native Messaging Host (mailman)
function connectToNativeHost() {
    console.log("Connecting to Native Messaging Host...");
    // Use chrome.runtime.connectNative to open a port
    port = chrome.runtime.connectNative('com.pairofdice.vieworganizer');

    // Add an onDisconnect listener to handle when the port is closed
    port.onDisconnect.addListener(() => {
        port = null; // Reset the port variable when the port is closed
    });
    console.log("Connected to Native Messaging Host...");
}

function stopInterval() {
    clearInterval(iObject);
}

chrome.tabs.onCreated.addListener(scheduleAnUpdateNow());
chrome.tabs.onUpdated.addListener(scheduleAnUpdateNow());
chrome.tabs.onRemoved.addListener(scheduleAnUpdateNow());
chrome.windows.onCreated.addListener(scheduleAnUpdateNow());
chrome.windows.onRemoved.addListener(scheduleAnUpdateNow());

// Set initial alarm after extension installation/refresh
chrome.runtime.onInstalled.addListener(() => {
    console.log("Creating alarm");
    chrome.alarms.create('myAlarm', { delayInMinutes: 0.5, periodInMinutes: 1.2 });
});

// Add alarm event listener
chrome.alarms.onAlarm.addListener((alarm) => {
    console.log("Alarm triggered. Checking for a scheduled send");
    if (alarm.name === 'myAlarm') {
        checkForActivation();
        // Schedule the next alarm
        const randomDelay = Math.random() * 2.5 + 1.2; // Random delay between 0.5 and 3 seconds
        console.log("Recreating alarm");
        chrome.alarms.create('myAlarm', { delayInMinutes: randomDelay / 60, periodInMinutes: 1.2 });
    }
});

// Call the connectToNativeHost function when the extension is loaded
connectToNativeHost();

// Call updateHandlesToUrls() function when the extension is loaded and launched
scheduleAnUpdateNow();