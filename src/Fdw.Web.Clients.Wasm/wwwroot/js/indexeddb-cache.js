/**
 * FractalDataWorks IndexedDB browser cache.
 * DB name: fdw-cache, object store: responses.
 * Each entry: { key, data (JSON string), etag, timestamp }.
 * LRU eviction when cache exceeds 100 entries.
 */
(function () {
    "use strict";

    const DB_NAME = "fdw-cache";
    const DB_VERSION = 1;
    const STORE_NAME = "responses";
    const MAX_ENTRIES = 100;

    let dbPromise = null;

    /**
     * Opens or creates the IndexedDB database.
     * @returns {Promise<IDBDatabase>}
     */
    function openDb() {
        if (dbPromise) {
            return dbPromise;
        }

        dbPromise = new Promise(function (resolve, reject) {
            const request = indexedDB.open(DB_NAME, DB_VERSION);

            request.onupgradeneeded = function (event) {
                const db = event.target.result;
                if (!db.objectStoreNames.contains(STORE_NAME)) {
                    db.createObjectStore(STORE_NAME, { keyPath: "key" });
                }
            };

            request.onsuccess = function (event) {
                resolve(event.target.result);
            };

            request.onerror = function (event) {
                dbPromise = null;
                reject(new Error("IndexedDB open failed: " + event.target.error));
            };
        });

        return dbPromise;
    }

    /**
     * Retrieves a cached entry by key.
     * @param {string} cacheKey
     * @returns {Promise<string|null>} JSON string of the entry, or null.
     */
    async function get(cacheKey) {
        const db = await openDb();
        return new Promise(function (resolve, reject) {
            const tx = db.transaction(STORE_NAME, "readwrite");
            const store = tx.objectStore(STORE_NAME);
            const request = store.get(cacheKey);

            request.onsuccess = function () {
                const result = request.result;
                if (!result) {
                    resolve(null);
                    return;
                }
                // Update timestamp for LRU tracking
                result.timestamp = new Date().toISOString();
                store.put(result);
                resolve(JSON.stringify(result));
            };

            request.onerror = function () {
                reject(new Error("IndexedDB get failed: " + request.error));
            };
        });
    }

    /**
     * Stores a cache entry, with LRU eviction if needed.
     * @param {string} cacheKey
     * @param {string} entryJson - JSON string of the cache entry.
     * @returns {Promise<void>}
     */
    async function set(cacheKey, entryJson) {
        const db = await openDb();
        const entry = JSON.parse(entryJson);
        entry.key = cacheKey;

        return new Promise(function (resolve, reject) {
            const tx = db.transaction(STORE_NAME, "readwrite");
            const store = tx.objectStore(STORE_NAME);

            // Count existing entries
            const countRequest = store.count();

            countRequest.onsuccess = function () {
                const count = countRequest.result;

                if (count >= MAX_ENTRIES) {
                    // LRU eviction: get all entries, sort by timestamp, delete oldest
                    const getAllRequest = store.getAll();

                    getAllRequest.onsuccess = function () {
                        const entries = getAllRequest.result;
                        entries.sort(function (a, b) {
                            const timeA = a.timestamp || "";
                            const timeB = b.timestamp || "";
                            if (timeA < timeB) return -1;
                            if (timeA > timeB) return 1;
                            return 0;
                        });

                        // Delete oldest entries to make room
                        const toDelete = entries.slice(0, count - MAX_ENTRIES + 1);
                        for (let i = 0; i < toDelete.length; i++) {
                            store.delete(toDelete[i].key);
                        }

                        store.put(entry);
                    };

                    getAllRequest.onerror = function () {
                        // Even if count fails, try to insert
                        store.put(entry);
                    };
                } else {
                    store.put(entry);
                }
            };

            countRequest.onerror = function () {
                // Even if count fails, try to insert
                store.put(entry);
            };

            tx.oncomplete = function () {
                resolve();
            };

            tx.onerror = function () {
                reject(new Error("IndexedDB set failed: " + tx.error));
            };
        });
    }

    /**
     * Removes a cached entry by key.
     * @param {string} cacheKey
     * @returns {Promise<void>}
     */
    async function invalidate(cacheKey) {
        const db = await openDb();
        return new Promise(function (resolve, reject) {
            const tx = db.transaction(STORE_NAME, "readwrite");
            const store = tx.objectStore(STORE_NAME);
            store.delete(cacheKey);

            tx.oncomplete = function () {
                resolve();
            };

            tx.onerror = function () {
                reject(new Error("IndexedDB invalidate failed: " + tx.error));
            };
        });
    }

    /**
     * Clears all cached entries.
     * @returns {Promise<void>}
     */
    async function clear() {
        const db = await openDb();
        return new Promise(function (resolve, reject) {
            const tx = db.transaction(STORE_NAME, "readwrite");
            const store = tx.objectStore(STORE_NAME);
            store.clear();

            tx.oncomplete = function () {
                resolve();
            };

            tx.onerror = function () {
                reject(new Error("IndexedDB clear failed: " + tx.error));
            };
        });
    }

    // Expose on window for JS interop
    window.fdwCache = {
        get: get,
        set: set,
        invalidate: invalidate,
        clear: clear
    };
})();
