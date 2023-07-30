// Initialize the handlesToUrls object to store window handles and their corresponding tabs
let handlesToUrls = {};
let scheduleAnUpdate = false;
let updateInterval = 1000; //milliseconds
let lastUpdateTime = Date.now();
let port = null; // Variable to store the port for communication with the Native Messaging Host

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
    // Use chrome.runtime.connectNative to open a port
    port = chrome.runtime.connectNative('com.pairofdice.vieworganizer');

    // Add an onDisconnect listener to handle when the port is closed
    port.onDisconnect.addListener(() => {
        port = null; // Reset the port variable when the port is closed
    });
}

function stopInterval() {
    clearInterval(iObject);
}

self.addEventListener('extMessage', (event) => {
    // Respond to messages sent from other parts of the extension
    if (event.data && event.data.type === 'connect') {
        connectToNativeHost();
    }
    if (event.data && event.data.type === 'schedule') {
        scheduleAnUpdateNow();
    }
    if (event.data && event.data.type === 'check') {
        checkForActivation();
    }
});