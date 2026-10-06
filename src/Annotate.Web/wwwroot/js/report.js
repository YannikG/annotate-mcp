(function () {
  function download(html, fileName) {
    try {
      const blob = new Blob([html], { type: "text/html;charset=utf-8" });
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = fileName;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
      return null;
    } catch (error) {
      return error && error.message ? error.message : String(error);
    }
  }

  function close() {
    window.close();
  }

  window.annotateReport = { download: download, close: close };
})();
