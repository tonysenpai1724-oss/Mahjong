var plugin = {
  sum: function (a, b) {
    return a + b;
  },
  IsMobile: function () {
    var userAgent = navigator.userAgent + navigator.vendor + (window.opera || "");
    console.log(userAgent);
    return /android|iphone|ipad|ipod|blackberry|iemobile|opera mini/i.test(userAgent) ? 1 : 0;
  },
    PrivyInitModule: function (objectNamePtr, methodNamePtr) {
    const objectName = typeof UTF8ToString !== "undefined" ? UTF8ToString(objectNamePtr) : objectNamePtr;
    const methodName = typeof UTF8ToString !== "undefined" ? UTF8ToString(methodNamePtr) : methodNamePtr;

    console.log("InitModule: Listening to events");

    // Define a helper function to send messages to Unity
    function sendMessageToUnity(eventName, eventData) {
      const data = JSON.stringify({ event: eventName, data: eventData });
      if (typeof Module !== "undefined" && typeof Module.SendMessage === "function") {
        Module.SendMessage(objectName, methodName, data);
      } else {
        console.warn("Module.SendMessage is not available");
      }
    }

    // Listen to "login" event
    window.addEventListener("login", function (event) {
      console.log("Login event received:", event.data);
      sendMessageToUnity("login", event.data);
    });

    // Listen to "logout" event
    window.addEventListener("logout", function (event) {
      console.log("Logout event received:", event.data);
      sendMessageToUnity("logout", event.data);
    });

    // Listen to "accountinfo" event
    window.addEventListener("accountinfo", function (event) {
      console.log("AccountInfo event received:", event.data);
      sendMessageToUnity("accountinfo", event.data);
    });
    
    try {
      console.log("dispatchReactUnityEvent: InitListener");
      window.dispatchReactUnityEvent("InitListener", objectName);
    } catch (e) {
      console.warn("Failed to dispatch event InitListener");
    }
    try {
      console.log("dispatchReactUnityEvent: CheckLogin");
      window.dispatchReactUnityEvent("CheckLogin");
    } catch (e) {
      console.warn("Failed to dispatch event CheckLogin");
    }
  },
  PrivyLogin: function () {
    try {
      console.log("dispatchReactUnityEvent: Login");
      window.dispatchReactUnityEvent("Login", "");
    } catch (e) {
      console.warn("Failed to dispatch event");
    }
  },

  PrivyLogout: function () {
    try {
      console.log("dispatchReactUnityEvent: Logout");
      window.dispatchReactUnityEvent("Logout", "");
    } catch (e) {
      console.warn("Failed to dispatch event");
    }
  },


  PrivyAccountInfo: function () {
    try {
      console.log("dispatchReactUnityEvent: AccountInfo");
      window.dispatchReactUnityEvent("AccountInfo", "");
    } catch (e) {
      console.warn("Failed to dispatch event");
    }
  },

  PrivyExportWallet: function () {
    try {
      console.log("dispatchReactUnityEvent: ExportWallet");
      window.dispatchReactUnityEvent("ExportWallet", "");
    } catch (e) {
      console.warn("Failed to dispatch event");
    }
  },

  PrivyCallContractActionLog: function () {
    try {
      console.log("dispatchReactUnityEvent: CallContractActionLog");
      window.dispatchReactUnityEvent("CallContractActionLog", "");
    } catch (e) {
      console.warn("Failed to dispatch event");
    }
  },
  ListenToResize: function (objectNamePtr, methodNamePtr) {
    const objectName = typeof UTF8ToString !== "undefined" ? UTF8ToString(objectNamePtr) : objectNamePtr;
    const methodName = typeof UTF8ToString !== "undefined" ? UTF8ToString(methodNamePtr) : methodNamePtr;
    console.log("ListenToResize");
    window.addEventListener("resize", function () {
      console.log("Window resized:", window.innerWidth, window.innerHeight);
      const screenSize = JSON.stringify({ width: window.innerWidth, height: window.innerHeight });

      if (typeof Module !== "undefined" && typeof Module.SendMessage === "function") {
        Module.SendMessage(objectName, methodName, screenSize);
      } else {
        console.warn("Module.SendMessage is not available");
      }
    });
  },
  CheckRotateScreen: function () {
    try {
      console.log("dispatchReactUnityEvent: CheckRotateScreen");
      if (typeof window.dispatchReactUnityEvent === "function") {
        window.dispatchReactUnityEvent("CheckRotateScreen", "");
      }
    } catch (e) {
      console.warn("Failed to dispatch event", e);
    }
  },
};

mergeInto(LibraryManager.library, plugin);
