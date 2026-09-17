// Assets/Plugins/ClipboardPlugin.jslib
mergeInto(LibraryManager.library, {
    CopyToClipboard: function(text) {
        var str = UTF8ToString(text); // Chuyển đổi từ C-style string
        navigator.clipboard.writeText(str).then(function() {
            console.log("Text copied to clipboard: " + str);
        }).catch(function(error) {
            console.error("Copy failed: ", error);
        });
    }
});