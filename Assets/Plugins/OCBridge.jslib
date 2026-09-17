mergeInto(LibraryManager.library, {
InitFirstTime: function () {
    window.initFirstTime();
},


  GetProcessQuest: function (gameObjPtr, methodPtr) {
    const gameObj = UTF8ToString(gameObjPtr);
    const method = UTF8ToString(methodPtr);
 window.getProcessQuest()
        .then(result => {
          const json = JSON.stringify(result);
          SendMessage(gameObj, method, json);
        })
        .catch(err => {
          SendMessage(gameObj, method, JSON.stringify({ error: err.toString() }));
        });
  },

  GetClaimQuest: function (gameObjPtr, methodPtr) {
    const gameObj = UTF8ToString(gameObjPtr);
    const method = UTF8ToString(methodPtr);

      window.getClaimQuest()
        .then(result => {
          const json = JSON.stringify(result);
          SendMessage(gameObj, method, json);
        })
        .catch(err => {
          SendMessage(gameObj, method, JSON.stringify({ error: err.toString() }));
        });
  },

  SetGameData: function (jsonPtr) {
    const jsonStr = UTF8ToString(jsonPtr);

      const data = JSON.parse(jsonStr);
        window.setGameData(data);
  },

  GetGameData: function (gameObjPtr, methodPtr) {
    const gameObj = UTF8ToString(gameObjPtr);
    const method = UTF8ToString(methodPtr);

      const result = window.getGameData();
      const json = JSON.stringify(result);
  }
});
