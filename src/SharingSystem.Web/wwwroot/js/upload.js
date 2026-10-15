// Chunked file upload used by the upload page ("create" mode) and the edit share page ("edit" mode).
(function () {
    "use strict";

    const MAX_CHUNK_RETRIES = 3;
    const RETRY_DELAY_MS = 1500;
    const ID_LENGTH = 8;
    const ID_ALPHABET = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    const UPLOAD_TOKEN_HEADER = "X-Upload-Token";
    const ANTIFORGERY_HEADER = "RequestVerificationToken";
    const HTTP_SERVER_ERROR = 500;

    const root = document.querySelector("[data-upload-app]");
    if (!root) return;

    const app = window.app;
    const config = {
        mode: root.dataset.mode,
        shareId: root.dataset.shareId || null,
        chunkSize: Number(root.dataset.chunkSize),
        maxSize: Number(root.dataset.maxSize),
        reverseToken: root.dataset.reverseToken || null,
        simplified: root.dataset.simplified === "true"
    };
    const csrfToken = document.querySelector('meta[name="csrf-token"]')?.content ?? "";

    const dropzone = root.querySelector("[data-dropzone]");
    const fileInput = root.querySelector("[data-file-input]");
    const filePanel = root.querySelector("[data-file-panel]");
    const fileList = root.querySelector("[data-file-list]");
    const fileActions = root.querySelector("[data-file-actions]");
    const totalSizeLabel = root.querySelector("[data-total-size]");
    const startButton = root.querySelector("[data-start]");
    const optionsDialog = document.getElementById("share-options");
    const optionsForm = document.querySelector("[data-options-form]");
    const resultDialog = document.getElementById("share-result");

    /** @type {{file: File, row: HTMLTableRowElement, bar?: HTMLElement, status?: HTMLElement}[]} */
    let items = [];
    let uploading = false;

    // ---------- File selection ----------
    dropzone.addEventListener("click", () => !uploading && fileInput.click());
    dropzone.addEventListener("keydown", (event) => {
        if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            fileInput.click();
        }
    });
    fileInput.addEventListener("change", () => {
        addFiles(fileInput.files);
        fileInput.value = "";
    });
    ["dragenter", "dragover"].forEach((name) =>
        dropzone.addEventListener(name, (event) => {
            event.preventDefault();
            dropzone.classList.add("dragover");
        }));
    ["dragleave", "drop"].forEach((name) =>
        dropzone.addEventListener(name, (event) => {
            event.preventDefault();
            dropzone.classList.remove("dragover");
        }));
    dropzone.addEventListener("drop", (event) => addFiles(event.dataTransfer.files));

    function addFiles(fileListLike) {
        if (uploading) return;
        for (const file of fileListLike) {
            const exists = items.some((item) => item.file.name === file.name && item.file.size === file.size);
            if (!exists) items.push({ file, row: createRow(file) });
        }
        render();
    }

    function createRow(file) {
        const row = document.createElement("tr");
        const name = document.createElement("td");
        name.className = "file-name";
        name.innerHTML = '<svg class="icon"><use href="#i-file" /></svg>';
        name.append(document.createTextNode(file.name));

        const size = document.createElement("td");
        size.className = "col-size";
        size.textContent = app.formatBytes(file.size);

        const status = document.createElement("td");
        status.className = "col-status text-right";
        const remove = document.createElement("button");
        remove.type = "button";
        remove.className = "btn btn-ghost btn-icon text-danger";
        remove.title = app.t("Remove");
        remove.innerHTML = '<svg class="icon"><use href="#i-trash" /></svg>';
        remove.addEventListener("click", () => {
            items = items.filter((item) => item.file !== file);
            render();
        });
        status.appendChild(remove);

        row.append(name, size, status);
        return row;
    }

    function totalSize() {
        return items.reduce((sum, item) => sum + item.file.size, 0);
    }

    function render() {
        fileList.replaceChildren(...items.map((item) => item.row));
        const hasFiles = items.length > 0;
        filePanel.hidden = !hasFiles;
        fileActions.hidden = !hasFiles;

        const total = totalSize();
        const tooLarge = total > config.maxSize;
        totalSizeLabel.textContent = app.t("{0} file(s) · {1}", app.formatNumber(items.length), app.formatBytes(total)) +
            (tooLarge ? app.t(" — exceeds the limit of {0}", app.formatBytes(config.maxSize)) : "");
        totalSizeLabel.classList.toggle("text-danger", tooLarge);
        startButton.disabled = !hasFiles || tooLarge || uploading;
    }

    // ---------- HTTP helpers ----------
    async function readError(response) {
        try {
            const data = await response.json();
            if (data.message) return data.message;
            if (data.errors) return Object.values(data.errors).flat().join(" ");
            if (data.title) return data.title;
        } catch { /* not JSON */ }
        return app.t("Request failed ({0})", response.status);
    }

    async function postJson(url, body, uploadToken) {
        const headers = { "Content-Type": "application/json", [ANTIFORGERY_HEADER]: csrfToken };
        if (uploadToken) headers[UPLOAD_TOKEN_HEADER] = uploadToken;
        const response = await fetch(url, { method: "POST", headers, body: body ? JSON.stringify(body) : null });
        if (!response.ok) throw new Error(await readError(response));
        return response.json();
    }

    /** Sends one chunk with XMLHttpRequest to report upload progress. */
    function sendChunk(url, blob, uploadToken, onProgress) {
        return new Promise((resolve, reject) => {
            const request = new XMLHttpRequest();
            request.open("POST", url);
            request.setRequestHeader("Content-Type", "application/octet-stream");
            request.setRequestHeader(ANTIFORGERY_HEADER, csrfToken);
            if (uploadToken) request.setRequestHeader(UPLOAD_TOKEN_HEADER, uploadToken);
            request.upload.onprogress = (event) => event.lengthComputable && onProgress(event.loaded);
            request.onload = () => {
                let data = null;
                try { data = JSON.parse(request.responseText); } catch { /* ignore */ }
                if (request.status >= 200 && request.status < 300) {
                    resolve(data);
                } else {
                    const error = new Error(data?.message || data?.title || app.t("Upload failed ({0})", request.status));
                    error.retryable = request.status >= HTTP_SERVER_ERROR;
                    reject(error);
                }
            };
            request.onerror = () => {
                const error = new Error(app.t("Network error"));
                error.retryable = true;
                reject(error);
            };
            request.send(blob);
        });
    }

    const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

    async function uploadFile(shareId, uploadToken, item) {
        const { file } = item;
        const totalChunks = Math.max(1, Math.ceil(file.size / config.chunkSize));
        let fileId = null;

        for (let index = 0; index < totalChunks; index++) {
            const start = index * config.chunkSize;
            const blob = file.slice(start, start + config.chunkSize);
            const params = new URLSearchParams({ name: file.name, chunkIndex: index, totalChunks });
            if (fileId) params.set("fileId", fileId);
            const url = `/api/shares/${encodeURIComponent(shareId)}/files?${params}`;

            for (let attempt = 0; ; attempt++) {
                try {
                    const result = await sendChunk(url, blob, uploadToken, (loaded) =>
                        setProgress(item, file.size === 0 ? 1 : (start + loaded) / file.size));
                    fileId = result.fileId;
                    break;
                } catch (error) {
                    if (!error.retryable || attempt >= MAX_CHUNK_RETRIES) throw error;
                    await delay(RETRY_DELAY_MS);
                }
            }
        }

        setProgress(item, 1, true);
    }

    function prepareProgress(item) {
        const cell = item.row.lastElementChild;
        cell.innerHTML = '<div class="progress"><div class="progress-bar"></div></div><div class="status-text"></div>';
        item.bar = cell.querySelector(".progress-bar");
        item.status = cell.querySelector(".status-text");
        item.status.textContent = app.t("Waiting…");
    }

    function setProgress(item, fraction, done) {
        const percent = Math.min(100, Math.round(fraction * 100));
        item.bar.style.width = percent + "%";
        item.bar.classList.toggle("done", !!done);
        item.status.textContent = done ? app.t("Done") : app.formatNumber(percent) + "%";
    }

    async function uploadAll(shareId, uploadToken) {
        uploading = true;
        render();
        items.forEach(prepareProgress);
        for (const item of items) {
            try {
                await uploadFile(shareId, uploadToken, item);
            } catch (error) {
                item.bar.classList.add("error");
                item.status.textContent = app.t("Failed");
                throw error;
            }
        }
    }

    // ---------- Share options ----------
    function randomId() {
        const values = crypto.getRandomValues(new Uint32Array(ID_LENGTH));
        return Array.from(values, (value) => ID_ALPHABET[value % ID_ALPHABET.length]).join("");
    }

    function readOptions() {
        const field = (selector) => optionsForm.querySelector(selector);
        const never = field("[data-opt-never]");
        const maxViews = field("[data-opt-max-views]").value;
        const recipients = field("[data-opt-recipients]")?.value ?? "";

        return {
            id: field("[data-opt-id]").value.trim(),
            name: field("[data-opt-name]").value.trim() || null,
            description: field("[data-opt-description]").value.trim() || null,
            password: field("[data-opt-password]").value || null,
            maxViews: maxViews ? Number(maxViews) : null,
            expiration: {
                neverExpires: never ? never.checked : false,
                value: Number(field("[data-opt-exp-value]").value),
                unit: field("[data-opt-exp-unit]").value
            },
            recipients: recipients.split(/[\s,;]+/).filter(Boolean)
        };
    }

    if (optionsForm) {
        const idInput = optionsForm.querySelector("[data-opt-id]");
        const idError = optionsForm.querySelector("[data-id-error]");

        optionsForm.querySelector("[data-generate-id]").addEventListener("click", () => {
            idInput.value = randomId();
            idError.textContent = "";
        });

        idInput.addEventListener("change", async () => {
            idError.textContent = "";
            if (!idInput.checkValidity()) return;
            const response = await fetch(`/api/shares/available?id=${encodeURIComponent(idInput.value.trim())}`);
            if (response.ok && !(await response.json()).isAvailable) {
                idError.textContent = app.t("This link is already in use.");
            }
        });

        optionsForm.addEventListener("submit", (event) => {
            event.preventDefault();
            if (idError.textContent) return;
            optionsDialog.close();
            createAndUpload(readOptions());
        });
    }

    // ---------- Main flow ----------
    async function createAndUpload(options) {
        startButton.disabled = true;
        try {
            const created = await postJson("/api/shares", { ...options, reverseShareToken: config.reverseToken });
            await uploadAll(created.id, created.uploadToken);
            const completed = await postJson(`/api/shares/${encodeURIComponent(created.id)}/complete`, null, created.uploadToken);
            showResult(completed);
        } catch (error) {
            app.toast(error.message, "error");
            uploading = false;
            render();
        }
    }

    function showResult(completed) {
        const isPublic = completed.isPublic && completed.url;
        resultDialog.querySelector("[data-result-public]").hidden = !isPublic;
        resultDialog.querySelector("[data-result-private]").hidden = isPublic;
        const openLink = resultDialog.querySelector("[data-result-open]");
        openLink.hidden = !isPublic;
        if (isPublic) {
            resultDialog.querySelector("[data-result-url]").value = completed.url;
            openLink.href = completed.url;
        }
        resultDialog.querySelector("[data-result-done]").onclick = () => window.location.reload();
        resultDialog.addEventListener("cancel", () => window.location.reload(), { once: true });
        resultDialog.showModal();
    }

    startButton.addEventListener("click", async () => {
        if (config.mode === "edit") {
            startButton.disabled = true;
            try {
                await uploadAll(config.shareId, null);
                window.location.reload();
            } catch (error) {
                app.toast(error.message, "error");
                uploading = false;
                render();
            }
            return;
        }

        if (config.simplified) {
            createAndUpload({ id: null, expiration: { neverExpires: false, value: 1, unit: "Days" }, recipients: [] });
            return;
        }

        optionsDialog.showModal();
    });
})();
