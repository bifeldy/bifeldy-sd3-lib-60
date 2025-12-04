// swagger-ndjson.js (internal store + execute patch)
(function () {
    "use strict";

    /** Detect escaped JSON or NDJSON */
    function looksEscaped(s) {
        return (
            typeof s === "string" &&
            (s.includes('\\"') || s.includes("\\n") || /^".*"$/.test(s))
        );
    }

    /** Unescape NDJSON */
    function unescapeNdjson(s) {
        if (!s || typeof s !== "string") return s;

        // strip wrapping quotes
        if (
            (s.startsWith('"') && s.endsWith('"')) ||
            (s.startsWith("'") && s.endsWith("'"))
        ) {
            s = s.substring(1, s.length - 1);
        }

        return s
            .replace(/\\"/g, '"')
            .replace(/\\n/g, "\n")
            .replace(/\\r/g, "\r");
    }

    /** Patch Swagger UI’s internal system BEFORE textarea gets filled */
    function patchSwaggerSystem() {
        const proto = SwaggerUIBundle.prototype;

        if (!proto.getSystem || proto._ndjsonPatched) return;
        proto._ndjsonPatched = true;

        const originalGetSystem = proto.getSystem;

        proto.getSystem = function () {
            const system = originalGetSystem.apply(this, arguments);

            if (!system || !system.fn || !system.fn.operations) return system;

            /** Patch request examples */
            const origExampleFn = system.fn.operations.getRequestExample;
            system.fn.operations.getRequestExample = function (...args) {
                const result = origExampleFn.apply(this, args);
                if (typeof result === "string" && looksEscaped(result)) {
                    return unescapeNdjson(result);
                }
                return result;
            };

            return system;
        };
    }

    /** ⭐ NEW ⭐ Patch execute() so Swagger sends correct NDJSON */
    function patchExecute() {
        function tryPatch() {
            const system = window.ui && window.ui.getSystem && window.ui.getSystem();
            if (!system || !system.fn || !system.fn.fetch) {
                return setTimeout(tryPatch, 50);
            }

            const originalFetch = system.fn.fetch;

            system.fn.fetch = function (req) {
                if (req && req.body && looksEscaped(req.body)) {
                    req.body = unescapeNdjson(req.body);
                }
                return originalFetch(req);
            };
        }

        tryPatch();
    }

    /** Backup DOM patch (fix any missed render paths) */
    function patchDomDisplays() {
        function fixTextareas() {
            document.querySelectorAll(".opblock textarea").forEach((el) => {
                const raw = el.value || "";
                if (!looksEscaped(raw)) return;
                const fixed = unescapeNdjson(raw);
                if (el.value !== fixed) {
                    el.value = fixed;

                    // update Swagger internal state
                    el.dispatchEvent(new Event("input", { bubbles: true }));
                    el.dispatchEvent(new Event("change", { bubbles: true }));
                }
            });
        }

        function fixExamples() {
            document
                .querySelectorAll(
                    ".opblock .example pre, .opblock .example code, pre > code"
                )
                .forEach((el) => {
                    const raw = el.textContent || "";
                    if (!looksEscaped(raw)) return;
                    el.textContent = unescapeNdjson(raw);
                });
        }

        new MutationObserver(() => {
            requestAnimationFrame(() => {
                fixExamples();
                fixTextareas();
            });
        }).observe(document.body, { childList: true, subtree: true });
    }

    /** Wait until Swagger UI is loaded */
    (function wait() {
        if (window.SwaggerUIBundle && window.ui) {
            patchSwaggerSystem();
            patchExecute();        // ⭐ important
            patchDomDisplays();
        } else {
            setTimeout(wait, 50);
        }
    })();
})();
