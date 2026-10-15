// File previews on the public share page.
(function () {
    "use strict";

    const MAX_TEXT_PREVIEW_BYTES = 1024 * 1024;
    const dialog = document.getElementById("preview-dialog");
    if (!dialog) return;

    const body = dialog.querySelector("[data-preview-body]");
    const title = dialog.querySelector("[data-preview-title]");

    function previewKind(contentType, size) {
        if (contentType === "image/svg+xml") return null;
        if (contentType.startsWith("image/")) return "image";
        if (contentType.startsWith("video/")) return "video";
        if (contentType.startsWith("audio/")) return "audio";
        if (contentType === "application/pdf") return "pdf";
        const isText = contentType.startsWith("text/") ||
            ["application/json", "application/xml", "application/javascript"].includes(contentType);
        return isText && size <= MAX_TEXT_PREVIEW_BYTES ? "text" : null;
    }

    async function render(kind, url) {
        body.replaceChildren();
        let element;
        switch (kind) {
            case "image":
                element = document.createElement("img");
                element.src = url;
                element.alt = title.textContent;
                break;
            case "video":
            case "audio":
                element = document.createElement(kind);
                element.src = url;
                element.controls = true;
                break;
            case "pdf":
                element = document.createElement("iframe");
                element.src = url;
                break;
            case "text": {
                element = document.createElement("pre");
                element.textContent = "Loading…";
                const response = await fetch(url);
                // textContent never interprets the file as HTML.
                element.textContent = response.ok ? await response.text() : "The preview could not be loaded.";
                break;
            }
        }
        body.appendChild(element);
    }

    document.querySelectorAll("[data-preview]").forEach((button) => {
        const kind = previewKind(button.dataset.previewType, Number(button.dataset.previewSize));
        if (!kind) return;
        button.hidden = false;
        button.addEventListener("click", () => {
            title.textContent = button.dataset.previewName;
            dialog.showModal();
            render(kind, button.dataset.preview);
        });
    });

    // Stop media playback when the dialog closes.
    dialog.addEventListener("close", () => body.replaceChildren());
})();
