/**
 * IndexedDB Wrapper for Offline Colaboraciones
 * Stores pending colaboraciones when offline using GUID-based IDs
 */

const ColaboracionesOfflineDB = {
    dbName: 'ColaboracionesOfflineDB',
    version: 1,
    storeName: 'colaboraciones',
    
    /**
     * Initialize the database
     */
    async init() {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(this.dbName, this.version);
            
            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve(request.result);
            
            request.onupgradeneeded = (event) => {
                const db = event.target.result;
                
                // Create object store if it doesn't exist
                if (!db.objectStoreNames.contains(this.storeName)) {
                    const objectStore = db.createObjectStore(this.storeName, { 
                        keyPath: 'id' // GUID generated client-side
                    });
                    
                    // Indexes for querying
                    objectStore.createIndex('syncStatus', 'syncStatus', { unique: false });
                    objectStore.createIndex('timestamp', 'timestamp', { unique: false });
                    objectStore.createIndex('updatedAt', 'updatedAt', { unique: false });
                    objectStore.createIndex('miembroId', 'miembroId', { unique: false });
                }
            };
        });
    },
    
    /**
     * Generate a GUID (v4 UUID)
     */
    generateGuid() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c === 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    },
    
    /**
     * Add a new colaboracion to offline queue
     */
    async addColaboracion(colaboracionData) {
        const db = await this.init();
        
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([this.storeName], 'readwrite');
            const store = transaction.objectStore(this.storeName);
            
            // Prepare record with GUID and sync metadata
            const record = {
                id: this.generateGuid(), // GUID generated client-side
                miembroId: colaboracionData.miembroId,
                mesInicial: colaboracionData.mesInicial,
                anioInicial: colaboracionData.anioInicial,
                mesFinal: colaboracionData.mesFinal,
                anioFinal: colaboracionData.anioFinal,
                montoTotal: colaboracionData.montoTotal,
                observaciones: colaboracionData.observaciones || '',
                tiposSeleccionados: colaboracionData.tiposSeleccionados || [],
                tipoPrioritario: colaboracionData.tipoPrioritario || null,
                registradoPor: colaboracionData.registradoPor || 'Usuario',
                syncStatus: 'pending', // pending, syncing, synced, failed
                timestamp: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
                retryCount: 0
            };
            
            const request = store.add(record);
            
            request.onsuccess = () => {
                console.log('[OfflineDB] Colaboración guardada con ID:', record.id);
                resolve(record);
            };
            request.onerror = () => {
                console.error('[OfflineDB] Error al guardar:', request.error);
                reject(request.error);
            };
        });
    },
    
    /**
     * Get all pending colaboraciones
     */
    async getPending() {
        const db = await this.init();
        
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([this.storeName], 'readonly');
            const store = transaction.objectStore(this.storeName);
            const index = store.index('syncStatus');
            const request = index.getAll('pending');
            
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    },
    
    /**
     * Get all colaboraciones (any status)
     */
    async getAll() {
        const db = await this.init();
        
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([this.storeName], 'readonly');
            const store = transaction.objectStore(this.storeName);
            const request = store.getAll();
            
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    },
    
    /**
     * Update sync status of a colaboracion
     */
    async updateSyncStatus(id, status, retryCount = 0) {
        const db = await this.init();
        
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([this.storeName], 'readwrite');
            const store = transaction.objectStore(this.storeName);
            const getRequest = store.get(id);
            
            getRequest.onsuccess = () => {
                const record = getRequest.result;
                if (record) {
                    record.syncStatus = status;
                    record.retryCount = retryCount;
                    record.lastSyncAttempt = new Date().toISOString();
                    
                    const updateRequest = store.put(record);
                    updateRequest.onsuccess = () => resolve(record);
                    updateRequest.onerror = () => reject(updateRequest.error);
                } else {
                    reject(new Error('Record not found'));
                }
            };
            getRequest.onerror = () => reject(getRequest.error);
        });
    },
    
    /**
     * Remove a colaboracion by ID (after successful sync)
     */
    async remove(id) {
        const db = await this.init();
        
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([this.storeName], 'readwrite');
            const store = transaction.objectStore(this.storeName);
            const request = store.delete(id);
            
            request.onsuccess = () => {
                console.log('[OfflineDB] Colaboración eliminada:', id);
                resolve();
            };
            request.onerror = () => reject(request.error);
        });
    },
    
    /**
     * Get count of pending colaboraciones
     */
    async getPendingCount() {
        const db = await this.init();
        
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([this.storeName], 'readonly');
            const store = transaction.objectStore(this.storeName);
            const index = store.index('syncStatus');
            const request = index.count('pending');
            
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    },
    
    /**
     * Clear all records (use with caution)
     */
    async clearAll() {
        const db = await this.init();
        
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([this.storeName], 'readwrite');
            const store = transaction.objectStore(this.storeName);
            const request = store.clear();
            
            request.onsuccess = () => {
                console.log('[OfflineDB] All records cleared');
                resolve();
            };
            request.onerror = () => reject(request.error);
        });
    },
    
    /**
     * Get a specific colaboracion by ID
     */
    async getById(id) {
        const db = await this.init();
        
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([this.storeName], 'readonly');
            const store = transaction.objectStore(this.storeName);
            const request = store.get(id);
            
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    }
};

// Initialize database when script loads
ColaboracionesOfflineDB.init().catch(error => {
    console.error('[OfflineDB] Initialization failed:', error);
});
