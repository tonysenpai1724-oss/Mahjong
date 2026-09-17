var TelegramPlugin = {
    // Khởi tạo Telegram SDK
    Telegram_InitSDK: function () {
        if (typeof window.telegramApps !== 'undefined' && typeof window.telegramApps.sdk !== 'undefined') {
            window.telegramApps.sdk.init();
            console.log('Telegram SDK initialized.');
        } else {
            console.error('Telegram SDK failed to load.');
        }
    },

    // Gửi dữ liệu tới bot
    Telegram_SendData: function (dataPtr) {
        var data = UTF8ToString(dataPtr);
        if (window.telegramApps && window.telegramApps.sdk) {
            window.telegramApps.sdk.sendData(data);
            console.log('Data sent to bot:', data);
        }
    },

    // Lắng nghe sự kiện từ Telegram Web App
    Telegram_OnEvent: function (eventPtr, gameObjectPtr, methodNamePtr) {
        var event = UTF8ToString(eventPtr);
        var gameObjectName = UTF8ToString(gameObjectPtr);
        var methodName = UTF8ToString(methodNamePtr);

        if (window.telegramApps && window.telegramApps.sdk) {
            window.telegramApps.sdk.onEvent(event, function () {
                // Gọi hàm trong Unity
                unityInstance.SendMessage(gameObjectName, methodName, event);
                console.log('Event received and sent to Unity:', event);
            });
        }
    },

    // Đóng Web App
    Telegram_Close: function () {
        if (window.telegramApps && window.telegramApps.sdk) {
            window.telegramApps.sdk.close();
            console.log('Web App closed.');
        }
    },

    // Thiết lập màu tiêu đề
    Telegram_SetHeaderColor: function (colorPtr) {
        var color = UTF8ToString(colorPtr);
        if (window.telegramApps && window.telegramApps.sdk) {
            window.telegramApps.sdk.setHeaderColor(color);
            console.log('Header color set to:', color);
        }
    },

    // Tùy chỉnh nút chính
    Telegram_SetMainButton: function (textPtr, isVisible) {
        var text = UTF8ToString(textPtr);
        if (window.telegramApps && window.telegramApps.sdk) {
            window.telegramApps.sdk.MainButton.setText(text);
            if (isVisible) {
                window.telegramApps.sdk.MainButton.show();
            } else {
                window.telegramApps.sdk.MainButton.hide();
            }
            console.log('Main button updated.');
        }
    },

    // Kích hoạt phản hồi rung trên thiết bị điện thoại 
    Telegram_HapticFeedback: function (typePtr) {
        var type = UTF8ToString(typePtr);
        if (window.telegramApps && window.telegramApps.sdk && window.telegramApps.sdk.HapticFeedback) {
            window.telegramApps.sdk.HapticFeedback.impactOccurred(type);
            console.log('Haptic feedback occurred with type:', type);
        }
    },
    // Kiểm tra xem Invoice có được hỗ trợ không
    Telegram_IsInvoiceSupported: function () {
        if (typeof window.telegramApps.sdk !== 'undefined' && window.telegramApps.sdk.invoice) {
            console.log('Invoice is supported.');
            return 1; // Hỗ trợ
        }
        console.error('Invoice is not supported.');
        return 0; // Không hỗ trợ
    },


    // Hàm mở hóa đơn invoice
    Telegram_OpenInvoice: function (urlPtr) {
        var url = UTF8ToString(urlPtr);

        if (typeof window.telegramApps.sdk !== 'undefined' && window.telegramApps.sdk.invoice) {
            // Sử dụng async/await để đơn giản hóa mã
            (async function () {
                try {
                    var result = await window.telegramApps.sdk.invoice.open(url, 'url');
                    console.log('Invoice result:', result);
                } catch (error) {
                    console.error('Invoice error:', error);
                }
            })();
        } else {
            console.error('Telegram invoice functionality is not available');
        }
    },



    // Khởi tạo utils
    Telegram_InitUtils: function () {
        if ( typeof window.telegramApps !== 'undefined' && typeof window.telegramApps.sdk !== 'undefined') {
            window.utils = window.telegramApps.sdk.initUtils();
            console.log('Utils initialized.');
            return 1; // Thành công
        } else {
            console.error('Utils initialization failed.');
            return 0; // Thất bại
        }
    },

    // Hàm mở liên kết
    Telegram_OpenLink: function (urlPtr) {
        var url = UTF8ToString(urlPtr);
        if (window.telegramApps.sdk) {
            window.telegramApps.sdk.openLink(url);
            console.log('Opened link:', url);
        } else {
            console.error('Utils is not initialized.');
        }
    },

    // Hàm mở liên kết Telegram
    Telegram_OpenTelegramLink: function (urlPtr) {
        var url = UTF8ToString(urlPtr);
        if (window.telegramApps.sdk) {
            window.telegramApps.sdk.openTelegramLink(url);
            console.log('Opened Telegram link:', url);
        } else {
            console.error('Utils is not initialized.');
        }
    },

    // Hàm chia sẻ URL
    Telegram_ShareURL: function (urlPtr, textPtr) {
        var url = UTF8ToString(urlPtr);
        var text = UTF8ToString(textPtr);
        if (window.telegramApps.sdk) {
            window.telegramApps.sdk.shareURL(text, url);
            console.log('Shared URL:', url);
        } else {
            console.error('Utils is not initialized.');
        }
    },

    // Hàm chia sẻ Message
    Telegram_ShareMessage: function (textPtr) {
        var text = UTF8ToString(textPtr);
        if (window.telegramApps.sdk) {
            window.telegramApps.sdk.shareMessage(text);
            console.log('Shared shareMessage:', text);
        } else {
            console.error('Utils is not initialized.');
        }
    },

    // Kiểm tra xem Popup có được hỗ trợ không
    Telegram_IsPopupSupported: function () {
        if (typeof window.telegramApps.sdk !== 'undefined' && window.telegramApps.sdk.popup) {
            return window.telegramApps.sdk.popup.isSupported() ? 1 : 0;
        }
        return 0;
    },

    // Kiểm tra xem Popup có đang mở không
    Telegram_IsPopupOpened: function () {
        if (typeof window.telegramApps.sdk !== 'undefined' && window.telegramApps.sdk.popup) {
            return window.telegramApps.sdk.popup.isOpened() ? 1 : 0;
        }
        return 0;
    },

    // Mở Popup
    Telegram_OpenPopup: function (titlePtr, messagePtr, buttonsPtr, gameObjectNamePtr, callbackFnPtr) {
        if (typeof window.telegramApps.sdk === 'undefined' || !window.telegramApps.sdk.popup || !window.telegramApps.sdk.popup.open.isAvailable()) {
            console.error('Popup is not available');
            return;
        }

        var title = UTF8ToString(titlePtr);
        var message = UTF8ToString(messagePtr);
        var buttonsJSON = UTF8ToString(buttonsPtr);
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackFn = UTF8ToString(callbackFnPtr);

        try {
            var buttons = JSON.parse(buttonsJSON);

            window.telegramApps.sdk.popup.open({
                title: title,
                message: message,
                buttons: buttons
            }).then(function (buttonId) {
                // Gọi callback trong Unity với kết quả
                var result = buttonId || "null";
                unityInstance.SendMessage(gameObjectName, callbackFn, result);
            }).catch(function (error) {
                console.error('Popup error:', error);
                unityInstance.SendMessage(gameObjectName, callbackFn, "error");
            });

        } catch (error) {
            console.error('Error parsing buttons JSON:', error);
        }
    }
};

// Xuất các hàm để Unity có thể gọi
mergeInto(LibraryManager.library, TelegramPlugin);
