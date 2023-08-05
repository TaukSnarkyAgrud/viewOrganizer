// Initialize the handlesToUrls object to store window handles and their corresponding tabs
self.debounceTimeout = null
self.debounceDelay = 2500; //milliseconds
self.port = null; // Variable to store the port for communication with the Native Messaging Host
self.isHostReady = false;

async function updateHandlesToUrls() {
    const tabsList = [];
    // Collect window handles and associated URLs
    const windowList = await new Promise((resolve, reject) => {
        chrome.windows.getAll({ populate: true }, (windows) => {
            let windowListM = []
            windows.forEach((window) => {
                windowListM.push(window.id)
            });
            resolve(windowListM)
        });
    });
    for (const windowId of windowList) {
        console.log(`Window ID: ${windowId}`);
        const tabsListPerWindow = await new Promise((resolve, reject) => {
            chrome.tabs.query({ windowId: windowId }, (tabs) => {
                console.log(`  Tabs count: ${tabs.length}`);
                resolve(tabs)
            });
        });
        tabsListPerWindow.forEach(m => tabsList.push(m))
    }
    logWindowData(tabsList);

    // Send the updated handlesToUrls object to the native messaging host
    sendMessageToNativeHost(JSON.stringify(tabsList));
}

function logWindowData(tabs) {
    console.log("Object log start \\/ \\/ \\/ \\/")
    for (const tab of tabs) {
        console.log(`  Tab ID: ${tab.id}`);
        console.log(`  Tab URL: ${tab.url}`);
        console.log(`  Tab window: ${tab.windowId}`);
    }
    console.log("Object log End /\\ /\\ /\\ /\\ ")
}

function scheduleAnUpdateNow() {
    console.log("Changes observed. Debounced action Scheduled.");
    // Clear any previously scheduled action
    clearTimeout(debounceTimeout);

    // Schedule a new action to be performed after the debounceDelay
    debounceTimeout = setTimeout(() => {
        updateHandlesToUrls();
    }, debounceDelay);
}

// Function to send a message to the native messaging host
function sendMessageToNativeHost(handlesToUrls) {
    // Send the message
    console.log("Sending update...");
    port.postMessage(
        { action: "urlsAndHandlesData", data: handlesToUrls }
    );
    console.log("Update sent...");
}

// Function to send a heartbeat message to the native messaging host
function sendHeartbeatMessageToNativeHost() {
    // Send the message
    console.log("Sending heartbeat...");
    port.postMessage(
        { action: "heartbeat" }
    );
    console.log("Heartbeat sent...");
}

// Event listeners to track changes in URLs, tabs, and windows


// Function to connect to the Native Messaging Host (mailman)
function connectToNativeHost() {
    console.log("Connecting to Native Messaging Host...");
    // Use chrome.runtime.connectNative to open a port
    port = chrome.runtime.connectNative('com.pairofdice.vieworganizer');

    // Add an onDisconnect listener to handle when the port is closed
    port.onDisconnect.addListener(() => {
        console.log("Disconnect detected...");
        port = null; // Reset the port variable when the port is closed
    });
    console.log("Connected to Native Messaging Host...");// Listen for read message


    console.log("Listening For Ready...");
    port.onMessage.addListener(function (message) {
        let parsedMessage = JSON.parse(message);
        if (parsedMessage.type === "ready") {
            // Set the variable to true when the "ready" message is received
            isHostReady = true;
            console.log("Native messaging host is ready.");
        } else {
            // Handle other message types if needed
            console.log("Received message:", parsedMessage);
        }
    });
}

chrome.tabs.onCreated.addListener((tabObject) => {
    console.log("Tab creation observed...");
    scheduleAnUpdateNow();
});
chrome.tabs.onRemoved.addListener((id, removeObject) => {
    console.log("Tab removal observed...");
    scheduleAnUpdateNow();
});
chrome.tabs.onUpdated.addListener((id, changeInfo, tabObject) => {
    console.log("Tab update observed...");
    scheduleAnUpdateNow();
});
chrome.tabs.onDetached.addListener((id, detachObject) => {
    console.log("Tab detached observed...");
    scheduleAnUpdateNow();
});
chrome.tabs.onAttached.addListener((id, attachObject) => {
    console.log("Tab attached observed...");
    scheduleAnUpdateNow();
});
chrome.windows.onCreated.addListener((windowObject) => {
    console.log("Window creation observed...");
    scheduleAnUpdateNow();
});
chrome.windows.onRemoved.addListener((windowId) => {
    console.log("Window removal observed...");
    scheduleAnUpdateNow();
});
// TODO: pass window state to manager and use this to help with mgmt https://developer.chrome.com/docs/extensions/reference/windows/#type-WindowState

// Call the connectToNativeHost function when the extension is loaded
connectToNativeHost();

// Call updateHandlesToUrls() function when the extension is loaded and launched
scheduleAnUpdateNow();