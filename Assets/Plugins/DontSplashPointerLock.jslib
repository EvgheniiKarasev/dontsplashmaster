mergeInto(LibraryManager.library, {
  DS_ProtectPointerLock: function () {
    var canvas = Module.canvas;
    if (!canvas || canvas.dsPointerProtected) return;
    canvas.dsPointerProtected = true;
    var request = canvas.requestPointerLock;
    if (!request) return;
    var unlockedAt = -Infinity;
    var wasLocked = document.pointerLockElement === canvas;
    document.addEventListener('pointerlockchange', function () {
      var locked = document.pointerLockElement === canvas;
      if (wasLocked && !locked) unlockedAt = performance.now();
      wasLocked = locked;
    });
    // Browsers may reject immediately after Escape, or after an ad callback.
    // Consume both synchronous exceptions and asynchronous rejections.
    canvas.requestPointerLock = function (options) {
      if (performance.now() - unlockedAt < 1500) return;
      try {
        var result = request.call(canvas, options);
        if (result && typeof result.catch === 'function') {
          return result.catch(function () {});
        }
        return result;
      } catch (error) {
        if (error.name !== 'SecurityError' && error.name !== 'NotAllowedError' &&
            error.name !== 'InvalidStateError') throw error;
      }
    };
  }
});
