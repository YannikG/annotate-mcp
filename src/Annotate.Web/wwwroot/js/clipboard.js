(function () {
  async function copy(text) {
    try {
      await navigator.clipboard.writeText(text);
      return null;
    } catch (error) {
      return error && error.message ? error.message : String(error);
    }
  }

  window.annotateClipboard = { copy: copy };
})();
